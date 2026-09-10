package com.caldeira.service;

/**
 * Montagem da linha SAFX: campos separados por TAB, truncada no tamanho exato
 * do leiaute. Cada arquivo SAFXnn.TXT contém apenas registros do seu próprio
 * tipo — não há coluna de tipo de registro nem prefixo por linha.
 */
final class SafxLinha {

    private SafxLinha() {
    }

    /**
     * Neutraliza os caracteres que quebrariam o arquivo: o TAB é o delimitador
     * de campo e o LF/CR encerra o registro. Texto vindo de XML (infCpl, xProd,
     * xDescServ) chega com os dois — no SPED isso não acontecia porque o
     * delimitador de origem é o pipe.
     */
    static String limpar(String valor) {
        if (valor == null) return null;
        String limpo = valor.replace('\t', ' ').replace('\r', ' ').replace('\n', ' ');
        return limpo.replaceAll(" {2,}", " ").trim();
    }

    static String montar(String[] campos, int quantidade) {
        StringBuilder sb = new StringBuilder(campos[0]);
        for (int i = 1; i < quantidade; i++) {
            sb.append('\t').append(campos[i]);
        }
        return sb.toString();
    }
}
