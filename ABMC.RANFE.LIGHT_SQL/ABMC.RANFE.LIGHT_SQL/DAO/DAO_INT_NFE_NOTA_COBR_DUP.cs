using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_COBR_DUP
    {
        public static void Salvar(INT_NFE_NOTA_COBR_DUP NotaCobDup)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaCobDup.ID > 0)
                    {
                        db.Entry(NotaCobDup).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_COBR_DUP.Add(NotaCobDup);
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