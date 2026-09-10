using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_IMP_IPI
    {
        public static void Salvar(INT_NFE_LINHA_IMP_IPI LinhaImpIpi)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaImpIpi.ID > 0)
                    {
                        db.Entry(LinhaImpIpi).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_IMP_IPI.Add(LinhaImpIpi);
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