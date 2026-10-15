package com.caldeira;

import com.caldeira.service.ConversorXml;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Verifica as regras de conversão portadas do sistema em C# (NF-e -> SAFX07/08/04/
 * 2013/2043 pelos scripts SQL, CT-e -> SAFX07/08/04 pelo LerCTes, classificação do
 * lote). Cada caso aponta a regra de origem em MAPEAMENTO-REGRAS-CSHARP-PARA-JAVA.md.
 *
 * Autônoma: não depende das demais suítes, que ainda referenciam as classes
 * SpedToSafxNN removidas. Rodar a partir da raiz de código-fonte-reforma:
 *
 * <pre>
 * javac -encoding UTF-8 -d /tmp/safx-classes $(find src/main/java -name '*.java') src/test/java/com/caldeira/Assercoes.java src/test/java/com/caldeira/TestesRegrasCsharp.java
 * java -cp /tmp/safx-classes:src/main/resources com.caldeira.TestesRegrasCsharp
 * </pre>
 */
public final class TestesRegrasCsharp {

    private static final String MEU_CNPJ = "42105890000901";

    private TestesRegrasCsharp() {
    }

    public static void main(String[] args) throws Exception {
        Path base = Files.createTempDirectory("safx-regras-");
        String dirOriginal = System.getProperty("user.dir");
        try {
            classificacaoELote(base.resolve("lote"));
            nfe(base.resolve("nfe"));
            cte(base.resolve("cte"));
        } finally {
            System.setProperty("user.dir", dirOriginal);
        }
        System.out.println("\n" + Assercoes.passou() + " ok, " + Assercoes.falhou() + " falhas");
        if (Assercoes.falhou() > 0) System.exit(1);
    }

    // ------------------------------------------------------------------ lote

    private static void classificacaoELote(Path caso) throws Exception {
        Assercoes.secao("lote misto: classificação por conteúdo e saídas");
        Path in = Files.createDirectories(caso.resolve("in"));
        Path out = Files.createDirectories(caso.resolve("out"));
        Files.writeString(in.resolve("nfe.xml"), nfe("4.00", 1, 1, "6101", "00", "", "SP", ""));
        // CT-e que carrega infDoc/infNFe: não pode ser classificado como NF-e
        Files.writeString(in.resolve("cte.xml"), cte(MEU_CNPJ, "5352", "2026-09-01T23:30:00-03:00",
                "ICMS00", "Fulano", true));
        Files.writeString(in.resolve("evento.xml"),
                "<procEventoCTe><evento><infEvento><chCTe>1</chCTe></infEvento></evento></procEventoCTe>");

        List<String> avisos = executar(out, in, false, false);
        chk("NF-e gerou SAFX07 com a nota", linhas(out, "SAFX07").size() == 2,
                "linhas=" + linhas(out, "SAFX07").size());
        Map<String, String> cteCapa = achar(out, "SAFX07", "COD_DOCTO", "CT-E");
        chk("CT-e com infDoc/infNFe foi para o CT-e (não para NF-e)", cteCapa != null, "");
        chk("evento de CT-e não vira documento",
                avisos.stream().anyMatch(a -> a.contains("evento.xml")), avisos.toString());
        chk("SAFX04 reúne o destinatário da NF-e e o emitente do CT-e",
                linhas(out, "SAFX04").size() >= 2, "" + linhas(out, "SAFX04").size());
        chk("SAFX2013/2043 só têm itens de NF-e", linhas(out, "SAFX2013").size() == 1
                && linhas(out, "SAFX2043").size() == 1, "");

        Path soNfse = Files.createDirectories(caso.resolve("so-cte-in"));
        Files.writeString(soNfse.resolve("cte.xml"), cte("11222333000181", "5352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Fulano", false));
        Path out2 = Files.createDirectories(caso.resolve("out2"));
        executar(out2, soNfse, false, false);
        chk("lote só de CT-e não exige NF-e/NFS-e e gera SAFX07/08/04",
                linhas(out2, "SAFX07").size() == 1 && linhas(out2, "SAFX08").size() == 1
                        && linhas(out2, "SAFX04").size() == 1, "");
        chk("lote só de CT-e não gera arquivos da Reforma",
                !Files.exists(out2.resolve("SAFX3007.csv")), "");
    }

