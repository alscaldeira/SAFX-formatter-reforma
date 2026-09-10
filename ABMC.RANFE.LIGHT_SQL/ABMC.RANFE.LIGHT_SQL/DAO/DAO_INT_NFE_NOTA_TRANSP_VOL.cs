using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_TRANSP_VOL
    {
        public static void Salvar(INT_NFE_NOTA_TRANSP_VOL NotaTanspVol)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaTanspVol.ID > 0)
                    {
                        db.Entry(NotaTanspVol).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_TRANSP_VOL.Add(NotaTanspVol);
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