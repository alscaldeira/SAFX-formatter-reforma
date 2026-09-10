package com.caldeira.service;

/** Item (grupo det) de uma NF-e. NFS-e não tem itens — ver {@link XmlNota}. */
class XmlItem {

    int numItem;
    String codProduto;
    String descricao;
    String ncm;
    String exTipi;
    String cfop;
    String unidadeComercial;
    String unidadeTributavel;
    double qtdComercial;
    double qtdTributavel;
    double vlUnitario;
    /** vProd — valor autoritativo do item; nunca recalculado a partir de qtd x unitário. */
    double vlProduto;
    /** vItem quando presente (vProd + IPI etc.); cai para vlProduto quando ausente. */
    double vlContabil;
    double vlDesconto;
    double vlFrete;
    double vlSeguro;
    double vlOutras;

    // ICMS
    String cstIcms;
    String origemIcms;
    double baseIcms;
    double aliqIcms;
    double vlIcms;

    // IPI
    String cstIpi;
    String codEnquadramentoIpi;
    double baseIpi;
    double aliqIpi;
    double vlIpi;

    // Imposto de importação (grupo imposto/II — só existe em nota de importação)
    double baseIi;
    double vlIi;
    double vlDespesasAduaneiras;
    double vlIof;

    // PIS / COFINS
    String cstPis;
    double basePis;
    double aliqPis;
    double vlPis;
    String cstCofins;
    double baseCofins;
    double aliqCofins;
    double vlCofins;

    /** Grupo det/prod/DI — presente apenas em notas de importação (CFOP 3xxx). */
    XmlDi di;

    /** Grupo IBSCBS do item (Reforma Tributária) — lido, não gravado nos SAFX 04/07/08/49. */
    XmlIbsCbs ibsCbs;
}
