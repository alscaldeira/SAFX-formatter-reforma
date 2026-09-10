package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;

/**
 * Geração aleatória com semente fixa (reprodutível) afirmando invariantes em
 * vez de valores: o que não pode acontecer em NENHUMA entrada.
 *
 * Use -Dsafx.fuzz.semente=N para investigar uma falha específica.
 */
public final class TestesFuzz {

    private static final String LIXO = "\t\n\r;\"'<>&%$#@ áéíóú ÇÃÕ \\ /|";

    private TestesFuzz() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Fuzz / invariantes");

        long semente = Long.getLong("safx.fuzz.semente", 20260318L);
        Random random = new Random(semente);
        int quantidade = Integer.getInteger("safx.fuzz.notas", 80);

        Path caso = Fixtures.caso(base, "fuzz");
        int itensEsperados = 0;
        for (int i = 0; i < quantidade; i++) {
            boolean saida = random.nextBoolean();
            String valor = String.format(java.util.Locale.US, "%.2f",
                    random.nextDouble() * 900000 + 0.01);
            String qtd = String.format(java.util.Locale.US, "%.4f",
                    random.nextDouble() * 5000 + 0.0001);
            String descricao = textoAleatorio(random);
            String unidade = new String[]{"TO", "TON", "KG", "UN", "CX", "kg"}[random.nextInt(6)];

            String xml = Fixtures.nfe(String.valueOf(20000 + i), saida,
                    saida ? "42105890000901" : "11222333000181",
                    saida ? "46754545000194" : "42105890000901",
                    descricao, unidade, qtd, valor);
            if (random.nextInt(4) == 0) {
                xml = xml.replace("</infNFe>",
                        "<infAdic><infCpl>" + Fixtures.escapar(textoAleatorio(random))
                                + "</infCpl></infAdic></infNFe>");
            }
            Files.write(caso.resolve("xmls/nfe" + i + ".xml"), xml.getBytes(StandardCharsets.UTF_8));
            itensEsperados++;
        }

        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeXml(caso.resolve("xmls").toString());

        Assercoes.check("fuzz: uma capa por XML (semente " + semente + ")",
                Fixtures.linhas(caso, "SAFX07").size() == quantidade,
                "" + Fixtures.linhas(caso, "SAFX07").size());
        Assercoes.check("fuzz: um item do SAFX08 por item lido",
                Fixtures.linhas(caso, "SAFX08").size() == itensEsperados,
                "" + Fixtures.linhas(caso, "SAFX08").size());

        List<String> violacoes = new ArrayList<>();
        verificarInvariantes(caso, "SAFX04", violacoes);
        verificarInvariantes(caso, "SAFX07", violacoes);
        verificarInvariantes(caso, "SAFX08", violacoes);
        Assercoes.check("fuzz: nenhum campo estourou tamanho, TAB ou quebra de linha",
                violacoes.isEmpty(), violacoes.isEmpty() ? "" : violacoes.size() + ": " + violacoes.get(0));
    }

    private static void verificarInvariantes(Path caso, String layout, List<String> violacoes)
            throws IOException {
        List<Leiaute.Campo> campos = Leiaute.campos(layout);
        for (String linha : Fixtures.linhas(caso, layout)) {
            String[] valores = linha.split("\t", -1);
            if (valores.length != campos.size()) {
                violacoes.add(layout + ": " + valores.length + " campos");
                continue;
            }
            for (int i = 0; i < valores.length; i++) {
                String v = valores[i];
                if (v.indexOf('\t') >= 0 || v.indexOf('\n') >= 0 || v.indexOf('\r') >= 0) {
                    violacoes.add(layout + " campo " + (i + 1) + ": caractere de controle");
                }
                if ("@".equals(v)) continue;
                int limite = campos.get(i).decimal() ? 16 : campos.get(i).tamanho;
                if (v.length() > limite) {
                    violacoes.add(layout + " campo " + (i + 1) + ": " + v.length() + " > " + limite);
                }
            }
        }
    }

    private static String textoAleatorio(Random random) {
        int tamanho = random.nextInt(120) + 1;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < tamanho; i++) {
            if (random.nextInt(5) == 0) {
                sb.append(LIXO.charAt(random.nextInt(LIXO.length())));
            } else {
                sb.append((char) ('A' + random.nextInt(26)));
            }
        }
        return sb.toString();
    }
}
