package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

public class SpedToSafx49 {

    // Estruturas de dados
    static class Fornecedor {
        String codPart;
        String nome;
    }

    static class Produto {
        String codItem;
        String descricao;
        String unidade;
        String ncm;
    }

    static class Importacao {
        // Layout oficial C120 (5 campos após o REG): COD_DOC_IMP(1) NUM_DOC_IMP(2)
        // PIS_IMP(3) COFINS_IMP(4) NUM_ACDRAW(5). Não existe data da DI no C120 —
        // DAT_DI só está disponível no XML da NF-e (grupo detDI).
        String tipoDocImp;   // COD_DOC_IMP: 1-DI, 2-DUIMP, 3-DSI
        String numDi;        // NUM_DOC_IMP
        double pisImp;        // PIS_IMP
        double cofinsImp;     // COFINS_IMP
    }

    static class ItemNota {
        int numItem;
        String codItem;
        double qtd;
        String unid;
        double vlUnit;
        double vlItem;
        double vlBcIcms;
        double aliqIcms;
        double vlIcms;
        double vlBcIpi;
        double aliqIpi;
        double vlIpi;
        double vlBcPis;
        double aliqPis;
        double vlPis;
        double vlBcCofins;
        double aliqCofins;
        double vlCofins;
        int cstIcms;
        int cstIpi;
        int cstPis;
        int cstCofins;
        int cfop;
        String descrCompl;
    }

    static class NotaFiscal {
        String codPart;
        String codMod;
        String codSit;
        String serie;
        String numDoc;
        String chaveNFe;
        String dtDoc;
        String dtES;
        double vlDoc;
        double vlIcms;
        double vlBcIcms;
        double vlIcmsSt;
        double vlBcIcmsSt;
        double vlIpi;
        double vlPis;
        double vlCofins;
        double vlFrete;
        double vlSeg;
        double vlOutr;
        double vlDesc;
        // Dados de importação (C120)
        Importacao importacao;
        List<ItemNota> itens = new ArrayList<>();
    }