    // ------------------------------------------------------------------ NF-e

    private static void nfe(Path caso) throws Exception {
        Assercoes.secao("NF-e -> SAFX07/08/04/2013/2043 (ScriptSQL)");

        // saída, venda normal
        Map<String, String> c = capaNfe(caso.resolve("a"), nfe("4.00", 1, 1, "6101", "00", "", "SP", ""));
        chk("MOVTO_E_S: tpNF=1 -> 9", "9".equals(c.get("MOVTO_E_S")), c.get("MOVTO_E_S"));
        chk("COD_DOCTO NFE, modelo 55, série fixa 1", "NFE".equals(c.get("COD_DOCTO"))
                && "55".equals(c.get("COD_MODELO")) && "1".equals(c.get("SERIE_DOCFIS")), c.toString());
        chk("NUM_DOCFIS 12 posições", "000000012345".equals(unico(c.get("NUM_DOCFIS"))),
                c.get("NUM_DOCFIS"));
        chk("COD_CFO inalterado na saída", "6101".equals(c.get("COD_CFO")), c.get("COD_CFO"));
        chk("empresa/estab fora da lista -> 001/0009", "001".equals(unico(c.get("COD_EMPRESA")))
                && "0009".equals(unico(c.get("COD_ESTAB"))), c.get("COD_ESTAB"));
        chk("COD_FIS_JUR é o CNPJ do destinatário", "11222333000181".equals(unico(c.get("COD_FIS_JUR"))),
                c.get("COD_FIS_JUR"));
        chk("VLR_PRODUTO em centavos, 17 posições",
                "00000000000100050".equals(unico(c.get("VLR_PRODUTO"))), c.get("VLR_PRODUTO"));
        chk("VLR_ALIQ_PIS 1,65 -> 0016500", "0016500".equals(unico(c.get("VLR_ALIQ_PIS"))),
                c.get("VLR_ALIQ_PIS"));
        chk("COD_SIT_PIS com 2 dígitos", "01".equals(unico(c.get("COD_SIT_PIS"))), c.get("COD_SIT_PIS"));
        chk("IND_TP_FRETE modFrete=0 -> 1", "1".equals(c.get("IND_TP_FRETE")), c.get("IND_TP_FRETE"));
        chk("NUM_CONTROLE_DOCTO R + número sem zeros", "R12345".equals(c.get("NUM_CONTROLE_DOCTO")),
                c.get("NUM_CONTROLE_DOCTO"));
        chk("NUM_AUTENTIC_NFE é a chave de 44 dígitos",
                unico(c.get("NUM_AUTENTIC_NFE")).length() == 44, c.get("NUM_AUTENTIC_NFE"));

        Map<String, String> i = itemNfe(caso.resolve("a"));
        chk("QUANTIDADE = qCom (4 casas) + '00' em 17 posições",
                "00000000025000000".equals(unico(i.get("QUANTIDADE"))), i.get("QUANTIDADE"));
        chk("VLR_UNIT em 19 posições", "0000000000000010550".equals(unico(i.get("VLR_UNIT"))),
                i.get("VLR_UNIT"));
        chk("DESCRICAO_COMPL sem acento", "ACUCAR REFINADO".equals(i.get("DESCRICAO_COMPL")),
                i.get("DESCRICAO_COMPL"));
        chk("TRIB_ICMS CST 00 -> 1", "1".equals(i.get("TRIB_ICMS")), i.get("TRIB_ICMS"));
        chk("COD_TRIB_IPI sempre 53", "53".equals(i.get("COD_TRIB_IPI")), i.get("COD_TRIB_IPI"));
        chk("COD_SITUACAO_B é o CST inteiro (00 chega como 0)", "0".equals(i.get("COD_SITUACAO_B")),
                i.get("COD_SITUACAO_B"));
        chk("COD_FEDERAL orig 0 -> 1", "1".equals(i.get("COD_FEDERAL")), i.get("COD_FEDERAL"));
        chk("DAT_LANC_PIS_COFINS = data de saída", "20260315".equals(i.get("DAT_LANC_PIS_COFINS")),
                i.get("DAT_LANC_PIS_COFINS"));
        chk("DATA_FISCAL = emissão, no fuso do próprio documento (23:30-03:00 fica no dia)",
                "20260314".equals(i.get("DATA_FISCAL")), i.get("DATA_FISCAL"));

        Map<String, String> p = linha(caso.resolve("a"), "SAFX2013");
        chk("SAFX2013: DATA_PRODUTO = AAAAMM01, CLAS_ITEM 99",
                "20260301".equals(p.get("DATA_PRODUTO")) && "99".equals(p.get("CLAS_ITEM")), p.toString());
        Map<String, String> n = linha(caso.resolve("a"), "SAFX2043");
        chk("SAFX2043: DATA_NBM/SECAO_IPI '0'", "0".equals(n.get("DATA_NBM"))
                && "0".equals(n.get("SECAO_IPI")), n.toString());

        // entrada própria: tpNF=0
        Map<String, String> e = capaNfe(caso.resolve("b"), nfe("4.00", 0, 4, "2207", "00", "", "SP", ""));
        chk("MOVTO_E_S: tpNF=0 -> 4", "4".equals(e.get("MOVTO_E_S")), e.get("MOVTO_E_S"));
        chk("NORM_DEV: finNFe=4 -> 2", "2".equals(e.get("NORM_DEV")), e.get("NORM_DEV"));
        chk("CFOP 2207 de entrada vira 6207", "6207".equals(e.get("COD_CFO")), e.get("COD_CFO"));
        chk("tpNF=0: a contraparte é o emitente", "11222333000181".equals(unico(e.get("COD_FIS_JUR")))
                || MEU_CNPJ.equals(unico(e.get("COD_FIS_JUR"))), e.get("COD_FIS_JUR"));

        // simples nacional (CSOSN): orig lido de "Orig" -> 0; CST nulo -> TRIB_ICMS 3
        Map<String, String> sn = itemNfe2(caso.resolve("c"), nfe("4.00", 1, 1, "5102", "SN101", "", "SP", ""));
        chk("ICMSSN101: sem CST, TRIB_ICMS 3 e BASE_ICMS = vProd",
                "3".equals(sn.get("TRIB_ICMS")) && "@".equals(sn.get("COD_SITUACAO_B"))
                        && "00000000000100050".equals(unico(sn.get("BASE_ICMS"))), sn.toString());

        // exportação
        Map<String, String> ex = linha2(caso.resolve("d"), nfe("4.00", 1, 1, "7101", "00", "", "EX", ""), "SAFX04");
        chk("SAFX04 destinatário EX: IND_CONTEM_COD 4, CPF_CGC vazio, INSC ISENTO",
                "4".equals(ex.get("IND_CONTEM_COD")) && "ISENTO".equals(ex.get("INSC_ESTADUAL")), ex.toString());

        // IE isenta (99) e CEP/fone
        Map<String, String> pf = linha2(caso.resolve("e"), nfe("4.00", 1, 1, "6101", "00", "ISENTO", "SP", ""),
                "SAFX04");
        chk("IE ISENTO -> 99", "99".equals(unico(pf.get("INSC_ESTADUAL"))), pf.get("INSC_ESTADUAL"));
        chk("COD_PAIS = 3 primeiros dígitos de cPais (1058 -> 105)", "105".equals(unico(pf.get("COD_PAIS"))),
                pf.get("COD_PAIS"));
        chk("COD_MUNICIPIO = posições 3 a 7 do IBGE", "50308".equals(unico(pf.get("COD_MUNICIPIO"))),
                pf.get("COD_MUNICIPIO"));
        chk("CEP com 8 posições", "01310100".equals(unico(pf.get("CEP"))), pf.get("CEP"));
        chk("TELEFONE fora de int vira 0 (Int32.TryParse)", "0".equals(pf.get("TELEFONE")),
                pf.get("TELEFONE"));

        // IND_TP_FRETE por versão
        chk("IND_TP_FRETE: contraparte fora da lista, modFrete 9 passa", "9".equals(
                capaNfe(caso.resolve("f"), nfe("4.00", 1, 1, "6101", "00", "", "SP", "9")).get("IND_TP_FRETE")), "");
        chk("IND_TP_FRETE: contraparte da lista, modFrete 9 -> 0", "0".equals(
                capaNfe(caso.resolve("g"), nfeParaDestino("03823704000152", "9")).get("IND_TP_FRETE")), "");
        chk("empresa pela lista: 03823704000152 -> 001/0001", "0001".equals(unico(
                capaNfe(caso.resolve("g"), nfeParaDestino("03823704000152", "9")).get("COD_ESTAB"))), "");
        chk("NF-e em duplicidade no lote é descartada",
                avisosDup(caso.resolve("h")), "");
    }

