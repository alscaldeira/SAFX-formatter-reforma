package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

/** Conversores SAFXnn -> CSV: escape, BOM, cabeçalho e arquivos ausentes/vazios. */
public final class TestesCsv {

    private TestesCsv() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Conversores SAFXnn -> CSV");

        Path caso = Fixtures.caso(base, "csv");
        // observação com ";" e aspas, que precisam de escape no CSV
        String xml = Fixtures.nfe("8001", true, "42105890000901", "46754545000194")
                .replace("</infNFe>", "<infAdic><infCpl>PEDIDO 1; ITEM \"A\"; VER OBS</infCpl></infAdic></infNFe>");
        Fixtures.escrever(caso, "nota.xml", xml);
        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeXml(caso.resolve("xmls").toString());

        Path csv = caso.resolve("out/SAFX07.csv");
        byte[] bytes = Files.readAllBytes(csv);
        Assercoes.check("csv: começa com BOM UTF-8",
                bytes.length > 3 && (bytes[0] & 0xFF) == 0xEF && (bytes[1] & 0xFF) == 0xBB
                        && (bytes[2] & 0xFF) == 0xBF, "bytes iniciais");

        List<String> linhas = Files.readAllLines(csv, StandardCharsets.UTF_8);
        Assercoes.check("csv: cabeçalho + 1 linha", linhas.size() == 2, "" + linhas.size());
        Assercoes.check("csv: cabeçalho com 302 colunas",
                linhas.get(0).replace("﻿", "").split(";", -1).length == 302,
                "" + linhas.get(0).split(";", -1).length);
        Assercoes.check("csv: valor com ';' foi escapado entre aspas",
                linhas.get(1).contains("\"PEDIDO 1; ITEM \"\"A\"\"; VER OBS\""),
                trecho(linhas.get(1)));

        // cabeçalho de cada CSV tem de bater com a quantidade de campos do leiaute
        for (String layout : new String[]{"SAFX04", "SAFX07", "SAFX08"}) {
            List<String> l = Files.readAllLines(caso.resolve("out/" + layout + ".csv"),
                    StandardCharsets.UTF_8);
            int colunas = l.get(0).replace("﻿", "").split(";", -1).length;
            Assercoes.check("csv: cabeçalho do " + layout + " bate com o leiaute",
                    colunas == Leiaute.quantidadeCampos(layout),
                    colunas + " x " + Leiaute.quantidadeCampos(layout));
        }

        // SAFX49 vazio: o CSV sai só com cabeçalho, sem estourar
        List<String> vazio = Files.readAllLines(caso.resolve("out/SAFX49.csv"), StandardCharsets.UTF_8);
        Assercoes.check("csv: SAFX49 vazio gera só o cabeçalho", vazio.size() == 1, "" + vazio.size());

        // arquivo de origem ausente
        Path semArquivo = Fixtures.caso(base, "csv-sem-origem");
        System.setProperty("user.dir", semArquivo.resolve("out").toString());
        String erro = null;
        try {
            Safx07ToCsv.convertToCsv();
        } catch (IOException e) {
            erro = e.getMessage();
        }
        Assercoes.check("csv: erro claro quando o SAFX07.txt não existe",
                erro != null && erro.contains("não encontrado"), String.valueOf(erro));
    }

    private static String trecho(String s) {
        return s.length() > 80 ? s.substring(0, 80) + "..." : s;
    }
}
