using System.Data;

namespace ABMC.RANFE.LIGHT_SQL.DAO
{
    public class DAO_SAFX
    {
        public static DataTable Consulta(string Comando)
        {
            DataTable dt = new DataTable();

            using (var db = new RanfeLight_Entities())
            {
                var conn = db.Database.Connection;
                var connectionState = conn.State;

                if (connectionState != ConnectionState.Open)
                    conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = Comando;// "SAFX07";
                    cmd.CommandType = CommandType.Text;

                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }

            return dt;
        }

        public static void ClearAllRecords()
        {
            try
            {
                using (var db = new RanfeLight_Entities())
                {
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_armas");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_combust");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_di");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_di_adi");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_cofins");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_icms");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_ii");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_ipi");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_issqn");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_imp_pis");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_medic");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha_veic");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_linha");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_avulsa");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_cana_deduc");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_cana_fordia");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_cobr_dup");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_cobr");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_dest");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_emit");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_entrega");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_inf_adic");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_inf_adic_cana");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_inf_adic_obscont");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_inf_adic_obsfisco");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_inf_adic_procref");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_retirada");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_totais");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_transp");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_transp_reboque");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_transp_vol");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_transp_vol_lac");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_nota_refer");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_xml");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_xml_tmp");
                    db.Database.ExecuteSqlCommand("TRUNCATE TABLE int_nfe_controle");
                }
            }
            catch
            { }
        }
    }
}