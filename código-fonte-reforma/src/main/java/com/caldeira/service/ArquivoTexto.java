package com.caldeira.service;

import java.io.IOException;
import java.nio.charset.MalformedInputException;
import java.nio.charset.StandardCharsets;
import java.nio.charset.UnmappableCharacterException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

/**
 * Leitura de arquivo texto com detecção de encoding.
 *
 * Arquivo SPED é gerado por ERP e circula tanto em UTF-8 quanto em ISO-8859-1
 * (Latin-1). Ler sempre como UTF-8 fazia a conversão morrer com
 * MalformedInputException num arquivo Latin-1 com acento — erro que não diz
 * nada ao usuário. Tenta-se UTF-8 primeiro (que também cobre ASCII puro) e,
 * se os bytes não forem UTF-8 válido, relê-se como Latin-1, que aceita
 * qualquer byte.
 */
final class ArquivoTexto {

    private ArquivoTexto() {
    }

    static List<String> linhas(Path arquivo) throws IOException {
        try {
            return Files.readAllLines(arquivo, StandardCharsets.UTF_8);
        } catch (MalformedInputException | UnmappableCharacterException e) {
            return Files.readAllLines(arquivo, StandardCharsets.ISO_8859_1);
        }
    }
}
