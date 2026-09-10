using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_COMBUST
    {
        public static void Salvar(INT_NFE_LINHA_COMBUST LinhaComb)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaComb.ID > 0)
                    {
                        db.Entry(LinhaComb).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_COMBUST.Add(LinhaComb);
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