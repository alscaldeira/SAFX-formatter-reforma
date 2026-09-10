using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_TRANSP
    {
        public static void Salvar(INT_NFE_NOTA_TRANSP NotaTransp)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaTransp.ID > 0)
                    {
                        db.Entry(NotaTransp).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_TRANSP.Add(NotaTransp);
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