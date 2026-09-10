using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_INF_ADIC_CANA
    {
        public static void Salvar(INT_NFE_NOTA_INF_ADIC_CANA NotaInfAdicCana)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaInfAdicCana.ID > 0)
                    {
                        db.Entry(NotaInfAdicCana).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_INF_ADIC_CANA.Add(NotaInfAdicCana);
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