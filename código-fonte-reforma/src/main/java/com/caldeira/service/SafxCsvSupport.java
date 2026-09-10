package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

/**
 * Suporte compartilhado pelos conversores SAFXnn -> CSV. Lê um SAFXnn.txt
 * (TAB-delimitado, gerado pelos conversores SpedToSafxNN em user.dir) e grava
 * um CSV equivalente, usando os nomes de campo do leiaute (safx-layout.json,
 * mesma ordem de posição) como cabeçalho. Os valores são copiados como estão
 * no SAFX de origem — inclusive "@" — para preservar fidelidade ao arquivo
 * que seria importado no MasterSAF; não há reformatação de número/data aqui.
 */
class SafxCsvSupport {

    static void converter(String nomeArquivoSafx, String[] cabecalho) throws IOException {
        String dir = System.getProperty("user.dir");
        Path entrada = Paths.get(dir, nomeArquivoSafx + ".txt");
        String saida = dir + "/" + nomeArquivoSafx + ".csv";

        if (!Files.exists(entrada)) {
            throw new FileNotFoundException(
                    "Arquivo " + entrada + " não encontrado. Rode a conversão SPED -> "
                            + nomeArquivoSafx + " antes de gerar o CSV.");
        }

        List<String> linhas = ArquivoTexto.linhas(entrada);

        try (BufferedWriter writer = new BufferedWriter(
                new OutputStreamWriter(new FileOutputStream(saida), Charset.forName("UTF-8")))) {
            // BOM UTF-8 + ";" como delimitador: mesma convenção do IMPORTACAO.csv,
            // para abrir corretamente no Excel em português (vírgula é decimal, não delimitador).
            writer.write('﻿');
            writer.write(linhaCsv(cabecalho));
            writer.newLine();

            int numLinha = 0;
            for (String linha : linhas) {
                numLinha++;
                if (linha.isEmpty()) continue;
                String[] campos = linha.split(Pattern.quote("\t"), -1);
                if (campos.length != cabecalho.length) {
                    throw new IOException(nomeArquivoSafx + ".txt linha " + numLinha + ": "
                            + campos.length + " campos, esperado " + cabecalho.length
                            + " (arquivo e leiaute desalinhados)");
                }
                writer.write(linhaCsv(campos));
                writer.newLine();
            }
        }

        System.out.println("Arquivo " + nomeArquivoSafx + ".csv gerado: " + saida);
    }

    private static String linhaCsv(String[] campos) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < campos.length; i++) {
            if (i > 0) sb.append(';');
            sb.append(celulaCsv(campos[i]));
        }
        return sb.toString();
    }

    /**
     * Uma célula do CSV, já protegida contra a conversão automática do Excel.
     *
     * Campos como CNPJ, CPF, número de documento e os valores zerados à esquerda
     * do SAFX são texto no leiaute, mas o Excel os lê como número: 07060718000112
     * viraria 7060718000112 (CNPJ inválido) e um valor de 17 dígitos viraria
     * notação científica. A fórmula ="..." força a leitura como texto sem alterar
     * o conteúdo do campo.
     */
    static String celulaCsv(String valor) {
        if (valor == null) valor = "";
        if (numeroQueOExcelDeturpa(valor)) {
            return "\"=\"\"" + valor + "\"\"\"";
        }
        if (valor.contains(";") || valor.contains("\"") || valor.contains("\n")) {
            return "\"" + valor.replace("\"", "\"\"") + "\"";
        }
        return valor;
    }

    /**
     * Só dígitos e: com zero à esquerda, com cara de documento (CPF/CNPJ e
     * códigos derivados deles, de 11 dígitos para cima) ou grande demais para o
     * Excel (15 dígitos significativos). O CNPJ é alfanumérico no leiaute —
     * 46754545000194 lido como número perde essa natureza e volta do Excel como
     * 4,67545E+13.
     */
    private static boolean numeroQueOExcelDeturpa(String valor) {
        if (valor.length() < 2 || !valor.matches("\\d+")) return false;
        return valor.charAt(0) == '0' || valor.length() >= 11;
    }
}
