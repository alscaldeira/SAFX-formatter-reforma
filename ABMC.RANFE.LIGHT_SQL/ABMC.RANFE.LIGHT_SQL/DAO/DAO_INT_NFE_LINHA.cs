using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA
    {
        public static INT_NFE_LINHA RetornoPorIdControleItem(int IdControle,int IdItem)
        {
            INT_NFE_LINHA oTmp = new INT_NFE_LINHA();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_LINHA.Where(t => t.ID_CONTROLE == IdControle && t.INFNFE_DET_NITEM == IdItem).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }
            return oTmp;
        }

        public static void Salvar(INT_NFE_LINHA Linha)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (Linha.ID > 0)
                    {
                        db.Entry(Linha).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA.Add(Linha);
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