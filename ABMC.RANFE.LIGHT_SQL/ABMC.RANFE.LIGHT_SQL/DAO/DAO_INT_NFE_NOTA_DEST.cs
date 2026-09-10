using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_DEST
    {
        public static void Salvar(INT_NFE_NOTA_DEST NotaDest)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaDest.ID > 0)
                    {
                        db.Entry(NotaDest).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_DEST.Add(NotaDest);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (ex is DbEntityValidationException)
                { 
                
                }
            }
        }
    }
}