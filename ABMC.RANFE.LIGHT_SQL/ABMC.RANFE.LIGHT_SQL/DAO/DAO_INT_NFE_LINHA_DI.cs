using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_DI
    {
        public static void Salvar(INT_NFE_LINHA_DI LinhaDi)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaDi.ID > 0)
                    {
                        db.Entry(LinhaDi).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_DI.Add(LinhaDi);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static INT_NFE_LINHA_DI RetornaLinhaDiPorControleNrItemNrItemDI(INT_NFE_CONTROLE controle, INT_NFE_LINHA linha, string NrDi)
        {
            INT_NFE_LINHA_DI oTmp = new INT_NFE_LINHA_DI();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_LINHA_DI.Where(ld => ld.ID_CONTROLE == controle.ID && ld.ID_LINHA == linha.ID && ld.INFNFE_DET_PROD_DI_NDI == NrDi).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }
            return oTmp;
        }
    }
}