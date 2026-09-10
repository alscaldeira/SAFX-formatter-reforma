using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_IMP_ICMS
    {
        public static void Salvar(INT_NFE_LINHA_IMP_ICMS LinhaImpIcms)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaImpIcms.ID > 0)
                    {
                        db.Entry(LinhaImpIcms).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_IMP_ICMS.Add(LinhaImpIcms);
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