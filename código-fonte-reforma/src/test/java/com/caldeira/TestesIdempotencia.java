package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.List;

/** Repetir a conversão e alternar de origem na mesma pasta não pode surpreender. */
public final class TestesIdempotencia {

    private TestesIdempotencia() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Idempotência e estado da pasta de saída");

        // --- duas execuções seguidas produzem exatamente o mesmo arquivo
        Path caso = Fixtures.caso(base, "idempotencia");
        Fixtures.escrever(caso, "v1.xml", Fixtures.nfe("3001", true, "42105890000901", "46754545000194"));
        Fixtures.escrever(caso, "v2.xml", Fixtures.nfe("3002", false, "11222333000181", "42105890000901"));
        Fixtures.escrever(caso, "s1.xml", Fixtures.nfse("3003", "11056737000142", "42105890000901"));
        System.setProperty("user.dir", caso.resolve("out").toString());

        Conversao.apartirDeXml(caso.resolve("xmls").toString());
        byte[][] primeira = ler(caso);
        Conversao.apartirDeXml(caso.resolve("xmls").toString());
        byte[][] segunda = ler(caso);

        boolean iguais = true;
        for (int i = 0; i < primeira.length; i++) {
            if (!Arrays.equals(primeira[i], segunda[i])) iguais = false;
        }
        Assercoes.check("idempotência: duas conversões seguidas geram bytes idênticos", iguais, "");

        // --- avisos também são estáveis (não acumulam entre execuções)
        List<String> avisos1 = Conversao.apartirDeXml(caso.resolve("xmls").toString());
        List<String> avisos2 = Conversao.apartirDeXml(caso.resolve("xmls").toString());
        Assercoes.check("idempotência: avisos não acumulam entre conversões",
                avisos1.size() == avisos2.size(), avisos1.size() + " x " + avisos2.size());

        // --- SPED e depois XML na mesma pasta: nada pode sobrar do anterior
        Path misto = Fixtures.caso(base, "troca-de-origem");
        Path sped = Fixtures.escreverSped(misto, "sped.txt", Fixtures.sped(2, 2),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", misto.resolve("out").toString());
        Conversao.apartirDeSped(sped.toString());
        int itensSped = Fixtures.linhas(misto, "SAFX49").size();
        Assercoes.check("troca de origem: SPED gerou SAFX49 com conteúdo", itensSped > 0,
                "" + itensSped);

        Fixtures.escrever(misto, "nota.xml",
                Fixtures.nfe("3100", true, "42105890000901", "46754545000194"));
        List<String> avisos = Conversao.apartirDeXml(misto.resolve("xmls").toString());
        Assercoes.check("troca de origem: SAFX49 do SPED foi sobrescrito, não mesclado",
                Fixtures.linhas(misto, "SAFX49").isEmpty(),
                "" + Fixtures.linhas(misto, "SAFX49").size());
        Assercoes.check("troca de origem: o esvaziamento do SAFX49 é avisado",
                avisos.stream().anyMatch(a -> a.contains("SAFX49")), String.valueOf(avisos));
        Assercoes.check("troca de origem: capas são só as do XML",
                Fixtures.linhas(misto, "SAFX07").size() == 1,
                "" + Fixtures.linhas(misto, "SAFX07").size());
    }

    private static byte[][] ler(Path caso) throws IOException {
        String[] nomes = {"SAFX04.txt", "SAFX07.txt", "SAFX08.txt", "SAFX49.txt",
                "SAFX04.csv", "SAFX07.csv", "SAFX08.csv", "SAFX49.csv"};
        byte[][] conteudos = new byte[nomes.length][];
        for (int i = 0; i < nomes.length; i++) {
            Path p = caso.resolve("out").resolve(nomes[i]);
            conteudos[i] = Files.exists(p) ? Files.readAllBytes(p) : new byte[0];
        }
        return conteudos;
    }
}