    private static boolean avisosDup(Path caso) throws Exception {
        Path in = Files.createDirectories(caso.resolve("in"));
        Path out = Files.createDirectories(caso.resolve("out"));
        Files.writeString(in.resolve("a.xml"), nfe("4.00", 1, 1, "6101", "00", "", "SP", ""));
        Files.writeString(in.resolve("b.xml"), nfe("4.00", 1, 1, "6101", "00", "", "SP", ""));
        List<String> avisos = executar(out, in, false, false);
        return linhas(out, "SAFX07").size() == 1 && avisos.stream().anyMatch(a -> a.contains("duplicidade"));
    }

    // ------------------------------------------------------------------ CT-e

    private static void cte(Path caso) throws Exception {
        Assercoes.secao("CT-e -> SAFX07/08/04 (HomeController.LerCTes)");

        Map<String, String> terceiro = unicoCte(caso.resolve("a"), cte("11222333000181", "5352",
                "2026-09-01T23:30:00-03:00", "ICMS00", "Transp SA", false), false, false, "SAFX07");
        chk("CFOP 5352 de terceiro -> 1352", "1352".equals(terceiro.get("COD_CFO")), terceiro.get("COD_CFO"));
        chk("terceiro: MOVTO_E_S 1", "1".equals(terceiro.get("MOVTO_E_S")), terceiro.get("MOVTO_E_S"));
        chk("DATA_EMISSAO na hora de parede do documento (23:30-03:00)",
                "20260901".equals(terceiro.get("DATA_EMISSAO")), terceiro.get("DATA_EMISSAO"));
        chk("COD_DOCTO CT-E para modelo 57", "CT-E".equals(terceiro.get("COD_DOCTO")), terceiro.get("COD_DOCTO"));
        chk("NUM_DOCFIS 12 posições", "000000000077".equals(unico(terceiro.get("NUM_DOCFIS"))),
                terceiro.get("NUM_DOCFIS"));
        chk("sem cálculo: PIS/COFINS zerados e COD_SIT 70", "70".equals(terceiro.get("COD_SIT_PIS"))
                && "00000000000000000".equals(unico(terceiro.get("VLR_PIS"))), terceiro.toString());
        chk("BASE_OUTR_ICMS = vBC quando não calcula ICMS",
                "00000000000100000".equals(unico(terceiro.get("BASE_OUTR_ICMS"))), terceiro.get("BASE_OUTR_ICMS"));
        chk("IND_TP_FRETE: toma3 0 -> 1", "1".equals(terceiro.get("IND_TP_FRETE")), terceiro.get("IND_TP_FRETE"));
        chk("IND_NAT_FRETE e IND_COMPRA_VENDA fixos", "9".equals(terceiro.get("IND_NAT_FRETE"))
                && "CP".equals(terceiro.get("IND_COMPRA_VENDA")), "");

        Map<String, String> calc = unicoCte(caso.resolve("b"), cte(MEU_CNPJ, "5352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Transp SA", false), true, true, "SAFX07");
        chk("CT-e próprio: CFOP intacto e saída 9", "5352".equals(calc.get("COD_CFO"))
                && "9".equals(calc.get("MOVTO_E_S")), calc.toString());
        chk("calcula PIS: 1,65% sobre vTPrest", "00000000000001650".equals(unico(calc.get("VLR_PIS"))),
                calc.get("VLR_PIS"));
        chk("calcula COFINS: 7,6% sobre vTPrest", "00000000000007600".equals(unico(calc.get("VLR_COFINS"))),
                calc.get("VLR_COFINS"));
        chk("COD_SIT_PIS 56 quando calcula", "56".equals(calc.get("COD_SIT_PIS")), "");
        chk("BASE_OUTR_ICMS não preenchida quando calcula ICMS", "@".equals(calc.get("BASE_OUTR_ICMS")),
                calc.get("BASE_OUTR_ICMS"));

        Map<String, String> item = unicoCte(caso.resolve("c"), cte("11222333000181", "6352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Transp SA", false), true, true, "SAFX08");
        chk("terceiro: PIS = (vTPrest - vICMS) x 1,65%", "00000000000001353".equals(unico(item.get("VLR_PIS")))
                , item.get("VLR_PIS"));
        chk("terceiro 6352 -> 2352", "2352".equals(item.get("COD_CFO")), item.get("COD_CFO"));
        chk("terceiro: COD_TRIB_IPI 03", "03".equals(unico(item.get("COD_TRIB_IPI"))), item.get("COD_TRIB_IPI"));
        chk("item único de produto FRETES", "1".equals(item.get("NUM_ITEM"))
                && "FRETES".equals(item.get("COD_PRODUTO")), item.toString());
        chk("calcula ICMS: alíquota x10000 em 7 posições", "0180000".equals(unico(item.get("VLR_ALIQ_ICMS"))),
                item.get("VLR_ALIQ_ICMS"));
        chk("calcula ICMS: TRIB_ICMS 1 e CST do XML", "1".equals(item.get("TRIB_ICMS"))
                && "00".equals(unico(item.get("COD_SITUACAO_B"))), item.toString());

        Map<String, String> semIcms = unicoCte(caso.resolve("d"), cte("11222333000181", "6352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Transp SA", false), false, false, "SAFX08");
        chk("sem calcular ICMS: CST 90, TRIB_ICMS 3, alíquota zerada", "90".equals(unico(semIcms.get("COD_SITUACAO_B")))
                && "3".equals(semIcms.get("TRIB_ICMS")) && "0000000".equals(unico(semIcms.get("VLR_ALIQ_ICMS"))),
                semIcms.toString());

        Map<String, String> fv = unicoCte(caso.resolve("e"), cte("11222333000181", "5352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Frete Valor", false), false, false, "SAFX07");
        chk("FRETE VALOR fatura vComp (não o total)", "00000000000040000".equals(unico(fv.get("VLR_PRODUTO")))
                , fv.get("VLR_PRODUTO"));

        Map<String, String> ic45 = unicoCte(caso.resolve("f"), cte("11222333000181", "5352",
                "2026-09-01T10:00:00-03:00", "ICMS45", "Transp SA", false), false, false, "SAFX07");
        chk("grupo ICMS diferente de ICMS00 é lido e avisado", "45".equals(unico(ic45.get("COD_SITUACAO_B"))), ic45.toString());

        chk("COD_CONTA 191000 e SUB_SERIE_DOCFIS @ no SAFX07 do CT-e", "191000".equals(unico(terceiro.get("COD_CONTA")))
                && "@".equals(terceiro.get("SUB_SERIE_DOCFIS")), terceiro.get("COD_CONTA"));
        chk("COD_CONTA 191000 e SUB_SERIE_DOCFIS @ no SAFX08 do CT-e", "191000".equals(unico(item.get("COD_CONTA")))
                && "@".equals(item.get("SUB_SERIE_DOCFIS")), item.get("COD_CONTA"));

        Path doisIn = Files.createDirectories(caso.resolve("dois/in"));
        Path doisOut = Files.createDirectories(caso.resolve("dois/out"));
        Files.writeString(doisIn.resolve("a.xml"), cte("11222333000181", "5352", "2026-09-01T10:00:00-03:00", "ICMS00", "Transp SA", false));
        Files.writeString(doisIn.resolve("b.xml"), cte("11222333000181", "5352", "2026-09-02T10:00:00-03:00", "ICMS00", "Transp SA", false)
                .replace("<nCT>77</nCT>", "<nCT>78</nCT>"));
        executar(doisOut, doisIn, false, false);
        chk("SAFX04: uma linha por CT-e, sem dedupe por CNPJ", linhas(doisOut, "SAFX04").size() == 2, "" + linhas(doisOut, "SAFX04").size());
        chk("SAFX04 com as 80 colunas do leiaute", linhas(doisOut, "SAFX04").get(0).size() == 80,
                "" + linhas(doisOut, "SAFX04").get(0).size());

        Map<String, String> pf = unicoCte(caso.resolve("g"), cte("11222333000181", "5352",
                "2026-09-01T10:00:00-03:00", "ICMS00", "Transp SA", false), false, false, "SAFX04");
        chk("SAFX04 do CT-e: COD_FIS_JUR = CNPJ cru do emitente, IND_FIS_JUR 2",
                "11222333000181".equals(unico(pf.get("COD_FIS_JUR"))) && "2".equals(pf.get("IND_FIS_JUR")), pf.toString());
        chk("SAFX04 do CT-e: COD_PAIS 105 e COD_MUNICIPIO 5 últimas posições",
                "105".equals(unico(pf.get("COD_PAIS"))) && "50308".equals(unico(pf.get("COD_MUNICIPIO"))), pf.toString());
        chk("SAFX04 do CT-e: TELEFONE = 10 últimas posições", "1133334444".equals(unico(pf.get("TELEFONE"))),
                pf.get("TELEFONE"));
    }

