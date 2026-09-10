package com.caldeira;

import com.caldeira.service.Conversao;
import com.caldeira.service.SpedToImportacaoCsv;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

/**
 * Fluxo SPED — a área com histórico de bug de índice (ver SAFX_RESEARCH.md) e
 * que até agora só tinha conferência manual.
 */
public final class TestesFluxoSped {

    private TestesFluxoSped() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Fluxo SPED");

        contagemBasica(base);
        participanteSemCnpj(base);
        estabelecimentoDoBlocoC010(base);
        linhaComCamposFaltando(base);
        notaSemItem(base);
        importacaoCsv(base);
    }

    private static void contagemBasica(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-contagem");
        Path sped = Fixtures.escreverSped(caso, "sped.txt", Fixtures.sped(3, 4), StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeSped(sped.toString());

        Assercoes.check("sped: 3 capas no SAFX07", Fixtures.linhas(caso, "SAFX07").size() == 3,
                "" + Fixtures.linhas(caso, "SAFX07").size());
        Assercoes.check("sped: 12 itens no SAFX08", Fixtures.linhas(caso, "SAFX08").size() == 12,
                "" + Fixtures.linhas(caso, "SAFX08").size());
        Assercoes.check("sped: 1 participante no SAFX04", Fixtures.linhas(caso, "SAFX04").size() == 1,
                "" + Fixtures.linhas(caso, "SAFX04").size());

        String[] capa = Fixtures.campos(caso, "SAFX07", 0);
        Assercoes.check("sped: COD_FIS_JUR com prefixo M", capa[6].equals("M9003065"), capa[6]);
        Assercoes.check("sped: data de emissão convertida para YYYYMMDD",
                capa[10].equals("20260313"), capa[10]);
        Assercoes.check("sped: VLR_PRODUTO = soma dos itens (4 x 100,00)",
                capa[21].equals("0000000000040000"), capa[21]);
        Assercoes.check("sped: VLR_TOT_NOTA = VL_DOC do C100",
                capa[22].equals("0000000000100000"), capa[22]);

        String[] item = Fixtures.campos(caso, "SAFX08", 0);
        Assercoes.check("sped: NCM veio do registro 0200", item[25].equals("76011000"), item[25]);
        Assercoes.check("sped: quantidade com 6 decimais", item[23].equals("0000000010000000"), item[23]);
        Assercoes.check("sped: CST do ICMS quebrado em A/B",
                item[29].equals("0") && item[30].equals("00"), item[29] + "/" + item[30]);
    }

    private static void participanteSemCnpj(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-part-exterior");
        String sped = Fixtures.sped(1, 1).replace(
                "|0150|9003065|FORNECEDOR TESTE|1058|11222333000181||111222333|3550308|||RUA X|10||CENTRO|",
                "|0150|9003065|FORNECEDOR DO EXTERIOR|2496|||||||OLD TRENTON ROAD||||");
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", sped, StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        SpedToSafx04.convertFromSped(arquivo.toString());

        String[] p = Fixtures.campos(caso, "SAFX04", 0);
        Assercoes.check("sped: participante do exterior com IND_CONTEM_COD 4", p[3].equals("4"), p[3]);
        Assercoes.check("sped: participante do exterior com UF EX", p[18].equals("EX"), p[18]);
        Assercoes.check("sped: participante do exterior sem CPF/CGC", p[5].equals("@"), p[5]);
        // COD_PAIS tem 3 posições no leiaute e o SPED traz o código BACEN de 4
        // dígitos: o campo fica nulo até existir o de-para BACEN -> TAX ONE.
        Assercoes.check("sped: código BACEN de 4 dígitos não é truncado no COD_PAIS",
                p[20].equals("@"), p[20]);
    }

    private static void estabelecimentoDoBlocoC010(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-c010");
        // C010 de uma filial diferente do 0000: a capa deve seguir o C010
        String sped = Fixtures.sped(1, 1)
                .replace("|C010|72977242000140|0|", "|C010|72977242000302|0|")
                .replace("|C100|0|1|", "|C100|1|1|"); // entrada de terceiro: usa o C010
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", sped, StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        SpedToSafx07.convertFromSped(arquivo.toString());

        String[] capa = Fixtures.campos(caso, "SAFX07", 0);
        Assercoes.check("sped: COD_ESTAB veio do bloco C010 aberto", capa[1].equals("0003"), capa[1]);
    }

    private static void linhaComCamposFaltando(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-truncado");
        String sped = Fixtures.sped(1, 1) + "|C170|2|ITEM1|SO ATE AQUI|\n";
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", sped, StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        SpedToSafx08.convertFromSped(arquivo.toString());

        List<String> itens = Fixtures.linhas(caso, "SAFX08");
        Assercoes.check("sped: linha truncada não derruba a conversão", itens.size() == 2,
                "" + itens.size());
        Assercoes.check("sped: linha truncada gera registro com 262 campos",
                itens.get(1).split("\t", -1).length == 262,
                "" + itens.get(1).split("\t", -1).length);
    }

    private static void notaSemItem(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-sem-item");
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", Fixtures.sped(2, 0),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        SpedToSafx07.convertFromSped(arquivo.toString());
        SpedToSafx08.convertFromSped(arquivo.toString());
        SpedToSafx49.convertFromSped(arquivo.toString());

        Assercoes.check("sped: capas geradas mesmo sem item",
                Fixtures.linhas(caso, "SAFX07").size() == 2,
                "" + Fixtures.linhas(caso, "SAFX07").size());
        Assercoes.check("sped: SAFX08 vazio quando não há C170",
                Fixtures.linhas(caso, "SAFX08").isEmpty(),
                "" + Fixtures.linhas(caso, "SAFX08").size());
        Assercoes.check("sped: VLR_PRODUTO zerado quando não há item",
                Fixtures.campos(caso, "SAFX07", 0)[21].equals("0000000000000000"),
                Fixtures.campos(caso, "SAFX07", 0)[21]);
    }

    private static void importacaoCsv(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "sped-importacao-csv");
        Path arquivo = Fixtures.escreverSped(caso, "sped.txt", Fixtures.sped(2, 2),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", caso.resolve("out").toString());
        SpedToImportacaoCsv.convertFromSped(arquivo.toString());

        Path csv = caso.resolve("out/IMPORTACAO.csv");
        Assercoes.check("sped: IMPORTACAO.csv gerado", Files.exists(csv), csv.toString());
        if (!Files.exists(csv)) return;
        List<String> linhas = Files.readAllLines(csv, StandardCharsets.UTF_8);
        // o relatório é por nota (uma linha por C100 com C120), não por item
        Assercoes.check("sped: IMPORTACAO.csv com cabeçalho + 2 notas", linhas.size() == 3,
                "" + linhas.size());
    }
}
