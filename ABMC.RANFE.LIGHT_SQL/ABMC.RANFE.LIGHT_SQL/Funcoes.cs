using ABMC.RANFE.LIGHT_SQL.DAO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ABMC.RANFE.LIGHT_SQL
{
    public class Funcoes
    {
        private const String NFe = "NF-e";
        private const String CTe = "CT-e";

        public static String versionMode = "";
        public static String validaSefaz = "";

        public void HelloWorld()
        { }

        public static void CarregarArquivo1(LerXml oLerXml)
        {
            INT_NFE_CONTROLE controleNFe = new INT_NFE_CONTROLE();
            ABMC_RACTE_CONTROLE controleCTe = new ABMC_RACTE_CONTROLE();

            string padraoXsd = "*.xsd";
            string padraoXml = "*.xml";
            
            oLerXml.PastaArquivoXsdNFe = System.Web.HttpContext.Current.Server.MapPath("~/VsdNFe/V9.99/");

            CarregaCompilaXsdNFe310(ref oLerXml, padraoXsd);

            List<INT_NFE_ORIGEM_XML> origemArquivo = DAO_INT_NFE_ORIGEM_XML.GetByTipoAcesso("DIRETORIO");

            if (origemArquivo == null || origemArquivo.Count == 0) return;
            
            foreach (INT_NFE_ORIGEM_XML itemOrigemXml in origemArquivo)
            {
                oLerXml.OrigemXml = itemOrigemXml;
                oLerXml.PastaArquivoXml = itemOrigemXml.FILE_DIR_READ;
                oLerXml.PastaBackupDeArquivos = itemOrigemXml.FILE_DIR_BKP;

                ExtractFiles(oLerXml);

                String[] arquivosXml = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~/UploadXML"), padraoXml, SearchOption.TopDirectoryOnly);

                foreach (String arqXml in arquivosXml)
                {
                    String xTexto = "";
                    String email = "";
                    String emailFrom = "";
                    bool eEmail = false;
                    int index = 0;

                    oLerXml.XmlNFeTmp = new INT_NFE_XML_TMP();
                    oLerXml.XmlCTeTmp = new ABMC_RACTE_XML_TMP();

                    oLerXml.NomeArquivo = arqXml;
                    eEmail = oLerXml.NomeArquivo.Contains("!%");
                    index = oLerXml.NomeArquivo.IndexOf("_DT_");

                    if (eEmail)
                    {
                        email = oLerXml.NomeArquivo.Substring(0, index).Replace("!%", "@").Replace(oLerXml.PastaArquivoXml, "");
                        emailFrom = oLerXml.NomeArquivo.Substring(0, index).Replace("!%", "@").Replace(oLerXml.PastaArquivoXml, "");
                    }
                    else
                    {
                        email = " ";
                        emailFrom = " ";
                    }

                    xTexto = LerArquivoParaTexto(ref oLerXml);
                    xTexto = LimpaTextoXml(xTexto);

                    try
                    {
                        oLerXml.XmlDocTexto.LoadXml(xTexto);
                    }
                    catch (Exception)
                    { }

                    try
                    {
                        oLerXml.XmlXDocument = XDocument.Parse(xTexto);

                        if (xTexto.Contains("infCte"))
                            oLerXml.XmlTipo = CTe;
                        else if (xTexto.Contains("infNFe"))
                            oLerXml.XmlTipo = NFe;
                        else
                        {
                            try
                            {
                                ApagaArquivo(oLerXml, true);
                            }
                            catch (Exception)
                            { }

                            continue;
                        }

                        if (oLerXml.XmlTipo.Equals(NFe))
                        {
                            #region Ler NF-e
                            var queryInfNFe = (from i in oLerXml.XmlXDocument.Descendants(NameSpace(1) + "infNFe")
                                                select new
                                                {
                                                    INFNFE_ID = (String)i.Attribute("Id"),
                                                    INFNFE_VERSAO = (String)i.Attribute("versao")
                                                }).SingleOrDefault();

                            if (queryInfNFe != null)
                            {
                                oLerXml.XmlNFeTmp = DAO_INT_NFE_XML_TMP.RetornaXmlPorNomeArq(queryInfNFe.INFNFE_ID);

                                if (oLerXml.XmlNFeTmp != null && oLerXml.XmlNFeTmp.ID > 0)
                                {
                                    if (oLerXml.XmlNFeTmp.ID_CONTROLE > 0)
                                    {
                                        ApagaArquivo(oLerXml, false);
                                        continue;
                                    }

                                    DAO_INT_NFE_XML_TMP.Exclui(oLerXml.XmlNFeTmp);
                                }

                                oLerXml.XmlNFeTmp = new INT_NFE_XML_TMP();
                                oLerXml.XmlNFeTmp.ID = 0;
                                oLerXml.XmlNFeTmp.ID_CONTROLE = 0;
                                oLerXml.XmlNFeTmp.MENSAGENS = "";
                                oLerXml.XmlNFeTmp.TENTATIVAS = 0;
                                oLerXml.XmlNFeTmp.XML_TEXTO = xTexto;
                                oLerXml.XmlNFeTmp.EMAIL_FROM = email;
                                oLerXml.XmlNFeTmp.XML_ARQUIVO = queryInfNFe.INFNFE_ID;
                                oLerXml.XmlNFeTmp.STATUS = "A";
                                oLerXml.XmlNFeTmp.ALTERADO_POR = 1;
                                oLerXml.XmlNFeTmp.CRIADO_POR = 1;
                                oLerXml.XmlNFeTmp.DT_ALTERACAO = DateTime.Now;
                                oLerXml.XmlNFeTmp.DT_CRIACAO = DateTime.Now;

                                oLerXml.XmlDocTexto.Schemas = new XmlSchemaSet();

                                if (queryInfNFe.INFNFE_VERSAO == "2.00")
                                    oLerXml.XmlDocTexto.Schemas.Add(oLerXml.SchemaXsd_200);
                                else if (queryInfNFe.INFNFE_VERSAO == "1.10")
                                    oLerXml.XmlDocTexto.Schemas.Add(oLerXml.SchemaXsd_110);
                                else if (queryInfNFe.INFNFE_VERSAO == "3.10")
                                    oLerXml.XmlDocTexto.Schemas.Add(oLerXml.SchemaXsd_310);

                                try
                                {
                                    bool xmlValido = true;
                                    
                                    try
                                    {
                                        if (versionMode == "F")
                                            xmlValido = oLerXml.ValidaXmlNoXsd();
                                        else
                                            xmlValido = true;
                                    }
                                    catch (Exception)
                                    {
                                        xmlValido = false;
                                    }

                                    if (xmlValido)
                                    {
                                        INT_NFE_CONTROLE controle = new INT_NFE_CONTROLE();
                                        controle = GravarXml310(oLerXml.XmlDocTexto.InnerXml);
                                                
                                        if (controle.ID > 0)
                                        {
                                            if (controle.STATUS == "0")
                                                oLerXml.XmlNFeTmp.ID_CONTROLE = controle.ID;
                                        }
                                        else
                                        {
                                            oLerXml.XmlNFeTmp.ID_CONTROLE = -1;
                                            oLerXml.XmlNFeTmp.MENSAGENS = "O Destinatário desta Nota Fiscal é Pessoa Física, ou a Nota está em Duplicidade";
                                        }
                                    }
                                    else
                                    {
                                        oLerXml.XmlNFeTmp.ID_CONTROLE = -1;
                                        oLerXml.XmlNFeTmp.MENSAGENS = String.Concat(oLerXml.XmlNFeTmp.MENSAGENS, " - Erro na estrutura do Xml.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    oLerXml.XmlNFeTmp.ID_CONTROLE = -1;
                                    oLerXml.XmlNFeTmp.MENSAGENS = ex.Message;
                                }
                            }
                            else
                            {
                                oLerXml.XmlNFeTmp.ID_CONTROLE = -1;
                                oLerXml.XmlNFeTmp.MENSAGENS = "XML Não é Documento de NF-e";
                            }
                            #endregion Fim Ler NF-e
                        }
                    }
                    catch (Exception ex)
                    {
                        if (oLerXml.XmlTipo.Equals(NFe))
                        {
                            oLerXml.XmlNFeTmp.ID_CONTROLE = -1;
                            oLerXml.XmlNFeTmp.MENSAGENS = String.Concat("Erro ", ex.ToString(), oLerXml.NomeArquivo.Replace(oLerXml.PastaArquivoXml, ""));
                        }
                    }

                    bool erro = false;

                    if (oLerXml.XmlTipo.Equals(NFe))
                    {
                        if (oLerXml.XmlNFeTmp.ID_CONTROLE == -1)
                        {
                            oLerXml.GravaXmlTmp();
                            erro = (oLerXml.XmlNFeTmp.ID_CONTROLE == -1);
                        }
                    }

                    try
                    {
                        ApagaArquivo(oLerXml, erro);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        private static void CarregaCompilaXsdNFe310(ref LerXml oLerXml, String padraoXsd)
        {
            String[] arquivosXsd;

            oLerXml.PastaArquivoXsdNFe = oLerXml.PastaArquivoXsdNFe.Replace("9.99", "3.10");

            arquivosXsd = Directory.GetFiles(oLerXml.PastaArquivoXsdNFe, padraoXsd, SearchOption.TopDirectoryOnly);

            oLerXml.SchemaXsd_310 = new XmlSchemaSet();

            foreach (String arqXsd in arquivosXsd)
            {
                if (arqXsd.Contains("schema"))
                    oLerXml.SchemaXsd_310.Add("http://www.w3.org/2000/09/xmldsig#", arqXsd);
                else
                    oLerXml.SchemaXsd_310.Add("http://www.portalfiscal.inf.br/nfe", arqXsd);
            }

            oLerXml.SchemaXsd_310.Compile();
        }

        public static XNamespace NameSpace(int Tipo)
        {
            XNamespace name;

            switch (Tipo)
            {
                case 1:
                    return "http://www.portalfiscal.inf.br/nfe";

                case 2:
                    return "http://www.synchro.com.br/nfe";

                case 3:
                    return "http://www.portalfiscal.inf.br/cte";

                default:
                    return "";
            }
        }

        private static void ApagaArquivo(LerXml oLerXml, bool erro)
        {
            if (oLerXml.PastaBackupDeArquivos != null && oLerXml.PastaBackupDeArquivos != String.Empty)
            {
                if (erro)
                    File.Copy(oLerXml.NomeArquivo, String.Concat(oLerXml.PastaBackupDeArquivos, "ParaAnalise\\", oLerXml.NomeArquivo.Replace(oLerXml.PastaArquivoXml, "")), true);
                else
                    File.Copy(oLerXml.NomeArquivo, String.Concat(System.Web.HttpContext.Current.Server.MapPath("~/UploadXML/Processados"), oLerXml.NomeArquivo.Replace(System.Web.HttpContext.Current.Server.MapPath("~/UploadXML"), "")), true);

                File.Delete(oLerXml.NomeArquivo);
            }
        }

        private static void ExtractFiles(LerXml oLerXml)
        {
            String padraoZip = ".zip";
            String padraoMsg = ".msg";
            String padraoEml = ".eml";
            bool continua = true;

            while (continua)
            {
                String[] arquivos = Directory.GetFiles(oLerXml.PastaArquivoXml, "*.*", SearchOption.TopDirectoryOnly);

                int countPadraoZip = (from a in arquivos
                                      where a.EndsWith(padraoZip)
                                      select a).Count();

                if (countPadraoZip > 0)
                {
                    ExtractZipFiles(oLerXml.PastaArquivoXml, oLerXml.PastaBackupDeArquivos);
                }

                int countPadraoMsg = (from a in arquivos
                                      where a.EndsWith(padraoMsg)
                                      select a).Count();

                if (countPadraoMsg > 0)
                {
                    ExtractMsgFiles(oLerXml);
                }

                int countPadraoEml = (from a in arquivos
                                      where a.EndsWith(padraoEml)
                                      select a).Count();

                if (countPadraoEml > 0)
                {
                    ExtractEmlFiles(oLerXml);
                }

                if (countPadraoZip == 0 && countPadraoMsg == 0 && countPadraoEml == 0)
                {
                    continua = false;
                }
            }
        }

        private static void ExtractMsgFiles(LerXml oLerXml)
        { }

        private static void ExtractEmlFiles(LerXml oLerXml)
        { }

        private static void ExtractZipFiles(String pastaArquivos, String pastaBackup)
        { }

        private static String LerArquivoParaTexto(ref LerXml oLerXml)
        {
            StringBuilder xTextoBuilderXml = new StringBuilder();

            using (StreamReader textoXml = new StreamReader(oLerXml.NomeArquivo))
            {
                xTextoBuilderXml.Append(textoXml.ReadToEnd());
            }

            return LimpaTextoXml(xTextoBuilderXml.ToString());
        }

        public static String LimpaTextoXml(String xTexto)
        {
            StringBuilder XmlTextoLimpo = new StringBuilder();

            xTexto = xTexto.Replace("\r\n ", " ");
            xTexto = xTexto.Replace(" \r\n", " ");

            while (xTexto.Contains("> "))
            {
                xTexto = xTexto.Replace("> ", ">");
            }

            while (xTexto.Contains(" >"))
            {
                xTexto = xTexto.Replace(" >", ">");
            }

            while (xTexto.Contains(" <"))
            {
                xTexto = xTexto.Replace(" <", "<");
            }

            while (xTexto.Contains("< "))
            {
                xTexto = xTexto.Replace("< ", "<");
            }

            xTexto = xTexto.Replace(">\r\n", ">");
            xTexto = xTexto.Replace("\r\n>", ">");
            xTexto = xTexto.Replace(">\r", ">");
            xTexto = xTexto.Replace(">\n", ">");
            xTexto = xTexto.Replace("\r>", ">");
            xTexto = xTexto.Replace("\n>", ">");

            xTexto = xTexto.Replace("\r\n<", "<");
            xTexto = xTexto.Replace("<\r\n", "<");
            xTexto = xTexto.Replace("\r<", "<");
            xTexto = xTexto.Replace("\n<", "<");
            xTexto = xTexto.Replace("<\r", "<");
            xTexto = xTexto.Replace("<\n", "<");

            XmlTextoLimpo.Append(xTexto);

            return XmlTextoLimpo.ToString();
        }

        public static String TiraAcento(String texto)
        {
            StringBuilder xTexto = new StringBuilder();

            texto = texto.Replace("Ç", "C");
            texto = texto.Replace("ç", "c");

            texto = texto.Replace("â", "a");
            texto = texto.Replace("Â", "A");
            texto = texto.Replace("ê", "e");
            texto = texto.Replace("Ê", "E");
            texto = texto.Replace("ô", "o");
            texto = texto.Replace("Ô", "O");

            texto = texto.Replace("Á", "A");
            texto = texto.Replace("á", "a");
            texto = texto.Replace("É", "E");
            texto = texto.Replace("é", "e");
            texto = texto.Replace("Í", "I");
            texto = texto.Replace("í", "i");
            texto = texto.Replace("Ó", "O");
            texto = texto.Replace("ó", "o");
            texto = texto.Replace("Ú", "U");
            texto = texto.Replace("ú", "u");

            texto = texto.Replace("Ã", "A");
            texto = texto.Replace("ã", "a");
            texto = texto.Replace("Õ", "O");
            texto = texto.Replace("õ", "o");

            xTexto.Append(texto);

            return xTexto.ToString();
        }

        /// <summary>
        /// Salva &lt;ide&gt; em banco de dados e o retorna no objeto INT_NFE_CONTROLE
        /// </summary>
        /// <param name="xTexto"></param>
        /// <returns></returns>
        private static INT_NFE_CONTROLE GravarXml310(String xTexto)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR", false);
            XDocument resposta = XDocument.Parse(xTexto);

            String INFNFE_ID = "";
            decimal INFNFE_VERSAO = 0;

            INT_NFE_CONTROLE controle = new INT_NFE_CONTROLE();
            INT_NFE_XML xml_classe = new INT_NFE_XML();

            var queryInfNFe = (from i in resposta.Descendants(NameSpace(1) + "infNFe")
                               select new
                               {
                                   INFNFE_ID = (string)i.Attribute("Id"),
                                   INFNFE_VERSAO = (decimal?)i.Attribute("versao")
                               }).SingleOrDefault();

            if (queryInfNFe != null)
            {
                INFNFE_ID = (string)queryInfNFe.INFNFE_ID.Substring(3, 44);
                INFNFE_VERSAO = Convert.ToDecimal(queryInfNFe.INFNFE_VERSAO);
                INT_NFE_CONTROLE xControle = DAO_INT_NFE_CONTROLE.RetornaControlePorChaveAcesso(INFNFE_ID);

                if (xControle != null)
                {
                    //???
                    DAO_INT_NFE_CONTROLE.RetornaControlePorID(xControle.ID);
                }
            }

            controle.INFNFE_ID = INFNFE_ID;
            controle.INFNFE_VERSAO = Convert.ToInt32(INFNFE_VERSAO);

            var queryide = (from i in resposta.Descendants(NameSpace(1) + "ide")
                            select new
                            {
                                INFNFE_IDE_CUF = (string)i.Element(NameSpace(1) + "cUF"),
                                INFNFE_IDE_CNF = (string)i.Element(NameSpace(1) + "cNF"),
                                INFNFE_IDE_CNOP = (string)i.Element(NameSpace(1) + "natOp"),
                                INFNFE_IDE_INDPAG = (string)i.Element(NameSpace(1) + "indPag"),
                                INFNFE_IDE_MOD = (string)i.Element(NameSpace(1) + "mod"),
                                INFNFE_IDE_SERIE = (string)i.Element(NameSpace(1) + "serie"),
                                INFNFE_IDE_NNF = (string)i.Element(NameSpace(1) + "nNF"),
                                INFNFE_IDE_DEMI = (string)i.Element(NameSpace(1) + "dhEmi"),
                                INFNFE_IDE_DSAIENT = (string)i.Element(NameSpace(1) + "dhSaiEnt"),
                                INFNFE_IDE_HSAIENT = "",
                                INFNFE_IDE_TPNF = (string)i.Element(NameSpace(1) + "tpNF"),
                                INFNFE_IDE_CMUNFG = (string)i.Element(NameSpace(1) + "cMunFG"),
                                INFNFE_IDE_TPIMP = (string)i.Element(NameSpace(1) + "tpImp"),
                                INFNFE_IDE_TPEMIS = (string)i.Element(NameSpace(1) + "tpEmis"),
                                INFNFE_IDE_CDV = (string)i.Element(NameSpace(1) + "cDV"),
                                INFNFE_IDE_TPAMB = (string)i.Element(NameSpace(1) + "tpAmb"),
                                INFNEF_IDE_IDDEST = (string)i.Element(NameSpace(1) + "idDest"),
                                INFNFE_IDE_INDFINAL = (string)i.Element(NameSpace(1) + "indFinal"),
                                INFNFE_IDE_INDPRES = (string)i.Element(NameSpace(1) + "indPres"),
                                INFNFE_IDE_FINNFE = (string)i.Element(NameSpace(1) + "finNFe"),
                                INFNFE_IDE_PROCEMI = (string)i.Element(NameSpace(1) + "procEmi"),
                                INFNFE_IDE_VERPROC = (string)i.Element(NameSpace(1) + "verProc"),
                                INFNFE_IDE_DHCONT = (string)i.Element(NameSpace(1) + "dhCont"),
                                INFNFE_IDE_XJUST = (string)i.Element(NameSpace(1) + "xJust"),
                                INFNFE_IDE_DEMI_20DATA=(string)i.Element(NameSpace(1) + "dEmi"),
                                INFNFE_IDE_DEMI_20HORA = (string)i.Element(NameSpace(1) + "hSaiEnt"),
                            }).SingleOrDefault();

            controle.INFNFE_IDE_CUF = Convert.ToInt16(queryide.INFNFE_IDE_CUF);
            controle.INFNFE_IDE_CNF = queryide.INFNFE_IDE_CNF.Substring(0, 8);
            controle.INFNFE_IDE_CNOP = queryide.INFNFE_IDE_CNOP;
            controle.INFNFE_IDE_INDPAG = Convert.ToInt16(queryide.INFNFE_IDE_INDPAG);
            controle.INFNFE_IDE_MOD = queryide.INFNFE_IDE_MOD;
            controle.INFNFE_IDE_SERIE = Convert.ToInt16(queryide.INFNFE_IDE_SERIE);
            controle.INFNFE_IDE_NNF = Convert.ToInt32(queryide.INFNFE_IDE_NNF);

            //##NovaRotina NFe3.10##
            if (INFNFE_VERSAO == 2)
            {
                if (queryide.INFNFE_IDE_DEMI_20HORA != null)
                    controle.INFNFE_IDE_DEMI = Convert.ToDateTime(queryide.INFNFE_IDE_DEMI_20DATA + "T" + queryide.INFNFE_IDE_DEMI_20HORA);
                else
                    controle.INFNFE_IDE_DEMI = Convert.ToDateTime(queryide.INFNFE_IDE_DEMI_20DATA);
            }
            else
            {
                controle.INFNFE_IDE_DEMI = Convert.ToDateTime(queryide.INFNFE_IDE_DEMI);
            }

            //##NovaRotina NFe3.10##
            if (queryide.INFNFE_IDE_DSAIENT != null)
            {
                controle.INFNFE_IDE_DSAIENT = Convert.ToDateTime(queryide.INFNFE_IDE_DSAIENT);
            }

            controle.INFNFE_IDE_TPNF = Convert.ToInt16(queryide.INFNFE_IDE_TPNF);
            controle.INFNFE_IDE_CMUNFG = Convert.ToInt32(queryide.INFNFE_IDE_CMUNFG);
            controle.INFNFE_IDE_TPIMP = Convert.ToInt16(queryide.INFNFE_IDE_TPIMP);
            controle.INFNFE_IDE_TPEMIS = Convert.ToInt16(queryide.INFNFE_IDE_TPEMIS);
            controle.INFNFE_IDE_CDV = Convert.ToInt16(queryide.INFNFE_IDE_CDV);
            controle.INFNFE_IDE_TPAMB = Convert.ToInt16(queryide.INFNFE_IDE_TPAMB);
            controle.INFNFE_IDE_FINNFE = Convert.ToInt16(queryide.INFNFE_IDE_FINNFE);
            controle.INFNFE_IDE_PROCEMI = Convert.ToInt16(queryide.INFNFE_IDE_PROCEMI);
            controle.INFNFE_IDE_VERPROC = queryide.INFNFE_IDE_VERPROC;

            if (String.IsNullOrEmpty(queryide.INFNFE_IDE_DHCONT))
                controle.INFNFE_IDE_DHCONT = DateTime.MinValue;
            else
                controle.INFNFE_IDE_DHCONT = Convert.ToDateTime(queryide.INFNFE_IDE_DHCONT);

            controle.INFNFE_IDE_XJUST = queryide.INFNFE_IDE_XJUST;

            var querydest = (from i in resposta.Descendants(NameSpace(1) + "dest")
                             select new
                             {
                                 NOME_NFE = (string)i.Element(NameSpace(1) + "xNome"),
                                 CNPJ_NFE = (string)i.Element(NameSpace(1) + "CNPJ")
                             }).SingleOrDefault();

            var queryemit = (from i in resposta.Descendants(NameSpace(1) + "emit")
                             select new
                             {
                                 NOME_NFE = (string)i.Element(NameSpace(1) + "xNome"),
                                 CNPJ_NFE = (string)i.Element(NameSpace(1) + "CNPJ")
                             }).SingleOrDefault();

            if (string.IsNullOrEmpty(querydest.CNPJ_NFE))
            {
                querydest = (from i in resposta.Descendants(NameSpace(1) + "dest")
                             select new
                             {
                                 NOME_NFE = (string)i.Element(NameSpace(1) + "xNome"),
                                 queryemit.CNPJ_NFE
                             }).SingleOrDefault();
            }

            if (controle.INFNFE_IDE_TPNF == 0 && String.IsNullOrEmpty(queryemit.CNPJ_NFE))
            {//swap dest com emi
                var xQuery = querydest;
                querydest = queryemit;
                queryemit = xQuery;
            }

            controle.NOME_EMIT_NFE = queryemit.NOME_NFE;
            controle.CNPJ_EMIT_NFE = queryemit.CNPJ_NFE;

            controle.INFNFE_IDE_IDDEST = queryide.INFNEF_IDE_IDDEST;
            controle.INFNFE_IDE_INDFINAL = queryide.INFNFE_IDE_INDFINAL;
            controle.INFNFE_IDE_INDPRES = queryide.INFNFE_IDE_INDFINAL;
            controle.INFNFE_IDE_INDPRES = queryide.INFNFE_IDE_INDPRES;

            controle.ID_SOL_NFE = 1;
            controle.CRIADO_POR = 1;
            controle.DT_CRIACAO = DateTime.Now;
            controle.ALTERADO_POR = 1;
            controle.DT_ALTERACAO = DateTime.Now;

            //Nessa versão light do RA não é preciso cadastrar empresa ou estabelecimento
            if (controle.ID == 0)
            {
                controle.STATUS = "0";
            }

            DAO_INT_NFE_CONTROLE.Salvar(controle);

            xml_classe = DAO_INT_NFE_XML.RetornoXmlPorControle(controle);

            if (xml_classe == null)
                xml_classe = new INT_NFE_XML();

            xml_classe.ID_CONTROLE = controle.ID;
            xml_classe.STATUS = "A";
            xml_classe.XML_TEXTO = xTexto;
            xml_classe.CRIADO_POR = 1;
            xml_classe.DT_CRIACAO = DateTime.Now;
            xml_classe.ALTERADO_POR = 1;
            xml_classe.DT_ALTERACAO = DateTime.Now;
            DAO_INT_NFE_XML.Salvar(xml_classe);

            return controle;
        }

        /// <summary>
        /// Não se deixe enganar pelo nome deste método.
        /// Ele salva praticamente tudo da NFe em várias tabelas diferentes.
        /// </summary>
        public static void NormalizarXMLNFe()
        {
            int erl = 0;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR", false);
            bool erro = false;

            #region Inicialização das Classes e Serviços
            INT_NFE_CONTROLE controle = new INT_NFE_CONTROLE();
            //BLL_Controle serviceControle = new BLL_Controle();

            INT_NFE_NOTA_REFER notarefer = new INT_NFE_NOTA_REFER();
            //DALRANFe<INT_NFE_NOTA_REFER, RANFeOracleEntities> serviceNotaRefer = new DALRANFe<INT_NFE_NOTA_REFER, RANFeOracleEntities>();

            INT_NFE_XML xml_classe = new INT_NFE_XML();
            //BLL_Xml serviceXml = new BLL_Xml();

            //BLL_Empresa serviceEmp = new BLL_Empresa();

            INT_NFE_NOTA_EMIT notaemit = new INT_NFE_NOTA_EMIT();
            //DALRANFe<INT_NFE_NOTA_EMIT, RANFeOracleEntities> serviceNotaEmit = new DALRANFe<INT_NFE_NOTA_EMIT, RANFeOracleEntities>();

            INT_NFE_NOTA_DEST notadest = new INT_NFE_NOTA_DEST();
            //DALRANFe<INT_NFE_NOTA_DEST, RANFeOracleEntities> serviceNotaDest = new DALRANFe<INT_NFE_NOTA_DEST, RANFeOracleEntities>();

            INT_NFE_NOTA_AVULSA notaavulta = new INT_NFE_NOTA_AVULSA();
            //DALRANFe<INT_NFE_NOTA_AVULSA, RANFeOracleEntities> serviceNotaAvulsa = new DALRANFe<INT_NFE_NOTA_AVULSA, RANFeOracleEntities>();

            INT_NFE_NOTA_RETIRADA notaretirada = new INT_NFE_NOTA_RETIRADA();
            //DALRANFe<INT_NFE_NOTA_RETIRADA, RANFeOracleEntities> serviceNotaRetirada = new DALRANFe<INT_NFE_NOTA_RETIRADA, RANFeOracleEntities>();

            INT_NFE_NOTA_ENTREGA notaentrega = new INT_NFE_NOTA_ENTREGA();
            //DALRANFe<INT_NFE_NOTA_ENTREGA, RANFeOracleEntities> serviceNotaEntrega = new DALRANFe<INT_NFE_NOTA_ENTREGA, RANFeOracleEntities>();

            INT_NFE_NOTA_TOTAIS notatotais = new INT_NFE_NOTA_TOTAIS();
            //DALRANFe<INT_NFE_NOTA_TOTAIS, RANFeOracleEntities> serviceNotaTotais = new DALRANFe<INT_NFE_NOTA_TOTAIS, RANFeOracleEntities>();

            INT_NFE_NOTA_TRANSP notatransp = new INT_NFE_NOTA_TRANSP();
            //DALRANFe<INT_NFE_NOTA_TRANSP, RANFeOracleEntities> serviceNotaTransp = new DALRANFe<INT_NFE_NOTA_TRANSP, RANFeOracleEntities>();

            INT_NFE_NOTA_TRANSP_REBOQUE notatranspreboque = new INT_NFE_NOTA_TRANSP_REBOQUE();
            //DALRANFe<INT_NFE_NOTA_TRANSP_REBOQUE, RANFeOracleEntities> serviceNotaTranspReboque = new DALRANFe<INT_NFE_NOTA_TRANSP_REBOQUE, RANFeOracleEntities>();

            INT_NFE_NOTA_TRANSP_VOL notatranspvol = new INT_NFE_NOTA_TRANSP_VOL();
            //DALRANFe<INT_NFE_NOTA_TRANSP_VOL, RANFeOracleEntities> serviceNotaTranspVol = new DALRANFe<INT_NFE_NOTA_TRANSP_VOL, RANFeOracleEntities>();

            INT_NFE_NOTA_TRANSP_VOL_LAC notatranspvollac = new INT_NFE_NOTA_TRANSP_VOL_LAC();
            //DALRANFe<INT_NFE_NOTA_TRANSP_VOL_LAC, RANFeOracleEntities> serviceNotaTranspVolLac = new DALRANFe<INT_NFE_NOTA_TRANSP_VOL_LAC, RANFeOracleEntities>();

            INT_NFE_LINHA linha = new INT_NFE_LINHA();
            //DALRANFe<INT_NFE_LINHA, RANFeOracleEntities> serviceLinha = new DALRANFe<INT_NFE_LINHA, RANFeOracleEntities>();

            INT_NFE_LINHA_ARMAS linhaarmas = new INT_NFE_LINHA_ARMAS();
            //DALRANFe<INT_NFE_LINHA_ARMAS, RANFeOracleEntities> serviceLinhaArmas = new DALRANFe<INT_NFE_LINHA_ARMAS, RANFeOracleEntities>();

            INT_NFE_LINHA_COMBUST linhacombust = new INT_NFE_LINHA_COMBUST();
            //DALRANFe<INT_NFE_LINHA_COMBUST, RANFeOracleEntities> serviceLinhaCombust = new DALRANFe<INT_NFE_LINHA_COMBUST, RANFeOracleEntities>();

            INT_NFE_LINHA_DI linhadi = new INT_NFE_LINHA_DI();
            //BLL_LinhaDi serviceLinhaDi = new BLL_LinhaDi();

            INT_NFE_LINHA_DI_ADI linhadiadi = new INT_NFE_LINHA_DI_ADI();
            //BLL_LinhaDiAdi serviceLinhaDiAdi = new BLL_LinhaDiAdi();

            INT_NFE_LINHA_IMP_COFINS linhaimpcofins = new INT_NFE_LINHA_IMP_COFINS();
            //DALRANFe<INT_NFE_LINHA_IMP_COFINS, RANFeOracleEntities> serviceLinhaImpCofins = new DALRANFe<INT_NFE_LINHA_IMP_COFINS, RANFeOracleEntities>();

            INT_NFE_LINHA_IMP_ICMS linhaimpicms = new INT_NFE_LINHA_IMP_ICMS();
            //DALRANFe<INT_NFE_LINHA_IMP_ICMS, RANFeOracleEntities> serviceLinhaImpIcms = new DALRANFe<INT_NFE_LINHA_IMP_ICMS, RANFeOracleEntities>();

            INT_NFE_LINHA_IMP_II linhaimpii = new INT_NFE_LINHA_IMP_II();
            //DALRANFe<INT_NFE_LINHA_IMP_II, RANFeOracleEntities> serviceLinhaImpIi = new DALRANFe<INT_NFE_LINHA_IMP_II, RANFeOracleEntities>();

            INT_NFE_LINHA_IMP_IPI linhaimpipi = new INT_NFE_LINHA_IMP_IPI();
            //DALRANFe<INT_NFE_LINHA_IMP_IPI, RANFeOracleEntities> serviceLinhaImpIpi = new DALRANFe<INT_NFE_LINHA_IMP_IPI, RANFeOracleEntities>();

            INT_NFE_LINHA_IMP_ISSQN linhaimpissqn = new INT_NFE_LINHA_IMP_ISSQN();
            //DALRANFe<INT_NFE_LINHA_IMP_ISSQN, RANFeOracleEntities> serviceLinhaImpIssQn = new DALRANFe<INT_NFE_LINHA_IMP_ISSQN, RANFeOracleEntities>();

            INT_NFE_LINHA_IMP_PIS linhaimppis = new INT_NFE_LINHA_IMP_PIS();
            //DALRANFe<INT_NFE_LINHA_IMP_PIS, RANFeOracleEntities> serviceLinhaImpPis = new DALRANFe<INT_NFE_LINHA_IMP_PIS, RANFeOracleEntities>();

            INT_NFE_LINHA_MEDIC linhamedic = new INT_NFE_LINHA_MEDIC();
            //DALRANFe<INT_NFE_LINHA_MEDIC, RANFeOracleEntities> serviceLinhaMedic = new DALRANFe<INT_NFE_LINHA_MEDIC, RANFeOracleEntities>();

            INT_NFE_LINHA_VEIC linhaveic = new INT_NFE_LINHA_VEIC();
            //DALRANFe<INT_NFE_LINHA_VEIC, RANFeOracleEntities> serviceLinhaVeic = new DALRANFe<INT_NFE_LINHA_VEIC, RANFeOracleEntities>();

            INT_NFE_NOTA_COBR notacobr = new INT_NFE_NOTA_COBR();
            //DALRANFe<INT_NFE_NOTA_COBR, RANFeOracleEntities> serviceNotaCobr = new DALRANFe<INT_NFE_NOTA_COBR, RANFeOracleEntities>();

            INT_NFE_NOTA_COBR_DUP notacobrdup = new INT_NFE_NOTA_COBR_DUP();
            //DALRANFe<INT_NFE_NOTA_COBR_DUP, RANFeOracleEntities> serviceNotaCobrDup = new DALRANFe<INT_NFE_NOTA_COBR_DUP, RANFeOracleEntities>();

            INT_NFE_NOTA_INF_ADIC notainfadic = new INT_NFE_NOTA_INF_ADIC();
            //DALRANFe<INT_NFE_NOTA_INF_ADIC, RANFeOracleEntities> serviceNotaInfAdic = new DALRANFe<INT_NFE_NOTA_INF_ADIC, RANFeOracleEntities>();

            INT_NFE_NOTA_INF_ADIC_CANA notainfadiccana = new INT_NFE_NOTA_INF_ADIC_CANA();
            // DALRANFe<INT_NFE_NOTA_INF_ADIC_CANA, RANFeOracleEntities> serviceNotaInfAdicCana = new DALRANFe<INT_NFE_NOTA_INF_ADIC_CANA, RANFeOracleEntities>();

            INT_NFE_NOTA_INF_ADIC_OBSCONT notainfadicobscont = new INT_NFE_NOTA_INF_ADIC_OBSCONT();
            //DALRANFe<INT_NFE_NOTA_INF_ADIC_OBSCONT, RANFeOracleEntities> serviceNotaInfAdicObsCont = new DALRANFe<INT_NFE_NOTA_INF_ADIC_OBSCONT, RANFeOracleEntities>();

            INT_NFE_NOTA_INF_ADIC_OBSFISCO notainfadicobsfisco = new INT_NFE_NOTA_INF_ADIC_OBSFISCO();
            //DALRANFe<INT_NFE_NOTA_INF_ADIC_OBSFISCO, RANFeOracleEntities> serviceNotaInfAdicObsFisco = new DALRANFe<INT_NFE_NOTA_INF_ADIC_OBSFISCO, RANFeOracleEntities>();

            INT_NFE_NOTA_INF_ADIC_PROCREF notainfadicprocref = new INT_NFE_NOTA_INF_ADIC_PROCREF();
            //DALRANFe<INT_NFE_NOTA_INF_ADIC_PROCREF, RANFeOracleEntities> serviceNotaInfAdicProcRef = new DALRANFe<INT_NFE_NOTA_INF_ADIC_PROCREF, RANFeOracleEntities>();

            INT_NFE_NOTA_CANA_FORDIA notacanafordia = new INT_NFE_NOTA_CANA_FORDIA();
            //DALRANFe<INT_NFE_NOTA_CANA_FORDIA, RANFeOracleEntities> serviceNotaCanaForDia = new DALRANFe<INT_NFE_NOTA_CANA_FORDIA, RANFeOracleEntities>();

            INT_NFE_NOTA_CANA_DEDUC notacanadeduc = new INT_NFE_NOTA_CANA_DEDUC();
            //DALRANFe<INT_NFE_NOTA_CANA_DEDUC, RANFeOracleEntities> serviceNotaCanaDeduc = new DALRANFe<INT_NFE_NOTA_CANA_DEDUC, RANFeOracleEntities>();
            #endregion Inicialização das Classes e Serviços

            var queryControle = DAO_INT_NFE_CONTROLE.RetornaControlePorStatus("0");

            if (queryControle == null || queryControle.Count == 0) return;

            foreach (INT_NFE_CONTROLE itemControle in queryControle)
            {
                try
                {
                    erro = false;
                    controle = itemControle;

                    // Carrega XML na memória
                    var queryXml = DAO_INT_NFE_XML.RetornoXmlPorControle(itemControle);
                    XDocument XmlTexto = XDocument.Parse(TiraAcento(queryXml.XML_TEXTO.Replace("\0", "").ToString()));

                    #region NFref
                    try
                    {
                        var queryNFref = (from i in XmlTexto.Descendants(NameSpace(1) + "NFref")
                                            select new
                                            {
                                                INFNFE_IDE_NFR_RNFE = (string)i.Element(NameSpace(1) + "refNFe"),
                                                qrefNF = (XElement)i.Element(NameSpace(1) + "refNF"),
                                                qrefNFP = (XElement)i.Element(NameSpace(1) + "refNFP"),
                                                qrefECF = (XElement)i.Element(NameSpace(1) + "refECF")
                                            }).ToList();

                        if (queryNFref != null && queryNFref.Count > 0)
                        {
                            foreach (var itemNFref in queryNFref)
                            {
                                notarefer = new INT_NFE_NOTA_REFER();
                                notarefer.ID_CONTROLE = itemControle.ID;
                                notarefer.INFNFE_IDE_NFR_RNFE = itemNFref.INFNFE_IDE_NFR_RNFE;

                                if (itemNFref.qrefNF != null)
                                {
                                    notarefer.INFNFE_IDE_NFR_RNF_CUF = (Int16?)itemNFref.qrefNF.Element(NameSpace(1) + "cUF");
                                    notarefer.INFNFE_IDE_NFR_RNF_AAMM = (Int16?)itemNFref.qrefNF.Element(NameSpace(1) + "AAMM");
                                    notarefer.INFNFE_IDE_NFR_RNF_CNPJ = (string)itemNFref.qrefNF.Element(NameSpace(1) + "CNPJ");
                                    notarefer.INFNFE_IDE_NFR_RNF_MOD = (string)itemNFref.qrefNF.Element(NameSpace(1) + "mod");
                                    notarefer.INFNFE_IDE_NFR_RNF_SERIE = (Int16?)itemNFref.qrefNF.Element(NameSpace(1) + "serie");
                                    notarefer.INFNFE_IDE_NFR_RNF_NNF = (int?)itemNFref.qrefNF.Element(NameSpace(1) + "nNF");
                                }

                                if (itemNFref.qrefNFP != null)
                                {
                                    notarefer.INFNFE_IDE_NFR_RNFP_CUF = (Int16?)itemNFref.qrefNFP.Element(NameSpace(1) + "cUF");
                                    notarefer.INFNFE_IDE_NFR_RNFP_AAMM = (Int16?)itemNFref.qrefNFP.Element(NameSpace(1) + "AAMM");
                                    notarefer.INFNFE_IDE_NFR_RNFP_CNPJ = (string)itemNFref.qrefNFP.Element(NameSpace(1) + "CNPJ");
                                    notarefer.INFNFE_IDE_NFR_RNFP_CPF = (string)itemNFref.qrefNFP.Element(NameSpace(1) + "CPF");
                                    notarefer.INFNFE_IDE_NFR_RNFP_IE = (string)itemNFref.qrefNFP.Element(NameSpace(1) + "IE");
                                    notarefer.INFNFE_IDE_NFR_RNFP_MOD = (Int16?)itemNFref.qrefNFP.Element(NameSpace(1) + "mod");
                                    notarefer.INFNFE_IDE_NFR_RNFP_SERIE = (Int16?)itemNFref.qrefNFP.Element(NameSpace(1) + "serie");
                                    notarefer.INFNFE_IDE_NFR_RNFP_NNF = (int?)itemNFref.qrefNFP.Element(NameSpace(1) + "nNF");
                                    notarefer.INFNFE_IDE_NFR_RNFP_RCTE = (string)itemNFref.qrefNFP.Element(NameSpace(1) + "refCTe");
                                }

                                if (itemNFref.qrefECF != null)
                                {
                                    notarefer.INFNFE_IDE_NFR_RNFP_RECF_MOD = (string)itemNFref.qrefECF.Element(NameSpace(1) + "mod");
                                    notarefer.INFNFE_IDE_NFR_RNFP_RECF_NECF = (Int16?)itemNFref.qrefECF.Element(NameSpace(1) + "nECF");
                                    notarefer.INFNFE_IDE_NFR_RNFP_RECF_NCOO = (Int16?)itemNFref.qrefECF.Element(NameSpace(1) + "nCOO");
                                }

                                notarefer.DT_CRIACAO = DateTime.Now;
                                notarefer.CRIADO_POR = 1;
                                notarefer.DT_ALTERACAO = DateTime.Now;
                                notarefer.ALTERADO_POR = 1;
                                notarefer.STATUS = "A";
                                DAO_INT_NFE_NOTA_REFER.Salvar(notarefer);
                            }
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        continue;
#endif
                    }
                    #endregion
                    erl = 100;
                    #region  dest
                    notaemit = new INT_NFE_NOTA_EMIT();

                    try
                    {
                        if (itemControle.INFNFE_IDE_TPNF == 1)
                        {
                            var queryNotaEmit = (from i in XmlTexto.Descendants(NameSpace(1) + "emit")
                                                    select new
                                                    {
                                                        INFNFE_EMIT_CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                        INFNFE_EMIT_CPF = (string)i.Element(NameSpace(1) + "CPF"),
                                                        INFNFE_EMIT_XNOME = (string)i.Element(NameSpace(1) + "xNome"),
                                                        INFNFE_EMIT_XFANT = (string)i.Element(NameSpace(1) + "xFant"),
                                                        INFNFE_EMIT_IE = (string)i.Element(NameSpace(1) + "IE"),
                                                        INFNFE_EMIT_IM = (string)i.Element(NameSpace(1) + "IM"),
                                                        INFNFE_EMIT_CNAE = (string)i.Element(NameSpace(1) + "CNAE"),
                                                        INFNFE_EMIT_CRT = (string)i.Element(NameSpace(1) + "CRT")
                                                    }).SingleOrDefault();

                            if (queryNotaEmit != null)
                            {
                                notaemit.INFNFE_EMIT_CNPJ = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_CNPJ) ? "99999999999999" : queryNotaEmit.INFNFE_EMIT_CNPJ;
                                notaemit.INFNFE_EMIT_CPF = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_CPF) ? "" : queryNotaEmit.INFNFE_EMIT_CPF;
                                notaemit.INFNFE_EMIT_XNOME = queryNotaEmit.INFNFE_EMIT_XNOME;
                                notaemit.INFNFE_EMIT_XFANT = queryNotaEmit.INFNFE_EMIT_XFANT;
                                notaemit.INFNFE_EMIT_IE = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_IE) ? "99999999999999" : queryNotaEmit.INFNFE_EMIT_IE;
                                notaemit.INFNFE_EMIT_IM = queryNotaEmit.INFNFE_EMIT_IM == null ? "" : queryNotaEmit.INFNFE_EMIT_IM;
                                notaemit.INFNFE_EMIT_CNAE = queryNotaEmit.INFNFE_EMIT_CNAE == null ? "" : queryNotaEmit.INFNFE_EMIT_CNAE;
                                notaemit.INFNFE_EMIT_CRT = Convert.ToInt16(queryNotaEmit.INFNFE_EMIT_CRT);
                            }

                            var queryNotaEmitEnder = (from i in XmlTexto.Descendants(NameSpace(1) + "enderEmit")
                                                        select new
                                                        {
                                                            INFNFE_EMIT_ENDEREMIT_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                            INFNFE_EMIT_ENDEREMIT_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                            INFNFE_EMIT_ENDEREMIT_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                            INFNFE_EMIT_ENDEREMIT_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                            INFNFE_EMIT_ENDEREMIT_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                            INFNFE_EMIT_ENDEREMIT_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                            INFNFE_EMIT_ENDEREMIT_UF = (string)i.Element(NameSpace(1) + "UF"),
                                                            INFNFE_EMIT_ENDEREMIT_CEP = (decimal?)i.Element(NameSpace(1) + "CEP"),
                                                            INFNFE_EMIT_ENDEREMIT_CPAIS = (decimal?)i.Element(NameSpace(1) + "cPais"),
                                                            INFNFE_EMIT_ENDEREMIT_XPAIS = (string)i.Element(NameSpace(1) + "xPais"),
                                                            INFNFE_EMIT_ENDEREMIT_FONE = (decimal?)i.Element(NameSpace(1) + "fone")
                                                        }).SingleOrDefault();

                            if (queryNotaEmitEnder != null)
                            {
                                notaemit.INFNFE_EMIT_ENDEREMIT_XLGR = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XLGR;
                                notaemit.INFNFE_EMIT_ENDEREMIT_NRO = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_NRO;
                                notaemit.INFNFE_EMIT_ENDEREMIT_XCPL = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XCPL == null ? "" : queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XCPL;
                                notaemit.INFNFE_EMIT_ENDEREMIT_XBAIRRO = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XBAIRRO;
                                notaemit.INFNFE_EMIT_ENDEREMIT_CMUN = Convert.ToInt32(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CMUN);
                                notaemit.INFNFE_EMIT_ENDEREMIT_XMUN = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XMUN;
                                notaemit.INFNFE_EMIT_ENDEREMIT_UF = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_UF;
                                notaemit.INFNFE_EMIT_ENDEREMIT_CEP = Convert.ToInt32(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CEP);
                                notaemit.INFNFE_EMIT_ENDEREMIT_CPAIS = Convert.ToInt16(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CPAIS);
                                notaemit.INFNFE_EMIT_ENDEREMIT_XPAIS = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XPAIS;
                                notaemit.INFNFE_EMIT_ENDEREMIT_FONE = Convert.ToInt64(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_FONE);
                            }
                        }
                        else
                        {
                            var queryNotaEmit = (from i in XmlTexto.Descendants(NameSpace(1) + "dest")
                                                    select new
                                                    {
                                                        INFNFE_EMIT_CNPJ = (String)i.Element(NameSpace(1) + "CNPJ"),
                                                        INFNFE_EMIT_CPF = (String)i.Element(NameSpace(1) + "CPF"),
                                                        INFNFE_EMIT_XNOME = (String)i.Element(NameSpace(1) + "xNome"),
                                                        INFNFE_EMIT_XFANT = "",
                                                        INFNFE_EMIT_IE = (String)i.Element(NameSpace(1) + "IE"),
                                                        INFNFE_EMIT_IM = "",
                                                        INFNFE_EMIT_CNAE = "",
                                                        INFNFE_EMIT_CRT = "0"
                                                    }).SingleOrDefault();

                            if (queryNotaEmit != null)
                            {
                                notaemit.INFNFE_EMIT_CNPJ = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_CNPJ) ? "99999999999999" : queryNotaEmit.INFNFE_EMIT_CNPJ;
                                notaemit.INFNFE_EMIT_CPF = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_CPF) ? "" : queryNotaEmit.INFNFE_EMIT_CPF;
                                notaemit.INFNFE_EMIT_XNOME = queryNotaEmit.INFNFE_EMIT_XNOME;
                                notaemit.INFNFE_EMIT_XFANT = queryNotaEmit.INFNFE_EMIT_XFANT;
                                notaemit.INFNFE_EMIT_IE = String.IsNullOrEmpty(queryNotaEmit.INFNFE_EMIT_IE) ? "99999999999999" : queryNotaEmit.INFNFE_EMIT_IE;
                                notaemit.INFNFE_EMIT_IM = queryNotaEmit.INFNFE_EMIT_IM == null ? "" : queryNotaEmit.INFNFE_EMIT_IM;
                                notaemit.INFNFE_EMIT_CNAE = queryNotaEmit.INFNFE_EMIT_CNAE == null ? "" : queryNotaEmit.INFNFE_EMIT_CNAE;
                                notaemit.INFNFE_EMIT_CRT = Convert.ToInt16(queryNotaEmit.INFNFE_EMIT_CRT);
                            }

                            var queryNotaEmitEnder = (from i in XmlTexto.Descendants(NameSpace(1) + "enderDest")
                                                        select new
                                                        {
                                                            INFNFE_EMIT_ENDEREMIT_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                            INFNFE_EMIT_ENDEREMIT_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                            INFNFE_EMIT_ENDEREMIT_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                            INFNFE_EMIT_ENDEREMIT_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                            INFNFE_EMIT_ENDEREMIT_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                            INFNFE_EMIT_ENDEREMIT_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                            INFNFE_EMIT_ENDEREMIT_UF = (string)i.Element(NameSpace(1) + "UF"),
                                                            INFNFE_EMIT_ENDEREMIT_CEP = (decimal?)i.Element(NameSpace(1) + "CEP"),
                                                            INFNFE_EMIT_ENDEREMIT_CPAIS = (decimal?)i.Element(NameSpace(1) + "cPais"),
                                                            INFNFE_EMIT_ENDEREMIT_XPAIS = (string)i.Element(NameSpace(1) + "xPais"),
                                                            INFNFE_EMIT_ENDEREMIT_FONE = (decimal?)i.Element(NameSpace(1) + "fone")
                                                        }).SingleOrDefault();

                            if (queryNotaEmitEnder != null)
                            {
                                notaemit.INFNFE_EMIT_ENDEREMIT_XLGR = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XLGR;
                                notaemit.INFNFE_EMIT_ENDEREMIT_NRO = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_NRO;
                                notaemit.INFNFE_EMIT_ENDEREMIT_XCPL = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XCPL == null ? "" : queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XCPL;
                                notaemit.INFNFE_EMIT_ENDEREMIT_XBAIRRO = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XBAIRRO;
                                notaemit.INFNFE_EMIT_ENDEREMIT_CMUN = Convert.ToInt32(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CMUN);
                                notaemit.INFNFE_EMIT_ENDEREMIT_XMUN = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XMUN;
                                notaemit.INFNFE_EMIT_ENDEREMIT_UF = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_UF;
                                notaemit.INFNFE_EMIT_ENDEREMIT_CEP = Convert.ToInt32(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CEP);
                                notaemit.INFNFE_EMIT_ENDEREMIT_CPAIS = Convert.ToInt16(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_CPAIS);
                                notaemit.INFNFE_EMIT_ENDEREMIT_XPAIS = queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_XPAIS;
                                notaemit.INFNFE_EMIT_ENDEREMIT_FONE = Convert.ToInt64(queryNotaEmitEnder.INFNFE_EMIT_ENDEREMIT_FONE);
                            }
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        continue;
#endif
                    }

                    try
                    {
                        notaemit.ID_CONTROLE = itemControle.ID;
                        notaemit.DT_CRIACAO = DateTime.Now;
                        notaemit.CRIADO_POR = 1;
                        notaemit.DT_ALTERACAO = DateTime.Now;
                        notaemit.ALTERADO_POR = 1;
                        notaemit.STATUS = "A";
                        DAO_INT_NFE_NOTA_EMIT.Salvar(notaemit);
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#endif
                    }
                    #endregion
                    erl = 200;
                    #region emit
                    notadest = new INT_NFE_NOTA_DEST();

                    try
                    {
                        if (itemControle.INFNFE_IDE_TPNF == 1)
                        {
                            var queryNotaDest = (from i in XmlTexto.Descendants(NameSpace(1) + "dest")
                                                    select new
                                                    {
                                                        INFNFE_DEST_CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                        INFNFE_DEST_CPF = (string)i.Element(NameSpace(1) + "CPF"),
                                                        INFNFE_DEST_XNOME = (string)i.Element(NameSpace(1) + "xNome"),
                                                        INFNFE_DEST_IE = (string)i.Element(NameSpace(1) + "IE"),
                                                        INFNFE_DEST_ISUF = (string)i.Element(NameSpace(1) + "ISUF"),
                                                        INFNFE_DEST_EMAIL = (string)i.Element(NameSpace(1) + "email"),
                                                        INFNFE_DEST_IDESTRANGEIRO = (string)i.Element(NameSpace(1) + "idEstrangeiro"),
                                                        INFNFE_DEST_INDIEDEST = (string)i.Element(NameSpace(1) + "indIEDest"),
                                                        INFNFE_DEST_IM = (string)i.Element(NameSpace(1) + "IM")
                                                    }).SingleOrDefault();

                            if (queryNotaDest != null)
                            {
                                notadest.ID_CONTROLE = itemControle.ID;
                                notadest.INFNFE_DEST_CNPJ = queryNotaDest.INFNFE_DEST_CNPJ == null ? queryNotaDest.INFNFE_DEST_IDESTRANGEIRO != null ? queryNotaDest.INFNFE_DEST_IDESTRANGEIRO : queryNotaDest.INFNFE_DEST_CPF : queryNotaDest.INFNFE_DEST_CNPJ;
                                notadest.INFNFE_DEST_CPF = queryNotaDest.INFNFE_DEST_CPF == null ? "" : queryNotaDest.INFNFE_DEST_CPF;
                                notadest.INFNFE_DEST_XNOME = queryNotaDest.INFNFE_DEST_XNOME;
                                
                                if (!string.IsNullOrEmpty(queryNotaDest.INFNFE_DEST_IE))
                                {
                                    if (queryNotaDest.INFNFE_DEST_IE.ToLower() == "isento")
                                    {
                                        notadest.INFNFE_DEST_IE = 99;
                                    }
                                    else
                                    {
                                        notadest.INFNFE_DEST_IE = Convert.ToInt64(queryNotaDest.INFNFE_DEST_IE);
                                    }
                                }
                                else
                                {
                                    notadest.INFNFE_DEST_IE = 0;
                                }

                                notadest.INFNFE_DEST_ISUF = queryNotaDest.INFNFE_DEST_ISUF == null ? "" : queryNotaDest.INFNFE_DEST_ISUF;
                                notadest.INFNFE_DEST_EMAIL = queryNotaDest.INFNFE_DEST_EMAIL == null ? "" : queryNotaDest.INFNFE_DEST_EMAIL;
                                notadest.INFNFE_DEST_INDIEDEST = queryNotaDest.INFNFE_DEST_INDIEDEST == null ? "" : queryNotaDest.INFNFE_DEST_INDIEDEST;
                                notadest.INFNFE_DEST_INDIEDEST = queryNotaDest.INFNFE_DEST_INDIEDEST == null ? "" : queryNotaDest.INFNFE_DEST_INDIEDEST;
                                notadest.INFNFE_DEST_IM = queryNotaDest.INFNFE_DEST_IM == null ? "" : queryNotaDest.INFNFE_DEST_IM;
                            }

                            Int32 foneDest = 0;

                            var queryNotaDestEnder = (from i in XmlTexto.Descendants(NameSpace(1) + "enderDest")
                                                        select new
                                                        {
                                                            INFNFE_DEST_ENDERDEST_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                            INFNFE_DEST_ENDERDEST_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                            INFNFE_DEST_ENDERDEST_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                            INFNFE_DEST_ENDERDEST_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                            INFNFE_DEST_ENDERDEST_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                            INFNFE_DEST_ENDERDEST_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                            INFNFE_DEST_ENDERDEST_UF = (string)i.Element(NameSpace(1) + "UF"),
                                                            INFNFE_DEST_ENDERDEST_CEP = (decimal?)i.Element(NameSpace(1) + "CEP"),
                                                            INFNFE_DEST_ENDERDEST_CPAIS = (string)i.Element(NameSpace(1) + "cPais"),
                                                            INFNFE_DEST_ENDERDEST_XPAIS = (string)i.Element(NameSpace(1) + "xPais"),
                                                            INFNFE_DEST_ENDERDEST_FONE = (string)i.Element(NameSpace(1) + "fone")
                                                        }).SingleOrDefault();
                            
                            if (queryNotaDestEnder != null)
                            {
                                notadest.INFNFE_DEST_ENDERDEST_XLGR = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XLGR;
                                notadest.INFNFE_DEST_ENDERDEST_NRO = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_NRO;
                                notadest.INFNFE_DEST_ENDERDEST_XCPL = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XCPL == null ? "" : queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XCPL;
                                notadest.INFNFE_DEST_ENDERDEST_XBAIRRO = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XBAIRRO;
                                notadest.INFNFE_DEST_ENDERDEST_CMUN = Convert.ToInt32(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CMUN);
                                notadest.INFNFE_DEST_ENDERDEST_XMUN = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XMUN;
                                notadest.INFNFE_DEST_ENDERDEST_UF = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_UF;
                                notadest.INFNFE_DEST_ENDERDEST_CEP = Convert.ToInt32(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CEP);
                                notadest.INFNFE_DEST_ENDERDEST_CPAIS = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CPAIS;
                                notadest.INFNFE_DEST_ENDERDEST_XPAIS = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XPAIS;

                                Int32.TryParse(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_FONE, out foneDest);
                                notadest.INFNFE_DEST_ENDERDEST_FONE = foneDest;
                            }
                        }
                        else
                        {
                            var queryNotaDest = (from i in XmlTexto.Descendants(NameSpace(1) + "emit")
                                                    select new
                                                    {
                                                        INFNFE_DEST_CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                        INFNFE_DEST_CPF = (string)i.Element(NameSpace(1) + "CPF"),
                                                        INFNFE_DEST_XNOME = (string)i.Element(NameSpace(1) + "xNome"),
                                                        INFNFE_DEST_IE = (string)i.Element(NameSpace(1) + "IE"),
                                                        INFNFE_DEST_ISUF = "",
                                                        INFNFE_DEST_EMAIL = ""
                                                    }).SingleOrDefault();

                            if (queryNotaDest != null)
                            {
                                notadest.ID_CONTROLE = itemControle.ID;
                                notadest.INFNFE_DEST_CNPJ = queryNotaDest.INFNFE_DEST_CNPJ;
                                notadest.INFNFE_DEST_CPF = queryNotaDest.INFNFE_DEST_CPF == null ? "" : queryNotaDest.INFNFE_DEST_CPF;
                                notadest.INFNFE_DEST_XNOME = queryNotaDest.INFNFE_DEST_XNOME;
                                notadest.INFNFE_DEST_IE = String.IsNullOrEmpty(queryNotaDest.INFNFE_DEST_IE) ? 0 : Convert.ToInt64(queryNotaDest.INFNFE_DEST_IE);
                                notadest.INFNFE_DEST_ISUF = queryNotaDest.INFNFE_DEST_ISUF == null ? "" : queryNotaDest.INFNFE_DEST_ISUF;
                                notadest.INFNFE_DEST_EMAIL = queryNotaDest.INFNFE_DEST_EMAIL == null ? "" : queryNotaDest.INFNFE_DEST_EMAIL;
                            }

                            var queryNotaDestEnder = (from i in XmlTexto.Descendants(NameSpace(1) + "enderEmit")
                                                        select new
                                                        {
                                                            INFNFE_DEST_ENDERDEST_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                            INFNFE_DEST_ENDERDEST_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                            INFNFE_DEST_ENDERDEST_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                            INFNFE_DEST_ENDERDEST_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                            INFNFE_DEST_ENDERDEST_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                            INFNFE_DEST_ENDERDEST_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                            INFNFE_DEST_ENDERDEST_UF = (string)i.Element(NameSpace(1) + "UF"),
                                                            INFNFE_DEST_ENDERDEST_CEP = (decimal?)i.Element(NameSpace(1) + "CEP"),
                                                            INFNFE_DEST_ENDERDEST_CPAIS = (string)i.Element(NameSpace(1) + "cPais"),
                                                            INFNFE_DEST_ENDERDEST_XPAIS = (string)i.Element(NameSpace(1) + "xPais"),
                                                            INFNFE_DEST_ENDERDEST_FONE = (string)i.Element(NameSpace(1) + "fone")
                                                        }).SingleOrDefault();
                            
                            if (queryNotaDestEnder != null)
                            {
                                notadest.INFNFE_DEST_ENDERDEST_XLGR = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XLGR;
                                notadest.INFNFE_DEST_ENDERDEST_NRO = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_NRO;
                                notadest.INFNFE_DEST_ENDERDEST_XCPL = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XCPL == null ? "" : queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XCPL;
                                notadest.INFNFE_DEST_ENDERDEST_XBAIRRO = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XBAIRRO;
                                notadest.INFNFE_DEST_ENDERDEST_CMUN = Convert.ToInt32(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CMUN);
                                notadest.INFNFE_DEST_ENDERDEST_XMUN = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XMUN;
                                notadest.INFNFE_DEST_ENDERDEST_UF = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_UF;
                                notadest.INFNFE_DEST_ENDERDEST_CEP = Convert.ToInt32(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CEP);
                                notadest.INFNFE_DEST_ENDERDEST_CPAIS = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_CPAIS;
                                notadest.INFNFE_DEST_ENDERDEST_XPAIS = queryNotaDestEnder.INFNFE_DEST_ENDERDEST_XPAIS;

                                Int32 foneDest = 0;
                                Int32.TryParse(queryNotaDestEnder.INFNFE_DEST_ENDERDEST_FONE, out foneDest);
                                notadest.INFNFE_DEST_ENDERDEST_FONE = foneDest;
                            }
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        continue;
#endif
                    }

                    try
                    {
                        notadest.ID_CONTROLE = itemControle.ID;
                        notadest.DT_CRIACAO = DateTime.Now;
                        notadest.CRIADO_POR = 1;
                        notadest.DT_ALTERACAO = DateTime.Now;
                        notadest.ALTERADO_POR = 1;
                        notadest.STATUS = "A";
                        DAO_INT_NFE_NOTA_DEST.Salvar(notadest);
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        continue;
#endif
                    }
                    #endregion
                    erl = 300;
                    #region retirada
                    try
                    {
                        var queryNotaRetirada = (from i in XmlTexto.Descendants(NameSpace(1) + "retirada")
                                                    select new
                                                    {
                                                        INFNFE_RETIRADA_CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                        INFNFE_RETIRADA_CPF = (string)i.Element(NameSpace(1) + "CPF"),
                                                        INFNFE_RETIRADA_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                        INFNFE_RETIRADA_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                        INFNFE_RETIRADA_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                        INFNFE_RETIRADA_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                        INFNFE_RETIRADA_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                        INFNFE_RETIRADA_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                        INFNFE_RETIRADA_UF = (string)i.Element(NameSpace(1) + "UF")
                                                    }).SingleOrDefault();

                        if (queryNotaRetirada != null)
                        {
                            notaretirada = new INT_NFE_NOTA_RETIRADA();
                            notaretirada.ID_CONTROLE = itemControle.ID;
                            notaretirada.INFNFE_RETIRADA_CNPJ = queryNotaRetirada.INFNFE_RETIRADA_CNPJ;
                            notaretirada.INFNFE_RETIRADA_CPF = queryNotaRetirada.INFNFE_RETIRADA_CPF;
                            notaretirada.INFNFE_RETIRADA_XLGR = queryNotaRetirada.INFNFE_RETIRADA_XLGR;
                            notaretirada.INFNFE_RETIRADA_NRO = queryNotaRetirada.INFNFE_RETIRADA_NRO;
                            notaretirada.INFNFE_RETIRADA_XCPL = queryNotaRetirada.INFNFE_RETIRADA_XCPL == null ? "" : queryNotaRetirada.INFNFE_RETIRADA_XCPL;
                            notaretirada.INFNFE_RETIRADA_XBAIRRO = queryNotaRetirada.INFNFE_RETIRADA_XBAIRRO;
                            notaretirada.INFNFE_RETIRADA_CMUN = Convert.ToInt32(queryNotaRetirada.INFNFE_RETIRADA_CMUN);
                            notaretirada.INFNFE_RETIRADA_XMUN = queryNotaRetirada.INFNFE_RETIRADA_XMUN;
                            notaretirada.INFNFE_RETIRADA_UF = queryNotaRetirada.INFNFE_RETIRADA_UF;

                            notaretirada.ID_CONTROLE = itemControle.ID;
                            notaretirada.DT_CRIACAO = DateTime.Now;
                            notaretirada.CRIADO_POR = 1;
                            notaretirada.DT_ALTERACAO = DateTime.Now;
                            notaretirada.ALTERADO_POR = 1;
                            notaretirada.STATUS = "A";
                            DAO_INT_NFE_NOTA_RETIRADA.Salvar(notaretirada);
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        continue;
#endif
                    }
                    #endregion
                    erl = 400;
                    #region entrega
                    try
                    {
                        var queryNotaEntrega = (from i in XmlTexto.Descendants(NameSpace(1) + "entrega")
                                                select new
                                                {
                                                    INFNFE_ENTREGA_CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                    INFNFE_ENTREGA_CPF = (string)i.Element(NameSpace(1) + "CPF"),
                                                    INFNFE_ENTREGA_XLGR = (string)i.Element(NameSpace(1) + "xLgr"),
                                                    INFNFE_ENTREGA_NRO = (string)i.Element(NameSpace(1) + "nro"),
                                                    INFNFE_ENTREGA_XCPL = (string)i.Element(NameSpace(1) + "xCpl"),
                                                    INFNFE_ENTREGA_XBAIRRO = (string)i.Element(NameSpace(1) + "xBairro"),
                                                    INFNFE_ENTREGA_CMUN = (decimal?)i.Element(NameSpace(1) + "cMun"),
                                                    INFNFE_ENTREGA_XMUN = (string)i.Element(NameSpace(1) + "xMun"),
                                                    INFNFE_ENTREGA_UF = (string)i.Element(NameSpace(1) + "UF")
                                                }).SingleOrDefault();

                        if (queryNotaEntrega != null)
                        {
                            notaentrega = new INT_NFE_NOTA_ENTREGA();
                            notaentrega.ID_CONTROLE = itemControle.ID;
                            notaentrega.INFNFE_ENTREGA_CNPJ = queryNotaEntrega.INFNFE_ENTREGA_CNPJ;
                            notaentrega.INFNFE_ENTREGA_CPF = queryNotaEntrega.INFNFE_ENTREGA_CPF;
                            notaentrega.INFNFE_ENTREGA_XLGR = queryNotaEntrega.INFNFE_ENTREGA_XLGR;
                            notaentrega.INFNFE_ENTREGA_NRO = queryNotaEntrega.INFNFE_ENTREGA_NRO;
                            notaentrega.INFNFE_ENTREGA_XCPL = queryNotaEntrega.INFNFE_ENTREGA_XCPL == null ? "" : queryNotaEntrega.INFNFE_ENTREGA_XCPL;
                            notaentrega.INFNFE_ENTREGA_XBAIRRO = queryNotaEntrega.INFNFE_ENTREGA_XBAIRRO;
                            notaentrega.INFNFE_ENTREGA_CMUN = Convert.ToInt32(queryNotaEntrega.INFNFE_ENTREGA_CMUN);
                            notaentrega.INFNFE_ENTREGA_XMUN = queryNotaEntrega.INFNFE_ENTREGA_XMUN;
                            notaentrega.INFNFE_ENTREGA_UF = queryNotaEntrega.INFNFE_ENTREGA_UF;

                            notaentrega.ID_CONTROLE = itemControle.ID;
                            notaentrega.DT_CRIACAO = DateTime.Now;
                            notaentrega.CRIADO_POR = 1;
                            notaentrega.DT_ALTERACAO = DateTime.Now;
                            notaentrega.ALTERADO_POR = 1;
                            notaentrega.STATUS = "A";
                            DAO_INT_NFE_NOTA_ENTREGA.Salvar(notaentrega);
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        erro = true;
#endif
                    }
                    #endregion
                    erl = 500;
                    #region Totais
                    try
                    {
                        var queryNotaTotaisICMSTot = (from i in XmlTexto.Descendants(NameSpace(1) + "ICMSTot")
                                                        select new
                                                        {
                                                            INFNFE_TOTAL_ICMSTOT_VBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                            INFNFE_TOTAL_ICMSTOT_VICMS = (decimal?)i.Element(NameSpace(1) + "vICMS"),
                                                            INFNFE_TOTAL_ICMSTOT_VBCST = (decimal?)i.Element(NameSpace(1) + "vBCST"),
                                                            INFNFE_TOTAL_ICMSTOT_VST = (decimal?)i.Element(NameSpace(1) + "vST"),
                                                            INFNFE_TOTAL_ICMSTOT_VPROD = (decimal?)i.Element(NameSpace(1) + "vProd"),
                                                            INFNFE_TOTAL_ICMSTOT_VFRETE = (decimal?)i.Element(NameSpace(1) + "vFrete"),
                                                            INFNFE_TOTAL_ICMSTOT_VSEG = (decimal?)i.Element(NameSpace(1) + "vSeg"),
                                                            INFNFE_TOTAL_ICMSTOT_VDESC = (decimal?)i.Element(NameSpace(1) + "vDesc"),
                                                            INFNFE_TOTAL_ICMSTOT_VII = (decimal?)i.Element(NameSpace(1) + "vII"),
                                                            INFNFE_TOTAL_ICMSTOT_VIPI = (decimal?)i.Element(NameSpace(1) + "vIPI"),
                                                            INFNFE_TOTAL_ICMSTOT_VPIS = (decimal?)i.Element(NameSpace(1) + "vPIS"),
                                                            INFNFE_TOTAL_ICMSTOT_VCOFINS = (decimal?)i.Element(NameSpace(1) + "vCOFINS"),
                                                            INFNFE_TOTAL_ICMSTOT_VOUTRO = (decimal?)i.Element(NameSpace(1) + "vOutro"),
                                                            INFNFE_TOTAL_ICMSTOT_VNF = (decimal?)i.Element(NameSpace(1) + "vNF"),
                                                            INFNFE_TOTAL_ICMSTOT_VICMSDESON = (decimal?)i.Element(NameSpace(1) + "vICMSDeson"),
                                                            INFNFE_TOTAL_ISSQNTOT_DCOMPET = (DateTime?)i.Element(NameSpace(1) + "dCompet"),
                                                            INFNFE_TOTAL_ISSQNTOT_VDEDUCAO = (decimal?)i.Element(NameSpace(1) + "vDeducao"),
                                                            INFNFE_TOTAL_ISSQNTOT_VOUTRO = (decimal?)i.Element(NameSpace(1) + "vOutro"),
                                                            INFNFE_TOTAL_ISSQNTOT_VDESCINCOND = (decimal?)i.Element(NameSpace(1) + "vDescIncond"),
                                                            INFNFE_TOTAL_ISSQNTOT_VDESCCOND = (decimal?)i.Element(NameSpace(1) + "vDescCond"),
                                                            INFNFE_TOTAL_ISSQNTOT_VISSRET = (decimal?)i.Element(NameSpace(1) + "vISSRet"),
                                                            INFNFE_TOTAL_ISSQNTOT_CREGTRIB = (string)i.Element(NameSpace(1) + "cRegTrib")
                                                        }).SingleOrDefault();
                        
                        if (queryNotaTotaisICMSTot != null)
                        {
                            try
                            {
                                notatotais = DAO_INT_NFE_NOTA_TOTAIS.RetornoPorIdControle(controle.ID);
                            }
                            catch
                            {
                                notatotais = null;
                            }

                            if (notatotais == null)
                            {
                                notatotais = new INT_NFE_NOTA_TOTAIS();
                            }

                            notatotais.INFNFE_TOTAL_ICMSTOT_VBC = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VBC);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VICMS = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VICMS);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VBCST = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VBCST);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VST = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VST);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VPROD = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VPROD);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VFRETE = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VFRETE);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VSEG = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VSEG);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VDESC = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VDESC);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VII = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VII);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VIPI = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VIPI);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VPIS = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VPIS);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VCOFINS = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VCOFINS);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VOUTRO = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VOUTRO);
                            notatotais.INFNFE_TOTAL_ICMSTOT_VNF = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VNF);

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VICMSDESON != null)
                                notatotais.INFNFE_TOTAL_ICMSTOT_VICMSDES = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ICMSTOT_VICMSDESON);
                            else
                                notatotais.INFNFE_TOTAL_ICMSTOT_VICMSDES = 0;

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_DCOMPET != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_DCOMPET = Convert.ToDateTime(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_DCOMPET);
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_DCOMPET = DateTime.MinValue;

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDEDUCAO != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDEDUC = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDEDUCAO);
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDEDUC = 0;

                            notatotais.INFNFE_TOTAL_ISSQNTOT_VOUTRO = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VOUTRO);

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDESCINCOND != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDESCIN = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDESCINCOND);
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDESCIN = 0;

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDESCCOND != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDESCC = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VDESCCOND);
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VDESCC = 0;

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VISSRET != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VISSRET = Convert.ToDecimal(queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_VISSRET);
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_VISSRET = 0;

                            if (queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_CREGTRIB != null)
                                notatotais.INFNFE_TOTAL_ISSQNTOT_CREGTRIB = queryNotaTotaisICMSTot.INFNFE_TOTAL_ISSQNTOT_CREGTRIB;
                            else
                                notatotais.INFNFE_TOTAL_ISSQNTOT_CREGTRIB = string.Empty;
                        }

                        var queryNotaTotaisISSQNtot = (from i in XmlTexto.Descendants(NameSpace(1) + "ISSQNtot")
                                                        select new
                                                        {
                                                            INFNFE_TOTAL_ISSQNTOT_VSERV = (decimal?)i.Element(NameSpace(1) + "vServ"),
                                                            INFNFE_TOTAL_ISSQNTOT_VBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                            INFNFE_TOTAL_ISSQNTOT_VISS = (decimal?)i.Element(NameSpace(1) + "vISS"),
                                                            INFNFE_TOTAL_ISSQNTOT_VPIS = (decimal?)i.Element(NameSpace(1) + "vPIS"),
                                                            INFNFE_TOTAL_ISSQNTOT_VCOFINS = (decimal?)i.Element(NameSpace(1) + "vCOFINS")
                                                        }).SingleOrDefault();

                        if (queryNotaTotaisISSQNtot != null)
                        {
                            notatotais.INFNFE_TOTAL_ISSQNTOT_VSERV = Convert.ToDecimal(queryNotaTotaisISSQNtot.INFNFE_TOTAL_ISSQNTOT_VBC);
                            notatotais.INFNFE_TOTAL_ISSQNTOT_VBC = Convert.ToDecimal(queryNotaTotaisISSQNtot.INFNFE_TOTAL_ISSQNTOT_VBC);
                            notatotais.INFNFE_TOTAL_ISSQNTOT_VISS = Convert.ToDecimal(queryNotaTotaisISSQNtot.INFNFE_TOTAL_ISSQNTOT_VISS);
                            notatotais.INFNFE_TOTAL_ISSQNTOT_VPIS = Convert.ToDecimal(queryNotaTotaisISSQNtot.INFNFE_TOTAL_ISSQNTOT_VPIS);
                            notatotais.INFNFE_TOTAL_ISSQNTOT_VCOFINS = Convert.ToDecimal(queryNotaTotaisISSQNtot.INFNFE_TOTAL_ISSQNTOT_VCOFINS);
                        }

                        var queryNotaTotaisretTrib = (from i in XmlTexto.Descendants(NameSpace(1) + "retTrib")
                                                        select new
                                                        {
                                                            INFNFE_TOTAL_RTRIB_VRETPIS = (decimal?)i.Element(NameSpace(1) + "vRetPIS"),
                                                            INFNFE_TOTAL_RTRIB_VRETCOFINS = (decimal?)i.Element(NameSpace(1) + "vRetCOFINS"),
                                                            INFNFE_TOTAL_RTRIB_VRETCSLL = (decimal?)i.Element(NameSpace(1) + "vRetCSLL"),
                                                            INFNFE_TOTAL_RTRIB_VBCIRRF = (decimal?)i.Element(NameSpace(1) + "vBCIRRF"),
                                                            INFNFE_TOTAL_RTRIB_VIRRF = (decimal?)i.Element(NameSpace(1) + "vIRRF"),
                                                            INFNFE_TOTAL_RTRIB_VBCRETPREV = (decimal?)i.Element(NameSpace(1) + "vBCRetPrev"),
                                                            INFNFE_TOTAL_RTRIB_VRETPREV = (decimal?)i.Element(NameSpace(1) + "vRetPrev")
                                                        }).SingleOrDefault();

                        if (queryNotaTotaisretTrib != null)
                        {
                            notatotais.INFNFE_TOTAL_RTRIB_VRETPIS = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VRETPIS);
                            notatotais.INFNFE_TOTAL_RTRIB_VRETCOFINS = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VRETCOFINS);
                            notatotais.INFNFE_TOTAL_RTRIB_VRETCSLL = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VRETCSLL);
                            notatotais.INFNFE_TOTAL_RTRIB_VBCIRRF = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VBCIRRF);
                            notatotais.INFNFE_TOTAL_RTRIB_VIRRF = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VIRRF);
                            notatotais.INFNFE_TOTAL_RTRIB_VBCRETPREV = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VBCRETPREV);
                            notatotais.INFNFE_TOTAL_RTRIB_VRETPREV = Convert.ToDecimal(queryNotaTotaisretTrib.INFNFE_TOTAL_RTRIB_VRETPREV);
                        }

                        notatotais.ID_CONTROLE = itemControle.ID;
                        notatotais.DT_CRIACAO = DateTime.Now;
                        notatotais.CRIADO_POR = 1;
                        notatotais.DT_ALTERACAO = DateTime.Now;
                        notatotais.ALTERADO_POR = 1;
                        notatotais.STATUS = "A";
                        DAO_INT_NFE_NOTA_TOTAIS.Salvar(notatotais);
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        erro = true;
#endif
                    }
                    #endregion

                    var queryLinha = (from i in XmlTexto.Descendants(NameSpace(1) + "det")
                                        select new
                                        {
                                            H01_nItem_H02 = (decimal)i.Attribute("nItem"),
                                            H01_prod_I01 = (XElement)i.Element(NameSpace(1) + "prod"),
                                            H01_imposto_M01 = (XElement)i.Element(NameSpace(1) + "imposto"),
                                            H01_infAdProd_V01 = (string)i.Element(NameSpace(1) + "infAdProd")
                                        }).OrderBy(z => z.H01_nItem_H02).ToList();

                    foreach (var H01 in queryLinha)
                    {
                        try
                        {
                            decimal nrItem = H01.H01_nItem_H02;

                            try
                            {
                                linha = DAO_INT_NFE_LINHA.RetornoPorIdControleItem(controle.ID, Convert.ToInt32(nrItem));
                            }
                            catch
                            {
                                linha = null;
                            }

                            if (linha == null)
                            {
                                linha = new INT_NFE_LINHA();
                            }
                            erl = 600;
                            #region det
                            try
                            {
                                linha.INFNFE_DET_NITEM = Convert.ToInt16(H01.H01_nItem_H02);
                                linha.INFNFE_DET_PROD_CPROD = (string)H01.H01_prod_I01.Element(NameSpace(1) + "cProd");
                                linha.INFNFE_DET_PROD_CEAN = (string)H01.H01_prod_I01.Element(NameSpace(1) + "cEAN") == null ? " " : (string)H01.H01_prod_I01.Element(NameSpace(1) + "cEAN");
                                linha.INFNFE_DET_PROD_XPROD = (string)H01.H01_prod_I01.Element(NameSpace(1) + "xProd");
                                linha.INFNFE_DET_PROD_NCM = (string)H01.H01_prod_I01.Element(NameSpace(1) + "NCM");
                                linha.INFNFE_DET_PROD_EXTIPI = (string)H01.H01_prod_I01.Element(NameSpace(1) + "EXTIPI") == null ? "" : (string)H01.H01_prod_I01.Element(NameSpace(1) + "EXTIPI");
                                linha.INFNFE_DET_PROD_CFOP = Convert.ToInt16((int?)H01.H01_prod_I01.Element(NameSpace(1) + "CFOP"));
                                linha.INFNFE_DET_PROD_UCOM = (string)H01.H01_prod_I01.Element(NameSpace(1) + "uCom");
                                linha.INFNFE_DET_PROD_QCOM = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "qCom"));
                                linha.INFNFE_DET_PROD_VUNCOM = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vUnCom"));
                                linha.INFNFE_DET_PROD_VPROD = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vProd"));
                                linha.INFNFE_DET_PROD_CEANTRIB = (string)H01.H01_prod_I01.Element(NameSpace(1) + "cEANTrib") == null ? " " : (string)H01.H01_prod_I01.Element(NameSpace(1) + "cEANTrib");
                                linha.INFNFE_DET_PROD_UTRIB = (string)H01.H01_prod_I01.Element(NameSpace(1) + "uTrib");
                                linha.INFNFE_DET_PROD_QTRIB = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "qTrib"));
                                linha.INFNFE_DET_PROD_VUNTRIB = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vUnTrib"));
                                linha.INFNFE_DET_PROD_VFRETE = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vFrete"));
                                linha.INFNFE_DET_PROD_VSEG = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vSeg"));
                                linha.INFNFE_DET_PROD_VDESC = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vDesc"));
                                linha.INFNFE_DET_PROD_VOUTRO = Convert.ToDecimal((decimal?)H01.H01_prod_I01.Element(NameSpace(1) + "vOutro"));
                                linha.INFNFE_DET_PROD_INDTOT = Convert.ToInt16((String)H01.H01_prod_I01.Element(NameSpace(1) + "indTot"));
                                linha.INFNFE_DET_PROD_XPED = (string)H01.H01_prod_I01.Element(NameSpace(1) + "xPed") == null ? "" : (string)H01.H01_prod_I01.Element(NameSpace(1) + "xPed");
                                linha.INFNFE_DET_PROD_NITEMPED = Convert.ToInt32((int?)H01.H01_prod_I01.Element(NameSpace(1) + "nItemPed"));

                                if (H01.H01_infAdProd_V01 == null)
                                {
                                    linha.INFNFE_DET_PROD_INFADPROD = "";
                                }
                                else
                                {
                                    linha.INFNFE_DET_PROD_INFADPROD = H01.H01_infAdProd_V01;
                                }

                                linha.ID_CONTROLE = itemControle.ID;
                                linha.DT_CRIACAO = DateTime.Now;
                                linha.CRIADO_POR = 1;
                                linha.DT_ALTERACAO = DateTime.Now;
                                linha.ALTERADO_POR = 1;
                                linha.STATUS = "A";

                                DAO_INT_NFE_LINHA.Salvar(linha);
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 700;
                            #region ICMS
                            try
                            {
                                linhaimpicms = new INT_NFE_LINHA_IMP_ICMS();

                                if (H01.H01_imposto_M01.Element(NameSpace(1) + "ICMS") != null)
                                {

                                    var queryM01N01 = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "ICMS")
                                                        select new
                                                        {
                                                            ICMS00 = (XElement)i.Element(NameSpace(1) + "ICMS00"),
                                                            ICMS10 = (XElement)i.Element(NameSpace(1) + "ICMS10"),
                                                            ICMS20 = (XElement)i.Element(NameSpace(1) + "ICMS20"),
                                                            ICMS30 = (XElement)i.Element(NameSpace(1) + "ICMS30"),
                                                            ICMS40 = (XElement)i.Element(NameSpace(1) + "ICMS40"),
                                                            ICMS51 = (XElement)i.Element(NameSpace(1) + "ICMS51"),
                                                            ICMS60 = (XElement)i.Element(NameSpace(1) + "ICMS60"),
                                                            ICMS70 = (XElement)i.Element(NameSpace(1) + "ICMS70"),
                                                            ICMS90 = (XElement)i.Element(NameSpace(1) + "ICMS90"),
                                                            ICMSPart = (XElement)i.Element(NameSpace(1) + "ICMSPart"),
                                                            ICMSST = (XElement)i.Element(NameSpace(1) + "ICMSST"),
                                                            ICMSSN101 = (XElement)i.Element(NameSpace(1) + "ICMSSN101"),
                                                            ICMSSN102 = (XElement)i.Element(NameSpace(1) + "ICMSSN102"),
                                                            ICMSSN201 = (XElement)i.Element(NameSpace(1) + "ICMSSN201"),
                                                            ICMSSN202 = (XElement)i.Element(NameSpace(1) + "ICMSSN202"),
                                                            ICMSSN500 = (XElement)i.Element(NameSpace(1) + "ICMSSN500"),
                                                            ICMSSN900 = (XElement)i.Element(NameSpace(1) + "ICMSSN900")
                                                        }).SingleOrDefault();

                                    if (queryM01N01.ICMS00 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS00";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS00.Element(NameSpace(1) + "vICMS"));
                                    }
                                    else if (queryM01N01.ICMS10 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS10";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS10.Element(NameSpace(1) + "vICMSST"));
                                    }
                                    else if (queryM01N01.ICMS20 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS20";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS20.Element(NameSpace(1) + "vICMS"));
                                    }
                                    else if (queryM01N01.ICMS30 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS30";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS30.Element(NameSpace(1) + "vICMSST"));
                                    }
                                    else if (queryM01N01.ICMS40 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS40";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS40.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS40.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS40.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MOTDESICMS = Convert.ToInt16((decimal?)queryM01N01.ICMS40.Element(NameSpace(1) + "motDesICMS"));
                                        linhaimpicms.TAG_VICMSDESON = Convert.ToDecimal((decimal?)queryM01N01.ICMS40.Element(NameSpace(1) + "vICMSDeson"));
                                    }
                                    else if (queryM01N01.ICMS51 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS51";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_VICMSOP = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "vICMSOp"));
                                        linhaimpicms.TAG_PDIF = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "pDif"));
                                        linhaimpicms.TAG_VICMSDIF = Convert.ToDecimal((decimal?)queryM01N01.ICMS51.Element(NameSpace(1) + "vICMSDif"));
                                    }
                                    else if (queryM01N01.ICMS60 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS60";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS60.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS60.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_VBCSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMS60.Element(NameSpace(1) + "vBCSTRet"));
                                        linhaimpicms.TAG_VICMSSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMS60.Element(NameSpace(1) + "vICMSSTRet"));
                                    }
                                    else if (queryM01N01.ICMS70 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS70";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS70.Element(NameSpace(1) + "vICMSST"));
                                    }
                                    else if (queryM01N01.ICMS90 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMS90";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMS90.Element(NameSpace(1) + "vICMSST"));
                                    }
                                    else if (queryM01N01.ICMSPart != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSPart";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "vICMSST"));
                                        linhaimpicms.TAG_PBCOP = Convert.ToDecimal((decimal?)queryM01N01.ICMSPart.Element(NameSpace(1) + "pBCOp"));
                                        linhaimpicms.TAG_UFST = (string)queryM01N01.ICMSPart.Element(NameSpace(1) + "UFST");
                                    }
                                    else if (queryM01N01.ICMSST != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSST";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CST = Convert.ToInt16((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "CST"));
                                        linhaimpicms.TAG_VBCSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "vBCSTRet"));
                                        linhaimpicms.TAG_VICMSSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "vICMSSTRet"));
                                        linhaimpicms.TAG_VBCSTDEST = Convert.ToDecimal((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "vBCSTDest"));
                                        linhaimpicms.TAG_VICMSSTDEST = Convert.ToDecimal((decimal?)queryM01N01.ICMSST.Element(NameSpace(1) + "vICMSSTDest"));
                                    }
                                    else if (queryM01N01.ICMSSN101 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN101";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN101.Element(NameSpace(1) + "Orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN101.Element(NameSpace(1) + "CSOSN"));
                                        linhaimpicms.TAG_PCREDSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN101.Element(NameSpace(1) + "pCredSN"));
                                        linhaimpicms.TAG_VCREDICMSSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN101.Element(NameSpace(1) + "vCredICMSSN"));
                                    }
                                    else if (queryM01N01.ICMSSN102 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN102";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN102.Element(NameSpace(1) + "orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN102.Element(NameSpace(1) + "CSOSN"));
                                    }
                                    else if (queryM01N01.ICMSSN201 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN201";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "Orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "CSOSN"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "vICMSST"));
                                        linhaimpicms.TAG_PCREDSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "pCredSN"));
                                        linhaimpicms.TAG_VCREDICMSSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN201.Element(NameSpace(1) + "vCredICMSSN"));
                                    }
                                    else if (queryM01N01.ICMSSN202 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN202";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "Orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "CSOSN"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN202.Element(NameSpace(1) + "vICMSST"));
                                    }
                                    else if (queryM01N01.ICMSSN500 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN500";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN500.Element(NameSpace(1) + "Orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN500.Element(NameSpace(1) + "CSOSN"));
                                        linhaimpicms.TAG_VBCSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN500.Element(NameSpace(1) + "vBCSTRet"));
                                        linhaimpicms.TAG_VICMSSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN500.Element(NameSpace(1) + "vICMSSTRet"));
                                    }
                                    else if (queryM01N01.ICMSSN900 != null)
                                    {
                                        linhaimpicms.TAG_GRUPO = "ICMSSN900";
                                        linhaimpicms.TAG_ORIG = Convert.ToInt16((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "Orig"));
                                        linhaimpicms.TAG_CSOSN = Convert.ToInt16((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "CSOSN"));
                                        linhaimpicms.TAG_MODBC = Convert.ToInt16((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "modBC"));
                                        linhaimpicms.TAG_VBC = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vBC"));
                                        linhaimpicms.TAG_PREDBC = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pRedBC"));
                                        linhaimpicms.TAG_PICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pICMS"));
                                        linhaimpicms.TAG_VICMS = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vICMS"));
                                        linhaimpicms.TAG_MODBCST = Convert.ToInt16((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "modBCST"));
                                        linhaimpicms.TAG_PMVAST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pMVAST"));
                                        linhaimpicms.TAG_PREDBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pRedBCST"));
                                        linhaimpicms.TAG_VBCST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vBCST"));
                                        linhaimpicms.TAG_PICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pICMSST"));
                                        linhaimpicms.TAG_VICMSST = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vICMSST"));
                                        linhaimpicms.TAG_VBCSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vBCSTRet"));
                                        linhaimpicms.TAG_VICMSSTRET = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vICMSSTRet"));
                                        linhaimpicms.TAG_PCREDSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "pCredSN"));
                                        linhaimpicms.TAG_VCREDICMSSN = Convert.ToDecimal((decimal?)queryM01N01.ICMSSN900.Element(NameSpace(1) + "vCredICMSSN"));
                                    }

                                    linhaimpicms.ID = 0;
                                    linhaimpicms.ID_CONTROLE = itemControle.ID;
                                    linhaimpicms.ID_LINHA = linha.ID;
                                    linhaimpicms.DT_CRIACAO = DateTime.Now;
                                    linhaimpicms.DT_ALTERACAO = DateTime.Now;
                                    linhaimpicms.ALTERADO_POR = 1;
                                    linhaimpicms.CRIADO_POR = 1;
                                    linhaimpicms.STATUS = "A";

                                    DAO_INT_NFE_LINHA_IMP_ICMS.Salvar(linhaimpicms);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 800;
                            #region IPI
                            try
                            {
                                linhaimpipi = new INT_NFE_LINHA_IMP_IPI();

                                var queryM01O01 = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "IPI")
                                                    select new
                                                    {
                                                        clEnq = (string)i.Element(NameSpace(1) + "clEnq"),
                                                        CNPJProd = (string)i.Element(NameSpace(1) + "CNPJProd"),
                                                        cSelo = (string)i.Element(NameSpace(1) + "cSelo"),
                                                        qSelo = (decimal?)i.Element(NameSpace(1) + "qSelo"),
                                                        cEnq = (string)i.Element(NameSpace(1) + "cEnq"),
                                                        IPITrib = (XElement)i.Element(NameSpace(1) + "IPITrib"),
                                                        IPINT = (XElement)i.Element(NameSpace(1) + "IPINT")
                                                    }).SingleOrDefault();

                                if (queryM01O01 != null)
                                {
                                    linhaimpipi.ID_CONTROLE = itemControle.ID;
                                    linhaimpipi.ID_LINHA = linha.ID;
                                    linhaimpipi.TAG_CENQ = queryM01O01.cEnq;
                                    linhaimpipi.TAG_CNPJPROD = queryM01O01.CNPJProd;
                                    linhaimpipi.TAG_CSELO = queryM01O01.cSelo;
                                    linhaimpipi.TAG_QSELO = Convert.ToInt32(queryM01O01.qSelo);
                                    linhaimpipi.TAG_CLENQ = queryM01O01.clEnq;
                                    
                                    if (queryM01O01.IPITrib != null)
                                    {
                                        linhaimpipi.TAG_IPITRIB_CST = (string)queryM01O01.IPITrib.Element(NameSpace(1) + "CST");
                                        linhaimpipi.TAG_IPITRIB_VBC = Convert.ToDecimal((decimal?)queryM01O01.IPITrib.Element(NameSpace(1) + "vBC"));
                                        linhaimpipi.TAG_IPITRIB_QUNID = Convert.ToDecimal((decimal?)queryM01O01.IPITrib.Element(NameSpace(1) + "qUnid"));
                                        linhaimpipi.TAG_IPITRIB_VUNID = Convert.ToDecimal((decimal?)queryM01O01.IPITrib.Element(NameSpace(1) + "vUnid"));
                                        linhaimpipi.TAG_IPITRIB_PIPI = Convert.ToDecimal((decimal?)queryM01O01.IPITrib.Element(NameSpace(1) + "pIPI"));
                                        linhaimpipi.TAG_IPITRIB_VIPI = Convert.ToDecimal((decimal?)queryM01O01.IPITrib.Element(NameSpace(1) + "vIPI"));
                                    }
                                    
                                    if (queryM01O01.IPINT != null)
                                    {
                                        linhaimpipi.TAG_IPINT_CST = (string)queryM01O01.IPINT.Element(NameSpace(1) + "CST");
                                    }

                                    linhaimpipi.ID = 0;
                                    linhaimpipi.DT_CRIACAO = DateTime.Now;
                                    linhaimpipi.DT_ALTERACAO = DateTime.Now;
                                    linhaimpipi.ALTERADO_POR = 1;
                                    linhaimpipi.CRIADO_POR = 1;
                                    linhaimpipi.STATUS = "A";

                                    DAO_INT_NFE_LINHA_IMP_IPI.Salvar(linhaimpipi);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 900;
                            #region II
                            try
                            {
                                linhaimpii = new INT_NFE_LINHA_IMP_II();

                                var queryII = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "II")
                                                select new
                                                {
                                                    vBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                    vDespAdu = (decimal?)i.Element(NameSpace(1) + "vDespAdu"),
                                                    vII = (decimal?)i.Element(NameSpace(1) + "vII"),
                                                    vIOF = (decimal?)i.Element(NameSpace(1) + "vIOF")
                                                }).SingleOrDefault();

                                if (queryII != null)
                                {
                                    linhaimpii.TAG_VBC = Convert.ToDecimal(queryII.vBC);
                                    linhaimpii.TAG_VDESPADU = Convert.ToDecimal(queryII.vDespAdu);
                                    linhaimpii.TAG_VII = Convert.ToDecimal(queryII.vII);
                                    linhaimpii.TAG_VIOF = Convert.ToDecimal(queryII.vIOF);

                                    linhaimpii.ID = 0;
                                    linhaimpii.CRIADO_POR = 1;
                                    linhaimpii.DT_CRIACAO = DateTime.Now;
                                    linhaimpii.ALTERADO_POR = 1;
                                    linhaimpii.STATUS = "A";
                                    linhaimpii.DT_ALTERACAO = DateTime.Now;
                                    linhaimpii.ID_CONTROLE = itemControle.ID;
                                    linhaimpii.ID_LINHA = linha.ID;

                                    DAO_INT_NFE_LINHA_IMP_II.Salvar(linhaimpii);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1000;
                            #region PIS
                            try
                            {
                                linhaimppis = new INT_NFE_LINHA_IMP_PIS();

                                var querypis = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "PIS")
                                                select new
                                                {
                                                    PISAliq = (XElement)i.Element(NameSpace(1) + "PISAliq"),
                                                    PISQtde = (XElement)i.Element(NameSpace(1) + "PISQtde"),
                                                    PISNT = (XElement)i.Element(NameSpace(1) + "PISNT"),
                                                    PISOutr = (XElement)i.Element(NameSpace(1) + "PISOutr")
                                                }).SingleOrDefault();

                                if (querypis != null)
                                {
                                    if (querypis.PISAliq != null)
                                    {
                                        linhaimppis.TAG_PISALIQ_CST = Convert.ToInt16(((String)querypis.PISAliq.Element(NameSpace(1) + "CST")).Replace("..", "."));
                                        linhaimppis.TAG_PISALIQ_VBC = Convert.ToDecimal(((String)querypis.PISAliq.Element(NameSpace(1) + "vBC")).Replace(".", ","));
                                        linhaimppis.TAG_PISALIQ_PPIS = Convert.ToDecimal(((String)querypis.PISAliq.Element(NameSpace(1) + "pPIS")).Replace(".", ","));
                                        linhaimppis.TAG_PISALIQ_VPIS = Convert.ToDecimal(((String)querypis.PISAliq.Element(NameSpace(1) + "vPIS")).Replace(".", ","));
                                    }

                                    if (querypis.PISQtde != null)
                                    {
                                        linhaimppis.TAG_PISQTDE_CST = Convert.ToInt16((decimal?)querypis.PISQtde.Element(NameSpace(1) + "CST"));
                                        linhaimppis.TAG_PISQTDE_QBCPROD = Convert.ToDecimal((decimal?)querypis.PISQtde.Element(NameSpace(1) + "qBCProd"));
                                        linhaimppis.TAG_PISQTDE_VALIQPROD = Convert.ToDecimal((decimal?)querypis.PISQtde.Element(NameSpace(1) + "vAliqProd"));
                                        linhaimppis.TAG_PISQTDE_VPIS = Convert.ToDecimal((decimal?)querypis.PISQtde.Element(NameSpace(1) + "vPIS"));
                                    }

                                    if (querypis.PISNT != null)
                                    {
                                        linhaimppis.TAG_PISNT_CST = Convert.ToInt16((decimal?)querypis.PISNT.Element(NameSpace(1) + "CST"));
                                    }

                                    if (querypis.PISOutr != null)
                                    {
                                        linhaimppis.TAG_PISOUTR_CST = Convert.ToInt16((decimal?)querypis.PISOutr.Element(NameSpace(1) + "CST"));
                                        linhaimppis.TAG_PISOUTR_VBC = Convert.ToDecimal((decimal?)querypis.PISOutr.Element(NameSpace(1) + "vBC"));
                                        linhaimppis.TAG_PISOUTR_PPIS = Convert.ToDecimal((decimal?)querypis.PISOutr.Element(NameSpace(1) + "pPIS"));
                                        linhaimppis.TAG_PISOUTR_QBCPROD = Convert.ToDecimal((decimal?)querypis.PISOutr.Element(NameSpace(1) + "qBCProd"));
                                        linhaimppis.TAG_PISOUTR_VALIQPROD = Convert.ToDecimal((decimal?)querypis.PISOutr.Element(NameSpace(1) + "vAliqProd"));
                                        linhaimppis.TAG_PISOUTR_VPIS = Convert.ToDecimal((decimal?)querypis.PISOutr.Element(NameSpace(1) + "vPIS"));
                                    }

                                    var queryPISST = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "PISST")
                                                        select new
                                                        {
                                                            vBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                            pPIS = (decimal?)i.Element(NameSpace(1) + "pPIS"),
                                                            qBCProd = (decimal?)i.Element(NameSpace(1) + "qBCProd"),
                                                            vAliqProd = (decimal?)i.Element(NameSpace(1) + "vAliqProd"),
                                                            vPIS = (decimal?)i.Element(NameSpace(1) + "vPIS")
                                                        }).SingleOrDefault();

                                    if (queryPISST != null)
                                    {
                                        linhaimppis.TAG_PISST_VBC = Convert.ToDecimal(queryPISST.vBC);
                                        linhaimppis.TAG_PISST_PPIS = Convert.ToDecimal(queryPISST.pPIS);
                                        linhaimppis.TAG_PISST_QBCPROD = Convert.ToDecimal(queryPISST.qBCProd);
                                        linhaimppis.TAG_PISST_VALIQPROD = Convert.ToDecimal(queryPISST.vAliqProd);
                                        linhaimppis.TAG_PISST_VPIS = Convert.ToDecimal(queryPISST.vPIS);
                                    }

                                    linhaimppis.ID = 0;
                                    linhaimppis.ID_LINHA = linha.ID;
                                    linhaimppis.ID_CONTROLE = itemControle.ID;
                                    linhaimppis.DT_ALTERACAO = DateTime.Now;
                                    linhaimppis.DT_CRIACAO = DateTime.Now;
                                    linhaimppis.ALTERADO_POR = 1;
                                    linhaimppis.CRIADO_POR = 1;
                                    linhaimppis.STATUS = "A";

                                    DAO_INT_NFE_LINHA_IMP_PIS.Salvar(linhaimppis);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1100;
                            #region COFINS
                            try
                            {
                                linhaimpcofins = new INT_NFE_LINHA_IMP_COFINS();

                                var querycofins = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "COFINS")
                                                    select new
                                                    {
                                                        COFINSAliq = (XElement)i.Element(NameSpace(1) + "COFINSAliq"),
                                                        COFINSQtde = (XElement)i.Element(NameSpace(1) + "COFINSQtde"),
                                                        COFINSNT = (XElement)i.Element(NameSpace(1) + "COFINSNT"),
                                                        COFINSOutr = (XElement)i.Element(NameSpace(1) + "COFINSOutr")
                                                    }).SingleOrDefault();

                                var queryCOFINSST = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "COFINSST")
                                                        select new
                                                        {
                                                            vBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                            pCOFINS = (decimal?)i.Element(NameSpace(1) + "pPIS"),
                                                            qBCProd = (decimal?)i.Element(NameSpace(1) + "qBCProd"),
                                                            vAliqProd = (decimal?)i.Element(NameSpace(1) + "vAliqProd"),
                                                            vCOFINS = (decimal?)i.Element(NameSpace(1) + "vPIS")
                                                        }).SingleOrDefault();

                                if (querycofins.COFINSAliq != null)
                                {
                                    linhaimpcofins.TAG_COFINSALIQ_CST = Convert.ToInt16((decimal?)querycofins.COFINSAliq.Element(NameSpace(1) + "CST"));
                                    linhaimpcofins.TAG_COFINSALIQ_VBC = Convert.ToDecimal((decimal?)querycofins.COFINSAliq.Element(NameSpace(1) + "vBC"));
                                    linhaimpcofins.TAG_COFINSALIQ_PCOFINS = Convert.ToDecimal((decimal?)querycofins.COFINSAliq.Element(NameSpace(1) + "pCOFINS"));
                                    linhaimpcofins.TAG_COFINSALIQ_VCOFINS = Convert.ToDecimal((decimal?)querycofins.COFINSAliq.Element(NameSpace(1) + "vCOFINS"));
                                }

                                if (querycofins.COFINSQtde != null)
                                {
                                    linhaimpcofins.TAG_COFINSQTDE_CST = Convert.ToInt16((decimal?)querycofins.COFINSQtde.Element(NameSpace(1) + "CST"));
                                    linhaimpcofins.TAG_COFINSQTDE_QBCPROD = Convert.ToDecimal((decimal?)querycofins.COFINSQtde.Element(NameSpace(1) + "qBCProd"));
                                    linhaimpcofins.TAG_COFINSQTDE_VALIQPRO = Convert.ToDecimal((decimal?)querycofins.COFINSQtde.Element(NameSpace(1) + "vAliqProd"));
                                    linhaimpcofins.TAG_COFINSQTDE_VCOFINS = Convert.ToDecimal((decimal?)querycofins.COFINSQtde.Element(NameSpace(1) + "vCOFINS"));
                                }

                                if (querycofins.COFINSNT != null)
                                {
                                    linhaimpcofins.TAG_COFINSNT_CST = Convert.ToInt16((decimal?)querycofins.COFINSNT.Element(NameSpace(1) + "CST"));
                                }

                                if (querycofins.COFINSOutr != null)
                                {
                                    linhaimpcofins.TAG_COFINSOUTR_CST = Convert.ToInt16((decimal?)querycofins.COFINSOutr.Element(NameSpace(1) + "CST"));
                                    linhaimpcofins.TAG_COFINSOUTR_VBC = Convert.ToDecimal((decimal?)querycofins.COFINSOutr.Element(NameSpace(1) + "vBC"));
                                    linhaimpcofins.TAG_COFINSOUTR_QBCPROD = Convert.ToDecimal((decimal?)querycofins.COFINSOutr.Element(NameSpace(1) + "qBCProd"));
                                    linhaimpcofins.TAG_COFINSOUTR_VALIQPROD = Convert.ToDecimal((decimal?)querycofins.COFINSOutr.Element(NameSpace(1) + "vAliqProd"));
                                    linhaimpcofins.TAG_COFINSOUTR_VCOFINS = Convert.ToDecimal((decimal?)querycofins.COFINSOutr.Element(NameSpace(1) + "vCOFINS"));
                                }

                                if (queryCOFINSST != null)
                                {
                                    linhaimpcofins.TAG_COFINSST_VBC = Convert.ToDecimal(queryCOFINSST.vBC);
                                    linhaimpcofins.TAG_COFINSST_PCOFINS = Convert.ToDecimal(queryCOFINSST.pCOFINS);
                                    linhaimpcofins.TAG_COFINSST_QBCPROD = Convert.ToDecimal(queryCOFINSST.qBCProd);
                                    linhaimpcofins.TAG_COFINSST_VALIQPROD = Convert.ToDecimal(queryCOFINSST.vAliqProd);
                                    linhaimpcofins.TAG_COFINSST_VCOFINS = Convert.ToDecimal(queryCOFINSST.vCOFINS);
                                }

                                if (querycofins.COFINSAliq != null || querycofins.COFINSQtde != null || querycofins.COFINSNT != null || querycofins.COFINSOutr != null || queryCOFINSST != null)
                                {
                                    linhaimpcofins.ID = 0;
                                    linhaimpcofins.ID_LINHA = linha.ID;
                                    linhaimpcofins.ID_CONTROLE = itemControle.ID;
                                    linhaimpcofins.DT_ALTERACAO = DateTime.Now;
                                    linhaimpcofins.DT_CRIACAO = DateTime.Now;
                                    linhaimpcofins.CRIADO_POR = 1;
                                    linhaimpcofins.ALTERADO_POR = 1;
                                    linhaimpcofins.STATUS = "A";

                                    DAO_INT_NFE_LINHA_IMP_COFINS.Salvar(linhaimpcofins);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1200;
                            #region ISSQN
                            try
                            {
                                linhaimpissqn = new INT_NFE_LINHA_IMP_ISSQN();

                                var queryIssQn = (from i in H01.H01_imposto_M01.Descendants(NameSpace(1) + "ISSQN")
                                                    select new
                                                    {
                                                        vBC = (decimal?)i.Element(NameSpace(1) + "vBC"),
                                                        vAliq = (decimal?)i.Element(NameSpace(1) + "vAliq"),
                                                        vISSQN = (decimal?)i.Element(NameSpace(1) + "vISSQN"),
                                                        cMunFG = (decimal?)i.Element(NameSpace(1) + "cMunFG"),
                                                        cListServ = (decimal?)i.Element(NameSpace(1) + "cListServ"),
                                                        cSitTrib = (string)i.Element(NameSpace(1) + "cSitTrib"),
                                                        vDeducao = (decimal?)i.Element(NameSpace(1) + "vDeducao"),
                                                        vOutro = (decimal?)i.Element(NameSpace(1) + "vOutro"),
                                                        vDescIncond = (decimal?)i.Element(NameSpace(1) + "vDescIncond"),
                                                        vDescCond = (decimal?)i.Element(NameSpace(1) + "vDescCond"),
                                                        vISSRet = (decimal?)i.Element(NameSpace(1) + "vISSRet"),
                                                        indISS = (string)i.Element(NameSpace(1) + "indISS"),
                                                        cServico = (string)i.Element(NameSpace(1) + "cServico"),
                                                        cMun = (string)i.Element(NameSpace(1) + "cMun"),
                                                        cPais = (string)i.Element(NameSpace(1) + "cPais"),
                                                        nProcesso = (string)i.Element(NameSpace(1) + "nProcesso"),
                                                        indIncentivo = (string)i.Element(NameSpace(1) + "indIncentivo")
                                                    }).SingleOrDefault();
                                
                                if (queryIssQn != null)
                                {
                                    linhaimpissqn.ID = 0;
                                    linhaimpissqn.ID_LINHA = linha.ID;
                                    linhaimpissqn.ID_CONTROLE = itemControle.ID;
                                    linhaimpissqn.DT_ALTERACAO = DateTime.Now;
                                    linhaimpissqn.DT_CRIACAO = DateTime.Now;
                                    linhaimpissqn.CRIADO_POR = 1;
                                    linhaimpissqn.ALTERADO_POR = 1;
                                    linhaimpissqn.STATUS = "A";

                                    linhaimpissqn.TAG_ISSQN_VBC = Convert.ToDecimal(queryIssQn.vBC);
                                    linhaimpissqn.TAG_ISSQN_VALIQ = Convert.ToDecimal(queryIssQn.vAliq);
                                    linhaimpissqn.TAG_ISSQN_VISSQN = Convert.ToDecimal(queryIssQn.cMunFG);
                                    linhaimpissqn.TAG_ISSQN_CLISTSERV = Convert.ToInt16(queryIssQn.cListServ);
                                    linhaimpissqn.TAG_ISSQN_CSITTRIB = queryIssQn.cSitTrib;
                                    linhaimpissqn.TAG_VDEDUCAO = Convert.ToDecimal(queryIssQn.vDeducao);
                                    linhaimpissqn.TAG_VOUTRO = Convert.ToDecimal(queryIssQn.vOutro);
                                    linhaimpissqn.TAG_VDESCINCOND = Convert.ToDecimal(queryIssQn.vDescIncond);
                                    linhaimpissqn.TAG_VDESCCOND = Convert.ToDecimal(queryIssQn.vDescCond);
                                    linhaimpissqn.TAG_VISSRET = Convert.ToDecimal(queryIssQn.vISSRet);
                                    linhaimpissqn.TAG_INDISS = queryIssQn.indISS;
                                    linhaimpissqn.TAG_CSERVICO = queryIssQn.cServico;
                                    linhaimpissqn.TAG_CMUN = queryIssQn.cMun;
                                    linhaimpissqn.TAG_CPAIS = queryIssQn.cPais;
                                    linhaimpissqn.TAG_NPROCESSO = queryIssQn.nProcesso;
                                    linhaimpissqn.TAG_INDINCENTIVO = queryIssQn.indIncentivo;
                                    DAO_INT_NFE_LINHA_IMP_ISSQN.Salvar(linhaimpissqn);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1300;
                            #region DI
                            try
                            {
                                var querydi = (from i in H01.H01_prod_I01.Descendants(NameSpace(1) + "DI")
                                                select new
                                                {
                                                    nDI = (String)i.Element(NameSpace(1) + "nDI"),
                                                    dDI = (String)i.Element(NameSpace(1) + "dDI"),
                                                    xLocDesemb = (string)i.Element(NameSpace(1) + "xLocDesemb"),
                                                    UFDesemb = (string)i.Element(NameSpace(1) + "UFDesemb"),
                                                    dDesemb = (String)i.Element(NameSpace(1) + "dDesemb"),
                                                    cExportador = (string)i.Element(NameSpace(1) + "cExportador"),
                                                    adi = (XElement)i.Element(NameSpace(1) + "adi"),
                                                    tpViaTransp = (string)i.Element(NameSpace(1) + "tpViaTransp"),
                                                    vAFRMM = (decimal?)i.Element(NameSpace(1) + "vAFRMM"),
                                                    tpIntermedio = (string)i.Element(NameSpace(1) + "tpIntermedio"),
                                                    CNPJ = (string)i.Element(NameSpace(1) + "CNPJ"),
                                                    UFTerceiro = (string)i.Element(NameSpace(1) + "UFTerceiro")
                                                }).ToList();

                                if (querydi != null)
                                {
                                    foreach (var itemdi in querydi)
                                    {
                                        linhadi = new INT_NFE_LINHA_DI();

                                        linhadi = DAO_INT_NFE_LINHA_DI.RetornaLinhaDiPorControleNrItemNrItemDI(itemControle, linha, itemdi.nDI);

                                        if (linhadi == null)
                                        {
                                            linhadi = new INT_NFE_LINHA_DI();
                                        }

                                        linhadi.ID_LINHA = linha.ID;
                                        linhadi.ID_CONTROLE = itemControle.ID;
                                        linhadi.DT_CRIACAO = DateTime.Now;
                                        linhadi.CRIADO_POR = 1;
                                        linhadi.DT_ALTERACAO = DateTime.Now;
                                        linhadi.ALTERADO_POR = 1;
                                        linhadi.STATUS = "A";
                                        linhadi.INFNFE_DET_PROD_DI_NDI = itemdi.nDI == null ? "" : itemdi.nDI;
                                        linhadi.INFNFE_DET_PROD_DI_DDI = Convert.ToDateTime(String.Concat(itemdi.dDI.Substring(8, 2), "/", itemdi.dDI.Substring(5, 2), "/", itemdi.dDI.Substring(0, 4)));
                                        linhadi.INFNFE_DET_PROD_DI_XLOCDESEMB = itemdi.xLocDesemb == null ? "" : itemdi.xLocDesemb;
                                        linhadi.INFNFE_DET_PROD_DI_UFDESEMB = itemdi.UFDesemb == null ? "" : itemdi.UFDesemb;
                                        linhadi.INFNFE_DET_PROD_DI_DDESEMB = Convert.ToDateTime(String.Concat(itemdi.dDesemb.Substring(8, 2), "/", itemdi.dDesemb.Substring(5, 2), "/", itemdi.dDesemb.Substring(0, 4)));
                                        linhadi.INFNFE_DET_PROD_DI_CEXPORTADOR = itemdi.cExportador == null ? "" : itemdi.cExportador;
                                        linhadi.INT_NFE_DET_PROD_DI_TPVIATRANS = itemdi.tpViaTransp == null ? "" : itemdi.tpViaTransp;
                                        if (itemdi.vAFRMM != null)
                                            linhadi.INT_NFE_DET_PROD_DI_VAFRMM = Convert.ToDecimal(itemdi.vAFRMM);
                                        else
                                            linhadi.INT_NFE_DET_PROD_DI_VAFRMM = 0;

                                        linhadi.INFNFE_DET_PROD_DI_TPINTERMED = itemdi.tpIntermedio == null ? "" : itemdi.tpIntermedio;
                                        linhadi.INT_NFE_DET_PROD_DI_CNPJ = itemdi.CNPJ == null ? "" : itemdi.CNPJ;
                                        linhadi.INT_NFE_DET_PROD_DI_UFTERCEIRO = itemdi.UFTerceiro == null ? "" : itemdi.UFTerceiro;

                                        DAO_INT_NFE_LINHA_DI.Salvar(linhadi);

                                        var queryadi = (from i in itemdi.adi.Descendants(NameSpace(1) + "adi")
                                                        select new
                                                        {
                                                            nAdicao = (decimal)i.Element(NameSpace(1) + "nAdicao"),
                                                            nSeqAdic = (decimal?)i.Element(NameSpace(1) + "nSeqAdic"),
                                                            cFabricante = (string)i.Element(NameSpace(1) + "cFabricante"),
                                                            vDescDI = (decimal?)i.Element(NameSpace(1) + "vDescDI"),
                                                            xPed = (string)i.Element(NameSpace(1) + "xPed"),
                                                            nItemPed = (decimal?)i.Element(NameSpace(1) + "nItemPed")
                                                        }).ToList();

                                        if (queryadi != null)
                                        {
                                            foreach (var itemadi in queryadi)
                                            {
                                                linhadiadi = new INT_NFE_LINHA_DI_ADI();

                                                linhadiadi = DAO_INT_NFE_LINHA_DI_ADI.RetornaLinhaDiAdiPorControleLinhaDiAdicao(itemControle, linhadi, itemadi.nAdicao);

                                                if (linhadiadi == null)
                                                {
                                                    linhadiadi = new INT_NFE_LINHA_DI_ADI();
                                                }

                                                linhadiadi.ID_DI = linhadi.ID;
                                                linhadiadi.ID_CONTROLE = itemControle.ID;
                                                linhadiadi.DT_CRIACAO = DateTime.Now;
                                                linhadiadi.DT_ALTERACAO = DateTime.Now;
                                                linhadiadi.CRIADO_POR = 1;
                                                linhadiadi.ALTERADO_POR = 1;
                                                linhadiadi.STATUS = "A";
                                                linhadiadi.INFNFE_DET_PROD_DI_ADI_NAD = Convert.ToInt16(itemadi.nAdicao);
                                                linhadiadi.INFNFE_DET_PROD_DI_ADI_NSQA = Convert.ToInt16(itemadi.nSeqAdic);
                                                linhadiadi.INFNFE_DET_PROD_DI_ADI_CFAB = itemadi.cFabricante;
                                                linhadiadi.INFNFE_DET_PROD_DI_ADI_VDEDI = Convert.ToDecimal(itemadi.vDescDI);

                                                DAO_INT_NFE_LINHA_DI_ADI.Salvar(linhadiadi);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1400;
                            #region veic
                            try
                            {
                                linhaveic = new INT_NFE_LINHA_VEIC();

                                var queryveicProd = (from i in H01.H01_prod_I01.Descendants(NameSpace(1) + "veicProd")
                                                        select new
                                                        {
                                                            tpOp = (decimal?)i.Element(NameSpace(1) + "tpOp"),
                                                            chassi = (string)i.Element(NameSpace(1) + "chassi"),
                                                            cCor = (string)i.Element(NameSpace(1) + "cCor"),
                                                            xCor = (string)i.Element(NameSpace(1) + "xCor"),
                                                            pot = (string)i.Element(NameSpace(1) + "pot"),
                                                            cilin = (string)i.Element(NameSpace(1) + "cilin"),
                                                            pesoL = (string)i.Element(NameSpace(1) + "pesoL"),
                                                            pesoB = (string)i.Element(NameSpace(1) + "pesoB"),
                                                            nSerie = (string)i.Element(NameSpace(1) + "nSerie"),
                                                            tpComb = (string)i.Element(NameSpace(1) + "tpComb"),
                                                            nMotor = (string)i.Element(NameSpace(1) + "nMotor"),
                                                            CMT = (string)i.Element(NameSpace(1) + "CMT"),
                                                            dist = (string)i.Element(NameSpace(1) + "dist"),
                                                            anoMod = (decimal?)i.Element(NameSpace(1) + "anoMod"),
                                                            anoFab = (decimal?)i.Element(NameSpace(1) + "anoFab"),
                                                            tpPint = (string)i.Element(NameSpace(1) + "tpPint"),
                                                            tpVeic = (decimal?)i.Element(NameSpace(1) + "tpVeic"),
                                                            espVeic = (decimal?)i.Element(NameSpace(1) + "espVeic"),
                                                            VIN = (string)i.Element(NameSpace(1) + "VIN"),
                                                            condVeic = (decimal?)i.Element(NameSpace(1) + "condVeic"),
                                                            cMod = (decimal?)i.Element(NameSpace(1) + "cMod"),
                                                            cCorDEN = (decimal?)i.Element(NameSpace(1) + "cCorDENATRAN"),
                                                            lota = (decimal?)i.Element(NameSpace(1) + "lota"),
                                                            tpRest = (decimal?)i.Element(NameSpace(1) + "tpRest")
                                                        }).SingleOrDefault();

                                if (queryveicProd != null)
                                {
                                    linhaveic.ID = 0;
                                    linhaveic.ID_LINHA = linha.ID;
                                    linhaveic.ID_CONTROLE = itemControle.ID;
                                    linhaveic.DT_ALTERACAO = DateTime.Now;
                                    linhaveic.DT_CRIACAO = DateTime.Now;
                                    linhaveic.CRIADO_POR = 1;
                                    linhaveic.ALTERADO_POR = 1;
                                    linhaveic.STATUS = "A";
                                    linhaveic.INFNFE_DET_PROD_VPROD_TPOP = Convert.ToInt16(queryveicProd.tpOp);
                                    linhaveic.INFNFE_DET_PROD_VPROD_TCHASSI = queryveicProd.chassi;
                                    linhaveic.INFNFE_DET_PROD_VPROD_CCOR = queryveicProd.cCor;
                                    linhaveic.INFNFE_DET_PROD_VPROD_XCOR = queryveicProd.xCor;
                                    linhaveic.INFNFE_DET_PROD_VPROD_POT = queryveicProd.pot;
                                    linhaveic.INFNFE_DET_PROD_VPROD_CILIN = queryveicProd.cilin;
                                    linhaveic.INFNFE_DET_PROD_VPROD_PESOL = queryveicProd.pesoL;
                                    linhaveic.INFNFE_DET_PROD_VPROD_PESOB = queryveicProd.pesoB;
                                    linhaveic.INFNFE_DET_PROD_VPROD_NSERIE = queryveicProd.nSerie;
                                    linhaveic.INFNFE_DET_PROD_VPROD_TPCOMB = queryveicProd.tpComb;
                                    linhaveic.INFNFE_DET_PROD_VPROD_NMOTOR = queryveicProd.nMotor;
                                    linhaveic.INFNFE_DET_PROD_VPROD_CMT = queryveicProd.CMT;
                                    linhaveic.INFNFE_DET_PROD_VPROD_DIST = queryveicProd.dist;
                                    linhaveic.INFNFE_DET_PROD_VPROD_ANOMOD = Convert.ToInt16(queryveicProd.anoMod);
                                    linhaveic.INFNFE_DET_PROD_VPROD_ANOFAB = Convert.ToInt16(queryveicProd.anoFab);
                                    linhaveic.INFNFE_DET_PROD_VPROD_TPPINT = queryveicProd.tpPint;
                                    linhaveic.INFNFE_DET_PROD_VPROD_TPVEIC = Convert.ToInt16(queryveicProd.tpVeic);
                                    linhaveic.INFNFE_DET_PROD_VPROD_ESPVEIC = Convert.ToInt16(queryveicProd.espVeic);
                                    linhaveic.INFNFE_DET_PROD_VPROD_VIN = queryveicProd.VIN;
                                    linhaveic.INFNFE_DET_PROD_VPROD_CONDVEIC = Convert.ToInt16(queryveicProd.condVeic);
                                    linhaveic.INFNFE_DET_PROD_VPROD_CMOD = Convert.ToInt32(queryveicProd.cMod);
                                    linhaveic.INFNFE_DET_PROD_VPROD_CCORDEN = Convert.ToInt16(queryveicProd.cCorDEN);
                                    linhaveic.INFNFE_DET_PROD_VPROD_LOTA = Convert.ToInt16(queryveicProd.lota);
                                    linhaveic.INFNFE_DET_PROD_VPROD_TPREST = Convert.ToInt16(queryveicProd.tpRest);

                                    DAO_INT_NFE_LINHA_VEIC.Salvar(linhaveic);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1500;
                            #region med
                            try
                            {
                                linhamedic = new INT_NFE_LINHA_MEDIC();

                                var querymed = (from i in H01.H01_prod_I01.Descendants(NameSpace(1) + "med")
                                                select new
                                                {
                                                    nLote = (string)i.Element(NameSpace(1) + "nLote"),
                                                    qLote = (decimal)i.Element(NameSpace(1) + "qLote"),
                                                    dFab = (String)i.Element(NameSpace(1) + "dFab"),
                                                    dVal = (String)i.Element(NameSpace(1) + "dVal"),
                                                    vPMC = (decimal)i.Element(NameSpace(1) + "vPMC")
                                                }).SingleOrDefault();

                                if (querymed != null)
                                {
                                    linhamedic.ID = 0;
                                    linhamedic.ID_LINHA = linha.ID;
                                    linhamedic.ID_CONTROLE = itemControle.ID;
                                    linhamedic.DT_CRIACAO = DateTime.Now;
                                    linhamedic.DT_ALTERACAO = DateTime.Now;
                                    linhamedic.CRIADO_POR = 1;
                                    linhamedic.ALTERADO_POR = 1;
                                    linhamedic.STATUS = "A";
                                    linhamedic.INFNFE_DET_PROD_MED_NLOTE = querymed.nLote;
                                    linhamedic.INFNFE_DET_PROD_MED_QLOTE = querymed.qLote;
                                    linhamedic.INFNFE_DET_PROD_MED_DFAB = Convert.ToDateTime(String.Concat(querymed.dFab.Substring(8, 2), "/", querymed.dFab.Substring(5, 2), "/", querymed.dFab.Substring(0, 4)));
                                    linhamedic.INFNFE_DET_PROD_MED_DVAL = Convert.ToDateTime(String.Concat(querymed.dVal.Substring(8, 2), "/", querymed.dVal.Substring(5, 2), "/", querymed.dVal.Substring(0, 4)));
                                    linhamedic.INFNFE_DET_PROD_MED_VPMC = querymed.vPMC;

                                    DAO_INT_NFE_LINHA_MEDIC.Salvar(linhamedic);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1600;
                            #region arma
                            try
                            {
                                linhaarmas = new INT_NFE_LINHA_ARMAS();

                                var queryarma = (from i in H01.H01_prod_I01.Descendants(NameSpace(1) + "arma")
                                                    select new
                                                    {
                                                        tpArma = (decimal?)i.Element(NameSpace(1) + "tpArma"),
                                                        nSerie = (decimal?)i.Element(NameSpace(1) + "nSerie"),
                                                        nCano = (decimal?)i.Element(NameSpace(1) + "nCano"),
                                                        descr = (string)i.Element(NameSpace(1) + "dVal")
                                                    }).SingleOrDefault();

                                if (queryarma != null)
                                {
                                    linhaarmas.ID = 0;
                                    linhaarmas.ID_LINHA = linha.ID;
                                    linhaarmas.ID_CONTROLE = itemControle.ID;
                                    linhaarmas.DT_CRIACAO = DateTime.Now;
                                    linhaarmas.DT_ALTERACAO = DateTime.Now;
                                    linhaarmas.CRIADO_POR = 1;
                                    linhaarmas.ALTERADO_POR = 1;
                                    linhaarmas.STATUS = "A";

                                    linhaarmas.INFNFE_DET_PROD_ARMAS_TPARMA = Convert.ToInt16(queryarma.tpArma);
                                    linhaarmas.INFNFE_DET_PROD_ARMAS_NSERIE = Convert.ToInt16(queryarma.nSerie);
                                    linhaarmas.INFNFE_DET_PROD_ARMAS_NCANO = Convert.ToInt16(queryarma.nCano);
                                    linhaarmas.INFNFE_DET_PROD_ARMAS_DESCR = queryarma.descr;

                                    DAO_INT_NFE_LINHA_ARMAS.Salvar(linhaarmas);
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                            erl = 1700;
                            #region comb
                            try
                            {
                                linhacombust = new INT_NFE_LINHA_COMBUST();

                                if (itemControle.INFNFE_VERSAO == 2)
                                {
                                    var querycomb = (from i in H01.H01_prod_I01.Descendants(NameSpace(1) + "comb")
                                                        select new
                                                        {
                                                            cProdANP = (decimal?)i.Element(NameSpace(1) + "cProdANP"),
                                                            CODIF = (decimal?)i.Element(NameSpace(1) + "CODIF"),
                                                            qTemp = (decimal?)i.Element(NameSpace(1) + "qTemp"),
                                                            UFCons = (string)i.Element(NameSpace(1) + "UFCons"),
                                                            CIDE = (XElement)i.Element(NameSpace(1) + "CIDE"),
                                                            pMixGN = (decimal?)i.Element(NameSpace(1) + "pMixGN")
                                                        }).SingleOrDefault();

                                    if (querycomb != null)
                                    {
                                        linhacombust.ID = 0;
                                        linhacombust.ID_LINHA = linha.ID;
                                        linhacombust.ID_CONTROLE = itemControle.ID;
                                        linhacombust.DT_CRIACAO = DateTime.Now;
                                        linhacombust.DT_ALTERACAO = DateTime.Now;
                                        linhacombust.CRIADO_POR = 1;
                                        linhacombust.ALTERADO_POR = 1;
                                        linhacombust.STATUS = "A";

                                        if (querycomb.cProdANP != null)
                                            linhacombust.INFNFE_DET_PROD_COMB_CPRODANP = Convert.ToInt16(querycomb.cProdANP);
                                        else
                                            linhacombust.INFNFE_DET_PROD_COMB_CPRODANP = 0;

                                        if (querycomb.CODIF != null)
                                            linhacombust.INFNFE_DET_PROD_COMB_CODIF = Convert.ToDecimal(querycomb.CODIF);
                                        else
                                            linhacombust.INFNFE_DET_PROD_COMB_CODIF = 0;

                                        if (querycomb.qTemp != null)
                                            linhacombust.INFNFE_DET_PROD_COMB_QTEMP = Convert.ToDecimal(querycomb.qTemp);
                                        else
                                            linhacombust.INFNFE_DET_PROD_COMB_QTEMP = 0;

                                        if (querycomb.UFCons != null)
                                            linhacombust.INFNFE_DET_PROD_COMB_UFCONS = querycomb.UFCons;
                                        else
                                            linhacombust.INFNFE_DET_PROD_COMB_UFCONS = string.Empty;
                                        try
                                        {
                                            if (querycomb.CIDE.Element(NameSpace(1) + "qBCprod") != null)
                                                linhacombust.INFNFE_DET_PROD_CB_CD_QBCPROD = Convert.ToDecimal((decimal?)querycomb.CIDE.Element(NameSpace(1) + "qBCprod"));
                                            else
                                                linhacombust.INFNFE_DET_PROD_CB_CD_QBCPROD = 0;
                                        }
                                        catch
                                        {
                                            linhacombust.INFNFE_DET_PROD_CB_CD_QBCPROD = 0;
                                        }

                                        try
                                        {
                                            if (querycomb.CIDE.Element(NameSpace(1) + "vAliqProd") != null)
                                                linhacombust.INFNFE_DET_PROD_CB_CD_VALQPRD = Convert.ToDecimal((decimal?)querycomb.CIDE.Element(NameSpace(1) + "vAliqProd"));
                                            else
                                                linhacombust.INFNFE_DET_PROD_CB_CD_VALQPRD = 0;
                                        }
                                        catch
                                        {
                                            linhacombust.INFNFE_DET_PROD_CB_CD_VALQPRD = 0;
                                        }

                                        try
                                        {
                                            if (querycomb.CIDE.Element(NameSpace(1) + "vCIDE") != null)
                                                linhacombust.INFNFE_DET_PROD_CB_CD_VCIDE = Convert.ToDecimal((decimal?)querycomb.CIDE.Element(NameSpace(1) + "vCIDE"));
                                            else
                                                linhacombust.INFNFE_DET_PROD_CB_CD_VCIDE = 0;
                                        }
                                        catch
                                        {
                                            linhacombust.INFNFE_DET_PROD_CB_CD_VCIDE = 0;
                                        }


                                        if (querycomb.pMixGN != null)
                                            linhacombust.INFNFE_DET_PROD_COMB_PMIXGN = Convert.ToDecimal(querycomb.pMixGN);
                                        else
                                            linhacombust.INFNFE_DET_PROD_COMB_PMIXGN = 0;

                                        DAO_INT_NFE_LINHA_COMBUST.Salvar(linhacombust);
                                    }
                                }
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }
                            #endregion
                        }
                        catch(Exception ex)
                        {
#if EXCEPTIONS_ENABLED
                            throw new Exception("erl: " + erl, ex);
#else
                            erro = true;
#endif
                        }
                    }
                    erl = 1800;
                    #region transp
                    try
                    {
                        notatransp = new INT_NFE_NOTA_TRANSP();

                        var queryNotaTransp = (from i in XmlTexto.Descendants(NameSpace(1) + "transp")
                                                select new
                                                {
                                                    modFrete = (decimal?)i.Element(NameSpace(1) + "modFrete"),
                                                    transporta = (XElement)i.Element(NameSpace(1) + "transporta"),
                                                    retTransp = (XElement)i.Element(NameSpace(1) + "retTransp"),
                                                    veicTransp = (XElement)i.Element(NameSpace(1) + "veicTransp"),
                                                    reboque = (XElement)i.Element(NameSpace(1) + "reboque"),
                                                    vagao = (string)i.Element(NameSpace(1) + "vagao"),
                                                    balsa = (string)i.Element(NameSpace(1) + "balsa"),
                                                    vol = (XElement)i.Element(NameSpace(1) + "vol")
                                                }).SingleOrDefault();

                        if (queryNotaTransp != null)
                        {
                            try
                            {
                                notatransp.ID = 0;
                                notatransp.ID_CONTROLE = itemControle.ID;
                                notatransp.DT_ALTERACAO = DateTime.Now;
                                notatransp.DT_CRIACAO = DateTime.Now;
                                notatransp.CRIADO_POR = 1;
                                notatransp.ALTERADO_POR = 1;
                                notatransp.STATUS = "A";

                                notatransp.INFNFE_TRANSP_MODFRETE = Convert.ToInt16(queryNotaTransp.modFrete);
                                notatransp.INFNFE_TRANSP_VAGAO = queryNotaTransp.vagao == null ? "" : queryNotaTransp.vagao;
                                notatransp.INFNFE_TRANSP_BALSA = queryNotaTransp.balsa == null ? "" : queryNotaTransp.balsa;

                                if (queryNotaTransp.transporta != null)
                                {
                                    notatransp.INFNFE_TRANSP_TRP_CNPJ = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "CNPJ") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "CNPJ");
                                    notatransp.INFNFE_TRANSP_TRP_CPF = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "CPF") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "CPF");
                                    notatransp.INFNFE_TRANSP_TRP_XNOME = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xNome") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xNome");
                                    notatransp.INFNFE_TRANSP_TRP_IE = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "IE") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "IE");
                                    notatransp.INFNFE_TRANSP_TRP_XENDER = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xEnder") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xEnder");
                                    notatransp.INFNFE_TRANSP_TRP_XMUN = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xMun") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "xMun");
                                    notatransp.INFNFE_TRANSP_TRP_UF = (string)queryNotaTransp.transporta.Element(NameSpace(1) + "UF") == null ? "" : (string)queryNotaTransp.transporta.Element(NameSpace(1) + "UF");
                                }

                                if (queryNotaTransp.retTransp != null)
                                {
                                    notatransp.INFNFE_TRANSP_RTRP_VSERV = Convert.ToDecimal((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "vServ"));
                                    notatransp.INFNFE_TRANSP_RTRP_VBCRET = Convert.ToDecimal((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "vBCRet"));
                                    notatransp.INFNFE_TRANSP_RTRP_PICMSRET = Convert.ToDecimal((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "pICMSRet"));
                                    notatransp.INFNFE_TRANSP_RTRP_VICMSRET = Convert.ToDecimal((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "vICMSRet"));
                                    notatransp.INFNFE_TRANSP_RTRP_CFOP = Convert.ToInt16((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "CFOP"));
                                    notatransp.INFNFE_TRANSP_RTRP_CMUNFG = Convert.ToInt32((decimal?)queryNotaTransp.retTransp.Element(NameSpace(1) + "cMunFG"));
                                }

                                if (queryNotaTransp.veicTransp != null)
                                {
                                    notatransp.INFNFE_TRANSP_RTRP_VTRP_PLACA = (string)queryNotaTransp.veicTransp.Element(NameSpace(1) + "placa");
                                    notatransp.INFNFE_TRANSP_RTRP_VTRP_UF = (string)queryNotaTransp.veicTransp.Element(NameSpace(1) + "UF");
                                    notatransp.INFNFE_TRANSP_RTRP_VTRP_RNTC = (string)queryNotaTransp.veicTransp.Element(NameSpace(1) + "RNTC");
                                }
                                
                                DAO_INT_NFE_NOTA_TRANSP.Salvar(notatransp);
                            }
                            catch
                            {
#if EXCEPTIONS_ENABLED
                                throw;
#else
                                erro = true;
#endif
                            }

                            if (queryNotaTransp.reboque != null)
                            {
                                notatranspreboque = new INT_NFE_NOTA_TRANSP_REBOQUE();

                                var queryreboque = (from i in queryNotaTransp.reboque.Descendants(NameSpace(1) + "reboque")
                                                    select new
                                                    {
                                                        placa = (string)i.Element(NameSpace(1) + "placa"),
                                                        UF = (string)i.Element(NameSpace(1) + "UF"),
                                                        RNTC = (string)i.Element(NameSpace(1) + "RNTC")
                                                    }).ToList();

                                if (queryreboque != null && queryreboque.Count > 0)
                                {
                                    foreach (var itemreboque in queryreboque)
                                    {
                                        notatranspreboque.ID = 0;
                                        notatranspreboque.ID_CONTROLE = itemControle.ID;
                                        notatranspreboque.DT_ALTERACAO = DateTime.Now;
                                        notatranspreboque.DT_CRIACAO = DateTime.Now;
                                        notatranspreboque.CRIADO_POR = 1;
                                        notatranspreboque.ALTERADO_POR = 1;
                                        notatranspreboque.STATUS = "A";
                                        notatranspreboque.INFNFE_TRANSP_REBOQUE_PLACA = itemreboque.placa == null ? "" : itemreboque.placa;
                                        notatranspreboque.INFNFE_TRANSP_REBOQUE_UF = itemreboque.UF == null ? "" : itemreboque.UF;
                                        notatranspreboque.INFNFE_TRANSP_REBOQUE_RNTC = itemreboque.RNTC == null ? "" : itemreboque.RNTC;

                                        DAO_INT_NFE_NOTA_TRANSP_REBOQUE.Salvar(notatranspreboque);
                                    };
                                }
                            }

                            if (queryNotaTransp.vol != null)
                            {
                                notatranspvol = new INT_NFE_NOTA_TRANSP_VOL();

                                notatranspvol.ID = 0;
                                notatranspvol.ID_CONTROLE = itemControle.ID;
                                notatranspvol.DT_ALTERACAO = DateTime.Now;
                                notatranspvol.DT_CRIACAO = DateTime.Now;
                                notatranspvol.CRIADO_POR = 1;
                                notatranspvol.ALTERADO_POR = 1;
                                notatranspvol.STATUS = "A";

                                notatranspvol.INFNFE_TRANSP_VOL_QVOL = Convert.ToInt32((decimal?)queryNotaTransp.vol.Element(NameSpace(1) + "qVol"));
                                notatranspvol.INFNFE_TRANSP_VOL_ESP = (string)queryNotaTransp.vol.Element(NameSpace(1) + "esp") == null ? "" : (string)queryNotaTransp.vol.Element(NameSpace(1) + "esp");
                                notatranspvol.INFNFE_TRANSP_VOL_MARCA = (string)queryNotaTransp.vol.Element(NameSpace(1) + "marca") == null ? "" : (string)queryNotaTransp.vol.Element(NameSpace(1) + "marca");
                                notatranspvol.INFNFE_TRANSP_VOL_NVOL = (string)queryNotaTransp.vol.Element(NameSpace(1) + "nVol") == null ? "" : (string)queryNotaTransp.vol.Element(NameSpace(1) + "nVol");
                                notatranspvol.INFNFE_TRANSP_VOL_PESOL = Convert.ToDecimal((decimal?)queryNotaTransp.vol.Element(NameSpace(1) + "pesoL"));
                                notatranspvol.INFNFE_TRANSP_VOL_PESOB = Convert.ToDecimal((decimal?)queryNotaTransp.vol.Element(NameSpace(1) + "pesoB"));

                                DAO_INT_NFE_NOTA_TRANSP_VOL.Salvar(notatranspvol);
                            }

                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        erro = true;
#endif
                    }
                    #endregion
                    erl = 1900;
                    #region cobr
                    try
                    {
                        notacobr = new INT_NFE_NOTA_COBR();

                        var queryNotaCobr = (from i in XmlTexto.Descendants(NameSpace(1) + "cobr")
                                                select new
                                                {
                                                    fat = (XElement)i.Element(NameSpace(1) + "fat"),
                                                    dup = (XElement)i.Element(NameSpace(1) + "dup")
                                                }).SingleOrDefault();

                        if (queryNotaCobr != null)
                        {
                            notacobr.ID = 0;
                            notacobr.ID_CONTROLE = itemControle.ID;
                            notacobr.DT_ALTERACAO = DateTime.Now;
                            notacobr.DT_CRIACAO = DateTime.Now;
                            notacobr.CRIADO_POR = 1;
                            notacobr.ALTERADO_POR = 1;
                            notacobr.STATUS = "A";

                            if (queryNotaCobr.fat != null)
                            {
                                notacobr.INFNFE_COBR_FAT_NFAT = (string)queryNotaCobr.fat.Element(NameSpace(1) + "nFat") == null ? "" : (string)queryNotaCobr.fat.Element(NameSpace(1) + "nFat");
                                notacobr.INFNFE_COBR_FAT_VORIG = Convert.ToDecimal((decimal?)queryNotaCobr.fat.Element(NameSpace(1) + "vOrig"));
                                notacobr.INFNFE_COBR_FAT_VDESC = Convert.ToDecimal((decimal?)queryNotaCobr.fat.Element(NameSpace(1) + "vLiq"));
                            }

                            DAO_INT_NFE_NOTA_COBR.Salvar(notacobr);

                            if (queryNotaCobr.dup != null)
                            {
                                notacobrdup = new INT_NFE_NOTA_COBR_DUP();

                                notacobrdup.ID = 0;
                                notacobrdup.ID_CONTROLE = itemControle.ID;
                                notacobrdup.ID_COBR = notacobr.ID;
                                notacobrdup.DT_ALTERACAO = DateTime.Now;
                                notacobrdup.DT_CRIACAO = DateTime.Now;
                                notacobrdup.CRIADO_POR = 1;
                                notacobrdup.ALTERADO_POR = 1;
                                notacobrdup.STATUS = "A";

                                notacobrdup.INFNFE_COBR_FAT_DUP_NDUP = (string)queryNotaCobr.dup.Element(NameSpace(1) + "nDup") == null ? "" : (string)queryNotaCobr.dup.Element(NameSpace(1) + "nDup");
                                String ddVenc = "";
                                
                                try
                                {
                                    ddVenc = (String)queryNotaCobr.dup.Element(NameSpace(1) + "dVenc");
                                    notacobrdup.INFNFE_COBR_FAT_DUP_DVENC = Convert.ToDateTime(String.Concat(ddVenc.Substring(8, 2), "/", ddVenc.Substring(5, 2), "/", ddVenc.Substring(0, 4)));
                                }
                                catch
                                {
                                    ddVenc = DateTime.Now.Date.ToShortDateString();
                                    notacobrdup.INFNFE_COBR_FAT_DUP_DVENC = null;
                                }
                                
                                notacobrdup.INFNFE_COBR_FAT_DUP_VDUP = Convert.ToDecimal((decimal?)queryNotaCobr.dup.Element(NameSpace(1) + "vDup"));

                                DAO_INT_NFE_NOTA_COBR_DUP.Salvar(notacobrdup);
                            }
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        erro = true;
#endif
                    }
                    #endregion
                    erl = 2000;
                    #region infAdic
                    try
                    {
                        notainfadic = new INT_NFE_NOTA_INF_ADIC();

                        var queryInfAdic = (from i in XmlTexto.Descendants(NameSpace(1) + "infAdic")
                                            select new
                                            {
                                                infAdFisco = (string)i.Element(NameSpace(1) + "infAdFisco"),
                                                infCpl = (string)i.Element(NameSpace(1) + "infCpl"),
                                                obsCont = (XElement)i.Element(NameSpace(1) + "obsCont"),
                                                obsFisco = (XElement)i.Element(NameSpace(1) + "obsFisco"),
                                                procRef = (XElement)i.Element(NameSpace(1) + "procRef")
                                            }).SingleOrDefault();

                        if (queryInfAdic != null)
                        {
                            notainfadic.ID = 0;
                            notainfadic.ID_CONTROLE = itemControle.ID;
                            notainfadic.DT_ALTERACAO = DateTime.Now;
                            notainfadic.DT_CRIACAO = DateTime.Now;
                            notainfadic.CRIADO_POR = 1;
                            notainfadic.ALTERADO_POR = 1;
                            notainfadic.STATUS = "A";

                            notainfadic.INFNFE_INFADIC_INFADFISCO = queryInfAdic.infAdFisco == null ? "" : queryInfAdic.infAdFisco;

                            String infCpl = queryInfAdic.infCpl == null ? "" : queryInfAdic.infCpl;

                            while (infCpl.IndexOf(".  ") > 0 || infCpl.IndexOf("  .") > 0)
                            {
                                infCpl = infCpl.Replace(".  ", ". ");
                                infCpl = infCpl.Replace("  .", " .");
                            }

                            notainfadic.INFNFE_INFADIC_INFCPL = infCpl;

                            var queryExporta = (from i in XmlTexto.Descendants(NameSpace(1) + "exporta")
                                                select new
                                                {
                                                    UFEmbarq = (string)i.Element(NameSpace(1) + "UFEmbarq"),
                                                    xLocEmbarq = (string)i.Element(NameSpace(1) + "xLocEmbarq")
                                                }).SingleOrDefault();

                            if (queryExporta != null)
                            {
                                notainfadic.INFNFE_INFADIC_EXP_UFEMBARQ = queryExporta.UFEmbarq == null ? "" : queryExporta.UFEmbarq;
                                notainfadic.INFNFE_INFADIC_EXP_XLOCEMBARQ = queryExporta.xLocEmbarq == null ? "" : queryExporta.xLocEmbarq;
                            }

                            var queryCompra = (from i in XmlTexto.Descendants(NameSpace(1) + "compra")
                                                select new
                                                {
                                                    xNEmp = (string)i.Element(NameSpace(1) + "xNEmp"),
                                                    xPed = (string)i.Element(NameSpace(1) + "xPed"),
                                                    xCont = (string)i.Element(NameSpace(1) + "xCont")
                                                }).SingleOrDefault();

                            if (queryCompra != null)
                            {
                                notainfadic.INFNFE_INFADIC_COMP_XNEMP = queryCompra.xNEmp == null ? "" : queryCompra.xNEmp;
                                notainfadic.INFNFE_INFADIC_COMP_XPED = queryCompra.xPed == null ? "" : queryCompra.xPed;
                                notainfadic.INFNFE_INFADIC_COMP_XCONT = queryCompra.xCont == null ? "" : queryCompra.xCont;
                            }

                            DAO_INT_NFE_NOTA_INF_ADIC.Salvar(notainfadic);

                            if (queryInfAdic.obsCont != null)
                            {
                                notainfadicobscont = new INT_NFE_NOTA_INF_ADIC_OBSCONT();

                                var queryobsCont = (from i in queryInfAdic.obsCont.Descendants(NameSpace(1) + "obsCont")
                                                    select new
                                                    {
                                                        xCampo = (string)i.Element(NameSpace(1) + "xCampo"),
                                                        xTexto = (string)i.Element(NameSpace(1) + "xTexto")
                                                    }).ToList();

                                foreach (var itemobsCont in queryobsCont)
                                {
                                    notainfadicobscont.ID = 0;
                                    notainfadicobscont.ID_CONTROLE = itemControle.ID;
                                    notainfadicobscont.DT_ALTERACAO = DateTime.Now;
                                    notainfadicobscont.DT_CRIACAO = DateTime.Now;
                                    notainfadicobscont.CRIADO_POR = 1;
                                    notainfadicobscont.ALTERADO_POR = 1;
                                    notainfadicobscont.STATUS = "A";

                                    notainfadicobscont.INFNFE_INFADIC_OBSCONT_XCAMPO = itemobsCont.xCampo == null ? "" : itemobsCont.xCampo;
                                    notainfadicobscont.INFNFE_INFADIC_OBSCONT_XTEXTO = itemobsCont.xTexto == null ? "" : itemobsCont.xTexto;

                                    DAO_INT_NFE_NOTA_INF_ADIC_OBSCONT.Salvar(notainfadicobscont);
                                }
                            }

                            if (queryInfAdic.obsCont != null)
                            {
                                var queryobsFisco = (from i in queryInfAdic.obsCont.Descendants(NameSpace(1) + "obsFisco")
                                                        select new
                                                        {
                                                            xCampo = (string)i.Element(NameSpace(1) + "xCampo"),
                                                            xTexto = (string)i.Element(NameSpace(1) + "xTexto")
                                                        }).ToList();

                                foreach (var itemobsfisco in queryobsFisco)
                                {
                                    notainfadicobsfisco = new INT_NFE_NOTA_INF_ADIC_OBSFISCO();

                                    notainfadicobsfisco.ID = 0;
                                    notainfadicobsfisco.ID_CONTROLE = itemControle.ID;
                                    notainfadicobsfisco.DT_ALTERACAO = DateTime.Now;
                                    notainfadicobsfisco.DT_CRIACAO = DateTime.Now;
                                    notainfadicobsfisco.CRIADO_POR = 1;
                                    notainfadicobsfisco.ALTERADO_POR = 1;
                                    notainfadicobsfisco.STATUS = "A";

                                    notainfadicobsfisco.INFNFE_INFADIC_OBSFISCO_XCAMPO = itemobsfisco.xCampo == null ? "" : itemobsfisco.xCampo;
                                    notainfadicobsfisco.INFNFE_INFADIC_OBSFISCO_XTEXTO = itemobsfisco.xTexto == null ? "" : itemobsfisco.xTexto;

                                    DAO_INT_NFE_NOTA_INF_ADIC_OBSFISCO.Salvar(notainfadicobsfisco);
                                }
                            }

                            if (queryInfAdic.procRef != null)
                            {
                                var queryprocRef = (from i in queryInfAdic.procRef.Descendants(NameSpace(1) + "procRef")
                                                    select new
                                                    {
                                                        nProc = (string)i.Element(NameSpace(1) + "nProc"),
                                                        indProc = (decimal?)i.Element(NameSpace(1) + "indProc")
                                                    }).ToList();

                                foreach (var itemprocRef in queryprocRef)
                                {
                                    notainfadicprocref = new INT_NFE_NOTA_INF_ADIC_PROCREF();

                                    notainfadicprocref.ID = 0;
                                    notainfadicprocref.ID_CONTROLE = itemControle.ID;
                                    notainfadicprocref.DT_ALTERACAO = DateTime.Now;
                                    notainfadicprocref.DT_CRIACAO = DateTime.Now;
                                    notainfadicprocref.CRIADO_POR = 1;
                                    notainfadicprocref.ALTERADO_POR = 1;
                                    notainfadicprocref.STATUS = "A";

                                    notainfadicprocref.INFNFE_INFADIC_PROCREF_NPROC = itemprocRef.nProc == null ? "" : itemprocRef.nProc;
                                    notainfadicprocref.INFNFE_INFADIC_PROCREF_INDPROC = Convert.ToInt16(itemprocRef.indProc);

                                    DAO_INT_NFE_NOTA_INF_ADIC_PROCREF.Salvar(notainfadicprocref);
                                }
                            }
                        }

                        var queryInfAdicCana = (from i in XmlTexto.Descendants(NameSpace(1) + "cana")
                                                select new
                                                {
                                                    safra = (string)i.Element(NameSpace(1) + "safra"),
                                                    cref = (string)i.Element(NameSpace(1) + "ref"),
                                                    forDia = (XElement)i.Element(NameSpace(1) + "forDia"),
                                                    deduc = (XElement)i.Element(NameSpace(1) + "deduc")
                                                }).SingleOrDefault();

                        if (queryInfAdicCana != null)
                        {
                            notainfadiccana = new INT_NFE_NOTA_INF_ADIC_CANA();
                            notainfadiccana.ID = 0;
                            notainfadiccana.ID_CONTROLE = itemControle.ID;
                            notainfadiccana.DT_ALTERACAO = DateTime.Now;
                            notainfadiccana.DT_CRIACAO = DateTime.Now;
                            notainfadiccana.CRIADO_POR = 1;
                            notainfadiccana.ALTERADO_POR = 1;
                            notainfadiccana.STATUS = "A";

                            notainfadiccana.INFNFE_INFADIC_CANA_SAFRA = queryInfAdicCana.safra == null ? "" : queryInfAdicCana.safra;
                            notainfadiccana.INFNFE_INFADIC_CANA_REF = queryInfAdicCana.cref == null ? "" : queryInfAdicCana.cref;

                            DAO_INT_NFE_NOTA_INF_ADIC_CANA.Salvar(notainfadiccana);

                            if (queryInfAdicCana.forDia != null)
                            {
                                var queryforDia = (from i in queryInfAdicCana.forDia.Descendants(NameSpace(1) + "forDia")
                                                    select new
                                                    {
                                                        dia = (decimal?)i.Element(NameSpace(1) + "dia"),
                                                        qtde = (decimal?)i.Element(NameSpace(1) + "qtde"),
                                                        qTotMes = (decimal?)i.Element(NameSpace(1) + "qTotMes"),
                                                        qTotAnt = (decimal?)i.Element(NameSpace(1) + "qTotAnt"),
                                                        qTotGer = (decimal?)i.Element(NameSpace(1) + "qTotGer")
                                                    }).ToList();

                                foreach (var itemforDia in queryforDia)
                                {
                                    notacanafordia = new INT_NFE_NOTA_CANA_FORDIA();

                                    notacanafordia.ID = 0;
                                    notacanafordia.DT_ALTERACAO = DateTime.Now;
                                    notacanafordia.DT_CRIACAO = DateTime.Now;
                                    notacanafordia.CRIADO_POR = 1;
                                    notacanafordia.ALTERADO_POR = 1;
                                    notacanafordia.STATUS = "A";

                                    notacanafordia.ID_CONTROLE = itemControle.ID;
                                    notacanafordia.INFNFE_CANA_FORDIA_DIA = Convert.ToInt16(itemforDia.dia);
                                    notacanafordia.INFNFE_CANA_FORDIA_QTDE = Convert.ToInt16(itemforDia.qtde);
                                    notacanafordia.INFNFE_CANA_FORDIA_QTOTMES = Convert.ToInt16(itemforDia.qTotMes);
                                    notacanafordia.INFNFE_CANA_FORDIA_QTOTANT = Convert.ToInt16(itemforDia.qTotAnt);
                                    notacanafordia.INFNFE_CANA_FORDIA_QTOTGER = Convert.ToInt16(itemforDia.qTotGer);

                                    DAO_INT_NFE_NOTA_CANA_FORDIA.Salvar(notacanafordia);
                                }
                            }

                            if (queryInfAdicCana.deduc != null)
                            {
                                var querydeduc = (from i in queryInfAdicCana.deduc.Descendants(NameSpace(1) + "deduc")
                                                    select new
                                                    {
                                                        xDed = (string)i.Element(NameSpace(1) + "xDed"),
                                                        vDed = (decimal?)i.Element(NameSpace(1) + "vDed"),
                                                        vFor = (decimal?)i.Element(NameSpace(1) + "vFor"),
                                                        vTotDed = (decimal?)i.Element(NameSpace(1) + "vTotDed"),
                                                        vLiqFor = (decimal?)i.Element(NameSpace(1) + "vLiqFor")
                                                    }).ToList();

                                foreach (var itemdeduc in querydeduc)
                                {
                                    notacanadeduc = new INT_NFE_NOTA_CANA_DEDUC();

                                    notacanadeduc.ID = 0;
                                    notacanadeduc.DT_ALTERACAO = DateTime.Now;
                                    notacanadeduc.DT_CRIACAO = DateTime.Now;
                                    notacanadeduc.CRIADO_POR = 1;
                                    notacanadeduc.ALTERADO_POR = 1;
                                    notacanadeduc.STATUS = "A";

                                    notacanadeduc.ID_CONTROLE = itemControle.ID;
                                    notacanadeduc.INFNFE_CANA_DEDUC_XDED = itemdeduc.xDed == null ? "" : itemdeduc.xDed;
                                    notacanadeduc.INFNFE_CANA_DEDUC_VDED = Convert.ToDecimal(itemdeduc.vDed);
                                    notacanadeduc.INFNFE_CANA_DEDUC_VFOR = Convert.ToDecimal(itemdeduc.vFor);
                                    notacanadeduc.INFNFE_CANA_DEDUC_VTOTDED = Convert.ToDecimal(itemdeduc.vTotDed);
                                    notacanadeduc.INFNFE_CANA_DEDUC_VLIQFOR = Convert.ToDecimal(itemdeduc.vLiqFor);

                                    DAO_INT_NFE_NOTA_CANA_DEDUC.Salvar(notacanadeduc);
                                }
                            }
                        }
                    }
                    catch
                    {
#if EXCEPTIONS_ENABLED
                        throw;
#else
                        erro = true;
#endif
                    }
                    #endregion

                    if (erro)
                    {
                        itemControle.STATUS = "9";
                    }
                    else
                    {
                        itemControle.STATUS = "1";
                    }

                    DAO_INT_NFE_CONTROLE.Salvar(itemControle);
                }
                catch(Exception ex)
                {
#if EXCEPTIONS_ENABLED
                    throw new Exception("erl: " + erl, ex);
#else
                    itemControle.DT_ALTERACAO = DateTime.Now;
                    itemControle.STATUS = "9";
                    DAO_INT_NFE_CONTROLE.Salvar(itemControle);
#endif
                }
            }
        }
    }
}