    // ------------------------------------------------------------- utilitários

    private static List<String> executar(Path out, Path in, boolean pisCofins, boolean icms) throws IOException {
        System.setProperty("user.dir", out.toString());
        return ConversorXml.converter(in.toString(), pisCofins, icms);
    }

    private static Map<String, String> unicoCte(Path caso, String xml, boolean pisCofins, boolean icms, String layout)
            throws Exception {
        Path in = Files.createDirectories(caso.resolve("in"));
        Path out = Files.createDirectories(caso.resolve("out"));
        Files.writeString(in.resolve("cte.xml"), xml);
        executar(out, in, pisCofins, icms);
        return linha(caso, layout);
    }

    private static Map<String, String> capaNfe(Path caso, String xml) throws Exception {
        return linha2(caso, xml, "SAFX07");
    }

    private static Map<String, String> itemNfe(Path caso) throws Exception {
        return linha(caso, "SAFX08");
    }

    private static Map<String, String> itemNfe2(Path caso, String xml) throws Exception {
        return linha2(caso, xml, "SAFX08");
    }

    private static Map<String, String> linha2(Path caso, String xml, String layout) throws Exception {
        Path in = Files.createDirectories(caso.resolve("in"));
        Path out = Files.createDirectories(caso.resolve("out"));
        Files.writeString(in.resolve("nfe.xml"), xml);
        executar(out, in, false, false);
        return linha(caso, layout);
    }

