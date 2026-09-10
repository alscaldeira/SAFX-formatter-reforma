package com.caldeira.service;

/**
 * Máscara de CPF/CNPJ dos campos de documento gravados nos arquivos gerados.
 *
 * O XML fiscal traz CNPJ e CPF sempre sem pontuação (é o que o schema exige),
 * mas os arquivos gerados aqui saem com a máscara usual — 000.000.000-00 para
 * CPF e 00.000.000/0000-00 para CNPJ — independentemente de como a origem
 * informou. Um documento que já chegue mascarado é normalizado e remascarado,
 * de modo que o formato de saída não depende do emitente.
 *
 * A comparação com os cadastros (participantes.properties,
 * estabelecimentos.properties) continua tolerante a formato, via
 * {@link CadastroProperties#chaveDocumento} — mascarar a gravação não muda a
 * chave de consulta.
 */
final class Documentos {

    /** Dígitos de um CPF; com máscara ele ocupa 14 posições. */
    private static final int DIGITOS_CPF = 11;
    /** Posições de um CNPJ; com máscara ele ocupa 18. */
    private static final int POSICOES_CNPJ = 14;

    private Documentos() {
    }

    /**
     * Documento com máscara. Valor que não tem cara de CPF nem de CNPJ volta
     * como veio — é o caso dos códigos sintéticos "EXnnnnnnnnnnnn" que a
     * exportação usa quando o destinatário do exterior não tem documento.
     */
    static String mascarar(String documento) {
        String limpo = semMascara(documento);
        if (limpo.length() == DIGITOS_CPF && soDigitos(limpo)) {
            return limpo.substring(0, 3) + "." + limpo.substring(3, 6) + "."
                    + limpo.substring(6, 9) + "-" + limpo.substring(9);
        }
        if (limpo.length() == POSICOES_CNPJ && cnpj(limpo)) {
            return limpo.substring(0, 2) + "." + limpo.substring(2, 5) + "."
                    + limpo.substring(5, 8) + "/" + limpo.substring(8, 12) + "-"
                    + limpo.substring(12);
        }
        return documento == null ? "" : documento.trim();
    }

    /**
     * Máscara aplicada só quando o resultado cabe no campo; senão, o documento
     * sem pontuação. Existe por causa do COD_FIS_JUR, que tem 14 posições: o
     * CPF mascarado cabe (14), o CNPJ mascarado não (18).
     */
    static String mascararSeCouber(String documento, int tamanho) {
        String mascarado = mascarar(documento);
        return mascarado.length() <= tamanho ? mascarado : semMascara(documento);
    }

    /** Documento sem pontuação, em maiúsculas (o CNPJ é alfanumérico desde 2026). */
    static String semMascara(String documento) {
        return CadastroProperties.chaveDocumento(documento);
    }

    /**
     * Formato do CNPJ alfanumérico (IN RFB 2.229/2024): as 12 primeiras
     * posições são letras ou dígitos e as 2 últimas, os dígitos verificadores,
     * são sempre numéricas. A regra também separa um CNPJ de um código
     * sintético de 14 posições, que dificilmente termina em dois dígitos.
     */
    private static boolean cnpj(String limpo) {
        return Character.isDigit(limpo.charAt(12)) && Character.isDigit(limpo.charAt(13));
    }

    private static boolean soDigitos(String valor) {
        for (int i = 0; i < valor.length(); i++) {
            if (!Character.isDigit(valor.charAt(i))) return false;
        }
        return true;
    }
}
