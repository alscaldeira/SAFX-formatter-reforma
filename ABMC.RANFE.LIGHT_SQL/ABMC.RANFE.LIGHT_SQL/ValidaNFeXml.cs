using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ABMC.RANFE.LIGHT_SQL
{
    [Serializable()]
    [XmlRoot(ElementName = "consSitNFe", Namespace = "http://www.portalfiscal.inf.br/nfe")]

    public class ValidaNFeXml
    {
        #region Construtor
        public ValidaNFeXml()
        {
            Versao = "2.00";// Versoes.Nfe;
        }
        #endregion

        #region Versao
        [XmlAttribute(AttributeName = "versao")]
        public string Versao { get; set; }
        #endregion

        #region TipoAmbiente
        [XmlElement(ElementName = "tpAmb")]
        public string TipoAmbiente { get; set; }
        #endregion

        #region TpServico
        [XmlElement(ElementName = "xServ")]
        public string TpServico { get; set; }
        #endregion

        #region ChaveNFe
        [XmlElement(ElementName = "chNFe")]
        public string ChaveNFe { get; set; }
        #endregion
    }
}