    private static Map<String, String> linha(Path caso, String layout) throws IOException {
        List<Map<String, String>> ls = linhas(caso.resolve("out"), layout);
        return ls.isEmpty() ? new HashMap<>() : ls.get(0);
    }

    private static Map<String, String> achar(Path out, String layout, String coluna, String valor) throws IOException {
        for (Map<String, String> l : linhas(out, layout)) {
            if (valor.equals(l.get(coluna))) return l;
        }
        return null;
    }

    /** Lê o CSV nomeado de um layout: uma lista de mapa coluna -> valor. */
    private static List<Map<String, String>> linhas(Path out, String layout) throws IOException {
        Path csv = out.resolve(layout + ".csv");
        List<Map<String, String>> res = new ArrayList<>();
        if (!Files.exists(csv)) return res;
        List<String> todas = Files.readAllLines(csv, StandardCharsets.UTF_8);
        String[] cab = todas.get(0).replace("﻿", "").split(";", -1);
        for (int i = 1; i < todas.size(); i++) {
            String[] v = todas.get(i).split(";", -1);
            Map<String, String> m = new HashMap<>();
            for (int k = 0; k < cab.length && k < v.length; k++) m.put(cab[k], v[k]);
            res.add(m);
        }
        return res;
    }

    private static void chk(String nome, boolean ok, String detalhe) {
        Assercoes.check(nome, ok, detalhe == null || detalhe.length() <= 300 ? detalhe : detalhe.substring(0, 300) + "...");
    }

