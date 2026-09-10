using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Leiaute;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Conversão XML -> SAFX da Reforma Tributária (SAFX3007 capa, SAFX3008 item de
    /// mercadoria, SAFX3009 item de serviço).
    ///
    /// O mapeamento é por NOME de campo: cada registro é montado como
    /// nome -> valor e só depois serializado na ordem declarada em
    /// <see cref="LayoutReforma"/>. Consequência prática: a ordem oficial das
    /// planilhas do cliente (130/88/64 campos) pode ser alterada no JSON sem
    /// tocar em C#.
    ///
    /// Campo declarado no leiaute mas sem origem no XML sai nulo — não se inventa
    /// valor. Enquanto um layout estiver com "ordemConfirmada": false, gera-se
    /// apenas o CSV nomeado.
    /// </summary>
    internal static class XmlToSafxReforma
    {
        internal static void Gerar(List<XmlNota> notas, ResultadoReforma resultado)
        {
            List<Dictionary<string, object>> capas = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> itensMercadoria = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> itensServico = new List<Dictionary<string, object>>();
            int semReforma = 0;

            foreach (XmlNota nota in notas)
            {
                bool temCapa = nota.IbsCbs != null && nota.IbsCbs.TemDados();
                if (temCapa)
                {
                    capas.Add(Capa(nota));
                }
                else
                {
                    semReforma++;
                }

                if (nota.Origem == XmlNota.TipoOrigem.Nfe)
                {
                    foreach (XmlItem item in nota.Itens)
                    {
                        if (item.IbsCbs != null && item.IbsCbs.TemDados())
                        {
                            itensMercadoria.Add(ItemMercadoria(nota, item));
                        }
                    }
                }
                else if (temCapa)
                {
                    itensServico.Add(ItemServico(nota));
                }
            }

            resultado.Safx3007 = Escrever("SAFX3007", capas, resultado);
            resultado.Safx3008 = Escrever("SAFX3008", itensMercadoria, resultado);
            resultado.Safx3009 = Escrever("SAFX3009", itensServico, resultado);

            if (semReforma > 0)
            {
                resultado.Avisos.Add(semReforma + " documento(s) sem grupo IBS/CBS no XML não geraram registro de "
                    + "Reforma Tributária — nota anterior ao novo regime não tem esses valores.");
            }
        }

        // ------------------------------------------------------------- registros

        private static Dictionary<string, object> Capa(XmlNota nota)
        {
            XmlIbsCbs ibs = nota.IbsCbs;
            Dictionary<string, object> r = new Dictionary<string, object>(StringComparer.Ordinal);
            Chave(r, nota);
            // Lista do manual: 1 = NFS-e, 2 = NF-e (não é a ordem que a intuição sugere)
            r["TIPO_CHAVE_DFE"] = nota.Origem == XmlNota.TipoOrigem.Nfe ? "2" : "1";
            r["CHAVE_DFE_REF"] = nota.ChaveReferenciada;

            if (nota.Origem == XmlNota.TipoOrigem.Nfe)
            {
                r["FINALIDADE_EMISSAO_NFE"] = nota.Finalidade;
                r["IND_OPER_FINAL"] = nota.IndicadorConsumidorFinal;
                r["IND_COMPRA_MOMENTO_OPER"] = nota.IndicadorPresenca;
                r["IND_INTERM"] = nota.IndicadorIntermediador;
                // A NF-e traz CST, classificação e alíquotas por item, não na capa.
                // Quando todos os itens compartilham o mesmo valor ele vale para o
                // documento e a capa é preenchida; com itens divergentes o campo
                // fica nulo, porque não existe valor único que represente a nota.
                r["CST_IBS_CBS"] = Comum(nota, i => i.Cst);
                r["CCLASS_IBS_CBS"] = Comum(nota, i => i.ClassificacaoTributaria);
                r["BC_IBS_CBS"] = ibs.BaseCalculo;
                r["ALIQ_IBS_UF"] = Comum(nota, i => (object)i.AliqIbsUf);
                r["ALIQ_IBS_MUN"] = Comum(nota, i => (object)i.AliqIbsMun);
                r["ALIQ_CBS"] = Comum(nota, i => (object)i.AliqCbs);
                // Valores do documento (campos 42 e 57): o XML só traz vIBSUF/vCBS
                // por item e no total; na capa vale o total, que é a soma dos itens.
                r["VLR_IBS_UF"] = ibs.VlIbsUf;
                r["VLR_CBS"] = ibs.VlCbs;
            }
            else
            {
                r["FINALIDADE_EMISSAO_NFSE"] = ibs.FinalidadeNfse;
                r["IND_DESTINATARIO_SERVICO"] = ibs.IndicadorDestinatario;
                r["IND_OPER_FORNECIMENTO"] = ibs.CodIndicadorOperacao;
                // a NFS-e traz CST, classificação e alíquotas já na capa
                r["CST_IBS_CBS"] = ibs.Cst;
                r["CCLASS_IBS_CBS"] = ibs.ClassificacaoTributaria;
                r["BC_IBS_CBS"] = ibs.BaseCalculo;
                r["ALIQ_IBS_UF"] = ibs.AliqIbsUf;
                r["PERC_RED_ALIQ_IBS_UF"] = ibs.AliqReducaoIbsUf;
                r["ALIQ_EFET_IBS_UF"] = ibs.AliqEfetivaIbsUf;
                r["ALIQ_IBS_MUN"] = ibs.AliqIbsMun;
                r["PERC_RED_ALIQ_IBS_MUN"] = ibs.AliqReducaoIbsMun;
                r["ALIQ_EFET_IBS_MUN"] = ibs.AliqEfetivaIbsMun;
                r["ALIQ_CBS"] = ibs.AliqCbs;
                r["PERC_RED_ALIQ_CBS"] = ibs.AliqReducaoCbs;
                r["ALIQ_EFET_CBS"] = ibs.AliqEfetivaCbs;
                // campos 42 e 57: totCIBS/gIBS/gIBSUFTot/vIBSUF e totCIBS/gCBS/vCBS
                r["VLR_IBS_UF"] = ibs.VlIbsUf;
                r["VLR_CBS"] = ibs.VlCbs;
            }

            // Totais do documento (campos 109 a 130 do leiaute)
            r["VLR_TOT_BC_IBS_CBS"] = ibs.BaseCalculo;
            r["VLR_TOT_DIF_IBS_UF"] = ibs.VlDiferimentoIbsUf;
            r["VLR_TOT_DEV_TRIB_IBS_UF"] = ibs.VlDevolucaoTributoIbsUf;
            r["VLR_TOT_IBS_UF"] = ibs.VlIbsUf;
            r["VLR_TOT_DIF_IBS_MUN"] = ibs.VlDiferimentoIbsMun;
            r["VLR_TOT_DEV_TRIB_IBS_MUN"] = ibs.VlDevolucaoTributoIbsMun;
            r["VLR_TOT_IBS_MUN"] = ibs.VlIbsMun;
            r["VLR_TOT_IBS"] = ibs.VlIbs;
            r["VLR_TOT_CRED_PRES_IBS"] = ibs.VlCreditoPresumidoIbs;
            r["VLR_TOT_CRED_PRES_C_SUS_IBS"] = ibs.VlCreditoPresumidoCondSuspensaoIbs;
            r["VLR_TOT_CRED_PRES_CBS"] = ibs.VlCreditoPresumidoCbs;
            r["VLR_TOT_CRED_PRES_C_SUS_CBS"] = ibs.VlCreditoPresumidoCondSuspensaoCbs;
            r["VLR_TOT_DIF_CBS"] = ibs.VlDiferimentoCbs;
            r["VLR_TOT_DEV_TRIB_CBS"] = ibs.VlDevolucaoTributoCbs;
            r["VLR_TOT_CBS"] = ibs.VlCbs;
            r["VLR_TOT_NF_IBS_CBS_IS"] = ibs.VlTotalDocumento;
            return r;
        }

        private static Dictionary<string, object> ItemMercadoria(XmlNota nota, XmlItem item)
        {
            XmlIbsCbs ibs = item.IbsCbs;
            Dictionary<string, object> r = new Dictionary<string, object>(StringComparer.Ordinal);
            Chave(r, nota);
            r["IND_BEM_PATR"] = "N";
            r["IND_PRODUTO"] = "5";
            r["COD_PRODUTO"] = item.CodProduto;
            r["COD_UND_PADRAO"] = Unidades.Normalizar(item.UnidadeComercial);
            r["NUM_ITEM"] = item.NumItem.ToString("00000", CultureInfo.InvariantCulture);
            r["CST_IBS_CBS"] = ibs.Cst;
            r["CCLASS_IBS_CBS"] = ibs.ClassificacaoTributaria;
            r["BC_IBS_CBS"] = ibs.BaseCalculo;
            r["ALIQ_IBS_UF"] = ibs.AliqIbsUf;
            r["VLR_IBS_UF"] = ibs.VlIbsUf;
            r["ALIQ_CBS"] = ibs.AliqCbs;
            r["VLR_CBS"] = ibs.VlCbs;
            r["VLR_TOT_ITEM"] = item.VlContabil;
            return r;
        }

        private static Dictionary<string, object> ItemServico(XmlNota nota)
        {
            XmlIbsCbs ibs = nota.IbsCbs;
            Dictionary<string, object> r = new Dictionary<string, object>(StringComparer.Ordinal);
            Chave(r, nota);
            // a NFS-e nacional descreve um serviço por documento
            r["NUM_ITEM"] = "00001";
            r["COD_SERVICO"] = CodServico(nota.CodTributacaoNacional);
            r["COD_MUN_FT_GER_IBS_CBS"] = ibs.CodLocalidadeIncidencia;
            r["CST_IBS_CBS"] = ibs.Cst;
            r["CCLASS_IBS_CBS"] = ibs.ClassificacaoTributaria;
            r["BC_IBS_CBS"] = ibs.BaseCalculo;
            // o leiaute do item de serviço só tem IBS municipal — o IBS estadual da
            // NFS-e vai na capa (SAFX3007, campos 41/46/47 e 112)
            r["ALIQ_IBS_MUN"] = ibs.AliqIbsMun;
            r["PERC_RED_ALIQ_IBS_MUN"] = ibs.AliqReducaoIbsMun;
            r["ALIQ_EFET_IBS_MUN"] = ibs.AliqEfetivaIbsMun;
            r["VLR_IBS_MUN"] = ibs.VlIbsMun;
            r["ALIQ_CBS"] = ibs.AliqCbs;
            r["PERC_RED_ALIQ_CBS"] = ibs.AliqReducaoCbs;
            r["ALIQ_EFET_CBS"] = ibs.AliqEfetivaCbs;
            r["VLR_CBS"] = ibs.VlCbs;
            r["VLR_TOT_ITEM"] = nota.VlServico;
            return r;
        }

        /// <summary>
        /// Valor do grupo IBS/CBS repetido em todos os itens da nota, ou null quando
        /// os itens divergem — aí não há valor único que represente o documento e o
        /// campo da capa fica nulo, em vez de eleger o de um item qualquer.
        /// </summary>
        private static object Comum(XmlNota nota, Func<XmlIbsCbs, object> campo)
        {
            object comum = null;
            foreach (XmlItem item in nota.Itens)
            {
                if (item.IbsCbs == null) continue;
                object v = campo(item.IbsCbs);
                if (comum == null) comum = v;
                else if (!Iguais(comum, v)) return null;
            }
            return comum;
        }

        /// <summary>
        /// Igualdade com a semântica do equals() do Java, que é o que decide se os
        /// itens divergem.
        ///
        /// Para double o Java compara os bits (Double.equals usa doubleToLongBits),
        /// então -0,0 e 0,0 são valores DIFERENTES para ele. O Equals do .NET
        /// compara por valor e os daria como iguais — a capa seria preenchida onde
        /// o programa original a deixa nula.
        /// </summary>
        private static bool Iguais(object a, object b)
        {
            if (a is double && b is double)
            {
                return BitConverter.DoubleToInt64Bits((double)a) == BitConverter.DoubleToInt64Bits((double)b);
            }
            return a.Equals(b);
        }

        /// <summary>Chave compartilhada com a tabela-base (SAFX07/08/09), campo a campo.</summary>
        private static void Chave(Dictionary<string, object> r, XmlNota nota)
        {
            // sem fórmula de Excel aqui: o valor gravado é o do leiaute (o .txt
            // posicional usa o mesmo registro); a proteção contra o Excel comer o
            // zero à esquerda fica na gravação do CSV, em SafxCsvSupport.CelulaCsv
            r["COD_EMPRESA"] = Estabelecimentos.Empresa(nota.CnpjEstab);
            r["COD_ESTAB"] = Estabelecimentos.Codigo(nota.CnpjEstab);
            // Leiaute campo 3: saída = data de emissão; entrada = data de saída/
            // recebimento (dhSaiEnt, com dhEmi como fallback na leitura do XML)
            r["DATA_FISCAL"] = nota.MovtoEs == "9" ? nota.DtEmissao : nota.DtSaidaEnt;
            r["MOVTO_E_S"] = nota.MovtoEs;
            r["NORM_DEV"] = nota.NormDev;
            r["COD_DOCTO"] = nota.CodDocto;
            r["IND_FIS_JUR"] = nota.Participante.Papel;
            r["COD_FIS_JUR"] = Participantes.CodFisJur(nota.Participante.Documento());
            r["NUM_DOCFIS"] = NumeroDocumento(nota.NumDoc);
            r["SERIE_DOCFIS"] = nota.Serie;
        }

        // ----------------------------------------------------------- gravação

        private static DataTable Escrever(string layout, List<Dictionary<string, object>> registros,
                                          ResultadoReforma resultado)
        {
            LayoutReforma.Layout declarado = LayoutReforma.De(layout);
            string[] nomes = Nomes(declarado);

            DataTable tabela = new DataTable(layout);
            foreach (string nome in nomes) tabela.Columns.Add(nome, typeof(string));

            List<string[]> linhas = new List<string[]>(registros.Count);
            foreach (Dictionary<string, object> registro in registros)
            {
                string[] valores = Valores(declarado, registro);
                linhas.Add(valores);
                tabela.Rows.Add(valores);
            }

            // CSV nomeado: sempre gerado, não depende da ordem oficial
            string csv = ReformaConfig.ArquivoSaida(layout + ".csv");
            using (StreamWriter writer = new StreamWriter(csv, false, new UTF8Encoding(false)))
            {
                // BOM UTF-8 + ";" como delimitador, para abrir corretamente no Excel
                // em português (vírgula é decimal, não delimitador)
                writer.Write('﻿');
                writer.Write(LinhaCsv(nomes));
                writer.Write(Environment.NewLine);
                foreach (string[] valores in linhas)
                {
                    writer.Write(LinhaCsv(valores));
                    writer.Write(Environment.NewLine);
                }
            }

            if (declarado.OrdemConfirmada)
            {
                string txt = ReformaConfig.ArquivoSaida(layout + ".txt");
                using (StreamWriter writer = new StreamWriter(txt, false, new UTF8Encoding(false)))
                {
                    foreach (string[] valores in linhas)
                    {
                        writer.Write(string.Join("\t", valores));
                        writer.Write(Environment.NewLine);
                    }
                }
                resultado.Mensagens.Add("Arquivo " + layout + " gerado: " + txt
                    + " (" + registros.Count + " registros)");
            }
            else
            {
                resultado.Mensagens.Add("Arquivo " + layout + ".csv gerado: " + csv
                    + " (" + registros.Count + " registros; .txt não gerado)");
                if (registros.Count > 0)
                {
                    resultado.Avisos.Add(layout + ": " + registros.Count + " registro(s) gerados apenas em CSV. "
                        + "O leiaute oficial tem " + declarado.FieldCountOficial + " campos e o projeto "
                        + "só tem " + declarado.Campos.Count + " mapeados, sem a ordem confirmada. "
                        + "Preencha " + LayoutReforma.NomeArquivo + " com a planilha do cliente e marque "
                        + "\"ordemConfirmada\": true para o .txt posicional ser gerado.");
                }
            }

            return tabela;
        }

        private static string[] Nomes(LayoutReforma.Layout layout)
        {
            string[] nomes = new string[layout.Campos.Count];
            for (int i = 0; i < nomes.Length; i++) nomes[i] = layout.Campos[i].Nome;
            return nomes;
        }

        private static string[] Valores(LayoutReforma.Layout layout, Dictionary<string, object> registro)
        {
            string[] valores = new string[layout.Campos.Count];
            for (int i = 0; i < valores.Length; i++)
            {
                LayoutReforma.Campo campo = layout.Campos[i];
                object valor;
                if (!registro.TryGetValue(campo.Nome, out valor)) valor = null;

                if (valor == null)
                {
                    valores[i] = "@";
                }
                else if (valor is double)
                {
                    // largura e escala exatamente como o manual declara; se o
                    // descritor não trouxer o tamanho, cai na convenção da família
                    // SAFX (16 dígitos com 2 decimais)
                    int digitos = campo.Digitos > 0 ? campo.Digitos : 16;
                    int decimais = campo.Decimal() ? campo.Decimais : 2;
                    valores[i] = SafxFormat.Numero((double)valor, decimais, digitos);
                }
                else
                {
                    string texto = SafxLinha.Limpar(Convert.ToString(valor, CultureInfo.InvariantCulture));
                    valores[i] = texto.Length == 0 ? "@" : texto;
                }
            }
            return valores;
        }

        private static string LinhaCsv(string[] campos)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < campos.Length; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(SafxCsvSupport.CelulaCsv(campos[i]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// COD_SERVICO (4 posições) a partir do código de tributação nacional da
        /// NFS-e (cTribNac/xTribNac, 6 dígitos): valem os 4 últimos, que são o item
        /// e o subitem da lista de serviços — "310103" -> "0103". Os 2 primeiros são
        /// o grupo, que o campo do TAX ONE não carrega.
        /// </summary>
        private static string CodServico(string codTributacaoNacional)
        {
            string digitos = ApenasDigitos(codTributacaoNacional);
            if (digitos.Length == 0) return null;
            return digitos.Length > 4 ? digitos.Substring(digitos.Length - 4) : digitos;
        }

        private static string NumeroDocumento(string numero)
        {
            string digitos = ApenasDigitos(numero);
            if (digitos.Length == 0) return "000000000000";
            if (digitos.Length > 12) digitos = digitos.Substring(digitos.Length - 12);
            return long.Parse(digitos, CultureInfo.InvariantCulture)
                .ToString("000000000000", CultureInfo.InvariantCulture);
        }

        private static string ApenasDigitos(string valor)
        {
            if (valor == null) return "";
            StringBuilder sb = new StringBuilder(valor.Length);
            foreach (char c in valor)
            {
                if (c >= '0' && c <= '9') sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
