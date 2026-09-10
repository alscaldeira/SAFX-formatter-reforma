using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_EMIT
    {
        public static void Salvar(INT_NFE_NOTA_EMIT Emit)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (Emit.ID > 0)
                    {
                        db.Entry(Emit).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_EMIT.Add(Emit);
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