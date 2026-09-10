using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_IMP_PIS
    {
        public static void Salvar(INT_NFE_LINHA_IMP_PIS LinhaImpPis)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaImpPis.ID > 0)
                    {
                        db.Entry(LinhaImpPis).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_IMP_PIS.Add(LinhaImpPis);
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