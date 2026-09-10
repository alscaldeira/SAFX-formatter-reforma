package com.caldeira.service;

import java.io.IOException;
import java.util.List;

/**
 * Fachada da conversão: descobre o tipo da origem, executa o fluxo certo e
 * devolve os avisos. Existe para que a decisão não fique dentro da classe
 * Swing — assim ela é testável sem abrir janela.
 */
public final class Conversao {

    public enum Origem { SPED, XML }

    private Conversao() {
    }

    /**
     * Descobre sozinho se é arquivo SPED, XML avulso ou pasta de XMLs — a tela
     * não precisa perguntar.
     */
    public static List<String> executar(String caminho) throws IOException {
        return executar(OrigemArquivo.detectar(caminho), caminho);
    }

    public static List<String> executar(Origem origem, String caminho) throws IOException {
        return origem == Origem.XML ? apartirDeXml(caminho) : apartirDeSped(caminho);
    }

    /** Exposto para a tela e para os testes conferirem a detecção. */
    public static Origem detectar(String caminho) throws IOException {
        return OrigemArquivo.detectar(caminho);
    }

    public static List<String> apartirDeXml(String pasta) throws IOException {
        return ConversorXml.converter(pasta);
    }

    public static List<String> apartirDeSped(String arquivo) throws IOException {

        SpedToImportacaoCsv.convertFromSped(arquivo);
        return List.of();
    }
}
