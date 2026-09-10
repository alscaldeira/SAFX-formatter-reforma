package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.List;

/**
 * Conversão XML -> SAFX da Reforma Tributária (SAFX3007/3008/3009).
 *
 * O foco é o mapeamento dos grupos IBS/CBS e o comportamento enquanto a ordem
 * oficial dos campos não está confirmada no descritor.
 */
public final class TestesReforma {

    private TestesReforma() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Reforma Tributária (SAFX3007/3008/3009)");

        capaEItemDeMercadoria(base);
        itemDeServico(base);
        documentoSemIbsCbs(base);
        ordemConfirmadaGeraTxt(base);
    }

    private static void capaEItemDeMercadoria(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "reforma-nfe");
        Fixtures.escrever(caso, "venda.xml", Fixtures.nfeComIbsCbs());
        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeXml(caso.resolve("xmls").toString());

        List<String> capa = csv(caso, "SAFX3007");
        Assercoes.check("reforma: SAFX3007 com cabeçalho + 1 registro", capa.size() == 2,
                "" + capa.size());
        if (capa.size() < 2) return;

        Assercoes.check("reforma: SAFX3007.txt com os 130 campos do manual",
                camposDoTxt(caso, "SAFX3007") == 130, "" + camposDoTxt(caso, "SAFX3007"));
        Assercoes.check("reforma: SAFX3008.txt com os 88 campos do manual",
                camposDoTxt(caso, "SAFX3008") == 88, "" + camposDoTxt(caso, "SAFX3008"));

        Assercoes.check("reforma: TIPO_CHAVE_DFE = 2 para NF-e (lista do manual)",
                valor(capa, "TIPO_CHAVE_DFE").equals("2"), valor(capa, "TIPO_CHAVE_DFE"));
        Assercoes.check("reforma: VLR_TOT_BC_IBS_CBS = vBCIBSCBS",
                valor(capa, "VLR_TOT_BC_IBS_CBS").equals(numero("SAFX3007", "VLR_TOT_BC_IBS_CBS", 615567.03)),
                valor(capa, "VLR_TOT_BC_IBS_CBS"));
        Assercoes.check("reforma: VLR_TOT_IBS_UF da capa",
                valor(capa, "VLR_TOT_IBS_UF").equals(numero("SAFX3007", "VLR_TOT_IBS_UF", 615.57)), valor(capa, "VLR_TOT_IBS_UF"));
        Assercoes.check("reforma: VLR_TOT_CBS da capa",
                valor(capa, "VLR_TOT_CBS").equals(numero("SAFX3007", "VLR_TOT_CBS", 5540.10)), valor(capa, "VLR_TOT_CBS"));
        Assercoes.check("reforma: crédito presumido lido do total",
                valor(capa, "VLR_TOT_CRED_PRES_IBS").equals(numero("SAFX3007", "VLR_TOT_CRED_PRES_IBS", 12.34)),
                valor(capa, "VLR_TOT_CRED_PRES_IBS"));
        Assercoes.check("reforma: diferimento do IBS UF lido do total",
                valor(capa, "VLR_TOT_DIF_IBS_UF").equals(numero("SAFX3007", "VLR_TOT_DIF_IBS_UF", 1.11)), valor(capa, "VLR_TOT_DIF_IBS_UF"));
        Assercoes.check("reforma: VLR_TOT_NF_IBS_CBS_IS = vNFTot",
                valor(capa, "VLR_TOT_NF_IBS_CBS_IS").equals(numero("SAFX3007", "VLR_TOT_NF_IBS_CBS_IS", 790848.69)),
                valor(capa, "VLR_TOT_NF_IBS_CBS_IS"));

        List<String> item = csv(caso, "SAFX3008");
        Assercoes.check("reforma: SAFX3008 com cabeçalho + 1 item", item.size() == 2, "" + item.size());
        if (item.size() < 2) return;
        Assercoes.check("reforma: CST do item", valor(item, "CST_IBS_CBS").equals("000"),
                valor(item, "CST_IBS_CBS"));
        Assercoes.check("reforma: classificação tributária do item",
                valor(item, "CCLASS_IBS_CBS").equals("000001"), valor(item, "CCLASS_IBS_CBS"));
        Assercoes.check("reforma: alíquota do IBS UF no item (4 decimais)",
                valor(item, "ALIQ_IBS_UF").equals(numero("SAFX3008", "ALIQ_IBS_UF", 0.10)), valor(item, "ALIQ_IBS_UF"));
        Assercoes.check("reforma: alíquota da CBS no item",
                valor(item, "ALIQ_CBS").equals(numero("SAFX3008", "ALIQ_CBS", 0.90)), valor(item, "ALIQ_CBS"));
        Assercoes.check("reforma: item referencia a mesma nota da capa",
                valor(item, "NUM_DOCFIS").equals(valor(capa, "NUM_DOCFIS")),
                valor(item, "NUM_DOCFIS") + " x " + valor(capa, "NUM_DOCFIS"));
    }

    private static void itemDeServico(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "reforma-nfse");
        Fixtures.escrever(caso, "servico.xml", Fixtures.nfseComIbsCbs());
        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeXml(caso.resolve("xmls").toString());

        List<String> capa = csv(caso, "SAFX3007");
        Assercoes.check("reforma: TIPO_CHAVE_DFE = 1 para NFS-e (lista do manual)",
                valor(capa, "TIPO_CHAVE_DFE").equals("1"), valor(capa, "TIPO_CHAVE_DFE"));

        List<String> item = csv(caso, "SAFX3009");
        Assercoes.check("reforma: SAFX3009 com cabeçalho + 1 item", item.size() == 2, "" + item.size());
        if (item.size() < 2) return;
        Assercoes.check("reforma: NUM_ITEM fixo (um serviço por NFS-e)",
                valor(item, "NUM_ITEM").equals("00001"), valor(item, "NUM_ITEM"));
        Assercoes.check("reforma: município de incidência do IBS",
                valor(item, "COD_MUN_FT_GER_IBS_CBS").equals("2111300"),
                valor(item, "COD_MUN_FT_GER_IBS_CBS"));
        Assercoes.check("reforma: alíquota efetiva da CBS (só a NFS-e traz)",
                valor(item, "ALIQ_EFET_CBS").equals(numero("SAFX3009", "ALIQ_EFET_CBS", 0.63)), valor(item, "ALIQ_EFET_CBS"));
        Assercoes.check("reforma: percentual de redução do IBS municipal",
                valor(item, "PERC_RED_ALIQ_IBS_MUN").equals(numero("SAFX3009", "PERC_RED_ALIQ_IBS_MUN", 30.00)),
                valor(item, "PERC_RED_ALIQ_IBS_MUN"));
        Assercoes.check("reforma: IBS estadual do serviço fica na capa, não no item",
                valor(item, "ALIQ_IBS_UF").isEmpty(), valor(item, "ALIQ_IBS_UF"));
        Assercoes.check("reforma: CST do serviço vem do grupo IBSCBS do DPS",
                valor(item, "CST_IBS_CBS").equals("200"), valor(item, "CST_IBS_CBS"));
        Assercoes.check("reforma: NFS-e não gera item de mercadoria",
                csv(caso, "SAFX3008").size() == 1, "" + csv(caso, "SAFX3008").size());
        Assercoes.check("reforma: SAFX3009.txt com os 64 campos do manual",
                camposDoTxt(caso, "SAFX3009") == 64, "" + camposDoTxt(caso, "SAFX3009"));
    }

    private static void documentoSemIbsCbs(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "reforma-ausente");
        Fixtures.escrever(caso, "antiga.xml",
                Fixtures.nfe("60001", true, "42105890000901", "46754545000194"));
        System.setProperty("user.dir", caso.resolve("out").toString());
        List<String> avisos = Conversao.apartirDeXml(caso.resolve("xmls").toString());

        Assercoes.check("reforma: nota sem grupo IBS/CBS não gera SAFX3007",
                csv(caso, "SAFX3007").size() == 1, "" + csv(caso, "SAFX3007").size());
        Assercoes.check("reforma: ausência do grupo IBS/CBS vira aviso",
                avisos.stream().anyMatch(a -> a.contains("sem grupo IBS/CBS")), String.valueOf(avisos));
        Assercoes.check("reforma: SAFX07/08 continuam sendo gerados normalmente",
                Fixtures.linhas(caso, "SAFX07").size() == 1
                        && Fixtures.linhas(caso, "SAFX08").size() == 1,
                Fixtures.linhas(caso, "SAFX07").size() + "/" + Fixtures.linhas(caso, "SAFX08").size());
    }

    /**
     * Com a ordem oficial confirmada no descritor, o .txt posicional passa a ser
     * gerado — é o que vai acontecer quando as planilhas do cliente entrarem.
     */
    private static void ordemConfirmadaGeraTxt(Path base) throws IOException {
        Path caso = Fixtures.caso(base, "reforma-ordem-confirmada");
        Fixtures.escrever(caso, "venda.xml", Fixtures.nfeComIbsCbs());

        String descritor = "{\"layouts\":{"
                + "\"SAFX3007\":{\"fieldCountOficial\":3,\"ordemConfirmada\":true,\"fields\":["
                + "{\"campo\":\"COD_EMPRESA\",\"origem\":\"x\",\"tipo\":\"A\"},"
                + "{\"campo\":\"NUM_DOCFIS\",\"origem\":\"x\",\"tipo\":\"A\"},"
                + "{\"campo\":\"VLR_IBS\",\"origem\":\"x\",\"tipo\":\"N\",\"tamanho\":\"015V002\"}]},"
                + "\"SAFX3008\":{\"fieldCountOficial\":2,\"ordemConfirmada\":true,\"fields\":["
                + "{\"campo\":\"NUM_ITEM\",\"origem\":\"x\",\"tipo\":\"A\"},"
                + "{\"campo\":\"VLR_CBS\",\"origem\":\"x\",\"tipo\":\"N\",\"tamanho\":\"015V002\"}]},"
                + "\"SAFX3009\":{\"fieldCountOficial\":1,\"ordemConfirmada\":true,\"fields\":["
                + "{\"campo\":\"NUM_ITEM\",\"origem\":\"x\",\"tipo\":\"A\"}]}}}";
        Files.write(caso.resolve("out/safx-reforma-layout.json"),
                descritor.getBytes(StandardCharsets.UTF_8));

        System.setProperty("user.dir", caso.resolve("out").toString());
        List<String> avisos = Conversao.apartirDeXml(caso.resolve("xmls").toString());

        Path txt = caso.resolve("out/SAFX3007.txt");
        Assercoes.check("reforma: .txt posicional gerado com a ordem confirmada",
                Files.exists(txt), txt.toString());
        if (!Files.exists(txt)) return;

        List<String> linhas = Files.readAllLines(txt, StandardCharsets.UTF_8);
        Assercoes.check("reforma: .txt com 1 registro", linhas.size() == 1, "" + linhas.size());
        String[] campos = linhas.get(0).split("\t", -1);
        Assercoes.check("reforma: .txt segue a ordem declarada no descritor",
                campos.length == 3 && campos[1].equals("000000055313"),
                Arrays.toString(campos));
        Assercoes.check("reforma: descritor local tem prioridade sobre o embutido (3 campos, não 130)",
                campos.length == 3, "" + campos.length);
        Assercoes.check("reforma: conversão sem aviso de layout incompleto",
                avisos.stream().noneMatch(a -> a.contains("apenas em CSV")), String.valueOf(avisos));
    }

    // ------------------------------------------------------------ utilidades

    private static int camposDoTxt(Path caso, String layout) throws IOException {
        Path p = caso.resolve("out").resolve(layout + ".txt");
        if (!Files.exists(p)) return -1;
        List<String> linhas = Files.readAllLines(p, StandardCharsets.UTF_8);
        if (linhas.isEmpty()) return 0;
        return linhas.get(0).split("\t", -1).length;
    }

    private static List<String> csv(Path caso, String layout) throws IOException {
        Path p = caso.resolve("out").resolve(layout + ".csv");
        if (!Files.exists(p)) return List.of();
        return Files.readAllLines(p, StandardCharsets.UTF_8);
    }

    private static String valor(List<String> csv, String campo) {
        if (csv.size() < 2) return "";
        String[] cabecalho = csv.get(0).replace("﻿", "").split(";", -1);
        String[] valores = csv.get(1).split(";", -1);
        for (int i = 0; i < cabecalho.length; i++) {
            if (cabecalho[i].equals(campo)) return i < valores.length ? valores[i] : "";
        }
        return "";
    }

    /**
     * Formata como o manual manda: a largura sai do tamanho declarado do campo
     * (015V002 = 17 dígitos, 003V004 = 7), não de uma constante no teste.
     */
    private static String numero(String layout, String campo, double v) throws IOException {
        for (Leiaute.Campo c : Leiaute.camposReforma(layout)) {
            if (c.nome.equals(campo)) {
                int digitos = c.tamanho + c.decimais;
                return String.format(java.util.Locale.US, "%0" + (digitos + 1) + "." + c.decimais + "f", v)
                        .replace(".", "");
            }
        }
        throw new IOException("campo " + campo + " não existe em " + layout);
    }

}
