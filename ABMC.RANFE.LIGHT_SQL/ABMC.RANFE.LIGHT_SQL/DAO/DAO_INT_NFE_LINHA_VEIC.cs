using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_VEIC
    {
        public static void Salvar(INT_NFE_LINHA_VEIC LinhaVeic)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaVeic.ID > 0)
                    {
                        db.Entry(LinhaVeic).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_VEIC.Add(LinhaVeic);
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