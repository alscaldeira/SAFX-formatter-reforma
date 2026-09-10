using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_IMP_COFINS
    {
        public static void Salvar(INT_NFE_LINHA_IMP_COFINS LinhaImpCofins)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaImpCofins.ID > 0)
                    {
                        db.Entry(LinhaImpCofins).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_IMP_COFINS.Add(LinhaImpCofins);
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