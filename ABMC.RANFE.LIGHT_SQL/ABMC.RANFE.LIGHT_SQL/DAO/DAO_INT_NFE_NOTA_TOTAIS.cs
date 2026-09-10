using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_NOTA_TOTAIS
    {
        public static INT_NFE_NOTA_TOTAIS RetornoPorIdControle(int IdControle)
        {
            INT_NFE_NOTA_TOTAIS oTmp = new INT_NFE_NOTA_TOTAIS();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_NOTA_TOTAIS.Where(t => t.ID_CONTROLE == IdControle).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }
            return oTmp;
        }

        public static void Salvar(INT_NFE_NOTA_TOTAIS Totais)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (Totais.ID > 0)
                    {
                        db.Entry(Totais).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_NOTA_TOTAIS.Add(Totais);
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