using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_ARMAS
    {
        public static void Salvar(INT_NFE_LINHA_ARMAS LinhaArmas)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaArmas.ID > 0)
                    {
                        db.Entry(LinhaArmas).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_ARMAS.Add(LinhaArmas);
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