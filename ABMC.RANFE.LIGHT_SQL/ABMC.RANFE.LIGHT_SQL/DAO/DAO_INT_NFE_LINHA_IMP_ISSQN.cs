using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_IMP_ISSQN
    {
        public static void Salvar(INT_NFE_LINHA_IMP_ISSQN LinhaImpIssqn)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaImpIssqn.ID > 0)
                    {
                        db.Entry(LinhaImpIssqn).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_IMP_ISSQN.Add(LinhaImpIssqn);
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