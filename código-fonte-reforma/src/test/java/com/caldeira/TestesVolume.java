package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.charset.StandardCharsets;

/**
 * Volume e desempenho. Tudo hoje é lido inteiro em memória (readAllLines no
 * SPED, um DOM por XML); o teste garante que a ordem de grandeza real do
 * cliente passa e serve de alarme se alguém introduzir custo quadrático.
 *
 * Ajuste a escala com -Dsafx.volume.notas=N e -Dsafx.volume.xmls=N.
 */
public final class TestesVolume {

    private TestesVolume() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Volume e desempenho");

        int notasSped = Integer.getInteger("safx.volume.notas", 2000);
        int qtdXmls = Integer.getInteger("safx.volume.xmls", 400);

        // --- SPED grande
        Path caso = Fixtures.caso(base, "volume-sped");
        Path sped = Fixtures.escreverSped(caso, "sped.txt", Fixtures.sped(notasSped, 5),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        long inicio = System.currentTimeMillis();
        Conversao.apartirDeSped(sped.toString());
        long msSped = System.currentTimeMillis() - inicio;

        Assercoes.check("volume: " + notasSped + " notas SPED convertidas",
                Fixtures.linhas(caso, "SAFX07").size() == notasSped,
                "" + Fixtures.linhas(caso, "SAFX07").size());
        Assercoes.check("volume: " + (notasSped * 5) + " itens SPED convertidos",
                Fixtures.linhas(caso, "SAFX08").size() == notasSped * 5,
                "" + Fixtures.linhas(caso, "SAFX08").size());
        Assercoes.check("volume: SPED em tempo aceitável (" + msSped + " ms)", msSped < 120_000,
                msSped + " ms");

        // --- muitos XMLs, em subpastas
        Path casoXml = Fixtures.caso(base, "volume-xml");
        for (int i = 0; i < qtdXmls; i++) {
            Path pasta = casoXml.resolve("xmls").resolve("lote" + (i % 10));
            Files.createDirectories(pasta);
            Files.write(pasta.resolve("nfe" + i + ".xml"),
                    Fixtures.nfe(String.valueOf(10000 + i), true, "42105890000901", "46754545000194")
                            .getBytes(StandardCharsets.UTF_8));
        }
        System.setProperty("user.dir", casoXml.resolve("out").toString());
        inicio = System.currentTimeMillis();
        Conversao.apartirDeXml(casoXml.resolve("xmls").toString());
        long msXml = System.currentTimeMillis() - inicio;

        Assercoes.check("volume: " + qtdXmls + " XMLs em subpastas convertidos",
                Fixtures.linhas(casoXml, "SAFX07").size() == qtdXmls,
                "" + Fixtures.linhas(casoXml, "SAFX07").size());
        Assercoes.check("volume: XML em tempo aceitável (" + msXml + " ms)", msXml < 120_000,
                msXml + " ms");

        // --- link simbólico circular não pode travar a varredura
        Path casoLink = Fixtures.caso(base, "volume-link");
        Fixtures.escrever(casoLink, "nota.xml",
                Fixtures.nfe("9001", true, "42105890000901", "46754545000194"));
        boolean criouLink = false;
        try {
            Files.createSymbolicLink(casoLink.resolve("xmls/ciclo"), casoLink.resolve("xmls"));
            criouLink = true;
        } catch (IOException | UnsupportedOperationException e) {
            // sistema sem suporte a symlink: o cenário não se aplica
        }
        if (criouLink) {
            System.setProperty("user.dir", casoLink.resolve("out").toString());
            Conversao.apartirDeXml(casoLink.resolve("xmls").toString());
            Assercoes.check("volume: link simbólico circular não trava a varredura",
                    Fixtures.linhas(casoLink, "SAFX07").size() == 1,
                    "" + Fixtures.linhas(casoLink, "SAFX07").size());
        }
    }
}
