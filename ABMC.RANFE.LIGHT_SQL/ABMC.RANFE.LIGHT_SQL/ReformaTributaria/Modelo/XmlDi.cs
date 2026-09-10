namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo
{
    /// <summary>Declaração de importação (det/prod/DI da NF-e).</summary>
    internal sealed class XmlDi
    {
        internal string Numero;
        internal string DataRegistro;
        internal string DataDesembaraco;
        internal string UfDesembaraco;
        internal string LocalDesembaraco;
        internal string CodExportador;
        internal string TipoViaTransporte;
        internal string TipoIntermedio;
        internal double VlAfrmm;

        /// <summary>Primeira adição — o registro de importação é por item, não por adição.</summary>
        internal string NumeroAdicao;
        internal string NumeroDrawback;
        internal string CodFabricante;
        internal double VlDescontoAdicao;
    }
}
