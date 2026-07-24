package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

public class SpedToSafx07 {

    static class Fornecedor {
        String codPart;
        String nome;
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
        List<ItemNota> itens = new ArrayList<>();
    }

    public static void convertFromSped(String inputFile) throws IOException {
        String outputFile = System.getProperty("user.dir") + "/SAFX07.txt";

        List<String> lines = Files.readAllLines(Paths.get(inputFile), Charset.forName("UTF-8"));
        Map<String, Fornecedor> fornecedores = new HashMap<>();
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

                case "C100":
                    // Layout oficial C100 (28 campos após o REG): IND_OPER(1) IND_EMIT(2)
                    // COD_PART(3) COD_MOD(4) COD_SIT(5) SER(6) NUM_DOC(7) CHV_NFE(8) DT_DOC(9)
                    // DT_E_S(10) VL_DOC(11) IND_PGTO(12) VL_DESC(13) VL_ABAT_NT(14) VL_MERC(15)
                    // IND_FRT(16) VL_FRT(17) VL_SEG(18) VL_OUT_DA(19) VL_BC_ICMS(20) VL_ICMS(21)
                    // VL_BC_ICMS_ST(22) VL_ICMS_ST(23) VL_IPI(24) VL_PIS(25) VL_COFINS(26)
                    // campos[0] é vazio (antes do primeiro '|') e campos[1] é o literal "C100".
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
                    notas.add(notaAtual);
                    break;

                case "C170":
                    if (notaAtual == null) break;
                    // Layout oficial C170 (37 campos após o REG): NUM_ITEM(1) COD_ITEM(2)
                    // DESCR_COMPL(3) QTD(4) UNID(5) VL_ITEM(6) VL_DESC(7) IND_MOV(8) CST_ICMS(9)
                    // CFOP(10) COD_NAT(11) VL_BC_ICMS(12) ALIQ_ICMS(13) VL_ICMS(14)
                    // VL_BC_ICMS_ST(15) ALIQ_ST(16) VL_ICMS_ST(17) IND_APUR(18) CST_IPI(19)
                    // COD_ENQ(20) VL_BC_IPI(21) ALIQ_IPI(22) VL_IPI(23) CST_PIS(24) VL_BC_PIS(25)
                    // ALIQ_PIS_PERC(26) QUANT_BC_PIS(27) ALIQ_PIS_REAIS(28) VL_PIS(29) CST_COFINS(30)
                    // VL_BC_COFINS(31) ALIQ_COFINS_PERC(32) QUANT_BC_COFINS(33) ALIQ_COFINS_REAIS(34)
                    // VL_COFINS(35) COD_CTA(36) VL_ABAT_NT(37). campos[1] é o literal "C170".
                    ItemNota item = new ItemNota();
                    item.numItem = (int) parseDouble(campo(campos, 2));
                    item.codItem = campo(campos, 3);
                    // campo 4 é descrição complementar, ignoramos
                    item.qtd = parseDouble(campo(campos, 5));
                    item.unid = campo(campos, 6);
                    // VL_ITEM já é o valor total do item — não é quantidade x preço unitário
                    item.vlItem = parseDouble(campo(campos, 7));
                    item.vlUnit = item.qtd != 0 ? item.vlItem / item.qtd : 0.0;
                    // campo 8 = VL_DESC, 9 = IND_MOV
                    item.cstIcms = (int) parseDouble(campo(campos, 10));
                    // campo 11 = CFOP, 12 = COD_NAT
                    item.vlBcIcms = parseDouble(campo(campos, 13));
                    item.aliqIcms = parseDouble(campo(campos, 14));
                    item.vlIcms = parseDouble(campo(campos, 15));
                    // campos 16-19 = ST/IND_APUR
                    item.cstIpi = (int) parseDouble(campo(campos, 20));
                    // campo 21 = COD_ENQ
                    item.vlBcIpi = parseDouble(campo(campos, 22));
                    item.aliqIpi = parseDouble(campo(campos, 23));
                    item.vlIpi = parseDouble(campo(campos, 24));
                    // campo 25 = CST_PIS
                    item.vlBcPis = parseDouble(campo(campos, 26));
                    item.aliqPis = parseDouble(campo(campos, 27));
                    item.vlPis = parseDouble(campo(campos, 30));
                    // campo 31 = CST_COFINS
                    item.vlBcCofins = parseDouble(campo(campos, 32));
                    item.aliqCofins = parseDouble(campo(campos, 33));
                    item.vlCofins = parseDouble(campo(campos, 36));
                    notaAtual.itens.add(item);
                    break;

                default:
                    break;
            }
        }

        // Gerar SAFX07 (apenas capas — itens vão para SAFX08.txt, gerado por SpedToSafx08)
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(outputFile))) {
            for (NotaFiscal nota : notas) {
                writer.write(gerarLinha07(nota));
                writer.newLine();
            }
        }

        System.out.println("Arquivo SAFX07 gerado: " + outputFile);
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

    private static String gerarLinha07(NotaFiscal nota) {
        String[] campos = new String[302];
        Arrays.fill(campos, "@");

        // 1- COD_EMPRESA
        campos[0] = "001";
        // 2- COD_ESTAB
        campos[1] = "0001";
        // 3- MOVTO_E_S
        campos[2] = "4";
        // 4- NORM_DEV
        campos[3] = "1";
        // 5- COD_DOCTO
        campos[4] = "NFI";
        // 6- IDENT_FIS_JUR
        campos[5] = "1";
        // 7- COD_FIS_JUR (prefixo M + código do fornecedor)
        campos[6] = "M" + nota.codPart;
        // 8- NUM_DOCFIS (12 dígitos)
        campos[7] = String.format("%012d", Long.parseLong(nota.numDoc));
        // 9- SERIE_DOCFIS
        campos[8] = nota.serie;
        // 10- (subsérie) não preencher
        // 11- DATA_EMISSAO
        campos[10] = formatDate(nota.dtDoc);
        // 12- COD_CLASS_DOC_FIS
        campos[11] = "1";
        // 13- COD_MODELO
        campos[12] = "55";
        // 14-19 - não preencher
        // 20- DATA_SAIDA_REC
        campos[19] = formatDate(nota.dtES);
        // 21- VLR_PRODUTO (soma dos itens)
        double vlProduto = nota.itens.stream().mapToDouble(i -> i.vlItem).sum();
        campos[21] = formatNumber(vlProduto);
        // 22- VLR_TOT_NOTA
        campos[22] = formatNumber(nota.vlDoc);
        // 23- VLR_FRETE
        campos[23] = formatNumber(nota.vlFrete);
        // 24- VLR_SEGURO
        campos[24] = formatNumber(nota.vlSeg);
        // 26- VLR_OUTRAS (VL_OUT_DA do C100 — outras despesas acessórias)
        campos[25] = formatNumber(nota.vlOutr);
        // 26- (não)
        // 28- VLR_DESCONTO (VL_DESC do C100)
        campos[27] = formatNumber(nota.vlDesc);
        // 28- CONTRIB_FINAL
        campos[28] = "S";
        // 29- SITUACAO
        campos[29] = "N";
        // 30-33 não
        // 34- VLR_ICMS
        double vlIcmsTotal = nota.itens.stream().mapToDouble(i -> i.vlIcms).sum();
        campos[34] = formatNumber(vlIcmsTotal);
        // 35-38 não
        // 39- VLR_IPI
        double vlIpiTotal = nota.itens.stream().mapToDouble(i -> i.vlIpi).sum();
        campos[39] = formatNumber(vlIpiTotal);
        // 40-49 não
        // 50- BASE_TRIB_ICMS
        double baseIcmsTotal = nota.itens.stream().mapToDouble(i -> i.vlBcIcms).sum();
        campos[50] = formatNumber(baseIcmsTotal);
        // 51-53 não
        // 54- BASE_TRIB_IPI
        double baseIpiTotal = nota.itens.stream().mapToDouble(i -> i.vlBcIpi).sum();
        campos[54] = formatNumber(baseIpiTotal);
        // 55-70 não
        // 71- IND_TP_FRETE
        campos[71] = "0";
        // 72-100 não
        // 101- BASE_PIS
        double basePis = nota.itens.stream().mapToDouble(i -> i.vlBcPis).sum();
        campos[101] = formatNumber(basePis);
        // 102- VALOR PIS
        double vlPis = nota.itens.stream().mapToDouble(i -> i.vlPis).sum();
        campos[102] = formatNumber(vlPis);
        // 103- BASE_COFINS
        double baseCofins = nota.itens.stream().mapToDouble(i -> i.vlBcCofins).sum();
        campos[103] = formatNumber(baseCofins);
        // 104- VALOR COFINS
        double vlCofins = nota.itens.stream().mapToDouble(i -> i.vlCofins).sum();
        campos[104] = formatNumber(vlCofins);
        // 163- ALIQ_PIS (pegar do primeiro item)
        if (!nota.itens.isEmpty()) {
            ItemNota primeiro = nota.itens.get(0);
            campos[163] = formatNumber(primeiro.aliqPis);
            campos[164] = formatNumber(primeiro.aliqCofins);
        }
        // 226- CHAVE ACESSO NFE
        campos[225] = nota.chaveNFe;
        // 229- COD_MODELO_COTEPE
        campos[228] = "55";
        // 247- DAT_LANC_PIS_COFINS
        campos[246] = formatDate(nota.dtES);
        // 251- IND_NAT_FRETE
        campos[250] = "2";

        // Montar com TAB (cada arquivo SAFXnn.TXT contém apenas registros do seu
        // próprio tipo — não há coluna de tipo de registro nem "prefixo" por linha)
        StringBuilder sb = new StringBuilder(campos[0]);
        for (int i = 1; i < campos.length; i++) {
            sb.append('\t').append(campos[i]);
        }
        return sb.toString();
    }
}