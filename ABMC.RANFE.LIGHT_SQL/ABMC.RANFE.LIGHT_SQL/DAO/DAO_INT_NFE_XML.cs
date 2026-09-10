using System;
using System.Linq;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_XML
    {
        public static INT_NFE_XML RetornoXmlPorControle(INT_NFE_CONTROLE controle)
        {
            INT_NFE_XML oTmp = new INT_NFE_XML();

            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_XML.Where(t => t.ID_CONTROLE == controle.ID).FirstOrDefault();
                }
            }
            catch
            { }
            
            return oTmp;
        }

        public static void Salvar(INT_NFE_XML NfeXml)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NfeXml.ID > 0)
                    {
                        db.Entry(NfeXml).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_XML.Add(NfeXml);
                    }

                    db.SaveChanges();
                }
            }
            catch
            { }
        }
    }
}