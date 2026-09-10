namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo
{
    /// <summary>
    /// Grupo IBS/CBS da Reforma Tributária, tal como aparece na NF-e 4.00 (grupos
    /// det/imposto/IBSCBS e total/IBSCBSTot) e na NFS-e nacional 1.01 (grupos
    /// infNFSe/IBSCBS e infDPS/IBSCBS).
    ///
    /// A mesma classe serve para capa e item — cada origem preenche o que tem.
    /// </summary>
    internal sealed class XmlIbsCbs
    {
        // --- identificação / classificação ---
        internal string Cst;
        internal string ClassificacaoTributaria;

        /// <summary>NFS-e: finNFSe, cIndOp e indDest do grupo IBSCBS do DPS.</summary>
        internal string FinalidadeNfse;
        internal string CodIndicadorOperacao;
        internal string IndicadorDestinatario;
        internal string CodLocalidadeIncidencia;
        internal string NomeLocalidadeIncidencia;

        // --- base de cálculo ---
        internal double BaseCalculo;

        // --- IBS estadual ---
        internal double AliqIbsUf;
        internal double AliqReducaoIbsUf;
        internal double AliqEfetivaIbsUf;
        internal double VlIbsUf;
        internal double VlDiferimentoIbsUf;
        internal double VlDevolucaoTributoIbsUf;

        // --- IBS municipal ---
        internal double AliqIbsMun;
        internal double AliqReducaoIbsMun;
        internal double AliqEfetivaIbsMun;
        internal double VlIbsMun;
        internal double VlDiferimentoIbsMun;
        internal double VlDevolucaoTributoIbsMun;

        // --- IBS total ---
        internal double VlIbs;
        internal double VlCreditoPresumidoIbs;
        internal double VlCreditoPresumidoCondSuspensaoIbs;

        // --- CBS ---
        internal double AliqCbs;
        internal double AliqReducaoCbs;
        internal double AliqEfetivaCbs;
        internal double VlCbs;
        internal double VlDiferimentoCbs;
        internal double VlDevolucaoTributoCbs;
        internal double VlCreditoPresumidoCbs;
        internal double VlCreditoPresumidoCondSuspensaoCbs;

        /// <summary>Total do documento sob o novo regime (vNFTot da NF-e, vTotNF da NFS-e).</summary>
        internal double VlTotalDocumento;

        /// <summary>true quando o grupo trouxe alguma informação — decide se gera registro.</summary>
        internal bool TemDados()
        {
            return BaseCalculo != 0 || VlIbs != 0 || VlCbs != 0 || VlIbsUf != 0 || VlIbsMun != 0
                   || VlTotalDocumento != 0 || !string.IsNullOrEmpty(Cst);
        }
    }
}
