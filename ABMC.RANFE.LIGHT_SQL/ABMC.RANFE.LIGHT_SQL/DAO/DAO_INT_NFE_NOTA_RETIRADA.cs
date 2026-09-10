using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_RETIRADA
    {
        public static void Salvar(INT_NFE_NOTA_RETIRADA NotaRet)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaRet.ID > 0)
                    {
                        db.Entry(NotaRet).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_RETIRADA.Add(NotaRet);
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