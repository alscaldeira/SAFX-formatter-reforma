package com.caldeira;

/** Contador global de asserções das suítes de teste. */
public final class Assercoes {

    private static int passou;
    private static int falhou;

    private Assercoes() {
    }

    public static void secao(String titulo) {
        System.out.println("\n-- " + titulo);
    }

    public static void check(String nome, boolean ok, String detalhe) {
        if (ok) {
            passou++;
            System.out.println("  ok    " + nome);
        } else {
            falhou++;
            System.out.println("  FALHA " + nome + "  ->  " + detalhe);
        }
    }

    public static int passou() {
        return passou;
    }

    public static int falhou() {
        return falhou;
    }
}
