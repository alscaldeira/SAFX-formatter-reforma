using ABMC.RANFE.LIGHT_SQL.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ABMC.RANFE.LIGHT_SQL
{
    public class LerXml
    {
        //clsError Errors = new clsError();

        private const String NFe = "NF-e";
        private const String CTe = "CT-e";

        private String _origem;
        private String _enderecoEmail;
        private String _enderecoWebService;
        private String _enderecoSefaz;
        private String _usuario;
        private String _senha;
        private String _pastaNoMailBox;
        private String _pastaBackupNoMailBox;
        private String _pastaArquivoXml;
        private String _pastaBackupDeArquivos;
        private String _pastaArquivoXsdNFe;
        private String _pastaArquivoXsdCTe;
        private XmlDocument _xmlDocTexto;
        private XDocument _xmlXDocument;
        private int _ambienteSefaz;
        private int _ambienteWebService;
        private ValidaNFeXml _validaNFeXml;
        private String _nomeArquivo;
        private XmlReaderSettings _xmlSettings;
        private XmlReader _xmlReader;
        private XmlTextReader _xmlTextReader;
        private StringBuilder _sbErro; // = new StringBuilder();
        private XmlSchemaSet _schemaXsd; // = new XmlSchemaSet();
        private XmlSchemaSet _schemaXsd_110;
        private XmlSchemaSet _schemaXsd_200;
        private XmlSchemaSet _schemaXsd_310;
        private XmlSchemaSet _schemaXsd_103;
        private XmlSchemaSet _schemaXsd_104;
        private INT_NFE_XML_TMP _xmlNFeTmp;
        private ABMC_RACTE_XML_TMP _xmlCTeTmp;
        private INT_NFE_ORIGEM_XML _origemXml;
        private bool _integra;
        private bool _gravaSynchro;
        private String _xmlTipo = "";

        public LerXml(String origem)
        {
            this.Origem = origem;
            _sbErro = new StringBuilder();
            _schemaXsd = new XmlSchemaSet();
            _schemaXsd_110 = new XmlSchemaSet();
            _schemaXsd_200 = new XmlSchemaSet();
            _schemaXsd_103 = new XmlSchemaSet();
            _schemaXsd_104 = new XmlSchemaSet();
            _xmlDocTexto = new XmlDocument();
            _xmlXDocument = new XDocument();
            _validaNFeXml = new ValidaNFeXml();
            //_validaCTeXml = new ValidaCTeXml();
            _xmlSettings = new XmlReaderSettings();
            _xmlNFeTmp = new INT_NFE_XML_TMP();
            _xmlCTeTmp = new ABMC_RACTE_XML_TMP();
            _origemXml = new INT_NFE_ORIGEM_XML();
            _integra = true;
            _gravaSynchro = false;
            _xmlTipo = "";
        }

        public String XmlTipo
        {
            get { return _xmlTipo; }
            set { _xmlTipo = value; }
        }

        public String Origem
        {
            get { return _origem; }
            set { _origem = value; }
        }

        public bool Integra
        {
            get { return _integra; }
            set { _integra = value; }
        }

        public bool GravaSynchro
        {
            get { return _gravaSynchro; }
            set { _gravaSynchro = value; }
        }

        public XmlSchemaSet SchemaXsd
        {
            get { return _schemaXsd; }
            set { _schemaXsd = value; }
        }

        public XmlSchemaSet SchemaXsd_110
        {
            get { return _schemaXsd_110; }
            set { _schemaXsd_110 = value; }
        }

        public XmlSchemaSet SchemaXsd_200
        {
            get { return _schemaXsd_200; }
            set { _schemaXsd_200 = value; }
        }

        public XmlSchemaSet SchemaXsd_310
        {
            get { return _schemaXsd_310; }
            set { _schemaXsd_310 = value; }
        }

        public XmlSchemaSet SchemaXsd_103
        {
            get { return _schemaXsd_103; }
            set { _schemaXsd_103 = value; }
        }

        public XmlSchemaSet SchemaXsd_104
        {
            get { return _schemaXsd_104; }
            set { _schemaXsd_104 = value; }
        }

        public XmlReaderSettings XmlSettings
        {
            get { return _xmlSettings; }
            set { _xmlSettings = value; }
        }

        public XmlReader XmlReader
        {
            get { return _xmlReader; }
            set { _xmlReader = value; }
        }

        public XmlTextReader XmlTextReader
        {
            get { return _xmlTextReader; }
            set { _xmlTextReader = value; }
        }

        public INT_NFE_ORIGEM_XML OrigemXml
        {
            get { return _origemXml; }
            set { _origemXml = value; }
        }

        public StringBuilder SbErro
        {
            get { return _sbErro; }
            set { _sbErro = value; }
        }

        public String PastaArquivoXml
        {
            get { return _pastaArquivoXml; }
            set { _pastaArquivoXml = value; }
        }

        public String PastaArquivoXsdNFe
        {
            get { return _pastaArquivoXsdNFe; }
            set { _pastaArquivoXsdNFe = value; }
        }

        public String PastaArquivoXsdCTe
        {
            get { return _pastaArquivoXsdCTe; }
            set { _pastaArquivoXsdCTe = value; }
        }

        public String PastaNoMailBox
        {
            get { return _pastaNoMailBox; }
            set { _pastaNoMailBox = value; }
        }

        public String PastaBackupNoMailBox
        {
            get { return _pastaBackupNoMailBox; }
            set { _pastaBackupNoMailBox = value; }
        }

        public String PastaBackupDeArquivos
        {
            get { return _pastaBackupDeArquivos; }
            set { _pastaBackupDeArquivos = value; }
        }

        public String EnderecoEmail
        {
            get { return _enderecoEmail; }
            set { _enderecoEmail = value; }
        }

        public String EnderecoWebService
        {
            get { return _enderecoWebService; }
            set { _enderecoWebService = value; }
        }

        public String EnderecoSefaz
        {
            get { return _enderecoSefaz; }
            set { _enderecoSefaz = value; }
        }

        public String Usuario
        {
            get { return _usuario; }
            set { _usuario = value; }
        }

        public String Senha
        {
            get { return _senha; }
            set { _senha = value; }
        }

        public XmlDocument XmlDocTexto
        {
            get { return _xmlDocTexto; }
            set { _xmlDocTexto = value; }
        }

        public XDocument XmlXDocument
        {
            get { return _xmlXDocument; }
            set { _xmlXDocument = value; }
        }

        public int AmbienteSefaz
        {
            get { return _ambienteSefaz; }
            set { _ambienteSefaz = value; }
        }

        public int AmbienteWebService
        {
            get { return _ambienteWebService; }
            set { _ambienteWebService = value; }
        }

        public String NomeArquivo
        {
            get { return _nomeArquivo; }
            set { _nomeArquivo = value; }
        }

        public ValidaNFeXml oValidaNFeXml
        {
            get { return _validaNFeXml; }
            set { _validaNFeXml = value; }
        }

        public bool ValidaXmlEnvio(XmlDocument xml)
        {
            bool retorno = true;

            ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);

            this.SbErro = new StringBuilder();

            xml.Validate(eventHandler);

            if (SbErro.ToString() != String.Empty)
            {
                if (SbErro.ToString().Length > 3000)
                {
                    this.XmlNFeTmp.MENSAGENS = SbErro.ToString().Substring(1, 3000);
                }
                else
                {
                    this.XmlNFeTmp.MENSAGENS = SbErro.ToString();
                }
                this.XmlNFeTmp.ID_CONTROLE = -1;
                retorno = false;
            }

            return retorno;
        }

        public bool ValidaXmlNoXsd()
        {
            bool retorno = true;
            ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);

            this.SbErro = new StringBuilder();

            try
            {
                this.XmlDocTexto.Validate(eventHandler);

                if (SbErro.ToString() != String.Empty)
                {
                    if (SbErro.ToString().Length > 3000)
                        this.XmlNFeTmp.MENSAGENS = SbErro.ToString().Substring(1, 3000);
                    else
                        this.XmlNFeTmp.MENSAGENS = SbErro.ToString();

                    this.XmlNFeTmp.ID_CONTROLE = -1;
                    retorno = false;
                }
            }
            catch (Exception ex)
            {
                this.XmlNFeTmp.MENSAGENS = ex.Message.ToString();
                retorno = false;
            }

            return retorno;
        }

        public INT_NFE_XML_TMP XmlNFeTmp
        {
            get { return _xmlNFeTmp; }
            set { _xmlNFeTmp = value; }
        }

        public ABMC_RACTE_XML_TMP XmlCTeTmp
        {
            get { return _xmlCTeTmp; }
            set { _xmlCTeTmp = value; }
        }

        public bool GravaXmlTmp()//ref clsError Errors)
        {
            bool retorno = true;
            try
            {
                DAO_INT_NFE_XML_TMP.Salvar(this.XmlNFeTmp);
            }
            catch (Exception)
            {
                retorno = false;
            }

            return retorno;
        }

        public bool GravaXmlnoControle()
        {
            bool retorno = false;

            return retorno;
        }

        public void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    SbErro.Append("Erro : " + e.Message + "\r\n");
                    break;
                case XmlSeverityType.Warning:
                    SbErro.Append("Aviso: " + e.Message + "'\r\n");
                    break;
            }
        }


    }
}
