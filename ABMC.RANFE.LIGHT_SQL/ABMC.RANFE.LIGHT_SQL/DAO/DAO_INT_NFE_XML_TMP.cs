using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_XML_TMP
    {
        public static INT_NFE_XML_TMP RetornaXmlPorNomeArq(String nomeArq)
        {
            INT_NFE_XML_TMP oTmp = new INT_NFE_XML_TMP();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_XML_TMP.Where(t => t.XML_ARQUIVO == nomeArq).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }
            return oTmp;
        }

        public static void Exclui(INT_NFE_XML_TMP XmlTemp)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    INT_NFE_XML_TMP oTmp = new INT_NFE_XML_TMP();
                    oTmp = db.INT_NFE_XML_TMP.Where(x => x.ID == XmlTemp.ID).FirstOrDefault();

                    db.INT_NFE_XML_TMP.Remove(oTmp);

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                //Errors.ErroNumber = 100;
                //Errors.Description = "Erro no método DAL_XmlTmp.Exclui do tipo " + ex.Message;
            }
        }

        public static void Salvar(INT_NFE_XML_TMP XMLTmp)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (XMLTmp.ID > 0)
                    {
                        db.Entry(XMLTmp).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_XML_TMP.Add(XMLTmp);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}