    /** O CSV protege zeros à esquerda com ="valor" (e aspas dobradas); o teste compara o valor. */
    private static String unico(String celula) {
        if (celula == null) return "";
        String s = celula;
        if (s.length() >= 2 && s.startsWith("\"") && s.endsWith("\"")) {
            s = s.substring(1, s.length() - 1).replace("\"\"", "\"");
        }
        if (s.startsWith("=\"") && s.endsWith("\"")) return s.substring(2, s.length() - 1);
        return s;
    }

    // --------------------------------------------------------------- fixtures

    private static String nfeParaDestino(String cnpjDest, String modFrete) {
        return nfe("4.00", 1, 1, "6101", "00", "", "SP", modFrete).replace("11222333000181", cnpjDest);
    }

    /** NF-e mínima com 1 item. grupo: "00" = ICMS00; "SN101" = ICMSSN101. */
    private static String nfe(String versao, int tpNf, int finNfe, String cfop, String grupo, String ie, String uf,
                              String modFrete) {
        String destDoc = "EX".equals(uf) ? "<idEstrangeiro>ABC123</idEstrangeiro>" : "<CNPJ>11222333000181</CNPJ>";
        String ieTag = ie.isEmpty() ? "" : "<IE>" + ie + "</IE>";
        String icms = grupo.equals("SN101")
                ? "<ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>1.00</pCredSN><vCredICMSSN>1.00</vCredICMSSN></ICMSSN101></ICMS>"
                : "<ICMS><ICMS00><orig>0</orig><CST>00</CST><modBC>3</modBC><vBC>1000.50</vBC><pICMS>18.00</pICMS><vICMS>180.09</vICMS></ICMS00></ICMS>";
        String mod = modFrete.isEmpty() ? "0" : modFrete;
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\""
                + versao + "\"><NFe><infNFe versao=\"" + versao + "\" Id=\"NFe21260342105890000901550010000123451155885785\">"
                + "<ide><cUF>21</cUF><cNF>15588578</cNF><natOp>Venda</natOp><mod>55</mod><serie>1</serie><nNF>12345</nNF>"
                + "<dhEmi>2026-03-14T23:30:00-03:00</dhEmi><dhSaiEnt>2026-03-15T08:00:00-03:00</dhSaiEnt><tpNF>" + tpNf
                + "</tpNF><idDest>2</idDest><cMunFG>2111300</cMunFG><tpImp>1</tpImp><tpEmis>1</tpEmis><cDV>5</cDV>"
                + "<tpAmb>1</tpAmb><finNFe>" + finNfe + "</finNFe><indFinal>0</indFinal><indPres>9</indPres>"
                + "<procEmi>0</procEmi><verProc>X</verProc></ide>"
                + "<emit><CNPJ>" + MEU_CNPJ + "</CNPJ><xNome>EMITENTE LTDA</xNome><enderEmit><xLgr>Rua A</xLgr><nro>1</nro>"
                + "<xBairro>Centro</xBairro><cMun>2111300</cMun><xMun>SAO LUIS</xMun><UF>MA</UF><CEP>65000000</CEP>"
                + "<cPais>1058</cPais><xPais>Brasil</xPais><fone>9833334444</fone></enderEmit><IE>123456789</IE><CRT>3</CRT></emit>"
                + "<dest>" + destDoc + "<xNome>Cliente Açúcar SA</xNome><enderDest><xLgr>Av B</xLgr><nro>2</nro>"
                + "<xBairro>Bela Vista</xBairro><cMun>3550308</cMun><xMun>SAO PAULO</xMun><UF>" + uf + "</UF><CEP>01310100</CEP>"
                + "<cPais>1058</cPais><xPais>Brasil</xPais><fone>11999998888</fone></enderDest>"
                + ieTag + "<indIEDest>" + (ie.isEmpty() ? "1" : "2") + "</indIEDest></dest>"
                + "<det nItem=\"1\"><prod><cProd>P1</cProd><cEAN>SEM GTIN</cEAN><xProd>AÇÚCAR REFINADO</xProd><NCM>17019900</NCM>"
                + "<CFOP>" + cfop + "</CFOP><uCom>KG</uCom><qCom>25.0000</qCom><vUnCom>1.0550</vUnCom><vProd>1000.50</vProd>"
                + "<cEANTrib>SEM GTIN</cEANTrib><uTrib>KG</uTrib><qTrib>25.0000</qTrib><vUnTrib>1.0550</vUnTrib><indTot>1</indTot></prod>"
                + "<imposto>" + icms + "<IPI><cEnq>999</cEnq><IPITrib><CST>50</CST><vBC>1000.50</vBC><pIPI>2.6000</pIPI><vIPI>26.01</vIPI></IPITrib></IPI>"
                + "<PIS><PISAliq><CST>01</CST><vBC>820.41</vBC><pPIS>1.6500</pPIS><vPIS>13.54</vPIS></PISAliq></PIS>"
                + "<COFINS><COFINSAliq><CST>01</CST><vBC>820.41</vBC><pCOFINS>7.6000</pCOFINS><vCOFINS>62.35</vCOFINS></COFINSAliq></COFINS>"
                + "</imposto></det>"
                + "<total><ICMSTot><vBC>1000.50</vBC><vICMS>180.09</vICMS><vICMSDeson>0.00</vICMSDeson><vBCST>0.00</vBCST><vST>0.00</vST>"
                + "<vProd>1000.50</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg><vDesc>0.00</vDesc><vII>0.00</vII><vIPI>26.01</vIPI>"
                + "<vPIS>13.54</vPIS><vCOFINS>62.35</vCOFINS><vOutro>0.00</vOutro><vNF>1026.51</vNF></ICMSTot></total>"
                + "<transp><modFrete>" + mod + "</modFrete><vol><qVol>1</qVol><pesoL>25.000</pesoL><pesoB>26.000</pesoB></vol></transp>"
                + "</infNFe></NFe></nfeProc>";
    }

