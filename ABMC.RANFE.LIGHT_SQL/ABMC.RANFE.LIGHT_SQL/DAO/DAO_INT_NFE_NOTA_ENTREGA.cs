using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_ENTREGA
    {
        public static void Salvar(INT_NFE_NOTA_ENTREGA NotaEnt)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaEnt.ID > 0)
                    {
                        db.Entry(NotaEnt).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_ENTREGA.Add(NotaEnt);
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