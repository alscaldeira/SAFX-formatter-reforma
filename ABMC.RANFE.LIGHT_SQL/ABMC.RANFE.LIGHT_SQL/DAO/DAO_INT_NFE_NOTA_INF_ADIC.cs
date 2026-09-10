using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_INF_ADIC
    {
        public static void Salvar(INT_NFE_NOTA_INF_ADIC NotaInfAdic)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaInfAdic.ID > 0)
                    {
                        db.Entry(NotaInfAdic).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_INF_ADIC.Add(NotaInfAdic);
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