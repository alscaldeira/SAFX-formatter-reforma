using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_CONTROLE
    {
        public static INT_NFE_CONTROLE RetornaControlePorChaveAcesso(String Chave)
        {
            INT_NFE_CONTROLE oTmp = new INT_NFE_CONTROLE();
            
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_CONTROLE.Where(t => t.INFNFE_ID == Chave).FirstOrDefault();
                }
            }
            catch
            { }
            
            return oTmp;
        }

        public static INT_NFE_CONTROLE RetornaControlePorID(int ID)
        {
            INT_NFE_CONTROLE oTmp = new INT_NFE_CONTROLE();
            
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    oTmp = db.INT_NFE_CONTROLE.Where(t => t.ID == ID).FirstOrDefault();
                }
            }
            catch
            { }
            
            return oTmp;
        }

        public static void Salvar(INT_NFE_CONTROLE Controle)
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    if (Controle.ID > 0)
                    {
                        db.Entry(Controle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.INT_NFE_CONTROLE.Add(Controle);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception)
            { }
        }

        public static List<INT_NFE_CONTROLE> RetornaControlePorStatus(string Status)
        {
            List<INT_NFE_CONTROLE> Lista = new List<INT_NFE_CONTROLE>();
            
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    var temp = db.INT_NFE_CONTROLE.SqlQuery("Select * from INT_NFE_CONTROLE");
                    var query = from p
                                in db.INT_NFE_CONTROLE.Where(x => x.STATUS == Status)
                                select p;
                    Lista = query.ToList();
                }
            }
            catch
            { }
            
            return Lista;
        }



    }
}