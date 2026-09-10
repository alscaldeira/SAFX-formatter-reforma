using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_ABMC_RACTE_XML_TMP
    {
        public static void Salvar(ABMC_RACTE_XML_TMP XMLTmp)
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
                        db.ABMC_RACTE_XML_TMP.Add(XMLTmp);
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