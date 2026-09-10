package com.caldeira.service;

import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.Reader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;
import java.util.Map;

/**
 * Descritor dos layouts da Reforma Tributária (SAFX3007/3008/3009), lido de
 * "safx-reforma-layout.json".
 *
 * A conversão é guiada por NOME de campo, não por posição fixa no código: a
 * ordem sai do descritor. Isso existe porque as planilhas oficiais de leiaute
 * (130, 88 e 64 campos) ainda não estão no projeto — quando chegarem, basta
 * completar o JSON e marcar "ordemConfirmada": true, sem tocar em Java.
 *
 * Como em {@link CadastroProperties}, um arquivo de mesmo nome ao lado do
 * programa (user.dir) tem prioridade sobre o embutido em resources.
 */
final class LayoutReforma {

    static final String NOME_ARQUIVO = "safx-reforma-layout.json";

    static final class Campo {
        final String nome;
        final String origem;
        /** "N" para numérico, "D" para data, "A" para alfanumérico. */
        final String tipo;
        /** Total de dígitos do campo: "015V002" -> 17; "003V004" -> 7. */
        final int digitos;
        /** Casas decimais implícitas: "015V002" -> 2; -1 quando não é decimal. */
        final int decimais;

        Campo(String nome, String origem, String tipo, String tamanho) {
            this.nome = nome;
            this.origem = origem;
            this.tipo = tipo;
            int v = tamanho == null ? -1 : tamanho.toUpperCase().indexOf('V');
            if (v > 0) {
                int inteiros = Integer.parseInt(tamanho.substring(0, v).trim());
                this.decimais = Integer.parseInt(tamanho.substring(v + 1).trim());
                this.digitos = inteiros + this.decimais;
            } else {
                this.decimais = -1;
                this.digitos = tamanho == null || tamanho.isEmpty() ? 0
                        : Integer.parseInt(tamanho.trim());
            }
        }

        boolean decimal() {
            return decimais >= 0;
        }
    }

    static final class Layout {
        final String nome;
        final List<Campo> campos;
        final boolean ordemConfirmada;
        final int fieldCountOficial;

        Layout(String nome, List<Campo> campos, boolean ordemConfirmada, int fieldCountOficial) {
            this.nome = nome;
            this.campos = campos;
            this.ordemConfirmada = ordemConfirmada;
            this.fieldCountOficial = fieldCountOficial;
        }
    }

    private static Map<String, Layout> layouts;
    private static String origemCache;
    private static long modificadoEmCache;

    private LayoutReforma() {
    }

    static synchronized Layout de(String nome) throws IOException {
        // mesma invalidação dos cadastros: editar o descritor e converter de novo
        // tem de valer sem reiniciar o programa
        Path externo = Paths.get(System.getProperty("user.dir"), NOME_ARQUIVO);
        long modificadoEm = Files.exists(externo) ? Files.getLastModifiedTime(externo).toMillis() : -1;
        if (layouts == null || !externo.toString().equals(origemCache) || modificadoEm != modificadoEmCache) {
            layouts = carregar();
            origemCache = externo.toString();
            modificadoEmCache = modificadoEm;
        }
        Layout layout = layouts.get(nome);
        if (layout == null) {
            throw new IOException("Layout " + nome + " não declarado em " + NOME_ARQUIVO);
        }
        return layout;
    }

    private static Map<String, Layout> carregar() throws IOException {
        String texto;
        Path externo = Paths.get(System.getProperty("user.dir"), NOME_ARQUIVO);
        if (Files.exists(externo)) {
            texto = Files.readString(externo, StandardCharsets.UTF_8);
        } else {
            try (InputStream in = LayoutReforma.class.getResourceAsStream("/" + NOME_ARQUIVO)) {
                if (in == null) {
                    throw new IOException("Arquivo " + NOME_ARQUIVO + " não encontrado.");
                }
                try (Reader r = new InputStreamReader(in, StandardCharsets.UTF_8)) {
                    StringBuilder sb = new StringBuilder();
                    char[] buffer = new char[8192];
                    int lidos;
                    while ((lidos = r.read(buffer)) > 0) sb.append(buffer, 0, lidos);
                    texto = sb.toString();
                }
            }
        }
        return JsonMinimo.layouts(texto);
    }
}
