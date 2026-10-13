using System;
using System.Collections.Generic;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// SAFX04 (cadastro de Pessoa Física/Jurídica) gerado a partir dos
    /// participantes do XML da Reforma Tributária.
    ///
    /// Diferente do SAFX3007/3008/3009, este NÃO é gerado para todo participante:
    /// só entra quem NÃO tem de-para em participantes.properties. Quem já tem
    /// de-para já está cadastrado no MasterSAF via COD_PART do SPED — gerar um
    /// segundo SAFX04 para ele, com o CNPJ cru como chave, duplicaria o cadastro
    /// (ver o comentário de <see cref="Participantes"/>). A lista de participantes
    /// sem de-para é a mesma que <see cref="XmlToSafxReforma"/> já populou ao
    /// montar os SAFX3007/3008/3009 — por isso este gerador roda depois dele.
    ///
    /// Também não é gerado SAFX07/08 a partir do XML: para NF-e esses registros já
    /// vêm do banco (carga SPED do mesmo upload) e gerá-los de novo aqui duplicaria
    /// a linha; para NFS-e não fazem sentido, por serem orientados a CFOP/ICMS de
    /// mercadoria.
    /// </summary>
    internal static class XmlToSafxCadastro
    {
        internal static void Gerar(List<XmlNota> notas, ResultadoReforma resultado)
        {
            List<Dictionary<string, object>> registros = Registros(notas);
            resultado.Safx04 = XmlToSafxReforma.Escrever("SAFX04", registros, resultado);
        }

        private static List<Dictionary<string, object>> Registros(List<XmlNota> notas)
        {
            HashSet<string> semCadastro = new HashSet<string>(Participantes.SemCadastro(), StringComparer.Ordinal);
            Dictionary<string, XmlParticipante> unicos = new Dictionary<string, XmlParticipante>(StringComparer.Ordinal);
            Dictionary<string, string> dataCompetencia = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (XmlNota nota in notas)
            {
                // NF-e de saída: nota.Participante é o destinatário (XmlLeitor.LerNfe),
                // e ScriptSQL/SAFX04.txt já gera uma linha de SAFX04 para ele
                // incondicionalmente (todo destinatário do lote, sem checar de-para).
                // Considerá-lo aqui de novo duplicaria a linha na aba SAFX04 sempre que
                // o participante não tiver de-para em participantes.properties. Para
                // NF-e de entrada (participante = emitente terceiro) e para NFS-e
                // (participante = prestador) não há equivalente vindo do SQL, e a
                // consideração continua necessária.
                bool jaCobertoPeloSql = nota.Origem == XmlNota.TipoOrigem.Nfe && nota.MovtoEs == "9";

                if (!jaCobertoPeloSql)
                {
                    Considerar(unicos, dataCompetencia, semCadastro, nota.Participante, nota.DtEmissao);
                }

                Considerar(unicos, dataCompetencia, semCadastro, nota.Transportadora, nota.DtEmissao);
            }

            List<Dictionary<string, object>> registros = new List<Dictionary<string, object>>(unicos.Count);
            foreach (KeyValuePair<string, XmlParticipante> e in unicos)
            {
                string competencia;
                dataCompetencia.TryGetValue(e.Key, out competencia);
                registros.Add(Registro(e.Value, competencia));
            }
            return registros;
        }

        private static void Considerar(Dictionary<string, XmlParticipante> destino,
                                        Dictionary<string, string> dataCompetencia,
                                        HashSet<string> semCadastro, XmlParticipante p, string dtEmissao)
        {
            if (p == null) return;
            string documento = p.Documento();
            if (documento.Length == 0) return;
            string chave = CadastroProperties.ChaveDocumento(documento);
            if (chave.Length == 0) chave = documento;
            // só entra quem o de-para de participantes.properties não conhece —
            // ver o porquê no comentário da classe.
            if (!semCadastro.Contains(chave)) return;

            if (!destino.ContainsKey(chave))
            {
                destino[chave] = p;
                dataCompetencia[chave] = Competencia(dtEmissao);
            }
        }

        /// <summary>DATA_X04 é o mês/ano de competência, dia sempre 01 — mesma convenção do SAFX04 do SPED.</summary>
        private static string Competencia(string dtEmissaoYyyyMmDd)
        {
            if (string.IsNullOrEmpty(dtEmissaoYyyyMmDd) || dtEmissaoYyyyMmDd.Length < 6) return null;
            return dtEmissaoYyyyMmDd.Substring(0, 6) + "01";
        }

        private static Dictionary<string, object> Registro(XmlParticipante p, string competencia)
        {
            bool estrangeiro = string.IsNullOrEmpty(p.Cnpj) && string.IsNullOrEmpty(p.Cpf);

            // CPF_CGC vai SEM máscara: a coluna equivalente vinda do banco (fluxo
            // SPED, SAFX04.txt) grava o CNPJ/CPF cru — com a máscara de
            // Documentos.Mascarar (14 -> 18 posições) o valor estoura o MaxLength
            // da coluna quando HomeController.Mesclar copia esta linha para lá.
            string documento = string.IsNullOrEmpty(p.Cnpj) ? p.Cpf : p.Cnpj;
            string cpfCgc = estrangeiro ? null : Documentos.SemMascara(documento);

            Dictionary<string, object> r = new Dictionary<string, object>(StringComparer.Ordinal);
            r["IND_FIS_JUR"] = p.Papel;
            r["COD_FIS_JUR"] = Participantes.CodFisJur(p.Documento());
            r["DATA_X04"] = competencia;
            r["IND_CONTEM_COD"] = estrangeiro ? "4" : "1";
            r["RAZAO_SOCIAL"] = Truncar(p.Nome, 50);
            r["CPF_CGC"] = cpfCgc;
            r["INSC_ESTADUAL"] = Truncar(estrangeiro ? "ISENTO" : p.Ie, 20);
            r["NOME_FANTASIA"] = Truncar(string.IsNullOrEmpty(p.NomeFantasia) ? p.Nome : p.NomeFantasia, 50);
            r["ENDERECO"] = Truncar(p.Logradouro, 60);
            r["NUM_ENDERECO"] = Truncar(p.Numero, 10);
            r["COMPL_ENDERECO"] = Truncar(p.Complemento, 30);
            r["BAIRRO"] = Truncar(p.Bairro, 20);
            r["CIDADE"] = Truncar(p.Municipio, 40);
            r["UF"] = Truncar(p.Uf, 2);
            r["CEP"] = Truncar(p.Cep, 8);
            r["COD_PAIS"] = CodPais(p.CodPais);
            r["TELEFONE"] = Truncar(p.Fone, 10);
            r["COD_MUNICIPIO"] = CodMunicipio(p.CodMunicipio);
            r["IND_SIMPLES_NAC"] = p.SimplesNacional ? "S" : "N";
            r["E_MAIL"] = Truncar(p.Email, 60);
            return r;
        }

        private static string CodPais(string cPais)
        {
            if (string.IsNullOrEmpty(cPais)) return "105";
            return cPais.Length > 3 ? cPais.Substring(0, 3) : cPais;
        }

        private static string CodMunicipio(string cMun)
        {
            if (string.IsNullOrEmpty(cMun)) return null;
            return cMun.Length > 5 ? cMun.Substring(cMun.Length - 5) : cMun;
        }

        /// <summary>
        /// Corta pelo tamanho do campo no leiaute oficial (safx-reforma-layout.json).
        /// Rede de segurança contra o mesmo estouro de MaxLength do CPF_CGC — a
        /// coluna real vinda do banco, em HomeController.Mesclar, pode ser mais
        /// estreita do que o XML entrega (nome de participante grande, endereço
        /// longo etc.).
        /// </summary>
        private static string Truncar(string valor, int tamanho)
        {
            if (string.IsNullOrEmpty(valor)) return valor;
            return valor.Length > tamanho ? valor.Substring(0, tamanho) : valor;
        }
    }
}
