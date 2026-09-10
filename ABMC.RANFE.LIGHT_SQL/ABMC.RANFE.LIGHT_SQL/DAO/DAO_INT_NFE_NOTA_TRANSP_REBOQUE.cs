using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_TRANSP_REBOQUE
    {
        public static void Salvar(INT_NFE_NOTA_TRANSP_REBOQUE NotaTransReb)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (NotaTransReb.ID > 0)
                    {
                        db.Entry(NotaTransReb).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_TRANSP_REBOQUE.Add(NotaTransReb);
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