package com.caldeira.service;

import java.io.IOException;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Extração dos layouts do safx-reforma-layout.json sem dependência externa (o
 * pom do projeto não tem nenhuma). Lê só o necessário: nome do layout, ordem
 * dos campos e o marcador "ordemConfirmada".
 */
final class JsonMinimo {

    private final String texto;
    private int pos;

    private JsonMinimo(String texto) {
        this.texto = texto;
    }

    static Map<String, LayoutReforma.Layout> layouts(String json) throws IOException {
        try {
            JsonMinimo parser = new JsonMinimo(json);
            Object raiz = parser.valor();
            Map<String, Object> mapa = comoMapa(raiz);
            Map<String, Object> declarados = comoMapa(mapa.get("layouts"));

            Map<String, LayoutReforma.Layout> resultado = new LinkedHashMap<>();
            for (Map.Entry<String, Object> e : declarados.entrySet()) {
                Map<String, Object> corpo = comoMapa(e.getValue());
                List<LayoutReforma.Campo> campos = new ArrayList<>();
                for (Object o : comoLista(corpo.get("fields"))) {
                    Map<String, Object> f = comoMapa(o);
                    campos.add(new LayoutReforma.Campo(
                            String.valueOf(f.get("campo")),
                            String.valueOf(f.get("origem")),
                            String.valueOf(f.get("tipo")),
                            f.get("tamanho") == null ? "" : String.valueOf(f.get("tamanho"))));
                }
                boolean confirmada = Boolean.TRUE.equals(corpo.get("ordemConfirmada"));
                int oficial = corpo.get("fieldCountOficial") instanceof Double
                        ? ((Double) corpo.get("fieldCountOficial")).intValue()
                        : campos.size();
                resultado.put(e.getKey(),
                        new LayoutReforma.Layout(e.getKey(), campos, confirmada, oficial));
            }
            return resultado;
        } catch (RuntimeException ex) {
            throw new IOException("Falha ao ler " + LayoutReforma.NOME_ARQUIVO + ": " + ex.getMessage(), ex);
        }
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> comoMapa(Object o) {
        return (Map<String, Object>) o;
    }

    @SuppressWarnings("unchecked")
    private static List<Object> comoLista(Object o) {
        return (List<Object>) o;
    }

    private Object valor() {
        espacos();
        char c = texto.charAt(pos);
        switch (c) {
            case '{': return objeto();
            case '[': return lista();
            case '"': return string();
            case 't': pos += 4; return Boolean.TRUE;
            case 'f': pos += 5; return Boolean.FALSE;
            case 'n': pos += 4; return null;
            default: return numero();
        }
    }

    private Map<String, Object> objeto() {
        Map<String, Object> mapa = new LinkedHashMap<>();
        pos++;
        espacos();
        if (texto.charAt(pos) == '}') { pos++; return mapa; }
        while (true) {
            espacos();
            String chave = string();
            espacos();
            pos++; // :
            mapa.put(chave, valor());
            espacos();
            if (texto.charAt(pos++) == '}') return mapa;
        }
    }

    private List<Object> lista() {
        List<Object> itens = new ArrayList<>();
        pos++;
        espacos();
        if (texto.charAt(pos) == ']') { pos++; return itens; }
        while (true) {
            itens.add(valor());
            espacos();
            if (texto.charAt(pos++) == ']') return itens;
        }
    }

    private String string() {
        StringBuilder sb = new StringBuilder();
        pos++;
        while (true) {
            char c = texto.charAt(pos++);
            if (c == '"') return sb.toString();
            if (c != '\\') { sb.append(c); continue; }
            char e = texto.charAt(pos++);
            switch (e) {
                case 'n': sb.append('\n'); break;
                case 't': sb.append('\t'); break;
                case 'r': sb.append('\r'); break;
                case 'u':
                    sb.append((char) Integer.parseInt(texto.substring(pos, pos + 4), 16));
                    pos += 4;
                    break;
                default: sb.append(e);
            }
        }
    }

    private Double numero() {
        int inicio = pos;
        while (pos < texto.length() && "-+.eE0123456789".indexOf(texto.charAt(pos)) >= 0) pos++;
        return Double.valueOf(texto.substring(inicio, pos));
    }

    private void espacos() {
        while (pos < texto.length() && Character.isWhitespace(texto.charAt(pos))) pos++;
    }
}
