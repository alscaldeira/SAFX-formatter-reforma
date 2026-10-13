using ABMC.RANFE.LIGHT_SQL.DAO;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Xml;

namespace ABMC.RANFE.LIGHT_SQL.Controllers
{
    /// <summary>
    /// Conversao de XML fiscal para os layouts SAFX do MasterSAF.
    ///
    /// Um upload, uma planilha. O lote chega misturado num campo so e o tipo de
    /// cada documento sai do proprio XML; NF-e, CT-e e NFS-e alimentam as mesmas
    /// abas, no mesmo arquivo. Antes eram dois campos de upload e dois .xlsx
    /// separados (um deles com sufixo "_cte"), o que obrigava o usuario a acertar
    /// a separacao a mao e a juntar as planilhas depois.
    /// </summary>
    public class HomeController : Controller
    {
        private const int HEADER = 1;

        private const string MEU_CNPJ = "42105890000901";

        private const string SEVEN_ZEROS = "0000000";

        private const string SEVENTEEN_ZEROS = "00000000000000000";

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(List<HttpPostedFileBase> iFile, string empresa, string chkPisCofins, string chkIcms)
        {
            bool calcPisCofins = !string.IsNullOrEmpty(chkPisCofins);
            bool calcIcms = !string.IsNullOrEmpty(chkIcms);

            // O model binder entrega null, e nao lista vazia, quando o formulario
            // vem sem nenhum arquivo — e um <input type="file"> deixado em branco
            // ainda chega como item de ContentLength zero em parte dos navegadores.
            // Os dois casos saem aqui, antes de qualquer limpeza de pasta ou de
            // banco: a versao anterior so descobria o lote vazio depois de rodar o
            // ClearAllRecords, e entao estourava deixando a base zerada.
            List<HttpPostedFileBase> enviados = new List<HttpPostedFileBase>();

            if (iFile != null)
            {
                foreach (HttpPostedFileBase item in iFile)
                {
                    if (item != null && item.ContentLength > 0)
                    {
                        enviados.Add(item);
                    }
                }
            }

            if (enviados.Count == 0)
            {
                ViewBag.Erro = "Selecione ao menos um arquivo XML para processar.";
                return View();
            }

            List<string> avisos = new List<string>();

            string pastaUpload = Server.MapPath("~/UploadXML");
            LimparPasta(pastaUpload);

            foreach (HttpPostedFileBase item in enviados)
            {
                string nome = Path.GetFileNameWithoutExtension(item.FileName) + Path.GetExtension(item.FileName);
                item.SaveAs(Path.Combine(pastaUpload, nome));
            }

            // Quem separa NF-e de CT-e e o conteudo do XML, nao o campo de origem.
            Dictionary<TipoXmlFiscal, List<string>> porTipo = ClassificadorXml.ClassificarPasta(pastaUpload);

            foreach (string ignorado in porTipo[TipoXmlFiscal.Desconhecido])
            {
                avisos.Add("Arquivo ignorado (nao e NF-e, NFS-e nem CT-e, ou esta ilegivel): "
                    + Path.GetFileName(ignorado));
            }

            // Os CT-e saem de ~/UploadXML antes da carga do banco. Funcoes.CarregarArquivo1
            // varre so o primeiro nivel da pasta, entao mover para uma pasta irma
            // mantem o fluxo de NF-e vendo exatamente o que via quando os dois
            // campos de upload eram separados.
            string pastaCTe = Path.Combine(Directory.GetParent(pastaUpload).FullName, "UploadCTe");
            LimparPasta(pastaCTe);

            foreach (string arquivo in porTipo[TipoXmlFiscal.CTe])
            {
                string destino = Path.Combine(pastaCTe, Path.GetFileName(arquivo));
                System.IO.File.Move(arquivo, destino);
            }

            Dictionary<int, int> ccByCfop = CarregarDeParaContaPorCfop();

            LoteCTe cte = LerCTes(pastaCTe, calcPisCofins, calcIcms, ccByCfop, avisos);

            // ---------------------------------------------------------- NF-e
            DataTable safx07 = null;
            DataTable safx08 = null;
            DataTable safx04 = null;
            DataTable safx2013 = null;

            if (porTipo[TipoXmlFiscal.NFe].Count > 0)
            {
                LerXml oLerXml = new LerXml("arquivo")
                {
                    Integra = true,
                    GravaSynchro = false,
                    PastaArquivoXsdNFe = Server.MapPath("~/VsdNFe/V3.10")
                };

                DAO_SAFX.ClearAllRecords();
                Funcoes.CarregarArquivo1(oLerXml);
                Funcoes.NormalizarXMLNFe();

                DataTable bruto07 = DAO_SAFX.Consulta(LerScript("SAFX07"));
                DataTable bruto08 = DAO_SAFX.Consulta(LerScript("SAFX08"));
                DataTable bruto04 = DAO_SAFX.Consulta(LerScript("SAFX04"));
                DataTable bruto2013 = DAO_SAFX.Consulta(LerScript("SAFX2013"));

                safx07 = TransformarSafx07(bruto07, bruto08, ccByCfop, calcPisCofins, calcIcms);
                safx08 = TransformarSafx08(bruto08, ccByCfop, calcPisCofins, calcIcms);
                safx04 = TransformarSafx04(bruto04);
                safx2013 = TransformarSafx2013(bruto2013);
            }

            // As linhas de CT-e entram nas MESMAS abas das de NF-e. A mescla e por
            // nome de coluna, nao por posicao: a tabela do CT-e vem da classe
            // DataTableSAFX07 e a de NF-e vem do script SQL, e as duas nao declaram
            // as colunas na mesma ordem.
            safx07 = Mesclar(safx07, cte.Safx07);
            safx08 = Mesclar(safx08, cte.Safx08);
            safx04 = Mesclar(safx04, cte.Safx04);

            // A pasta de saida e limpa aqui, e nao junto da gravacao da planilha:
            // a conversao da Reforma grava os SAFX3007/3008/3009 posicionais (.txt
            // e .csv) direto em ~/Export, e limpar depois apagaria justamente esses
            // arquivos — a planilha sairia com as abas e o cliente ficaria sem os
            // arquivos de importacao.
            string pastaExport = Server.MapPath("~/Export");
            LimparPasta(pastaExport);

            // ------------------------------------------- Reforma Tributaria
            //
            // SAFX3007/3008/3009 saem direto dos grupos IBSCBS do XML, que a carga
            // do banco nao guarda — por isso a conversao le a pasta de upload e nao
            // as tabelas int_nfe_*. A falha aqui nao pode derrubar o resto: XML
            // anterior ao novo regime nao tem grupo IBS/CBS.
            ResultadoReforma reforma = null;

            try
            {
                reforma = ConversorXmlReforma.Converter(
                    pastaUpload,
                    Server.MapPath("~/Export"),
                    Server.MapPath("~/ReformaTributaria/Dados"));
                avisos.AddRange(reforma.Avisos);
            }
            catch (Exception ex)
            {
                avisos.Add("Reforma Tributaria (SAFX3007/3008/3009) nao gerada: " + ex.Message);
            }

            // ------------------------------------------------------ planilha
            string nomeArquivo = empresa + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

            using (ExcelPackage pacote = new ExcelPackage(new FileInfo(Path.Combine(pastaExport, nomeArquivo))))
            {
                EscreverAba(pacote, "SAFX07", safx07);
                EscreverAba(pacote, "SAFX08", safx08);
                EscreverAba(pacote, "SAFX04", safx04);
                EscreverAba(pacote, "SAFX2013", safx2013);

                if (reforma != null)
                {
                    EscreverAba(pacote, "SAFX3007", reforma.Safx3007, true);
                    EscreverAba(pacote, "SAFX3008", reforma.Safx3008, true);
                    EscreverAba(pacote, "SAFX3009", reforma.Safx3009, true);
                }

                pacote.Save();
            }

            ViewBag.NomeArquivo = nomeArquivo;
            ViewBag.Avisos = avisos;
            return View();
        }