    /** CT-e com os grupos que o LerCTes exige. grupoIcms: ICMS00 ou ICMS45. */
    private static String cte(String cnpjEmit, String cfop, String dhEmi, String grupoIcms, String nomeEmit,
                              boolean comInfDoc) {
        String icms = grupoIcms.equals("ICMS45")
                ? "<ICMS45><CST>45</CST></ICMS45>"
                : "<ICMS00><CST>00</CST><vBC>1000.00</vBC><pICMS>18.00</pICMS><vICMS>180.00</vICMS></ICMS00>";
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><cteProc xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"3.00\">"
                + "<CTe><infCte versao=\"3.00\" Id=\"CTe35260911222333000181570010000000771000000770\">"
                + "<ide><cUF>35</cUF><CFOP>" + cfop + "</CFOP><natOp>Prestacao</natOp><mod>57</mod><serie>1</serie><nCT>77</nCT>"
                + "<dhEmi>" + dhEmi + "</dhEmi><cDV>0</cDV><UFEnv>SP</UFEnv><UFFim>MA</UFFim>"
                + "<toma3><toma>0</toma></toma3></ide>"
                + "<compl><xObs>Observação do frete</xObs></compl>"
                + "<emit><CNPJ>" + cnpjEmit + "</CNPJ><IE>111222333</IE><xNome>" + nomeEmit + "</xNome><xFant>TRANSP</xFant>"
                + "<enderEmit><xLgr>Rua C</xLgr><nro>3</nro><xBairro>Centro</xBairro><cMun>3550308</cMun><xMun>SAO PAULO</xMun>"
                + "<CEP>01310100</CEP><UF>SP</UF><fone>1133334444</fone></enderEmit></emit>"
                + "<rem><CNPJ>99888777000166</CNPJ><xNome>Remetente</xNome></rem>"
                + "<dest><CNPJ>03823704000152</CNPJ><xNome>Destino</xNome><enderDest><cMun>2111300</cMun></enderDest></dest>"
                + "<vPrest><vTPrest>1000.00</vTPrest><vRec>1000.00</vRec><Comp><xNome>FRETE VALOR</xNome><vComp>400.00</vComp></Comp></vPrest>"
                + "<imp><ICMS>" + icms + "</ICMS></imp>"
                + (comInfDoc ? "<infCTeNorm><infDoc><infNFe><chave>21260342105890000901550010000553131155885785</chave></infNFe></infDoc></infCTeNorm>" : "")
                + "</infCte></CTe><protCTe><infProt><chCTe>35260911222333000181570010000000771000000770</chCTe>"
                + "<dhRecbto>2026-09-02T08:15:00-03:00</dhRecbto></infProt></protCTe></cteProc>";
    }
}
