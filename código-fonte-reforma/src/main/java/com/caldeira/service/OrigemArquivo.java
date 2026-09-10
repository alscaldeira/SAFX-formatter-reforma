package com.caldeira.service;

import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Descoberta automática do tipo de origem, para que a interface não precise
 * perguntar "é arquivo ou pasta?".
 *
 * A decisão é por CONTEÚDO, não por extensão: pasta é lote de XML; arquivo que
 * começa com "&lt;" é XML; arquivo cuja primeira linha útil começa com "|" (ou
 * contém o registro |0000|) é SPED. Extensão só serve de desempate na mensagem
 * de erro, porque arquivo fiscal chega com todo tipo de nome.
 */
final class OrigemArquivo {

    private OrigemArquivo() {
    }

    static Conversao.Origem detectar(String caminho) throws IOException {
        Path p = Paths.get(caminho);
        if (Files.isDirectory(p)) {
            return Conversao.Origem.XML;
        }
        if (!Files.isRegularFile(p)) {
            throw new IOException("Caminho não encontrado: " + caminho);
        }

        String inicio = primeirosCaracteres(p, 4096);
        String util = inicio.replace("﻿", "").trim();

        if (util.isEmpty()) {
            throw new IOException("O arquivo " + p.getFileName() + " está vazio.");
        }
        if (util.charAt(0) == '<') {
            return Conversao.Origem.XML;
        }
        if (util.charAt(0) == '|' || util.contains("|0000|")) {
            return Conversao.Origem.SPED;
        }
        throw new IOException("Não foi possível identificar o arquivo " + p.getFileName()
                + ".\n\nEscolha um arquivo SPED (linhas delimitadas por \"|\", começando por |0000|), "
                + "um XML de NF-e/NFS-e, ou uma pasta com XMLs.\n\nO conteúdo começa com: \""
                + resumo(util) + "\"");
    }

    private static String primeirosCaracteres(Path arquivo, int quantidade) throws IOException {
        byte[] buffer = new byte[quantidade];
        int lidos;
        try (InputStream in = Files.newInputStream(arquivo)) {
            lidos = in.readNBytes(buffer, 0, quantidade);
        }
        // ISO-8859-1 aceita qualquer byte: aqui só interessam os caracteres de controle
        return new String(buffer, 0, Math.max(lidos, 0), StandardCharsets.ISO_8859_1);
    }

    private static String resumo(String texto) {
        String uma = texto.split("\\R", 2)[0];
        return uma.length() > 40 ? uma.substring(0, 40) + "..." : uma;
    }
}
