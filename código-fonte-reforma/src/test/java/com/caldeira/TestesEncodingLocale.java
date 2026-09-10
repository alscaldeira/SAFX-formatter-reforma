package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Locale;

/** Encoding do arquivo de origem e independência do Locale do sistema. */
public final class TestesEncodingLocale {

    private TestesEncodingLocale() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Encoding e Locale");

        // --- SPED em ISO-8859-1 (Latin-1), comum em ERP antigo
        Path caso = Fixtures.caso(base, "sped-latin1");
        String sped = Fixtures.sped(1, 1).replace("FORNECEDOR TESTE", "FORNECEDOR AÇÚCAR LTDA");
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", sped, StandardCharsets.ISO_8859_1);
        System.setProperty("user.dir", caso.resolve("out").toString());

        String erro = null;
        try {
            Conversao.apartirDeSped(arquivo.toString());
        } catch (Exception e) {
            erro = e.getClass().getSimpleName() + ": " + e.getMessage();
        }
        Assercoes.check("encoding: SPED em Latin-1 é convertido sem erro", erro == null,
                String.valueOf(erro));
        if (erro == null) {
            String nome = Fixtures.campos(caso, "SAFX04", 0)[4];
            Assercoes.check("encoding: acentuação do Latin-1 preservada",
                    nome.contains("AÇÚCAR"), nome);
        }

        // --- SPED em UTF-8 continua funcionando
        Path utf8 = Fixtures.caso(base, "sped-utf8");
        Path arquivoUtf8 = Fixtures.escreverSped(utf8, "sped.txt",
                Fixtures.sped(1, 1).replace("FORNECEDOR TESTE", "FORNECEDOR AÇÚCAR LTDA"),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", utf8.resolve("out").toString());
        Conversao.apartirDeSped(arquivoUtf8.toString());
        Assercoes.check("encoding: SPED em UTF-8 preserva a acentuação",
                Fixtures.campos(utf8, "SAFX04", 0)[4].contains("AÇÚCAR"),
                Fixtures.campos(utf8, "SAFX04", 0)[4]);

        // --- Locale com vírgula decimal não pode vazar para os números
        Locale original = Locale.getDefault();
        try {
            Locale.setDefault(Locale.GERMANY);
            Path localeCaso = Fixtures.caso(base, "locale-de");
            Fixtures.escrever(localeCaso, "nota.xml",
                    Fixtures.nfe("4001", true, "42105890000901", "46754545000194"));
            System.setProperty("user.dir", localeCaso.resolve("out").toString());
            Conversao.apartirDeXml(localeCaso.resolve("xmls").toString());

            boolean somenteDigitos = true;
            for (String linha : Fixtures.linhas(localeCaso, "SAFX07")) {
                for (String campo : linha.split("\t", -1)) {
                    if (campo.contains(",")) somenteDigitos = false;
                }
            }
            Assercoes.check("locale: nenhum número saiu com vírgula decimal em de_DE",
                    somenteDigitos, "");
            Assercoes.check("locale: valores continuam corretos em de_DE",
                    Fixtures.campos(localeCaso, "SAFX07", 0)[21].equals("0000000000100000"),
                    Fixtures.campos(localeCaso, "SAFX07", 0)[21]);
        } finally {
            Locale.setDefault(original);
        }

        // --- XML com BOM UTF-8
        Path bom = Fixtures.caso(base, "xml-bom");
        byte[] conteudo = Fixtures.nfe("4002", true, "42105890000901", "46754545000194")
                .getBytes(StandardCharsets.UTF_8);
        byte[] comBom = new byte[conteudo.length + 3];
        comBom[0] = (byte) 0xEF; comBom[1] = (byte) 0xBB; comBom[2] = (byte) 0xBF;
        System.arraycopy(conteudo, 0, comBom, 3, conteudo.length);
        Files.write(bom.resolve("xmls/nota.xml"), comBom);
        System.setProperty("user.dir", bom.resolve("out").toString());
        Conversao.apartirDeXml(bom.resolve("xmls").toString());
        Assercoes.check("encoding: XML com BOM UTF-8 é lido",
                Fixtures.linhas(bom, "SAFX07").size() == 1,
                "" + Fixtures.linhas(bom, "SAFX07").size());
    }
}