        /// <summary>Tabelas SAFX produzidas a partir dos CT-e do lote.</summary>
        private sealed class LoteCTe
        {
            public DataTable Safx07 = new DataTableSAFX07();
            public DataTable Safx08 = new DataTableSAFX08();
            public DataTable Safx04 = new DataTableSAFX04();
        }

        /// <summary>
        /// Le os CT-e da pasta para as tabelas SAFX07/08/04.
        ///
        /// Substitui o antigo CTeToXls, que alem de ler o XML gravava uma planilha
        /// propria — a segunda planilha que o usuario tinha de juntar a mao. Aqui a
        /// leitura devolve apenas os dados; quem escreve e o Index, uma vez so.
        ///
        /// Todo acesso ao XML passa por No()/Txt(). A versao anterior encadeava
        /// indexadores direto (xml["cteProc"]["CTe"]["infCte"]["emit"]["CNPJ"]) e o
        /// indexador de XmlElement devolve null quando o no nao existe, entao
        /// qualquer campo ausente virava NullReferenceException no meio do lote,
        /// sem dizer qual arquivo nem qual campo. Agora o documento incompleto e
        /// pulado com um aviso nomeando os dois.
        /// </summary>
        private LoteCTe LerCTes(string pasta, bool calcPisCofins, bool calcIcms,
            Dictionary<int, int> ccByCfop, List<string> avisos)
        {
            LoteCTe lote = new LoteCTe();

            if (!Directory.Exists(pasta))
            {
                return lote;
            }

            string aliqPis = 16500.00m.ToString(SEVEN_ZEROS, CultureInfo.InvariantCulture);
            string aliqCofins = 76000.0m.ToString(SEVEN_ZEROS, CultureInfo.InvariantCulture);

            foreach (FileInfo arquivo in new DirectoryInfo(pasta).GetFiles("*.xml"))
            {
                string nome = arquivo.Name;
                XmlDocument doc = new XmlDocument();

                try
                {
                    string texto = System.IO.File.ReadAllText(arquivo.FullName);
                    texto = Funcoes.LimpaTextoXml(texto);
                    texto = Funcoes.NoAccent(texto);
                    doc.LoadXml(texto);
                }
                catch (XmlException ex)
                {
                    avisos.Add(nome + ": XML ilegivel, documento ignorado (" + ex.Message + ").");
                    continue;
                }

                // A raiz varia: cteProc (CT-e autorizado), cteOSProc (CT-e OS) e,
                // quando o ERP remonta o arquivo, o proprio CTe/CTeOS sem protocolo.
                XmlElement infCte = No(doc, "cteProc", "CTe", "infCte")
                    ?? No(doc, "cteOSProc", "CTeOS", "infCte")
                    ?? No(doc, "CTe", "infCte")
                    ?? No(doc, "CTeOS", "infCte");

                if (infCte == null)
                {
                    avisos.Add(nome + ": nao foi possivel localizar o grupo infCte; documento ignorado.");
                    continue;
                }

                XmlElement ide = No(infCte, "ide");
                XmlElement emit = No(infCte, "emit");
                XmlElement enderEmit = No(emit, "enderEmit");
                XmlElement vPrest = No(infCte, "vPrest");
                XmlElement infProt = No(doc, "cteProc", "protCTe", "infProt")
                    ?? No(doc, "cteOSProc", "protCTe", "infProt");

                // Grupos sem os quais nao da para montar nenhuma das tres abas. O
                // que falta e dito pelo nome, para o usuario conferir no XML.
                string faltando = null;
                if (ide == null) faltando = "ide";
                else if (emit == null) faltando = "emit";
                else if (enderEmit == null) faltando = "emit/enderEmit";
                else if (vPrest == null) faltando = "vPrest";
                else if (infProt == null) faltando = "protCTe/infProt";

                if (faltando != null)
                {
                    avisos.Add(nome + ": grupo obrigatorio <" + faltando + "> ausente; documento ignorado.");
                    continue;
                }

                bool proprio = Txt(emit, "CNPJ") == MEU_CNPJ;

                int cfop;
                if (!int.TryParse(Txt(ide, "CFOP"), out cfop))
                {
                    avisos.Add(nome + ": CFOP ausente ou invalido em <ide>; documento ignorado.");
                    continue;
                }

                if (!proprio)
                {
                    if (cfop >= 1000 && cfop < 4000) cfop += 4000;
                    else if (cfop >= 5000 && cfop < 8000) cfop -= 4000;
                }

                // ICMS: o grupo depende do CST. A versao anterior lia sempre
                // ICMS00 e quebrava em qualquer CT-e que nao fosse tributado
                // integralmente — ICMS45 (isento/nao tributado) e comum em frete.
                XmlElement icms = No(infCte, "imp", "ICMS");
                XmlElement icmsGrupo = PrimeiroFilhoElemento(icms);

                if (icmsGrupo == null)
                {
                    avisos.Add(nome + ": grupo <imp/ICMS> sem detalhamento de CST; documento ignorado.");
                    continue;
                }

                if (icmsGrupo.Name != "ICMS00")
                {
                    avisos.Add(nome + ": ICMS informado como <" + icmsGrupo.Name + ">, e nao ICMS00. "
                        + "Base, aliquota e valor saem do que o grupo traz — confira o lancamento.");
                }

                string vTPrest = Txt(vPrest, "vTPrest");
                string vBC = Txt(icmsGrupo, "vBC");
                string vICMS = Txt(icmsGrupo, "vICMS");
                string cst = Txt(icmsGrupo, "CST");
                string dhRecbto = Txt(infProt, "dhRecbto");
                string modelo = Txt(ide, "mod");
                string codDocto = modelo == "57" ? "CT-E" : "CTE-OS";

                // ------------------------------------------------- SAFX04
                DataRow l04 = lote.Safx04.NewRow();
                lote.Safx04.Rows.Add(l04);

                l04["DATA_X04"] = ToDt(Txt(ide, "dhEmi"));
                l04["COD_FIS_JUR"] = Txt(emit, "CNPJ");
                l04["RAZAO_SOCIAL"] = Txt(emit, "xNome");
                l04["CPF_CGC"] = Txt(emit, "CNPJ");
                l04["INSC_ESTADUAL"] = Txt(emit, "IE");
                l04["NOME_FANTASIA"] = Txt(emit, "xFant");
                l04["ENDERECO"] = Txt(enderEmit, "xLgr");
                l04["NUM_ENDERECO"] = Txt(enderEmit, "nro");
                l04["COMPL_ENDERECO"] = Txt(enderEmit, "xCpl");
                l04["BAIRRO"] = Txt(enderEmit, "xBairro");
                l04["CIDADE"] = Txt(enderEmit, "xMun");
                l04["UF"] = Txt(enderEmit, "UF");
                l04["CEP"] = Txt(enderEmit, "CEP");

                string fone = Txt(enderEmit, "fone");
                l04["TELEFONE"] = fone.Length > 10 ? fone.Substring(fone.Length - 10, 10) : fone;

                string cPais = Txt(enderEmit, "cPais");
                l04["COD_PAIS"] = (cPais.Length >= 3 ? cPais : "105").Substring(0, 3);

                string cMun = Txt(enderEmit, "cMun");
                l04["COD_MUNICIPIO"] = cMun.Length > 5 ? cMun.Substring(cMun.Length - 5) : cMun;
                l04["IND_CONTEM_COD"] = string.IsNullOrEmpty(Txt(No(infCte, "rem"), "CNPJ")) ? "4" : "1";

                // ------------------------------------------------- SAFX07
                DataRow l07 = lote.Safx07.NewRow();
                lote.Safx07.Rows.Add(l07);

                l07["COD_FIS_JUR"] = Txt(emit, "CNPJ");
                if (!proprio) l07["MOVTO_E_S"] = "1";
                l07["COD_MODELO_COTEPE"] = modelo;
                l07["COD_MODELO"] = modelo;
                l07["COD_DOCTO"] = codDocto;
                l07["NUM_DOCFIS"] = Txt(ide, "nCT").Trim().PadLeft(12, '0');
                l07["SERIE_DOCFIS"] = Txt(ide, "serie");
                l07["DATA_EMISSAO"] = ToDt(Txt(ide, "dhEmi"));
                l07["DAT_FATO_GERADOR"] = ToDt(Txt(ide, "dhEmi"));
                l07["UF_ORIG_DEST"] = Txt(ide, "UFEnv");
                l07["UF_DESTINO"] = Txt(ide, "UFFim");
                l07["NUM_DV_NFE"] = Txt(ide, "cDV");
                l07["COD_CFO"] = cfop.ToString();
                l07["COD_MUNICIPIO_ORIG"] = Sub2(Txt(No(infCte, "exped", "enderExped"), "cMun"));
                l07["COD_MUNICIPIO_DEST"] = Sub2(Txt(No(infCte, "dest", "enderDest"), "cMun"));
                l07["VLR_ALIQ_PIS"] = calcPisCofins ? aliqPis : SEVEN_ZEROS;
                l07["VLR_ALIQ_COFINS"] = calcPisCofins ? aliqCofins : SEVEN_ZEROS;
                l07["COD_SIT_PIS"] = calcPisCofins ? "56" : "70";
                l07["COD_SIT_COFINS"] = calcPisCofins ? "56" : "70";

                // "FRETE VALOR" fatura o componente, nao o total da prestacao.
                string valorProduto = Txt(emit, "xNome").ToUpper() == "FRETE VALOR"
                    ? Txt(No(vPrest, "Comp"), "vComp")
                    : vTPrest;

                l07["VLR_PRODUTO"] = To17Z(valorProduto);
                l07["NUM_AUTENTIC_NFE"] = Txt(infProt, "chCTe");
                l07["DATA_SAIDA_REC"] = ToDt(dhRecbto);
                l07["DAT_LANC_PIS_COFINS"] = ToDt(dhRecbto);
                l07["VLR_TOT_NOTA"] = To17Z(vTPrest);

                if (calcPisCofins)
                {
                    // Base do PIS/COFINS: o total da prestacao quando o CT-e e
                    // proprio; o total menos o ICMS quando e de terceiro.
                    decimal baseCalculo = proprio ? Dec(vTPrest) : Dec(vTPrest) - Dec(vICMS);

                    l07["VLR_BASE_PIS"] = Z17(baseCalculo);
                    l07["VLR_BASE_COFINS"] = Z17(baseCalculo);
                    l07["VLR_PIS"] = Z17(decimal.Round(baseCalculo * 1.65m / 100m, 2));
                    l07["VLR_COFINS"] = Z17(decimal.Round(baseCalculo * 7.6m / 100m, 2));
                }
                else
                {
                    l07["VLR_PIS"] = SEVENTEEN_ZEROS;
                    l07["VLR_BASE_PIS"] = SEVENTEEN_ZEROS;
                    l07["VLR_BASE_COFINS"] = SEVENTEEN_ZEROS;
                    l07["VLR_COFINS"] = SEVENTEEN_ZEROS;
                }

                // Tomador: toma3 quando e um dos participantes do proprio CT-e,
                // toma4 quando e um terceiro. A versao anterior so lia toma3.
                string toma = Txt(No(ide, "toma3"), "toma");
                if (string.IsNullOrEmpty(toma)) toma = Txt(No(ide, "toma4"), "toma");

                if (toma == "0") l07["IND_TP_FRETE"] = "1";
                else if (toma == "3") l07["IND_TP_FRETE"] = "2";
                else if (!string.IsNullOrEmpty(toma)) l07["IND_TP_FRETE"] = toma;

                l07["COD_SITUACAO_B"] = cst;
                if (!calcIcms) l07["BASE_OUTR_ICMS"] = To17Z(vBC);
                if (ccByCfop.ContainsKey(cfop)) l07["COD_CONTA"] = ccByCfop[cfop];

                // ------------------------------------------------- SAFX08
                DataRow l08 = lote.Safx08.NewRow();
                lote.Safx08.Rows.Add(l08);

                if (!proprio)
                {
                    l08["MOVTO_E_S"] = "1";
                    l08["COD_TRIB_IPI"] = "03";
                }

                l08["DATA_FISCAL"] = ToDt(dhRecbto);
                l08["DAT_LANC_PIS_COFINS"] = ToDt(dhRecbto);
                l08["COD_DOCTO"] = codDocto;
                l08["COD_FIS_JUR"] = Txt(emit, "CNPJ");
                l08["NUM_DOCFIS"] = Txt(ide, "nCT").PadLeft(12, '0');
                l08["SERIE_DOCFIS"] = Txt(ide, "serie");
                l08["DESCRICAO_COMPL"] = Txt(No(infCte, "compl"), "xObs");
                l08["COD_CFO"] = cfop.ToString();
                l08["VLR_UNIT"] = (Dec(vTPrest) * 10000m).ToString("0000000000000000000", CultureInfo.InvariantCulture);
                l08["VLR_ITEM"] = To17Z(vTPrest);
                l08["COD_SITUACAO_B"] = calcIcms ? cst : "90";
                l08["BASE_ICMS"] = To17Z(vBC);

                if (calcIcms)
                {
                    l08["VLR_ALIQ_ICMS"] = (Dec(Txt(icmsGrupo, "pICMS")) * 10000m).ToString(SEVEN_ZEROS, CultureInfo.InvariantCulture);
                    l08["VLR_ICMS"] = To17Z(vICMS);
                    l08["TRIB_ICMS"] = "1";
                }
                else
                {
                    l08["VLR_ALIQ_ICMS"] = SEVEN_ZEROS;
                    l08["VLR_ICMS"] = SEVENTEEN_ZEROS;
                    l08["TRIB_ICMS"] = "3";
                }

                l08["VLR_CONTAB_ITEM"] = To17Z(vTPrest);

                if (calcPisCofins)
                {
                    decimal baseCalculo = proprio ? Dec(vTPrest) : Dec(vTPrest) - Dec(vICMS);

                    l08["VLR_ALIQ_PIS"] = aliqPis;
                    l08["VLR_ALIQ_COFINS"] = aliqCofins;
                    l08["COD_SITUACAO_PIS"] = "56";
                    l08["COD_SITUACAO_COFINS"] = "56";
                    l08["VLR_BASE_PIS"] = Z17(baseCalculo);
                    l08["VLR_BASE_COFINS"] = Z17(baseCalculo);
                    l08["VLR_PIS"] = Z17(decimal.Round(baseCalculo * 1.65m / 100m, 2));
                    l08["VLR_COFINS"] = Z17(decimal.Round(baseCalculo * 7.6m / 100m, 2));
                }
                else
                {
                    l08["VLR_BASE_PIS"] = SEVENTEEN_ZEROS;
                    l08["VLR_PIS"] = SEVENTEEN_ZEROS;
                    l08["VLR_BASE_COFINS"] = SEVENTEEN_ZEROS;
                    l08["VLR_COFINS"] = SEVENTEEN_ZEROS;
                    l08["VLR_ALIQ_PIS"] = SEVEN_ZEROS;
                    l08["VLR_ALIQ_COFINS"] = SEVEN_ZEROS;
                    l08["COD_SITUACAO_PIS"] = "70";
                    l08["COD_SITUACAO_COFINS"] = "70";
                }

                if (ccByCfop.ContainsKey(cfop)) l08["COD_CONTA"] = ccByCfop[cfop];
            }

            return lote;
        }

