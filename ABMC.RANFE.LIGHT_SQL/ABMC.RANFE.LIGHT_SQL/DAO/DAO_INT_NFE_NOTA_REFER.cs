using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_REFER
    {
        public static void Salvar(INT_NFE_NOTA_REFER NotaRef)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaRef.ID > 0)
                    {
                        db.Entry(NotaRef).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_REFER.Add(NotaRef);
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