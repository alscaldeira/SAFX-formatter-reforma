using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_INT_NFE_ORIGEM_XML
    {
        public static List<INT_NFE_ORIGEM_XML> GetByTipoAcesso(string Tipo)
        {
            List<INT_NFE_ORIGEM_XML> Lista = new List<INT_NFE_ORIGEM_XML>();
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    var query = from p in db.INT_NFE_ORIGEM_XML.Where(x => x.TIPO_ACESSO.Equals(Tipo))
                                select p;
                    Lista = query.ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return Lista;
        }

    }
}