        // ------------------------------------------------- acesso seguro ao XML

        /// <summary>Desce o caminho de nos; devolve null assim que um deles faltar.</summary>
        private static XmlElement No(XmlNode pai, params string[] caminho)
        {
            XmlNode atual = pai;

            foreach (string nome in caminho)
            {
                if (atual == null) return null;
                atual = atual[nome];
            }

            return atual as XmlElement;
        }

        /// <summary>Texto de um no filho; string vazia se o caminho nao existir.</summary>
        private static string Txt(XmlNode pai, params string[] caminho)
        {
            XmlElement no = No(pai, caminho);
            return no == null ? "" : no.InnerText;
        }

        private static XmlElement PrimeiroFilhoElemento(XmlElement pai)
        {
            if (pai == null) return null;

            foreach (XmlNode filho in pai.ChildNodes)
            {
                XmlElement elemento = filho as XmlElement;
                if (elemento != null) return elemento;
            }

            return null;
        }

        /// <summary>
        /// Numero do XML fiscal. SEMPRE com ponto decimal, independente do
        /// servidor.
        ///
        /// A versao anterior usava decimal.Parse sem cultura, que segue a do
        /// processo do IIS. Num servidor pt-BR o ponto de "1234.56" e lido como
        /// separador de milhar e o valor vira 123456 — cem vezes maior — em todo
        /// campo monetario do CT-e. Funciona hoje porque a cultura do servidor e
        /// invariante ou en-US; mudar o locale da maquina quebraria a conversao
        /// inteira, calada.
        /// </summary>
        private static decimal Dec(string valor)
        {
            decimal resultado;
            return decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado)
                ? resultado : 0m;
        }

