package com.caldeira.service;

/**
 * Grupo IBS/CBS da Reforma Tributária, tal como aparece na NF-e 4.00 (grupos
 * {@code det/imposto/IBSCBS} e {@code total/IBSCBSTot}) e na NFS-e nacional
 * 1.01 (grupos {@code infNFSe/IBSCBS} e {@code infDPS/IBSCBS}).
 *
 * A mesma classe serve para capa e item — cada origem preenche o que tem. Os
 * campos de Imposto Seletivo ainda não aparecem em nenhum XML de exemplo e por
 * isso não foram modelados.
 */
class XmlIbsCbs {

    // --- identificação / classificação ---
    String cst;
    String classificacaoTributaria;
    /** NFS-e: finNFSe, cIndOp e indDest do grupo IBSCBS do DPS. */
    String finalidadeNfse;
    String codIndicadorOperacao;
    String indicadorDestinatario;
    String codLocalidadeIncidencia;
    String nomeLocalidadeIncidencia;

    // --- base de cálculo ---
    double baseCalculo;

    // --- IBS estadual ---
    double aliqIbsUf;
    double aliqReducaoIbsUf;
    double aliqEfetivaIbsUf;
    double vlIbsUf;
    double vlDiferimentoIbsUf;
    double vlDevolucaoTributoIbsUf;

    // --- IBS municipal ---
    double aliqIbsMun;
    double aliqReducaoIbsMun;
    double aliqEfetivaIbsMun;
    double vlIbsMun;
    double vlDiferimentoIbsMun;
    double vlDevolucaoTributoIbsMun;

    // --- IBS total ---
    double vlIbs;
    double vlCreditoPresumidoIbs;
    double vlCreditoPresumidoCondSuspensaoIbs;

    // --- CBS ---
    double aliqCbs;
    double aliqReducaoCbs;
    double aliqEfetivaCbs;
    double vlCbs;
    double vlDiferimentoCbs;
    double vlDevolucaoTributoCbs;
    double vlCreditoPresumidoCbs;
    double vlCreditoPresumidoCondSuspensaoCbs;

    /** Total do documento sob o novo regime (vNFTot da NF-e, vTotNF da NFS-e). */
    double vlTotalDocumento;

    /** true quando o grupo trouxe alguma informação — decide se gera registro. */
    boolean temDados() {
        return baseCalculo != 0 || vlIbs != 0 || vlCbs != 0 || vlIbsUf != 0 || vlIbsMun != 0
                || vlTotalDocumento!= 0 || (cst != null && !cst.isEmpty());
    }
}
