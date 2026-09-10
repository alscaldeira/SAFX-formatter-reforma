package com.caldeira;

import java.io.IOException;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Acesso ao safx-layout.json para os testes de conformidade. As regras de
 * validação saem daqui, não de constantes duplicadas no teste.
 */
public final class Leiaute {

    /** Um campo do leiaute, com o tamanho já interpretado. */
    public static final class Campo {
        public final int posicao;
        public final String nome;
        public final String descricao;
        /** Dígitos inteiros ("015V002" -> 15; "070" -> 70). */
        public final int tamanho;
        /** Casas decimais ("015V002" -> 2); -1 quando o campo não é decimal. */
        public final int decimais;
        public final boolean chave;
        public final boolean preencher;

        Campo(int posicao, String nome, String descricao, int tamanho, int decimais,
              boolean chave, boolean preencher) {
            this.posicao = posicao;
            this.nome = nome;
            this.descricao = descricao;
            this.tamanho = tamanho;
            this.decimais = decimais;
            this.chave = chave;
            this.preencher = preencher;
        }

        public boolean decimal() {
            return decimais >= 0;
        }

        /** Campo de data: 8 posições e nome/descrição indicando data. */
        public boolean data() {
            if (decimal() || tamanho != 8) return false;
            String n = nome.toUpperCase();
            return n.startsWith("DAT") || n.startsWith("DT_") || n.contains("_DAT");
        }
    }

    private static Map<String, Object> raiz;
    private static Map<String, Object> raizReforma;

    private Leiaute() {
    }

    public static Path caminho() {
        String prop = System.getProperty("safx.layout");
        return prop != null ? Paths.get(prop) : Paths.get("src/main/resources/safx-layout.json");
    }

    @SuppressWarnings("unchecked")
    public static List<Campo> campos(String layout) throws IOException {
        if (raiz == null) raiz = Json.ler(caminho());
        Map<String, Object> layouts = (Map<String, Object>) raiz.get("layouts");
        Map<String, Object> alvo = (Map<String, Object>) layouts.get(layout);
        List<Object> lista = (List<Object>) alvo.get("fields");

        List<Campo> campos = new ArrayList<>();
        for (Object o : lista) {
            Map<String, Object> f = (Map<String, Object>) o;
            String size = String.valueOf(f.get("size")).trim();
            int tamanho;
            int decimais = -1;
            int v = size.toUpperCase().indexOf('V');
            if (v > 0) {
                tamanho = Integer.parseInt(size.substring(0, v).trim());
                decimais = Integer.parseInt(size.substring(v + 1).trim());
            } else {
                tamanho = Integer.parseInt(size);
            }
            campos.add(new Campo(
                    (int) Double.parseDouble(String.valueOf(f.get("position"))),
                    String.valueOf(f.get("field")),
                    String.valueOf(f.get("description")),
                    tamanho,
                    decimais,
                    Boolean.TRUE.equals(f.get("isKey")),
                    Boolean.TRUE.equals(f.get("populate"))));
        }
        return campos;
    }

    /** Campos dos layouts da Reforma Tributária (safx-reforma-layout.json). */
    @SuppressWarnings("unchecked")
    public static List<Campo> camposReforma(String layout) throws IOException {
        if (raizReforma == null) {
            raizReforma = Json.ler(Paths.get("src/main/resources/safx-reforma-layout.json"));
        }
        Map<String, Object> layouts = (Map<String, Object>) raizReforma.get("layouts");
        Map<String, Object> alvo = (Map<String, Object>) layouts.get(layout);
        if (alvo == null) return List.of();
        List<Object> lista = (List<Object>) alvo.get("fields");

        List<Campo> campos = new ArrayList<>();
        for (Object o : lista) {
            Map<String, Object> f = (Map<String, Object>) o;
            String size = String.valueOf(f.get("tamanho")).trim();
            int tamanho;
            int decimais = -1;
            int v = size.toUpperCase().indexOf('V');
            if (v > 0) {
                tamanho = Integer.parseInt(size.substring(0, v).trim());
                decimais = Integer.parseInt(size.substring(v + 1).trim());
            } else {
                tamanho = Integer.parseInt(size);
            }
            campos.add(new Campo(
                    (int) Double.parseDouble(String.valueOf(f.get("posicao"))),
                    String.valueOf(f.get("campo")),
                    String.valueOf(f.get("descricao")),
                    tamanho, decimais,
                    false,
                    Boolean.TRUE.equals(f.get("obrigatorio"))));
        }
        return campos;
    }

    @SuppressWarnings("unchecked")
    public static int quantidadeCampos(String layout) throws IOException {
        if (raiz == null) raiz = Json.ler(caminho());
        Map<String, Object> layouts = (Map<String, Object>) raiz.get("layouts");
        Map<String, Object> alvo = (Map<String, Object>) layouts.get(layout);
        return (int) Double.parseDouble(String.valueOf(alvo.get("fieldCount")));
    }
}
