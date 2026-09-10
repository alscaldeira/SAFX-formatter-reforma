using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_LINHA_DI_ADI
    {
        public static void Salvar(INT_NFE_LINHA_DI_ADI LinhaDiAdi)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (LinhaDiAdi.ID > 0)
                    {
                        db.Entry(LinhaDiAdi).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_LINHA_DI_ADI.Add(LinhaDiAdi);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static INT_NFE_LINHA_DI_ADI RetornaLinhaDiAdiPorControleLinhaDiAdicao(INT_NFE_CONTROLE controle, INT_NFE_LINHA_DI linhadi, decimal p)
        {
            INT_NFE_LINHA_DI_ADI oTmp = new INT_NFE_LINHA_DI_ADI();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_LINHA_DI_ADI.Where(la => la.ID_CONTROLE == controle.ID && la.ID_DI == linhadi.ID && la.INFNFE_DET_PROD_DI_ADI_NAD == p).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }
            return oTmp;
        }
    }
}