package com.caldeira.service;

import java.io.IOException;
import java.util.Map;

/**
 * De-para de unidade de medida do XML para a unidade cadastrada no MasterSAF.
 *
 * Existe porque a NF-e pode escrever a mesma unidade de duas formas no mesmo
 * item (uCom = "TO" e uTrib = "TON"), e os campos 17 (COD_UND_PADRAO) e 25
 * (COD_MEDIDA) do SAFX08 são campos-chave: divergência ali vira item duplicado.
 * A conversão usa sempre uCom, normalizado por esta tabela.
 */
final class Unidades {

    static final String NOME_ARQUIVO = "unidades.properties";

    private static Map<String, String> bruto;
    private static Map<String, String> mapa;

    private Unidades() {
    }

    static String normalizar(String unidade) throws IOException {
        if (unidade == null) return "";
        String chave = unidade.trim().toUpperCase();
        if (chave.isEmpty()) return "";
        String convertida = carregar().get(chave);
        return convertida != null ? convertida : chave;
    }

    private static synchronized Map<String, String> carregar() throws IOException {
        Map<String, String> atual = CadastroProperties.carregar(NOME_ARQUIVO, false);
        if (atual == bruto) return mapa;

        java.util.Map<String, String> normalizado = new java.util.LinkedHashMap<>();
        for (Map.Entry<String, String> e : atual.entrySet()) {
            normalizado.put(e.getKey().trim().toUpperCase(), e.getValue().trim().toUpperCase());
        }
        bruto = atual;
        mapa = normalizado;
        return mapa;
    }
}
