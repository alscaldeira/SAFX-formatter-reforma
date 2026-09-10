using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_CANA_DEDUC
    {
        public static void Salvar(INT_NFE_NOTA_CANA_DEDUC NotaCana)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaCana.ID > 0)
                    {
                        db.Entry(NotaCana).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_CANA_DEDUC.Add(NotaCana);
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