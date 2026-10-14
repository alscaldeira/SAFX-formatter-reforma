package com.caldeira.service;

import java.math.BigDecimal;
import java.math.RoundingMode;

/**
 * Formatação de valores no padrão dos scripts SQL do sistema original
 * (right(replicate('0',N)+cast(REPLACE(col,'.','') as varchar(N)),N)).
 *
 * O ponto é a ESCALA da coluna: o SQL Server grava o decimal na escala da
 * coluna (15,2 / 15,4 / 5,2 ...) e o cast para varchar devolve exatamente
 * essas casas, então "10.5" numa coluna (15,2) vira "10.50" -> "1050".
 */
final class Valores {

    private static final java.util.regex.Pattern DATA_ISO =
            java.util.regex.Pattern.compile("^\\d{4}-\\d{2}-\\d{2}");

    private Valores() {
    }

    /** Número do XML, sempre com ponto decimal; null se ausente ou ilegível. */
    static BigDecimal dec(String valor) {
        if (valor == null) return null;
        String v = valor.trim().replace(",", ".");
        if (v.isEmpty()) return null;
        try {
            return new BigDecimal(v);
        } catch (NumberFormatException e) {
            return null;
        }
    }

    /** Como o Convert.ToDecimal((decimal?)x) do original: ausente vale zero. */
    static BigDecimal decOuZero(String valor) {
        BigDecimal d = dec(valor);
        return d == null ? BigDecimal.ZERO : d;
    }

    /** Dígitos do decimal na escala da coluna, sem o ponto; null se o valor é null. */
    static String digitos(BigDecimal valor, int escala) {
        if (valor == null) return null;
        return valor.setScale(escala, RoundingMode.HALF_UP).toPlainString().replace(".", "");
    }

    /** right(replicate('0',n) + texto, n); null propaga (concatenação com NULL é NULL no SQL). */
    static String direita(String texto, int n) {
        if (texto == null) return null;
        String completo = "0".repeat(n) + texto;
        return completo.substring(completo.length() - n);
    }

    /** Coluna decimal de escala e largura dadas: z(valor, 2, 17) = campo monetário de 17 posições. */
    static String z(BigDecimal valor, int escala, int largura) {
        return direita(digitos(valor, escala), largura);
    }

    /** Valor monetário (escala 2) em 17 posições. */
    static String z17(BigDecimal valor) {
        return z(valor, 2, 17);
    }

    /**
     * Valor bruto de uma coluna decimal como o Excel o mostra (célula numérica):
     * sem zeros à direita e "0" para zero. Usado nas colunas que o script SQL
     * devolve sem formatar (VLR_FRETE, VLR_OUTRAS, VLR_ALIQ_SUB_ICMS ...).
     */
    static String bruto(BigDecimal valor) {
        if (valor == null) return null;
        BigDecimal s = valor.stripTrailingZeros();
        return s.signum() == 0 ? "0" : s.toPlainString();
    }

    /** Convert.ToInt16 / ToInt32 de um campo numérico do XML: ausente vale 0. */
    static int inteiro(String valor) {
        BigDecimal d = dec(valor);
        return d == null ? 0 : d.setScale(0, RoundingMode.HALF_EVEN).intValue();
    }

    static String doisDigitos(Integer valor) {
        return valor == null ? null : direita(String.valueOf(valor), 2);
    }

    /** "2026-03-13T09:31:45-03:00" ou "2026-03-13" -> "20260313" (hora de parede do documento). */
    static String data(String iso) {
        if (iso == null) return "";
        String s = iso.trim();
        if (!DATA_ISO.matcher(s).find()) return "";
        return s.substring(0, 4) + s.substring(5, 7) + s.substring(8, 10);
    }

    /** O mesmo mapa de caracteres do TiraAcento do original (só estes; os demais ficam). */
    static String tiraAcento(String texto) {
        if (texto == null) return null;
        String de = "ÇçâÂêÊôÔÁáÉéÍíÓóÚúÃãÕõ";
        String para = "CcaAeEoOAaEeIiOoUuAaOo";
        StringBuilder sb = new StringBuilder(texto.length());
        for (char c : texto.toCharArray()) {
            int i = de.indexOf(c);
            sb.append(i >= 0 ? para.charAt(i) : c);
        }
        return sb.toString();
    }
}
