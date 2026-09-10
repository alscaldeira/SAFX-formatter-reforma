using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_INF_ADIC_OBSCONT
    {
        public static void Salvar(INT_NFE_NOTA_INF_ADIC_OBSCONT NotaInfAdicObs)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaInfAdicObs.ID > 0)
                    {
                        db.Entry(NotaInfAdicObs).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_INF_ADIC_OBSCONT.Add(NotaInfAdicObs);
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