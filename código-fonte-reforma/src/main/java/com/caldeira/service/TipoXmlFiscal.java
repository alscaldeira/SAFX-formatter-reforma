package com.caldeira.service;

/**
 * Tipo fiscal de um XML do lote, deduzido do próprio arquivo (e não do campo
 * de upload de onde veio).
 */
enum TipoXmlFiscal {
    /** XML ilegível, ou fiscal mas de um tipo que o conversor não exporta (evento, cancelamento, inutilização). */
    DESCONHECIDO,
    NFE,
    CTE,
    /** NFS-e Nacional (raiz NFSe): fonte do SAFX3009 na Reforma. */
    NFSE
}
