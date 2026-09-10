package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

/**
 * Testes de caracterização: congelam a saída validada dos dois fluxos e
 * comparam byte a byte a cada alteração. É o que permite mexer em SafxFormat,
 * Estabelecimentos ou nos leitores sem depender de conferência manual.
 *
 * Para regravar as referências depois de uma mudança intencional:
 * {@code -Dsafx.golden.regravar=true}.
 */
public final class TestesGolden {

    private static final String[] ARQUIVOS = {"SAFX04", "SAFX07", "SAFX08", "SAFX49"};

    private TestesGolden() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Golden files (caracterização dos dois fluxos)");
        boolean regravar = Boolean.getBoolean("safx.golden.regravar");

        Path sped = Paths.get("src/main/resources/SPED.txt");
        if (Files.exists(sped)) {
            Path caso = Fixtures.caso(base, "golden-sped");
            System.setProperty("user.dir", caso.resolve("out").toString());
            Conversao.apartirDeSped(sped.toString());
            comparar("SPED", caso.resolve("out"), Paths.get("src/test/resources/golden/sped"), regravar);
        } else {
            Assercoes.check("golden SPED: arquivo de origem presente", false, sped.toString());
        }

        Path xmls = Paths.get("src/main/resources/XMLs");
        if (Files.exists(xmls)) {
            Path caso = Fixtures.caso(base, "golden-xml");
            System.setProperty("user.dir", caso.resolve("out").toString());
            Conversao.apartirDeXml(xmls.toString());
            comparar("XML", caso.resolve("out"), Paths.get("src/test/resources/golden/xml"), regravar);
        } else {
            Assercoes.check("golden XML: pasta de origem presente", false, xmls.toString());
        }
    }

    private static void comparar(String origem, Path saida, Path golden, boolean regravar)
            throws IOException {
        Files.createDirectories(golden);
        for (String arquivo : ARQUIVOS) {
            Path gerado = saida.resolve(arquivo + ".txt");
            Path referencia = golden.resolve(arquivo + ".txt");

            if (regravar) {
                Files.copy(gerado, referencia, java.nio.file.StandardCopyOption.REPLACE_EXISTING);
                Assercoes.check(origem + " " + arquivo + ": referência regravada", true, "");
                continue;
            }
            if (!Files.exists(referencia)) {
                Assercoes.check(origem + " " + arquivo + ": referência existe", false,
                        "faltando " + referencia + " (rode com -Dsafx.golden.regravar=true)");
                continue;
            }
            List<String> esperado = Files.readAllLines(referencia, StandardCharsets.UTF_8);
            List<String> obtido = Files.readAllLines(gerado, StandardCharsets.UTF_8);
            String diferenca = primeiraDiferenca(esperado, obtido);
            Assercoes.check(origem + " " + arquivo + ": idêntico à referência (" + esperado.size()
                    + " linhas)", diferenca == null, String.valueOf(diferenca));
        }
    }

    private static String primeiraDiferenca(List<String> esperado, List<String> obtido) {
        if (esperado.size() != obtido.size()) {
            return "linhas: esperado " + esperado.size() + ", obtido " + obtido.size();
        }
        for (int i = 0; i < esperado.size(); i++) {
            if (!esperado.get(i).equals(obtido.get(i))) {
                String[] a = esperado.get(i).split("\t", -1);
                String[] b = obtido.get(i).split("\t", -1);
                for (int c = 0; c < Math.min(a.length, b.length); c++) {
                    if (!a[c].equals(b[c])) {
                        return "linha " + (i + 1) + " campo " + (c + 1)
                                + ": esperado [" + a[c] + "], obtido [" + b[c] + "]";
                    }
                }
                return "linha " + (i + 1) + " difere";
            }
        }
        return null;
    }
}
