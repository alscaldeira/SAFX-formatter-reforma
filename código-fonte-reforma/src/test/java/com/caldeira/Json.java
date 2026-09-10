package com.caldeira;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Leitor de JSON mínimo, só para os testes lerem o safx-layout.json. O projeto
 * não tem dependências no pom e o leiaute é a fonte da verdade das regras de
 * conformidade — vale mais lê-lo do que reescrever as regras no teste.
 */
public final class Json {

    private final String texto;
    private int pos;

    private Json(String texto) {
        this.texto = texto;
    }

    @SuppressWarnings("unchecked")
    public static Map<String, Object> ler(Path arquivo) throws IOException {
        Json json = new Json(Files.readString(arquivo, StandardCharsets.UTF_8));
        json.espacos();
        return (Map<String, Object>) json.valor();
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
        pos++; // {
        espacos();
        if (texto.charAt(pos) == '}') { pos++; return mapa; }
        while (true) {
            espacos();
            String chave = string();
            espacos();
            pos++; // :
            mapa.put(chave, valor());
            espacos();
            char c = texto.charAt(pos++);
            if (c == '}') return mapa;
        }
    }

    private List<Object> lista() {
        List<Object> itens = new ArrayList<>();
        pos++; // [
        espacos();
        if (texto.charAt(pos) == ']') { pos++; return itens; }
        while (true) {
            itens.add(valor());
            espacos();
            char c = texto.charAt(pos++);
            if (c == ']') return itens;
        }
    }

    private String string() {
        StringBuilder sb = new StringBuilder();
        pos++; // "
        while (true) {
            char c = texto.charAt(pos++);
            if (c == '"') return sb.toString();
            if (c != '\\') { sb.append(c); continue; }
            char e = texto.charAt(pos++);
            switch (e) {
                case 'n': sb.append('\n'); break;
                case 't': sb.append('\t'); break;
                case 'r': sb.append('\r'); break;
                case 'b': sb.append('\b'); break;
                case 'f': sb.append('\f'); break;
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
