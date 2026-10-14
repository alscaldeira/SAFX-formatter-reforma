package com.caldeira.service;

import java.io.BufferedWriter;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Gravação dos SAFX07/08/04/2013/2043 gerados direto do XML (NF-e e CT-e).
 *
 * A ORDEM das colunas é a do script SQL do sistema original
 * (ScriptSQL/SAFXnn.txt), extraída para safx-nfe-colunas.txt: é essa a ordem
 * das abas que o sistema em C# exporta. Cada registro é montado nome -> valor e
 * só aqui vira linha; coluna sem valor sai "@" (nulo do SAFX) e nome que não
 * existe na lista é erro de programação, não dado — estoura em vez de sumir.
 */
final class SafxSaida {

    static final String RECURSO_COLUNAS = "/safx-nfe-colunas.txt";

    private static Map<String, List<String>> colunas;

    private SafxSaida() {
    }

    static synchronized List<String> colunas(String layout) throws IOException {
        if (colunas == null) {
            Map<String, List<String>> lidas = new LinkedHashMap<>();
            try (InputStream in = SafxSaida.class.getResourceAsStream(RECURSO_COLUNAS)) {
                if (in == null) throw new IOException("Recurso " + RECURSO_COLUNAS + " não encontrado.");
                String texto = new String(in.readAllBytes(), StandardCharsets.UTF_8);
                for (String linha : texto.split("\\R")) {
                    int igual = linha.indexOf('=');
                    if (igual <= 0) continue;
                    List<String> nomes = new ArrayList<>();
                    for (String nome : linha.substring(igual + 1).split(",")) {
                        if (!nome.trim().isEmpty()) nomes.add(nome.trim());
                    }
                    lidas.put(linha.substring(0, igual).trim(), nomes);
                }
            }
            colunas = lidas;
        }
        List<String> nomes = colunas.get(layout);
        if (nomes == null) throw new IOException("Layout " + layout + " não declarado em " + RECURSO_COLUNAS);
        return nomes;
    }

    /** Grava layout.txt (TAB, "@" nos nulos) e layout.csv (";" com BOM, cabeçalho nomeado). */
    static void gravar(String layout, List<Map<String, String>> registros, List<String> avisos)
            throws IOException {
        List<String> nomes = colunas(layout);
        String dir = System.getProperty("user.dir");

        List<String[]> linhas = new ArrayList<>(registros.size());
        for (Map<String, String> registro : registros) {
            for (String chave : registro.keySet()) {
                if (!nomes.contains(chave)) {
                    throw new IllegalStateException("Coluna " + chave + " não existe no " + layout);
                }
            }
            String[] valores = new String[nomes.size()];
            for (int i = 0; i < valores.length; i++) {
                String texto = SafxLinha.limpar(registro.get(nomes.get(i)));
                // SUB_SERIE_DOCFIS não usa o sentinela "@": sem subsérie o campo sai vazio
                boolean vazioIntencional = "SUB_SERIE_DOCFIS".equals(nomes.get(i));
                valores[i] = texto == null || texto.isEmpty() ? (vazioIntencional ? "" : "@") : texto;
            }
            linhas.add(valores);
        }

        String txt = dir + "/" + layout + ".txt";
        try (BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(
                new FileOutputStream(txt), StandardCharsets.UTF_8))) {
            for (String[] valores : linhas) {
                writer.write(String.join("\t", valores));
                writer.newLine();
            }
        }

        String csv = dir + "/" + layout + ".csv";
        try (BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(
                new FileOutputStream(csv), StandardCharsets.UTF_8))) {
            writer.write('﻿');
            writer.write(linhaCsv(nomes.toArray(new String[0])));
            writer.newLine();
            for (String[] valores : linhas) {
                writer.write(linhaCsv(valores));
                writer.newLine();
            }
        }
        System.out.println("Arquivo " + layout + " gerado: " + txt + " (" + registros.size() + " registros)");
    }

    private static String linhaCsv(String[] campos) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < campos.length; i++) {
            if (i > 0) sb.append(';');
            sb.append(SafxCsvSupport.celulaCsv(campos[i]));
        }
        return sb.toString();
    }
}
