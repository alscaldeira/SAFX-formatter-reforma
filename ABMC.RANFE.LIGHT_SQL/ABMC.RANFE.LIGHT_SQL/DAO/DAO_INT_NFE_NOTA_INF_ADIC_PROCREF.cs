using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_INF_ADIC_PROCREF
    {
        public static void Salvar(INT_NFE_NOTA_INF_ADIC_PROCREF NotaInf)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaInf.ID > 0)
                    {
                        db.Entry(NotaInf).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_INF_ADIC_PROCREF.Add(NotaInf);
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