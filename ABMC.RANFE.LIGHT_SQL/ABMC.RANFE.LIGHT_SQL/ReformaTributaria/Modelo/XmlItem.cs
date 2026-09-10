namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo
{
    /// <summary>Item (grupo det) de uma NF-e. NFS-e não tem itens — ver <see cref="XmlNota"/>.</summary>
    internal sealed class XmlItem
    {
        internal int NumItem;
        internal string CodProduto;
        internal string Descricao;
        internal string Ncm;
        internal string ExTipi;
        internal string Cfop;
        internal string UnidadeComercial;
        internal string UnidadeTributavel;
        internal double QtdComercial;
        internal double QtdTributavel;
        internal double VlUnitario;

        /// <summary>vProd — valor autoritativo do item; nunca recalculado a partir de qtd x unitário.</summary>
        internal double VlProduto;

        /// <summary>vItem quando presente (vProd + IPI etc.); cai para VlProduto quando ausente.</summary>
        internal double VlContabil;
        internal double VlDesconto;
        internal double VlFrete;
        internal double VlSeguro;
        internal double VlOutras;

        // ICMS
        internal string CstIcms;
        internal string OrigemIcms;
        internal double BaseIcms;
        internal double AliqIcms;
        internal double VlIcms;

        // IPI
        internal string CstIpi;
        internal string CodEnquadramentoIpi;
        internal double BaseIpi;
        internal double AliqIpi;
        internal double VlIpi;

        // Imposto de importação (grupo imposto/II — só existe em nota de importação)
        internal double BaseIi;
        internal double VlIi;
        internal double VlDespesasAduaneiras;
        internal double VlIof;

        // PIS / COFINS
        internal string CstPis;
        internal double BasePis;
        internal double AliqPis;
        internal double VlPis;
        internal string CstCofins;
        internal double BaseCofins;
        internal double AliqCofins;
        internal double VlCofins;

        /// <summary>Grupo det/prod/DI — presente apenas em notas de importação (CFOP 3xxx).</summary>
        internal XmlDi Di;

        /// <summary>Grupo IBSCBS do item (Reforma Tributária).</summary>
        internal XmlIbsCbs IbsCbs;
    }
}
