package com.caldeira.service;

import java.util.Locale;

/**
 * Formatação numérica compartilhada pelos geradores SAFXnn. Todos os campos
 * numéricos do SAFX são gravados com 16 dígitos, sem separador decimal — o que
 * muda de campo para campo é apenas a escala (quantas casas decimais estão
 * implícitas no final do número).
 *
 * Ex.: 13,00 com 6 casas -> "0000000013000000"; 18,00 com 4 casas -> "0000000000180000".
 */
final class SafxFormat {

    /** Quantidade de dígitos gravados em todo campo numérico do SAFX. */
    private static final int DIGITOS = 16;

    private SafxFormat() {
    }

    static String numero(double valor, int decimais) {
        return numero(valor, decimais, DIGITOS);
    }

    /**
     * Variante com largura explícita. O SAFX49 não usa a notação "NNNVNNNN" dos
     * demais layouts: os campos de alíquota têm 7 posições no total, onde os 16
     * dígitos padrão não caberiam.
     */
    static String numero(double valor, int decimais, int digitos) {
        // largura = digitos + 1 por causa do ponto, que é removido em seguida
        String mascara = "%0" + (digitos + 1) + "." + decimais + "f";
        String formatado = String.format(Locale.US, mascara, valor).replace(".", "");
        if (formatado.length() != digitos) {
            // Estourar a largura desalinharia todos os campos seguintes do
            // registro; é melhor falhar dizendo qual valor não coube.
            throw new IllegalArgumentException("Valor " + valor + " não cabe em "
                    + digitos + " dígitos com " + decimais + " casas decimais (gerou "
                    + formatado.length() + ").");
        }
        return formatado;
    }
}