        /// <summary>Valor em centavos, 17 posicoes com zeros a esquerda.</summary>
        private static string Z17(decimal valor)
        {
            return (valor * 100m).ToString(SEVENTEEN_ZEROS, CultureInfo.InvariantCulture);
        }

        private static string To17Z(string valor)
        {
            return Z17(Dec(valor));
        }

        private static string ToDt(string valor)
        {
            // dhEmi/dhRecbto chegam com offset (2026-09-01T23:30:00-03:00). DateTime
            // .TryParse converteria para a hora LOCAL do servidor — num IIS em UTC,
            // um CT-e emitido depois das 21h sairia com a data do dia seguinte.
            // DateTimeOffset.DateTime devolve a hora de parede do proprio documento.
            DateTimeOffset comOffset;
            if (DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out comOffset))
            {
                return comOffset.DateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            }

            DateTime data;
            return DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                ? data.ToString("yyyyMMdd", CultureInfo.InvariantCulture) : "";
        }

        private static string Sub2(string valor)
        {
            string texto = string.IsNullOrEmpty(valor) ? "   " : valor;
            return texto.Length > 2 ? texto.Substring(2) : texto;
        }

        // ------------------------------------------------------------ apoio

        private static void LimparPasta(string pasta)
        {
            if (!Directory.Exists(pasta))
            {
                Directory.CreateDirectory(pasta);
                return;
            }

            foreach (FileInfo arquivo in new DirectoryInfo(pasta).GetFiles())
            {
                System.IO.File.Delete(arquivo.FullName);
            }
        }

        private string LerScript(string layout)
        {
            using (StreamReader leitor = new StreamReader(Server.MapPath("~/ScriptSQL/" + layout + ".txt")))
            {
                return leitor.ReadToEnd();
            }
        }

        private static Dictionary<int, int> CarregarDeParaContaPorCfop()
        {
            Dictionary<int, int> ccByCfop = new Dictionary<int, int>();

            using (DbConnection conexao = ((DbContext)new RanfeLight_Entities()).Database.Connection)
            {
                conexao.Open();

                DbCommand comando = conexao.CreateCommand();
                comando.Connection = conexao;
                comando.CommandText = "SELECT DE, PARA FROM TB_DE_PARA ORDER BY DE";

                using (DbDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        ccByCfop.Add(leitor.GetInt32(0), leitor.GetInt32(1));
                    }
                }
            }

            return ccByCfop;
        }

        /// <summary>
        /// Acrescenta as linhas de <paramref name="origem"/> em
        /// <paramref name="destino"/> casando as colunas pelo NOME.
        ///
        /// As duas tabelas descrevem o mesmo layout SAFX mas nascem de fontes
        /// diferentes — script SQL de um lado, DataTableSAFX* do outro — e nao
        /// declaram as colunas na mesma ordem nem no mesmo numero. Copiar por
        /// indice embaralharia os campos.
        /// </summary>
        private static DataTable Mesclar(DataTable destino, DataTable origem)
        {
            if (origem == null || origem.Rows.Count == 0) return destino;
            if (destino == null) return Normalizar(origem);

            foreach (DataRow linha in origem.Rows)
            {
                DataRow nova = destino.NewRow();

                foreach (DataColumn coluna in destino.Columns)
                {
                    if (!origem.Columns.Contains(coluna.ColumnName)) continue;

                    object valor = linha[coluna.ColumnName];
                    nova[coluna.ColumnName] = Vazio(valor) ? "@" : valor;
                }

                destino.Rows.Add(nova);
            }

            return destino;
        }

        /// <summary>Campo nulo do SAFX e "@" — no arquivo e na planilha.</summary>
        private static bool Vazio(object valor)
        {
            return valor == null || valor == DBNull.Value || valor.ToString().Trim() == "";
        }

        /// <summary>Copia a tabela trocando vazio por "@", para uso quando nao ha NF-e no lote.</summary>
        private static DataTable Normalizar(DataTable origem)
        {
            DataTable saida = CriarSaida(origem);

            foreach (DataRow linha in origem.Rows)
            {
                DataRow nova = saida.NewRow();

                foreach (DataColumn coluna in origem.Columns)
                {
                    object valor = linha[coluna.ColumnName];
                    nova[coluna.ColumnName] = Vazio(valor) ? "@" : valor;
                }

                saida.Rows.Add(nova);
            }

            return saida;
        }

        /// <summary>
        /// Tabela de saida com as mesmas colunas da origem, todas do tipo object.
        ///
        /// As transformacoes abaixo gravam ora string ora int (COD_CFO e COD_CONTA
        /// viram numero), e a planilha recebia esse valor cru. Tipar as colunas
        /// como object preserva isso: um int continua chegando ao Excel como
        /// numero, e nao como texto.
        /// </summary>
        private static DataTable CriarSaida(DataTable origem)
        {
            DataTable saida = new DataTable();

            foreach (DataColumn coluna in origem.Columns)
            {
                saida.Columns.Add(coluna.ColumnName, typeof(object));
            }

            return saida;
        }

        // ------------------------------ transformacoes do fluxo NF-e (banco)
        //
        // Regras preservadas na integra da versao anterior. A unica mudanca e o
        // destino: antes cada valor ia direto para uma celula do Excel, agora vai
        // para uma DataTable de saida — e e por isso que as linhas de CT-e podem
        // entrar na mesma aba. Aplicar as regras de NF-e sobre linhas de CT-e
        // corromperia os dados (COD_FIS_JUR sai de SUB_SERIE_DOCFIS, que o CT-e
        // nao preenche), entao a mescla acontece depois daqui.

        private static DataTable TransformarSafx07(DataTable bruto07, DataTable bruto08,
            Dictionary<int, int> ccByCfop, bool calcPisCofins, bool calcIcms)
        {
            DataTable saida = CriarSaida(bruto07);

            for (int i = 0; i < bruto07.Rows.Count; i++)
            {
                DataRow origem = bruto07.Rows[i];
                DataRow nova = saida.NewRow();
                bool proprio = origem.Field<string>("SUB_SERIE_DOCFIS") == MEU_CNPJ;

                foreach (DataColumn coluna in bruto07.Columns)
                {
                    string nome = coluna.ColumnName;
                    object valor = origem[nome];

                    if (Vazio(valor)) valor = "@";

                    switch (nome)
                    {
                        case "COD_FIS_JUR":
                            valor = origem.Field<string>("SUB_SERIE_DOCFIS");
                            break;
                        case "SUB_SERIE_DOCFIS":
                            valor = "@";
                            break;
                        case "MOVTO_E_S":
                            valor = proprio ? "9" : "1";
                            break;
                        case "COD_TRIB_IPI":
                            if (!proprio) valor = "03";
                            break;
                        case "COD_CFO":
                            if (!proprio) valor = AjustarCfop(valor);
                            break;
                        case "COD_CONTA":
                            valor = ContaDoCfop(origem["COD_CFO"], proprio, ccByCfop, valor);
                            break;
                        case "COD_SIT_PIS":
                        case "COD_SIT_COFINS":
                            valor = SituacaoPisCofins(valor, bruto08.Rows[i], calcPisCofins);
                            break;
                        case "COD_SITUACAO_B":
                            valor = SituacaoIcms(valor, calcIcms);
                            break;
                        case "VLR_ALIQ_PIS":
                        case "VLR_ALIQ_COFINS":
                            if (valor.ToString() == "@" || !calcPisCofins) valor = SEVEN_ZEROS;
                            break;
                        case "VLR_ALIQ_ICMS":
                            if (valor.ToString() == "@" || !calcIcms) valor = SEVEN_ZEROS;
                            break;
                        case "VLR_PIS":
                        case "VLR_BASE_PIS":
                        case "VLR_COFINS":
                        case "VLR_BASE_COFINS":
                            if (valor.ToString() == "@" || !calcPisCofins) valor = SEVENTEEN_ZEROS;
                            break;
                        case "VLR_ICMS":
                        case "VLR_BASE_ICMS":
                            if (valor.ToString() == "@" || !calcIcms) valor = SEVENTEEN_ZEROS;
                            break;
                        case "IND_NF_REG_ESPECIAL":
                            valor = "S";
                            break;
                    }

                    nova[nome] = valor;
                }

                saida.Rows.Add(nova);
            }

            return saida;
        }

        private static DataTable TransformarSafx08(DataTable bruto08,
            Dictionary<int, int> ccByCfop, bool calcPisCofins, bool calcIcms)
        {
            DataTable saida = CriarSaida(bruto08);

            foreach (DataRow origem in bruto08.Rows)
            {
                DataRow nova = saida.NewRow();
                bool proprio = origem.Field<string>("SUB_SERIE_DOCFIS") == MEU_CNPJ;

                foreach (DataColumn coluna in bruto08.Columns)
                {
                    string nome = coluna.ColumnName;
                    object valor = origem[nome];

                    if (Vazio(valor)) valor = "@";

                    switch (nome)
                    {
                        case "COD_FIS_JUR":
                            valor = origem.Field<string>("SUB_SERIE_DOCFIS");
                            break;
                        case "SUB_SERIE_DOCFIS":
                            valor = "@";
                            break;
                        case "MOVTO_E_S":
                            valor = proprio ? "9" : "1";
                            break;
                        case "COD_TRIB_IPI":
                            valor = proprio ? "53" : "03";
                            break;
                        case "COD_CFO":
                            if (!proprio) valor = AjustarCfop(valor);
                            break;
                        case "COD_CONTA":
                            valor = ContaDoCfop(origem["COD_CFO"], proprio, ccByCfop, valor);
                            break;
                        case "COD_SITUACAO_PIS":
                        case "COD_SITUACAO_COFINS":
                            valor = SituacaoPisCofins(valor, origem, calcPisCofins);
                            break;
                        case "COD_SITUACAO_B":
                            valor = SituacaoIcms(valor, calcIcms);
                            break;
                        case "VLR_ALIQ_PIS":
                        case "VLR_ALIQ_COFINS":
                            if (valor.ToString() == "@" || !calcPisCofins) valor = SEVEN_ZEROS;
                            break;
                        case "VLR_ALIQ_ICMS":
                            if (valor.ToString() == "@" || !calcIcms) valor = SEVEN_ZEROS;
                            break;
                        case "VLR_PIS":
                        case "VLR_BASE_PIS":
                        case "VLR_COFINS":
                        case "VLR_BASE_COFINS":
                            if (valor.ToString() == "@" || !calcPisCofins) valor = SEVENTEEN_ZEROS;
                            break;
                        case "VLR_ICMS":
                        case "VLR_BASE_ICMS":
                            if (valor.ToString() == "@" || !calcIcms) valor = SEVENTEEN_ZEROS;
                            break;
                    }

                    nova[nome] = valor;
                }

                saida.Rows.Add(nova);
            }

            return saida;
        }

        private static DataTable TransformarSafx04(DataTable bruto04)
        {
            DataTable saida = CriarSaida(bruto04);

            foreach (DataRow origem in bruto04.Rows)
            {
                DataRow nova = saida.NewRow();

                foreach (DataColumn coluna in bruto04.Columns)
                {
                    string nome = coluna.ColumnName;
                    object valor = origem[nome];

                    if (Vazio(valor)) valor = "@";

                    switch (nome)
                    {
                        case "CPF_CGC":
                        case "COD_FIS_JUR":
                            valor = origem.Field<string>("COD_ATIVIDADE");
                            break;
                        case "COD_ATIVIDADE":
                            valor = "@";
                            break;
                    }

                    nova[nome] = valor;
                }

                saida.Rows.Add(nova);
            }

            return saida;
        }

        private static DataTable TransformarSafx2013(DataTable bruto2013)
        {
            DataTable saida = CriarSaida(bruto2013);

            foreach (DataRow origem in bruto2013.Rows)
            {
                DataRow nova = saida.NewRow();

                foreach (DataColumn coluna in bruto2013.Columns)
                {
                    string nome = coluna.ColumnName;
                    object valor = origem[nome];

                    if (Vazio(valor)) valor = "@";
                    if (nome == "COD_MEDIDA" || nome == "COD_UND_PADRAO") valor = "UN";

                    nova[nome] = valor;
                }

                saida.Rows.Add(nova);
            }

            return saida;
        }

        private static object AjustarCfop(object valor)
        {
            int cfop;

            if (!int.TryParse(valor.ToString(), out cfop)) return valor;
            if (cfop >= 5000 && cfop < 8000) cfop -= 4000;

            return cfop;
        }

        private static object ContaDoCfop(object cfopOrigem, bool proprio,
            Dictionary<int, int> ccByCfop, object valorAtual)
        {
            int cfop;

            if (!int.TryParse(Convert.ToString(cfopOrigem), out cfop)) return valorAtual;
            if (!proprio && cfop >= 5000 && cfop < 8000) cfop -= 4000;

            return ccByCfop.ContainsKey(cfop) ? (object)ccByCfop[cfop] : valorAtual;
        }

        private static object SituacaoPisCofins(object valor, DataRow linha08, bool calcPisCofins)
        {
            bool semValor = long.Parse(linha08.Field<string>("VLR_PIS") ?? "0") == 0L
                && long.Parse(linha08.Field<string>("VLR_COFINS") ?? "0") == 0L;

            if (valor.ToString() == "@" || semValor || !calcPisCofins) return "70";

            return "56";
        }

        private static object SituacaoIcms(object valor, bool calcIcms)
        {
            if (valor.ToString() == "@") return "00";
            if (!calcIcms) return "90";

            return valor;
        }


        // --------------- demais actions, recuperadas sem alteracao de regra

        public ActionResult DePara()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DePara(List<string> ids, List<string> des, List<string> paras, List<bool> excls)
        {
            if (ids == null)
            {
                return View();
            }
            List<object> list = new List<object>();
            List<IDbCommand> list2 = new List<IDbCommand>();
            List<IDbCommand> list3 = new List<IDbCommand>();
            List<IDbCommand> list4 = new List<IDbCommand>();
            using (DbConnection dbConnection = ((DbContext)new RanfeLight_Entities()).Database.Connection)
            {
            dbConnection.Open();
            DbTransaction dbTransaction = dbConnection.BeginTransaction();
            for (int i = 0; i < ids.Count; i++)
            {
                DbCommand dbCommand = dbConnection.CreateCommand();
                dbCommand.Connection = dbConnection;
                dbCommand.Transaction = dbTransaction;
                int num = int.Parse(ids[i]);
                int num2 = int.Parse(des[i]);
                int num3 = int.Parse(paras[i]);
                bool flag = excls[i];
                DbParameter dbParameter = dbCommand.CreateParameter();
                dbParameter.ParameterName = "@id";
                dbParameter.Value = num;
                DbParameter dbParameter2 = dbCommand.CreateParameter();
                dbParameter2.ParameterName = "@de";
                dbParameter2.Value = num2;
                DbParameter dbParameter3 = dbCommand.CreateParameter();
                dbParameter3.ParameterName = "@para";
                dbParameter3.Value = num3;
                if (flag)
                {
                    dbCommand.CommandText = "DELETE FROM TB_DE_PARA WHERE ID=@id";
                    dbCommand.Parameters.Add(dbParameter);
                    list2.Add(dbCommand);
                    continue;
                }
                switch (num)
                {
                case 0:
                    dbCommand.CommandText = "INSERT INTO TB_DE_PARA(DE, PARA) VALUES(@de, @para)";
                    list3.Add(dbCommand);
                    dbCommand.Parameters.Add(dbParameter2);
                    dbCommand.Parameters.Add(dbParameter3);
                    break;
                default:
                    dbCommand.CommandText = "UPDATE TB_DE_PARA SET DE=@de, PARA=@para WHERE ID=@id";
                    dbCommand.Parameters.Add(dbParameter2);
                    dbCommand.Parameters.Add(dbParameter3);
                    dbCommand.Parameters.Add(dbParameter);
                    list4.Add(dbCommand);
                    break;
                case -1:
                    break;
                }
            }
            foreach (IDbCommand item in list2)
            {
                item.ExecuteNonQuery();
            }
            foreach (IDbCommand item2 in list3)
            {
                item2.ExecuteNonQuery();
            }
            foreach (IDbCommand item3 in list4)
            {
                item3.ExecuteNonQuery();
            }
            dbTransaction.Commit();
            DbCommand dbCommand2 = dbConnection.CreateCommand();
            dbCommand2.CommandText = "SELECT ID, DE, PARA FROM TB_DE_PARA ORDER BY DE";
            using (DbDataReader dbDataReader = dbCommand2.ExecuteReader())
            {
                while (dbDataReader.Read())
                {
                    list.Add(new
                    {
                        id = dbDataReader.GetInt32(0),
                        de = dbDataReader.GetInt32(1),
                        para = dbDataReader.GetInt32(2)
                    });
                }
            }
            return Json(list);
            }
        }

        public ActionResult Config()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Config(List<HttpPostedFileBase> iFile, string ddlTipoScript)
        {
            if (!(ddlTipoScript == "SAFX07"))
            {
                if (ddlTipoScript == "SAFX08")
                {
                    System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX08_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".txt"));
                    foreach (HttpPostedFileBase item in iFile)
                    {
                        item.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"));
                    }
                }
            }
            else
            {
                System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX07_" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".txt"));
                foreach (HttpPostedFileBase item2 in iFile)
                {
                    item2.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"));
                }
            }
            return View();
        }

        public ActionResult CargaExcel()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CargaExcel(List<HttpPostedFileBase> iFile)
        {
            FileInfo[] files = new DirectoryInfo(base.Server.MapPath("~/UploadXML")).GetFiles();
            foreach (FileSystemInfo fileSystemInfo in files)
            {
                System.IO.File.Delete(fileSystemInfo.FullName);
            }
            string text = "";
            foreach (HttpPostedFileBase item in iFile)
            {
                text = Path.GetFileNameWithoutExtension(item.FileName);
                text += Path.GetExtension(item.FileName);
                item.SaveAs(base.Server.MapPath("~/UploadXML/") + text);
            }
            DataTable dataTable = Exceldata(base.Server.MapPath("~/UploadXML/") + text);
            for (int j = 0; j < dataTable.Rows.Count; j++)
            {
                if (dataTable.Rows[j][1].ToString() != "")
                {
                    System.IO.File.WriteAllText(base.Server.MapPath("/UploadXML/") + dataTable.Rows[j]["CHAVE_ACESSO"].ToString() + ".xml", dataTable.Rows[j]["XML"].ToString());
                }
            }
            return View();
        }

        private static DataTable Exceldata(string filePath)
        {
            DataTable dataTable = new DataTable();
            string text = ((!(filePath.Substring(filePath.LastIndexOf('.')).ToLower() == ".xlsx")) ? ("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=\"Excel 8.0;HDR=No;IMEX=0\"") : ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=0\""));
            OleDbConnection val = new OleDbConnection(text);
            val.Open();
            string text2 = val.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[4] { null, null, null, "TABLE" }).Rows[0]["TABLE_NAME"].ToString();
            if (!text2.EndsWith("_"))
            {
                OleDbDataAdapter val2 = new OleDbDataAdapter("SELECT  * FROM [" + text2 + "]", val);
                dataTable.Locale = CultureInfo.CurrentCulture;
                val2.Fill(dataTable);
            }
            val.Close();
            return dataTable;
        }




        /// <summary>Renderiza a Index como string. Preservado da versao anterior.</summary>
        public string Render()
        {
            return new HtmlHelper(
                new ViewContext(ControllerContext, new WebFormView(ControllerContext, "Index"),
                    new ViewDataDictionary(), new TempDataDictionary(), new StringWriter()),
                new ViewPage()).Action("Home").ToHtmlString();
        }

        // ----------------------------------------------------------- escrita

        /// <summary>
        /// Uma aba por layout: cabecalho na primeira linha, registros abaixo.
        ///
        /// CEP e INSC_ESTADUAL levam formato explicito porque o Excel, deixado a
        /// vontade, come o zero a esquerda do CEP e joga inscricao longa para
        /// notacao cientifica. O formato vai na COLUNA, e nao celula a celula: um
        /// lote grande passa de meio milhao de celulas e estilizar uma a uma
        /// estoura o executionTimeout do IIS.
        /// </summary>
        /// <param name="tudoTexto">layouts da Reforma: todos os campos sao
        /// alfanumericos no leiaute mesmo quando so tem digitos, e deixar o Excel
        /// interpretar comeria o zero a esquerda do CNPJ e mandaria o numero do
        /// documento, de 17 digitos, para notacao cientifica.</param>
        private static void EscreverAba(ExcelPackage pacote, string nome, DataTable dados, bool tudoTexto = false)
        {
            if (dados == null) return;

            ExcelWorksheet aba = pacote.Workbook.Worksheets.Add(nome);

            for (int coluna = 0; coluna < dados.Columns.Count; coluna++)
            {
                string nomeColuna = dados.Columns[coluna].ColumnName;

                aba.Cells[HEADER, coluna + 1].Value = nomeColuna;

                if (tudoTexto || nomeColuna == "CEP")
                {
                    aba.Column(coluna + 1).Style.Numberformat.Format = "@";
                }
                else if (nomeColuna == "INSC_ESTADUAL")
                {
                    aba.Column(coluna + 1).Style.Numberformat.Format = "0";
                }

                for (int linha = 0; linha < dados.Rows.Count; linha++)
                {
                    aba.Cells[linha + HEADER + 1, coluna + 1].Value = dados.Rows[linha][coluna];
                }
            }
        }
    }
}
