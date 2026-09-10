using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_MEDIC
    {
        public static void Salvar(INT_NFE_LINHA_MEDIC LinhaMedic)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaMedic.ID > 0)
                    {
                        db.Entry(LinhaMedic).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_MEDIC.Add(LinhaMedic);
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