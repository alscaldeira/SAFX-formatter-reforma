package com.caldeira.service;

import java.util.ArrayList;
import java.util.List;

/**
 * Modelo neutro de um documento fiscal lido de XML — serve tanto para NF-e
 * (modelo 55) quanto para NFS-e nacional. É deliberadamente independente do
 * layout SAFX: o {@link XmlLeitor} preenche, os geradores XmlToSafxNN leem.
 *
 * As datas já chegam aqui no formato SAFX (YYYYMMDD) e as horas em HHMMSS,
 * porque a conversão a partir do formato ISO do XML é responsabilidade do leitor.
 */
class XmlNota {

    enum Origem { NFE, NFSE }

    /** Papéis do domínio de IND_FIS_JUR (SAFX04 campo 1 / SAFX07 campo 6). */
    static final String PAPEL_FORNECEDOR = "1";
    static final String PAPEL_CLIENTE = "2";
    static final String PAPEL_ESTABELECIMENTO = "3";
    static final String PAPEL_TRANSPORTADORA = "4";
    static final String PAPEL_MISTO = "5";

    Origem origem;
    String arquivo;

    /** CNPJ do estabelecimento da empresa (de-para em {@link Estabelecimentos}). */
    String cnpjEstab;

    /** Contraparte do documento — cliente, fornecedor ou o próprio estabelecimento. */
    XmlParticipante participante;
    /** Transportadora, quando o XML traz o grupo transp/transporta. */
    XmlParticipante transportadora;

    // --- identificação da capa ---
    String movtoES;
    String normDev;
    String codDocto;
    String codClassDocFis;
    String codModelo;
    String situacao;
    String numDoc;
    String serie;
    String chave;
    String dvChave;
    String dtEmissao;
    String horaEmissao;
    String dtSaidaEnt;
    String dtAutenticacao;
    String natOp;
    String cfop;
    String indTpFrete;
    String indFatura;
    String finalidade;
    String observacao;
    /** ide/indFinal, indPres e indIntermed — usados pelos layouts da Reforma. */
    String indicadorConsumidorFinal;
    String indicadorPresenca;
    String indicadorIntermediador;
    /** Chave do DF-e referenciado, quando existe exatamente uma. */
    String chaveReferenciada;

    /** Documento referenciado: só preenchido quando existe exatamente um. */
    String numDocRef;
    String serieDocRef;
    /** Quantidade de refNFe encontradas (para o aviso quando for > 1). */
    int qtdReferencias;

    // --- valores da capa ---
    double vlProdutos;
    double vlTotal;
    double vlFrete;
    double vlSeguro;
    double vlOutras;
    double vlDesconto;
    double vlIcms;
    double baseIcms;
    double vlIpi;
    double baseIpi;
    double vlPis;
    double basePis;
    double vlCofins;
    double baseCofins;
    double aliqPis;
    double aliqCofins;
    double vlIpiDevolvido;

    // --- valores de serviço (NFS-e) ---
    double vlServico;
    double baseIss;
    double aliqIss;
    double vlIss;
    double vlIssRetido;
    double vlIr;
    double vlCsll;
    double vlInss;
    String codMunicipioIss;
    String numRps;
    String serieRps;
    String dtRps;
    String descricaoServico;
    String codVerificacao;
    String codTributacaoNacional;

    /**
     * Grupo IBS/CBS da Reforma Tributária. Nenhum campo dos layouts 04/07/08/49
     * o recebe — é lido para que os futuros geradores 3007/3008/3042/3043 não
     * precisem reprocessar os XMLs.
     */
    XmlIbsCbs ibsCbs;

    List<XmlItem> itens = new ArrayList<>();

    boolean temImportacao() {
        for (XmlItem item : itens) {
            if (item.di != null) return true;
        }
        return false;
    }
}
