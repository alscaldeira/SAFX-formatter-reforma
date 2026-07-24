package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

public class SpedToSafx08 {

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
        int cstIcms;       // 3 dígitos
        int cstIpi;        // 2 dígitos
        int cstPis;        // 2 dígitos
        int cstCofins;     // 2 dígitos
        int cfop;          // 4 dígitos
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
        List<ItemNota> itens = new ArrayList<>();
    }

    public static void convertFromSped(String inputFile) throws IOException {
        String outputFile = System.getProperty("user.dir") + "/SAFX08.txt";

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
                    notas.add(notaAtual);
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
                    // campo 8 = VL_DESC, 9 = IND_MOV
                    item.cstIcms = (int) parseDouble(campo(campos, 10));
                    item.cfop = (int) parseDouble(campo(campos, 11));
                    // campo 12 = COD_NAT
                    item.vlBcIcms = parseDouble(campo(campos, 13));
                    item.aliqIcms = parseDouble(campo(campos, 14));
                    item.vlIcms = parseDouble(campo(campos, 15));
                    // campos 16-19 = ST/IND_APUR
                    item.cstIpi = (int) parseDouble(campo(campos, 20));
                    // campo 21 = COD_ENQ
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

        // Gerar arquivo SAFX08
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(outputFile))) {
            for (NotaFiscal nota : notas) {
                for (ItemNota item : nota.itens) {
                    writer.write(gerarLinha08(nota, item, produtos));
                    writer.newLine();
                }
            }
        }

        System.out.println("Arquivo SAFX08 gerado: " + outputFile);
        System.out.println("Total de itens processados: " + notas.stream().mapToInt(n -> n.itens.size()).sum());
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

    private static String gerarLinha08(NotaFiscal nota, ItemNota item, Map<String, Produto> produtos) {
        // 300 campos para compatibilidade (o leiaute tem 262)
        String[] campos = new String[300];
        Arrays.fill(campos, "@");

        // Campos obrigatórios e que temos dados
        // 01 - COD_EMPRESA
        campos[0] = "001";
        // 02 - COD_ESTAB
        campos[1] = "0001";
        // 03 - DATA_FISCAL (data de recebimento)
        campos[2] = formatDate(nota.dtES);
        // 04 - MOVTO_E_S (entrada própria)
        campos[3] = "4";
        // 05 - NORM_DEV
        campos[4] = "1";
        // 06 - COD_DOCTO
        campos[5] = "NFEIMP";
        // 07 - IND_FIS_JUR
        campos[6] = "1";
        // 08 - COD_FIS_JUR (fornecedor com prefixo M)
        campos[7] = "M" + nota.codPart;
        // 09 - NUM_DOCFIS (12 dígitos)
        try {
            campos[8] = String.format("%012d", Long.parseLong(nota.numDoc));
        } catch (NumberFormatException e) {
            campos[8] = "000000000000";
        }
        // 10 - SERIE_DOCFIS
        campos[9] = nota.serie;
        // 11 - SUB_SERIE_DOCFIS (não informar)
        // 12 - IND_BEM_PATR (N = produto, não bem)
        campos[11] = "N";
        // 13 - IND_PRODUTO (5 = outros, pois não temos classificação)
        campos[12] = "5";
        // 14 - COD_PRODUTO (código do item)
        campos[13] = item.codItem;
        // 15 - COD_BEM (@)
        // 16 - COD_INC_BEM (@)
        // 17 - COD_UND_PADRAO (unidade padrão)
        campos[16] = item.unid;
        // 18 - NUM_ITEM (número do item, com 5 dígitos)
        campos[17] = String.format("%05d", item.numItem);
        // 19 - COD_ALMOX (@)
        // 20 - COD_CUSTO (@)
        // 21 - DESCRICAO_COMPL (descrição complementar)
        campos[20] = item.descrCompl;
        // 22 - COD_CFO (CFOP com 4 dígitos)
        campos[21] = String.format("%04d", item.cfop);
        // 23 - COD_NATUREZA_OP (@)
        // 24 - QUANTIDADE
        campos[23] = formatNumber(item.qtd);
        // 25 - COD_MEDIDA (unidade de medida)
        campos[24] = item.unid;
        // 26 - COD_NBM (NCM do registro 0200, quando disponível)
        Produto produto = produtos.get(item.codItem);
        if (produto != null && produto.ncm != null && !produto.ncm.isEmpty()) {
            campos[25] = produto.ncm;
        }
        // 27 - VLR_UNIT
        campos[26] = formatNumber(item.vlUnit);
        // 28 - VLR_ITEM
        campos[27] = formatNumber(item.vlItem);
        // 29 - VLR_DESCONTO (não temos, usamos 0)
        campos[28] = formatNumber(0.0);
        // 30 - COD_SITUACAO_A (origem do CST ICMS - primeiro dígito)
        String cstIcmsStr = String.format("%03d", item.cstIcms);
        campos[29] = cstIcmsStr.substring(0, 1);
        // 31 - COD_SITUACAO_B (tributação do CST ICMS - últimos 2 dígitos)
        campos[30] = cstIcmsStr.substring(1);
        // 32 - COD_FEDERAL (@)
        // 33 - IND_IPI_INCLUSO (@)
        // 34-38 = @
        // 39 - VLR_FRETE (rateio - usamos 0)
        campos[38] = formatNumber(0.0);
        // 40 - VLR_SEGURO
        campos[39] = formatNumber(0.0);
        // 41 - VLR_OUTRAS
        campos[40] = formatNumber(0.0);
        // 42 - VLR_ALIQ_ICMS
        campos[41] = formatNumber(item.aliqIcms);
        // 43 - VLR_ICMS
        campos[42] = formatNumber(item.vlIcms);
        // 44 - DIF_ALIQ_ICMS (@)
        // 45 - OBS_ICMS (@)
        // 46 - COD_APUR_ICMS (@)
        // 47 - VLR_ALIQ_IPI
        campos[46] = formatNumber(item.aliqIpi);
        // 48 - VLR_IPI
        campos[47] = formatNumber(item.vlIpi);
        // 49 - OBS_IPI (@)
        // 50 - COD_APUR_IPI (@)
        // 51-54 = @
        // 55 - TRIB_ICMS (1 = tributado, 2 = isento, 3 = outras)
        // Vamos classificar: se CST for 00,10,20,30,40,50,60,70,90 -> 1; se 41,51 -> 3; se 40,41? Mas simplificando: usamos 1.
        campos[54] = "1";
        // 56 - BASE_ICMS
        campos[55] = formatNumber(item.vlBcIcms);
        // 57 - BASE_REDU_ICMS (@)
        // 58 - TRIB_IPI (similar, usamos 1)
        campos[57] = "1";
        // 59 - BASE_IPI
        campos[58] = formatNumber(item.vlBcIpi);
        // 60 - BASE_REDU_IPI (@)
        // 61 - BASE_SUB_TRIB_ICMS (@)
        // 62 - VLR_CONTAB_COMPL (@)
        // 63 - VLR_ALIQ_DESTINO (@)
        // 64 - VLR_CONTAB_ITEM (valor contábil do item = vlItem)
        campos[63] = formatNumber(item.vlItem);
        // 65-85 = @
        // 86 - VLR_BASE_PIS
        campos[85] = formatNumber(item.vlBcPis);
        // 87 - VLR_PIS
        campos[86] = formatNumber(item.vlPis);
        // 88 - VLR_BASE_COFINS
        campos[87] = formatNumber(item.vlBcCofins);
        // 89 - VLR_COFINS
        campos[88] = formatNumber(item.vlCofins);
        // 90-104 = @
        // 105 - COD_CONTA (não temos)
        // 106-129 = @
        // 130 - VLR_ALIQ_COFINS
        campos[129] = formatNumber(item.aliqCofins);
        // 131-145 = @
        // 146 - COD_TRIB_IPI (código de tributação IPI - podemos usar o CST IPI)
        campos[145] = String.format("%02d", item.cstIpi);
        // 147-174 = @
        // 175 - COD_SITUACAO_PIS (CST PIS)
        campos[174] = String.format("%02d", item.cstPis);
        // 176-177 = @
        // 178 - COD_SITUACAO_COFINS (CST COFINS)
        campos[177] = String.format("%02d", item.cstCofins);
        // 179-195 = @
        // 196 - DAT_LANC_PIS_COFINS (data de lançamento = data fiscal)
        campos[195] = formatDate(nota.dtES);
        // 197 - IND_PIS_COFINS_EXTEMP (@)
        // 198 - IND_NATUREZA_FRETE (2 = compra com crédito)
        campos[197] = "2";
        // 199-207 = @
        // 208-212 = @
        // 213-218 = @
        // 219-262 = @

        // Montar linha com TAB (sem coluna de tipo de registro — ver SpedToSafx07)
        StringBuilder sb = new StringBuilder(campos[0]);
        for (int i = 1; i < 262; i++) { // até campo 262 conforme leiaute
            sb.append('\t').append(campos[i]);
        }
        return sb.toString();
    }
}