    public static void convertFromSped(String inputFile) throws IOException {
        String outputFile = System.getProperty("user.dir") + "/SAFX49.txt";

        List<String> lines = Files.readAllLines(Paths.get(inputFile), Charset.forName("UTF-8"));
        Map<String, Fornecedor> fornecedores = new HashMap<>();
        Map<String, Produto> produtos = new HashMap<>();
        List<NotaFiscal> notas = new ArrayList<>();
        NotaFiscal notaAtual = null;

        for (String line : lines) {
            String[] campos = line.split(Pattern.quote("|"), -1);
            if (campos.length == 0) continue;
            String tipo = campos[1];

            switch (tipo) {
                case "0150":
                    Fornecedor f = new Fornecedor();
                    f.codPart = campos[1];
                    f.nome = campos[2];
                    fornecedores.put(f.codPart, f);
                    break;

                case "0200":
                    // Layout oficial 0200: COD_ITEM(1) DESCR_ITEM(2) COD_BARRA(3)
                    // COD_ANT_ITEM(4) UNID_INV(5) TIPO_ITEM(6) COD_NCM(7) EX_IPI(8)
                    // COD_GEN(9) COD_LST(10) ALIQ_ICMS(11). campos[1] é o literal "0200".
                    Produto p = new Produto();
                    p.codItem = campo(campos, 2);
                    p.descricao = campo(campos, 3);
                    p.unidade = campo(campos, 6);
                    p.ncm = campo(campos, 8);
                    produtos.put(p.codItem, p);
                    break;

                case "C100":
                    // Ver mapeamento oficial em SpedToSafx07 (campos[0] vazio, campos[1]="C100")
                    notaAtual = new NotaFiscal();
                    notaAtual.codPart = campo(campos, 4);
                    notaAtual.codMod = campo(campos, 5);
                    notaAtual.codSit = campo(campos, 6);
                    notaAtual.serie = campo(campos, 7);
                    notaAtual.numDoc = campo(campos, 8);
                    notaAtual.chaveNFe = campo(campos, 9);
                    notaAtual.dtDoc = campo(campos, 10);
                    notaAtual.dtES = campo(campos, 11);
                    notaAtual.vlDoc = parseDouble(campo(campos, 12));
                    notaAtual.vlDesc = parseDouble(campo(campos, 14));
                    notaAtual.vlFrete = parseDouble(campo(campos, 18));
                    notaAtual.vlSeg = parseDouble(campo(campos, 19));
                    notaAtual.vlOutr = parseDouble(campo(campos, 20));
                    notaAtual.vlBcIcms = parseDouble(campo(campos, 21));
                    notaAtual.vlIcms = parseDouble(campo(campos, 22));
                    notaAtual.vlBcIcmsSt = parseDouble(campo(campos, 23));
                    notaAtual.vlIcmsSt = parseDouble(campo(campos, 24));
                    notaAtual.vlIpi = parseDouble(campo(campos, 25));
                    notaAtual.vlPis = parseDouble(campo(campos, 26));
                    notaAtual.vlCofins = parseDouble(campo(campos, 27));
                    // Inicializa importação como null
                    notaAtual.importacao = null;
                    notas.add(notaAtual);
                    break;

                case "C120":
                    if (notaAtual == null) break;
                    // Layout oficial C120 (5 campos após o REG): COD_DOC_IMP(1) NUM_DOC_IMP(2)
                    // PIS_IMP(3) COFINS_IMP(4) NUM_ACDRAW(5). campos[1] é o literal "C120".
                    // Não existe data da DI neste registro — DAT_DI fica "@" (só disponível no XML da NF-e).
                    Importacao imp = new Importacao();
                    imp.tipoDocImp = campo(campos, 2);
                    imp.numDi = campo(campos, 3);
                    imp.pisImp = parseDouble(campo(campos, 4));
                    imp.cofinsImp = parseDouble(campo(campos, 5));
                    notaAtual.importacao = imp;
                    break;

                case "C170":
                    if (notaAtual == null) break;
                    // Ver mapeamento oficial em SpedToSafx07 (campos[1]="C170")
                    ItemNota item = new ItemNota();
                    item.numItem = (int) parseDouble(campo(campos, 2));
                    item.codItem = campo(campos, 3);
                    item.descrCompl = campo(campos, 4);
                    item.qtd = parseDouble(campo(campos, 5));
                    item.unid = campo(campos, 6);
                    // VL_ITEM já é o valor total do item — não é quantidade x preço unitário
                    item.vlItem = parseDouble(campo(campos, 7));
                    item.vlUnit = item.qtd != 0 ? item.vlItem / item.qtd : 0.0;
                    item.cstIcms = (int) parseDouble(campo(campos, 10));
                    item.cfop = (int) parseDouble(campo(campos, 11));
                    item.vlBcIcms = parseDouble(campo(campos, 13));
                    item.aliqIcms = parseDouble(campo(campos, 14));
                    item.vlIcms = parseDouble(campo(campos, 15));
                    item.cstIpi = (int) parseDouble(campo(campos, 20));
                    item.vlBcIpi = parseDouble(campo(campos, 22));
                    item.aliqIpi = parseDouble(campo(campos, 23));
                    item.vlIpi = parseDouble(campo(campos, 24));
                    item.cstPis = (int) parseDouble(campo(campos, 25));
                    item.vlBcPis = parseDouble(campo(campos, 26));
                    item.aliqPis = parseDouble(campo(campos, 27));
                    item.vlPis = parseDouble(campo(campos, 30));
                    item.cstCofins = (int) parseDouble(campo(campos, 31));
                    item.vlBcCofins = parseDouble(campo(campos, 32));
                    item.aliqCofins = parseDouble(campo(campos, 33));
                    item.vlCofins = parseDouble(campo(campos, 36));
                    notaAtual.itens.add(item);
                    break;

                default:
                    break;
            }
        }

        // Gerar arquivo SAFX49 (apenas para notas com importação)
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(outputFile))) {
            for (NotaFiscal nota : notas) {
                if (nota.importacao == null) continue;
                for (ItemNota item : nota.itens) {
                    writer.write(gerarLinha49(nota, item, produtos));
                    writer.newLine();
                }
            }
        }

        System.out.println("Arquivo SAFX49 gerado: " + outputFile);
        long totalItensImportacao = notas.stream()
                .filter(n -> n.importacao != null)
                .mapToInt(n -> n.itens.size()).sum();
        System.out.println("Total de itens de importação processados: " + totalItensImportacao);
    }

    private static String campo(String[] campos, int idx) {
        return idx < campos.length ? campos[idx] : "";
    }

    private static double parseDouble(String s) {
        if (s == null || s.trim().isEmpty()) return 0.0;
        s = s.replace(",", ".");
        try { return Double.parseDouble(s); } catch (NumberFormatException e) { return 0.0; }
    }

    private static String formatDate(String ddmmyyyy) {
        if (ddmmyyyy == null || ddmmyyyy.length() != 8) return "";
        return ddmmyyyy.substring(4) + ddmmyyyy.substring(2, 4) + ddmmyyyy.substring(0, 2);
    }

    private static String formatNumber(double v) {
        return String.format(Locale.US, "%017.2f", v).replace(".", "");
    }

    private static String padLeft(String s, int len) {
        if (s == null) s = "";
        while (s.length() < len) s = "0" + s;
        return s.length() > len ? s.substring(0, len) : s;
    }

    private static String gerarLinha49(NotaFiscal nota, ItemNota item, Map<String, Produto> produtos) {
        // Leiaute real do SAFX49 (72 posições, estimado a partir do processo SAP
        // ZSAFE041 — ver safx-layout.json). Campos que o SPED genuinamente não
        // fornece (dados aduaneiros presentes só no XML da NF-e: DAT_DI, país,
        // moeda, II, IOF, despesas aduaneiras, DAT_DESEMBARACO) ficam "@" —
        // não inferidos a partir de outros campos.
        String[] campos = new String[72];
        Arrays.fill(campos, "@");

        Produto produto = produtos.get(item.codItem); // NCM só é conhecido se houver 0200 correspondente
        double vlUnitReal = item.qtd != 0 ? item.vlItem / item.qtd : 0.0;

        campos[0] = "001";                                   // 1 - COD_EMPRESA
        campos[1] = "0001";                                  // 2 - COD_ESTAB
        // 3 - DAT_DI: não disponível no C120 (só no XML da NF-e) -> "@"
        campos[3] = nota.importacao.numDi;                   // 4 - NUM_DI
        campos[4] = formatDate(nota.dtDoc);                  // 5 - DAT_NF
        campos[5] = "1";                                     // 6 - IND_FIS_JUR
        campos[6] = "M" + nota.codPart;                      // 7 - COD_FIS_JUR
        try {
            campos[7] = String.format("%012d", Long.parseLong(nota.numDoc)); // 8 - NUM_NF
        } catch (NumberFormatException e) {
            campos[7] = "000000000000";
        }
        campos[8] = nota.serie;                              // 9 - SERIE_NF
        // 10 - SUB_SERIE_NF: não disponível -> "@"
        campos[10] = "5";                                    // 11 - IND_PRODUTO
        campos[11] = item.codItem;                           // 12 - COD_PRODUTO
        campos[12] = String.format("%05d", item.numItem);    // 13 - NUM_ITEM
        campos[13] = formatDate(nota.dtES);                  // 14 - DAT_ENTRADA
        // 15 - COD_NBM: só disponível se o 0200 do produto trouxer NCM
        if (produto != null && produto.ncm != null && !produto.ncm.isEmpty()) {
            campos[14] = produto.ncm;
        }
        // 16 - COD_EX: não disponível -> "@"
        campos[16] = "55";                                   // 17 - COD_MODELO
        campos[17] = item.unid;                               // 18 - COD_MEDIDA
        // 19 - COD_MEDIDA_COM: SPED só traz uma unidade (UNID) -> "@"
        // 20-23 - dados de ato concessório: não disponíveis -> "@"
        // 24-25 - peso bruto/líquido: não disponíveis -> "@"
        campos[25] = formatNumber(vlUnitReal);               // 26 - VLR_UNIT
        campos[26] = formatNumber(item.qtd);                 // 27 - QTD_UND_MED
        // 28 - QTD_UND_COM: SPED não distingue unidade de medida da comercial -> "@"
        campos[28] = formatNumber(item.vlItem);              // 29 - VLR_PRODUTO
        // 30-34 - frete/seguro/despesas aduaneiras/dedução: só disponíveis por nota
        // no C100 (não rateados por item) ou apenas no XML da NF-e -> "@"
        // 35 - VLR_NF: fórmula de referência SAP depende de frete/seguro/despesas
        // aduaneiras não disponíveis por item -> não inferir, "@"
        campos[35] = formatNumber(item.vlBcIpi);             // 36 - BASE_IPI
        campos[36] = formatNumber(item.aliqIpi);             // 37 - VLR_ALIQ_IPI
        campos[37] = formatNumber(item.vlIpi);               // 38 - VLR_IPI
        // 39-40 - isenta/outras IPI: não disponíveis -> "@"
        campos[40] = formatNumber(item.vlBcIcms);            // 41 - BASE_ICMS
        campos[41] = formatNumber(item.aliqIcms);            // 42 - VLR_ALIQ_ICMS
        campos[42] = formatNumber(item.vlIcms);              // 43 - VLR_ICMS
        // 44-53 - isenta/outras ICMS, II (base/alíq/valor), país, moeda, valores
        // em dólar/moeda da operação: não disponíveis no SPED -> "@"
        // 54-64 - observações, indicadores de ajuste, frete/seguro em moeda: "@"
        campos[64] = formatNumber(vlUnitReal);               // 65 - VLR_UNIT_REAL
        // 66 - COD_FORN_ORIG: não é o mesmo conceito do fornecedor nacional -> "@"
        campos[66] = formatNumber(nota.importacao.pisImp);   // 67 - VLR_PIS (PIS_IMP do C120)
        campos[67] = formatNumber(nota.importacao.cofinsImp);// 68 - VLR_COFINS (COFINS_IMP do C120)
        // 69 - VLR_IOF: não disponível no SPED -> "@"
        // 70 - VLR_ADUAN_SEM_ICMS: não disponível -> "@"
        // 71 - DAT_DESEMBARACO: não disponível no C120 (só no XML da NF-e) -> "@"
        campos[71] = nota.importacao.tipoDocImp;             // 72 - TIPO_DI (COD_DOC_IMP)

        // Montar linha com TAB (sem coluna de tipo de registro — ver SpedToSafx07)
        StringBuilder sb = new StringBuilder(campos[0]);
        for (int i = 1; i < campos.length; i++) {
            sb.append('\t').append(campos[i]);
        }
        return sb.toString();
    }
}
