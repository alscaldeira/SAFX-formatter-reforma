//PROJETO ANTIGO
using ABMC.RANFE.LIGHT_SQL.DAO;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace ABMC.RANFE.LIGHT_SQL.Controllers
{
    public class HomeController : Controller
    {
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

            string path = base.Server.MapPath("~/UploadXML");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] files = directoryInfo.GetFiles();

            foreach (FileInfo fileInfo in files)
            {
                System.IO.File.Delete(fileInfo.FullName);
            }

            // O model binder entrega null, e nao lista vazia, quando o formulario
            // vem sem nenhum arquivo — e um <input type="file"> que o usuario deixou
            // em branco ainda chega como item de ContentLength zero em parte dos
            // navegadores. Os dois casos precisam sair aqui: mais adiante o lote e
            // percorrido sem checagem, e um null nesse ponto vira
            // NullReferenceException no meio da exportacao, depois de o banco ja
            // ter sido limpo.
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
                base.ViewBag.Erro = "Selecione ao menos um arquivo XML para processar.";
                return View();
            }

            foreach (HttpPostedFileBase item in enviados)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.FileName);
                fileNameWithoutExtension += Path.GetExtension(item.FileName);
                item.SaveAs(base.Server.MapPath("~/uploadxml/") + fileNameWithoutExtension);
            }

            // Campo de upload unico: o lote vem misturado e quem separa NF-e de
            // CT-e e o conteudo de cada XML. As listas abaixo sao o que os fluxos
            // de exportacao por tipo consomem — e ficam vazias, nunca nulas,
            // quando o lote nao traz aquele documento.
            Dictionary<TipoXmlFiscal, List<string>> xmlPorTipo =
                ClassificadorXml.ClassificarPasta(path);

            List<string> xmlNFe = xmlPorTipo[TipoXmlFiscal.NFe];
            List<string> xmlCTe = xmlPorTipo[TipoXmlFiscal.CTe];
            List<string> xmlNFSe = xmlPorTipo[TipoXmlFiscal.NFSe];
            List<string> xmlIgnorados = xmlPorTipo[TipoXmlFiscal.Desconhecido];

            List<string> avisosCTe = new List<string>();

            // Os CT-e saem de ~/UploadXML antes de Funcoes.CarregarArquivo1: essa
            // funcao varre a pasta inteira e, dependendo da origem configurada no
            // banco (INT_NFE_ORIGEM_XML), pode apagar cada arquivo que passa por
            // ela — inclusive um CT-e, que ela reconhece ("infCte") mas nao
            // processa. Mover para uma pasta irma antes preserva o arquivo para o
            // LerCTes abaixo e evita que ele seja classificado e descartado a toa.
            string pastaCTe = Path.Combine(Directory.GetParent(path).FullName, "UploadCTe");
            LimparPasta(pastaCTe);

            foreach (string arquivo in xmlCTe)
            {
                string destino = Path.Combine(pastaCTe, Path.GetFileName(arquivo));
                System.IO.File.Move(arquivo, destino);
            }

            LoteCTe cte = LerCTes(pastaCTe, calcPisCofins, calcIcms, avisosCTe);

            // A classificacao roda antes de Funcoes.CarregarArquivo1, que e quem
            // descompacta .zip/.eml/.msg. Um lote enviado nesses formatos ainda nao
            // tem XML nenhum na pasta neste ponto, e contar zero aqui nao significa
            // lote vazio — so significa que o conteudo ainda esta dentro do
            // container. Por isso os avisos de "nenhuma NF-e" ficam suprimidos
            // quando o upload traz algo que nao seja .xml solto.
            bool lotePodeEstarCompactado = false;

            foreach (HttpPostedFileBase item in enviados)
            {
                string extensao = Path.GetExtension(item.FileName);

                if (extensao == null || !extensao.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    lotePodeEstarCompactado = true;
                    break;
                }
            }

            LerXml lerXml = new LerXml("arquivo");
            lerXml.Integra = true;
            lerXml.GravaSynchro = false;
            DAO_SAFX.ClearAllRecords();

            Funcoes.CarregarArquivo1(lerXml);
            Funcoes.NormalizarXMLNFe();

            string comando = "";
            string comando2 = "";
            string comando3 = "";
            string comando4 = "";
            string comando5 = "";

            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX07.txt")))
            {
                comando = streamReader.ReadToEnd(); //Capa
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX08.txt")))
            {
                comando2 = streamReader.ReadToEnd(); //Linhas
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX04.txt")))
            {
                comando3 = streamReader.ReadToEnd(); //Cadastro do emitente
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX2013.txt")))
            {
                comando4 = streamReader.ReadToEnd(); //Produtos
            }
            
            using (StreamReader streamReader = new StreamReader(base.Server.MapPath("~/ScriptSQL/SAFX2043.txt")))
            {
                comando5 = streamReader.ReadToEnd(); //Codigos NBM 
            }

            DataTable dataTable = DAO_SAFX.Consulta(comando);
            DataTable dataTable2 = DAO_SAFX.Consulta(comando2);
            DataTable dataTable3 = DAO_SAFX.Consulta(comando3);
            DataTable dataTable4 = DAO_SAFX.Consulta(comando4);
            DataTable dataTable5 = DAO_SAFX.Consulta(comando5);

            // As linhas de CT-e entram nas MESMAS abas das de NF-e. dataTable/2/3
            // sempre existem com o layout completo (a consulta SQL roda mesmo sem
            // nenhuma linha de NF-e no lote), entao a mescla so precisa casar as
            // colunas que o CT-e preenche — o resto da linha fica em branco.
            dataTable = Mesclar(dataTable, cte.Safx07);
            dataTable2 = Mesclar(dataTable2, cte.Safx08);
            dataTable3 = Mesclar(dataTable3, cte.Safx04);

            string path2 = base.Server.MapPath("~/Export");
            DirectoryInfo directoryInfo2 = new DirectoryInfo(path2);
            files = directoryInfo2.GetFiles();

            foreach (FileInfo fileInfo in files)
            {
                System.IO.File.Delete(fileInfo.FullName);
            }

            // Extensoes da Reforma Tributaria (IBS/CBS): SAFX3007/3008/3009.
            //
            // Os SAFX07/08/04/2013/2043 acima saem do banco, via script SQL; os da
            // Reforma saem direto dos grupos IBSCBS do XML, que a carga do banco
            // nao guarda. Por isso a conversao le a mesma pasta ~/UploadXML que
            // acabou de ser carregada, e nao as tabelas int_nfe_*.
            //
            // A falha aqui nao pode derrubar a exportacao existente: o XML anterior
            // ao novo regime nao tem grupo IBS/CBS, e o cliente que ainda nao usa a
            // Reforma continua recebendo as demais abas normalmente. O que deu
            // errado vai para a tela, em ViewBag.AvisosReforma.
            ResultadoReforma reforma = null;
            List<string> avisosReforma = new List<string>();

            // A composicao do lote so e conhecida quando os XMLs estao soltos na
            // pasta. Se o upload veio compactado, a extracao ainda nao aconteceu e
            // qualquer contagem aqui seria zero por motivo errado — nesse caso o
            // conversor nao tem o que relatar sobre o lote, so sobre a Reforma.
            if (!lotePodeEstarCompactado)
            {
                // Um XML nao reconhecido nao para o lote, mas tambem nao pode sumir
                // calado: quem arrastou a pasta inteira precisa saber que um evento
                // de cancelamento ou um arquivo corrompido entrou no meio e nao
                // esta na planilha. NFS-e NAO entra aqui — ela nao alimenta as abas
                // vindas do banco, mas e fonte do SAFX3009 na Reforma.
                foreach (string ignorado in xmlIgnorados)
                {
                    avisosReforma.Add("Arquivo ignorado (nao e NF-e, NFS-e nem CT-e, ou esta ilegivel): "
                        + Path.GetFileName(ignorado));
                }

                if (xmlNFe.Count == 0 && xmlNFSe.Count > 0)
                {
                    avisosReforma.Add("Lote so de NFS-e: as abas SAFX07/08/04/2013/2043 vem da carga "
                        + "de NF-e no banco e sairam vazias. Os servicos estao no SAFX3009.");
                }
                else if (xmlNFe.Count == 0 && xmlCTe.Count == 0)
                {
                    avisosReforma.Add("Nenhuma NF-e no lote: as abas SAFX07/08/04/2013/2043 sairam vazias.");
                }
            }

            // Avisos do LerCTes: documento de CT-e ignorado, grupo obrigatorio
            // ausente, ICMS informado fora do CST esperado etc.
            avisosReforma.AddRange(avisosCTe);

            try
            {
                reforma = ConversorXmlReforma.Converter(
                    base.Server.MapPath("~/UploadXML"),
                    base.Server.MapPath("~/Export"),
                    base.Server.MapPath("~/ReformaTributaria/Dados"));
                avisosReforma.AddRange(reforma.Avisos);
            }
            catch (Exception ex)
            {
                avisosReforma.Add("Reforma Tributaria (SAFX3007/3008/3009) nao gerada: " + ex.Message);
            }

            // SAFX04 dos participantes sem de-para (NFS-e, e NF-e/CT-e cujo
            // participante nao esta em participantes.properties). Mesmo padrao de
            // mescla usado acima para o CT-e: casa pelo NOME da coluna, o resto da
            // linha fica em branco.
            if (reforma != null)
            {
                dataTable3 = Mesclar(dataTable3, reforma.Safx04);
            }

            var now = DateTime.Now;
            string text = $"{empresa}_{now.Year}_{now.Month:00}_{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}.xlsx";
            FileInfo newFile = new FileInfo(base.Server.MapPath("~/Export/" + text));

            using (ExcelPackage excelPackage = new ExcelPackage(newFile))
            {
                int num = 1;
                ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add("SAFX07");
                
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet.Cells[1, num].Value = ((object)dataTable.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable.Rows.Count; k++)
                    {
                        excelWorksheet.Cells[num2, num].Value = dataTable.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet2 = excelPackage.Workbook.Worksheets.Add("SAFX08");
                
                for (int j = 0; j < dataTable2.Columns.Count; j++)
                {
                    int num2 = 2;
                    string nomeColuna = dataTable2.Columns[j].ColumnName;
                    excelWorksheet2.Cells[1, num].Value = ((object)dataTable2.Columns[j]).ToString();

                    for (int k = 0; k < dataTable2.Rows.Count; k++)
                    {
                        object value = dataTable2.Rows[k][j];
                        // SUB_SERIE_DOCFIS segue a mesma regra do SAFX3007/3008 da Reforma
                        // (XmlToSafxReforma.Valores, "vazioIntencional"): o campo sai
                        // realmente vazio quando não há subsérie, nunca com o marcador "@".
                        if (nomeColuna != "SUB_SERIE_DOCFIS" && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
                            value = "@";
                        excelWorksheet2.Cells[num2, num].Value = value;
                        num2++;
                    }

                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet3 = excelPackage.Workbook.Worksheets.Add("SAFX04");
                
                for (int j = 0; j < dataTable3.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet3.Cells[1, num].Value = ((object)dataTable3.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable3.Rows.Count; k++)
                    {
                        excelWorksheet3.Cells[num2, num].Value = dataTable3.Rows[k][j];
                        if (j == 7 && !dataTable3.Rows[k][j].ToString().Equals("0"))
                        {
                            excelWorksheet3.Cells[num2, num].Style.Numberformat.Format = "0";
                        }
                        if (j == 19)
                        {
                            excelWorksheet3.Cells[num2, num].Style.Numberformat.Format = "@";
                        }
                        
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet4 = excelPackage.Workbook.Worksheets.Add("SAFX2013");
                
                for (int j = 0; j < dataTable4.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet4.Cells[1, num].Value = ((object)dataTable4.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable4.Rows.Count; k++)
                    {
                        excelWorksheet4.Cells[num2, num].Value = dataTable4.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                num = 1;
                ExcelWorksheet excelWorksheet5 = excelPackage.Workbook.Worksheets.Add("SAFX2043");
                
                for (int j = 0; j < dataTable5.Columns.Count; j++)
                {
                    int num2 = 2;
                    excelWorksheet5.Cells[1, num].Value = ((object)dataTable5.Columns[j]).ToString();
                    
                    for (int k = 0; k < dataTable4.Rows.Count; k++)
                    {
                        excelWorksheet5.Cells[num2, num].Value = dataTable5.Rows[k][j];
                        num2++;
                    }
                    
                    num++;
                }
                
                if (reforma != null)
                {
                    // As tres abas da Reforma repetem o conteudo dos arquivos
                    // SAFX3007/3008/3009 ja gravados em ~/Export: mesmas colunas,
                    // mesmos valores (inclusive o "@" de campo nulo), para que a
                    // planilha e o arquivo de importacao nunca divirjam.
                    AdicionarAba(excelPackage, "SAFX3007", reforma.Safx3007);
                    AdicionarAba(excelPackage, "SAFX3008", reforma.Safx3008);
                    AdicionarAba(excelPackage, "SAFX3009", reforma.Safx3009);
                }

                excelPackage.Save();
            }
            
            base.ViewBag.NomeArquivo = text;
            base.ViewBag.AvisosReforma = avisosReforma;
            return View();
        }

        // --------------------------------------------------------- CT-e

        /// <summary>Linhas SAFX07/08/04 produzidas a partir dos CT-e do lote.</summary>
        private sealed class LoteCTe
        {
            public DataTable Safx07 = CriarTabela(
                "COD_FIS_JUR", "MOVTO_E_S", "COD_MODELO_COTEPE", "COD_MODELO", "COD_DOCTO",
                "NUM_DOCFIS", "SERIE_DOCFIS", "DATA_EMISSAO", "DAT_FATO_GERADOR", "UF_ORIG_DEST",
                "UF_DESTINO", "NUM_DV_NFE", "COD_CFO", "COD_MUNICIPIO_ORIG", "COD_MUNICIPIO_DEST",
                "VLR_ALIQ_PIS", "VLR_ALIQ_COFINS", "COD_SIT_PIS", "COD_SIT_COFINS", "VLR_PRODUTO",
                "NUM_AUTENTIC_NFE", "DATA_SAIDA_REC", "DAT_LANC_PIS_COFINS", "VLR_TOT_NOTA",
                "VLR_BASE_PIS", "VLR_BASE_COFINS", "VLR_PIS", "VLR_COFINS", "IND_TP_FRETE",
                "COD_SITUACAO_B", "BASE_OUTR_ICMS", "VLR_FRETE", "VLR_SEGURO", "VLR_OUTRAS", "VLR_ALIQ_IPI",
                "VLR_SUBST_ICMS");

            public DataTable Safx08 = CriarTabela(
                "MOVTO_E_S", "COD_TRIB_IPI", "DATA_FISCAL", "DAT_LANC_PIS_COFINS", "COD_DOCTO",
                "COD_FIS_JUR", "NUM_DOCFIS", "SERIE_DOCFIS", "DESCRICAO_COMPL", "COD_CFO",
                "VLR_UNIT", "VLR_ITEM", "COD_SITUACAO_B", "BASE_ICMS", "VLR_ALIQ_ICMS", "VLR_ICMS",
                "TRIB_ICMS", "VLR_CONTAB_ITEM", "VLR_ALIQ_PIS", "VLR_ALIQ_COFINS",
                "COD_SITUACAO_PIS", "COD_SITUACAO_COFINS", "VLR_BASE_PIS", "VLR_BASE_COFINS",
                "VLR_PIS", "VLR_COFINS", "NUM_ITEM", "VLR_FRETE", "VLR_OUTRAS", "VLR_OUTROS1");

            public DataTable Safx04 = CriarTabela(
                "DATA_X04", "COD_FIS_JUR", "RAZAO_SOCIAL", "CPF_CGC", "INSC_ESTADUAL",
                "NOME_FANTASIA", "ENDERECO", "NUM_ENDERECO", "COMPL_ENDERECO", "BAIRRO", "CIDADE",
                "UF", "CEP", "TELEFONE", "COD_PAIS", "COD_MUNICIPIO", "IND_CONTEM_COD");

            private static DataTable CriarTabela(params string[] colunas)
            {
                DataTable tabela = new DataTable();

                foreach (string coluna in colunas)
                {
                    tabela.Columns.Add(coluna, typeof(object));
                }

                return tabela;
            }
        }

        /// <summary>
        /// Le os CT-e da pasta para linhas SAFX07/08/04.
        ///
        /// Todo acesso ao XML passa por No()/Txt(): o indexador de XmlElement
        /// devolve null quando o no nao existe, e encadear indexadores direto
        /// vira NullReferenceException no meio do lote sem dizer qual arquivo nem
        /// qual campo. Aqui o documento incompleto e pulado com um aviso nomeando
        /// os dois.
        ///
        /// Nao ha, neste projeto, um "De/Para CFOP -> conta contabil" (o script
        /// SQL de NF-e ja deixa COD_CONTA em branco) — por isso o CT-e tambem
        /// sai sem COD_CONTA, em vez de inventar uma fonte para esse dado.
        /// </summary>
        private LoteCTe LerCTes(string pasta, bool calcPisCofins, bool calcIcms, List<string> avisos)
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
                    texto = Funcoes.TiraAcento(texto);
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

                // ICMS: o grupo depende do CST. Ler sempre ICMS00 quebraria em
                // qualquer CT-e que nao fosse tributado integralmente — ICMS45
                // (isento/nao tributado) e comum em frete.
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
                // toma4 quando e um terceiro. Ler so toma3 quebra quando o
                // tomador e toma4.
                string toma = Txt(No(ide, "toma3"), "toma");
                if (string.IsNullOrEmpty(toma)) toma = Txt(No(ide, "toma4"), "toma");

                if (toma == "0") l07["IND_TP_FRETE"] = "1";
                else if (toma == "3") l07["IND_TP_FRETE"] = "2";
                else if (!string.IsNullOrEmpty(toma)) l07["IND_TP_FRETE"] = toma;

                l07["COD_SITUACAO_B"] = cst;

                // Componentes que o CT-e nao calcula. Sem eles a coluna saia em
                // branco (a mescla nao herda mais o 0 padrao das colunas inteiras
                // do SQL), e o campo numerico do SAFX nao e para ficar vazio.
                l07["VLR_FRETE"] = SEVENTEEN_ZEROS;
                l07["VLR_SEGURO"] = SEVENTEEN_ZEROS;
                l07["VLR_OUTRAS"] = SEVENTEEN_ZEROS;
                l07["VLR_SUBST_ICMS"] = SEVENTEEN_ZEROS;
                l07["VLR_ALIQ_IPI"] = SEVEN_ZEROS;
                if (!calcIcms) l07["BASE_OUTR_ICMS"] = To17Z(vBC);

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

                // O CT-e tem um unico item (a prestacao). Item 0 nao existe no
                // SAFX; os demais componentes nao se aplicam e saem zerados.
                l08["NUM_ITEM"] = "1";
                l08["VLR_FRETE"] = SEVENTEEN_ZEROS;
                l08["VLR_OUTRAS"] = SEVENTEEN_ZEROS;
                l08["VLR_OUTROS1"] = SEVENTEEN_ZEROS;

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
        /// servidor: decimal.Parse sem cultura segue a do processo do IIS, e num
        /// servidor pt-BR o ponto de "1234.56" e lido como separador de milhar.
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

        /// <summary>
        /// Acrescenta as linhas de <paramref name="origem"/> em
        /// <paramref name="destino"/> casando as colunas pelo NOME, e devolve a
        /// tabela resultante.
        ///
        /// destino (a tabela vinda do banco, layout completo) e origem (a tabela
        /// do CT-e, so com as colunas que LerCTes preenche) nao declaram as
        /// colunas na mesma ordem nem no mesmo numero — copiar por indice
        /// embaralharia os campos.
        ///
        /// A tabela devolvida tem colunas sem tipo e sem limite de tamanho
        /// (object): o SQL Server tipa pelo literal do script, entao
        /// "'' COD_MUNICIPIO_ORIG" vira varchar(1) e "'NFE' COD_DOCTO" varchar(3),
        /// e o DataColumn herda esse MaxLength — cortar o valor do CT-e por ele
        /// reduzia "CT-E" a "CT-" e "11300" a "1". O mesmo vale para o tipo:
        /// TELEFONE e Int32 no banco e o fone de 10 digitos do XML estoura. Os
        /// valores ja carregados do banco sao mantidos como estao; a tabela so
        /// alimenta a gravacao do Excel, que escreve o valor de cada celula.
        /// </summary>
        private static DataTable Mesclar(DataTable destino, DataTable origem)
        {
            if (origem == null || origem.Rows.Count == 0 || destino == null) return destino;

            DataTable solto = new DataTable(destino.TableName);

            foreach (DataColumn coluna in destino.Columns)
            {
                solto.Columns.Add(coluna.ColumnName, typeof(object));
            }

            foreach (DataRow existente in destino.Rows)
            {
                solto.Rows.Add(existente.ItemArray);
            }

            foreach (DataRow linha in origem.Rows)
            {
                DataRow nova = solto.NewRow();

                foreach (DataColumn coluna in solto.Columns)
                {
                    if (!origem.Columns.Contains(coluna.ColumnName))
                    {
                        // Coluna que a origem nao preenche (ex.: campo do SAFX04
                        // que so o SQL do SPED traz, como COD_ATIVIDADE) — mesma
                        // convencao do proprio script SQL, que usa '' fixo para
                        // esses campos.
                        nova[coluna] = "";
                    }
                    else
                    {
                        object valor = linha[coluna.ColumnName];
                        nova[coluna] = Vazio(valor) ? "@" : valor;
                    }
                }

                solto.Rows.Add(nova);
            }

            return solto;
        }

        /// <summary>Campo nulo do SAFX e "@" — mesma convencao do restante do arquivo.</summary>
        private static bool Vazio(object valor)
        {
            return valor == null || valor == DBNull.Value || valor.ToString().Trim() == "";
        }

        /// <summary>
        /// Uma aba por layout da Reforma: a primeira linha recebe os nomes de
        /// campo do leiaute e as seguintes, os registros. Todas as celulas sao
        /// gravadas como texto ("@" de formato), porque os campos do SAFX sao
        /// alfanumericos no leiaute mesmo quando so tem digitos — deixar o Excel
        /// interpretar comeria o zero a esquerda do CNPJ e do numero do documento
        /// e mandaria um valor de 17 digitos para notacao cientifica.
        /// </summary>
        private static void AdicionarAba(ExcelPackage pacote, string nome, DataTable dados)
        {
            if (dados == null) return;

            ExcelWorksheet aba = pacote.Workbook.Worksheets.Add(nome);

            for (int coluna = 0; coluna < dados.Columns.Count; coluna++)
            {
                // Formato de texto na COLUNA, nao celula a celula: o SAFX3007 tem
                // 130 campos e um lote de milhares de notas passa de meio milhao de
                // celulas. Aplicar estilo em cada uma leva dezenas de segundos e
                // estoura o executionTimeout do IIS; por coluna, o custo e o numero
                // de campos do leiaute.
                aba.Column(coluna + 1).Style.Numberformat.Format = "@";
                aba.Cells[1, coluna + 1].Value = dados.Columns[coluna].ColumnName;
            }

            for (int linha = 0; linha < dados.Rows.Count; linha++)
            {
                for (int coluna = 0; coluna < dados.Columns.Count; coluna++)
                {
                    aba.Cells[linha + 2, coluna + 1].Value = dados.Rows[linha][coluna];
                }
            }
        }

        public ActionResult Config()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Config(List<HttpPostedFileBase> iFile, string ddlTipoScript)
        {
            var now = DateTime.Now;
            var f = $"{now.Year}_{now.Month:00}_{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}.txt";

            if (ddlTipoScript.Equals("SAFX07"))
            {
                System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX07_" + f));
                
                foreach (HttpPostedFileBase item in iFile)
                {
                    item.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX07.txt"));
                }
            }
            else if (ddlTipoScript.Equals("SAFX08"))
            {
                System.IO.File.Move(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"), base.Server.MapPath("~/ScriptSQL/Versions/SAFX08_" + f));
                
                foreach (HttpPostedFileBase item2 in iFile)
                {
                    item2.SaveAs(base.Server.MapPath("~/ScriptSQL/SAFX08.txt"));
                }
            }
            
            return View();
        }

        public ActionResult CargaExcel()
        {
            return (ActionResult)this.View();
        }

        [HttpPost]
        public ActionResult CargaExcel(List<HttpPostedFileBase> iFile)
        {
            foreach (FileSystemInfo file in new DirectoryInfo(this.Server.MapPath("~/UploadXML")).GetFiles())
                System.IO.File.Delete(file.FullName);
            string str = "";
            
            foreach (HttpPostedFileBase httpPostedFileBase in iFile)
            {
                str = Path.GetFileNameWithoutExtension(httpPostedFileBase.FileName);
                str += Path.GetExtension(httpPostedFileBase.FileName);
                httpPostedFileBase.SaveAs(this.Server.MapPath("~/UploadXML/") + str);
            }
            
            DataTable dataTable = HomeController.exceldata(this.Server.MapPath("~/UploadXML/") + str);
            
            for (int index = 0; index < dataTable.Rows.Count; ++index)
            {
                if (dataTable.Rows[index][1].ToString() != "")
                    System.IO.File.WriteAllText(this.Server.MapPath("/UploadXML/") + (dataTable.Rows[index]["CHAVE_ACESSO"].ToString() + ".xml"), dataTable.Rows[index]["XML"].ToString());
            }
            
            return (ActionResult)this.View();
        }

        private static DataTable exceldata(string filePath)
        {
            DataTable dataTable = new DataTable();
            string str1 = false ? "Yes" : "No";
            string connectionString;
            
            if (filePath.Substring(filePath.LastIndexOf('.')).ToLower() == ".xlsx")
                connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=\"Excel 12.0;HDR=" + str1 + ";IMEX=0\"";
            else
                connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=\"Excel 8.0;HDR=" + str1 + ";IMEX=0\"";
            
            OleDbConnection selectConnection = new OleDbConnection(connectionString);
            selectConnection.Open();
            
            string str2 = selectConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[4]
            {
        null,
        null,
        null,
        (object) "TABLE"
            }).Rows[0]["TABLE_NAME"].ToString();
            
            if (!str2.EndsWith("_"))
            {
                OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter("SELECT  * FROM [" + str2 + "]", selectConnection);
                dataTable.Locale = CultureInfo.CurrentCulture;
                oleDbDataAdapter.Fill(dataTable);
            }
            
            selectConnection.Close();
            return dataTable;
        }
    }
}
