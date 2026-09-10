package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;
import java.util.regex.Pattern;

/**
 * Gera um relatório CSV de operações de importação (uma linha por nota fiscal
 * com Declaração de Importação vinculada), no mesmo espírito dos conversores
 * SAFX: lê apenas os registros disponíveis no SPED (C100, C120, C170, 0150) e
 * não inventa dado que a origem não fornece (valores em dólar, FOB/CIF, II,
 * antidumping, Siscomex, AFRMM, ATAERO, dados do representante legal etc. só
 * existem no XML da NF-e/DI ou em preenchimento manual — ficam em branco).
 */
public class SpedToImportacaoCsv {

    private static final String[] CABECALHO = {
            "Número", "Data", "Fornecedor", "NF-e", "Status", "Data de Entrada",
            "Frete", "Valor Frete", "Valor Frete $", "Valor dos Produtos",
            "Valor FOB", "Valor FOB $", "Valor CIF", "Valor CIF $", "Valor do seguro",
            "II", "IPI", "PIS", "COFINS", "ICMS", "Antidumping", "Siscomex",
            "Acresimo/Capatazia", "Multa", "Taxa AFRMM", "Taxa ATAERO",
            "Outras despesas acessórias", "UF", "Local", "Data",
            "Nome Representante Legal", "CPF Representante Legal"
    };

    static class Participante {
        String codPart;
        String nome;
    }

    static class Importacao {
        String tipoDocImp;
        String numDi;
        double pisImp;
        double cofinsImp;
    }

    static class ItemNota {
        double vlItem;
    }

    static class NotaFiscal {
        String codPart;
        String codSit;
        String numDoc;
        String dtES;
        double vlDoc;
        double vlIcms;
        double vlIpi;
        double vlFrete;
        double vlSeg;
        double vlOutr;
        String indFrete;
        Importacao importacao;
        List<ItemNota> itens = new ArrayList<>();
    }

    public static void convertFromSped(String inputFile) throws IOException {
        String outputFile = System.getProperty("user.dir") + "/IMPORTACAO.csv";

        List<String> lines = ArquivoTexto.linhas(Paths.get(inputFile));
        Map<String, Participante> participantes = new HashMap<>();
        List<NotaFiscal> notas = new ArrayList<>();
        NotaFiscal notaAtual = null;

        for (String line : lines) {
            String[] campos = line.split(Pattern.quote("|"), -1);
            if (campos.length == 0) continue;
            String tipo = campos[1];

            switch (tipo) {
                case "0150":
                    // Layout oficial 0150 (ver SpedToSafx04): COD_PART(1) NOME(2)...
                    Participante p = new Participante();
                    p.codPart = campo(campos, 2);
                    p.nome = campo(campos, 3);
                    participantes.put(p.codPart, p);
                    break;

                case "C100":
                    // Layout oficial C100 (ver SpedToSafx07).
                    notaAtual = new NotaFiscal();
                    notaAtual.codPart = campo(campos, 4);
                    notaAtual.codSit = campo(campos, 6);
                    notaAtual.numDoc = campo(campos, 8);
                    notaAtual.dtES = campo(campos, 11);
                    notaAtual.vlDoc = parseDouble(campo(campos, 12));
                    notaAtual.indFrete = campo(campos, 17);
                    notaAtual.vlFrete = parseDouble(campo(campos, 18));
                    notaAtual.vlSeg = parseDouble(campo(campos, 19));
                    notaAtual.vlOutr = parseDouble(campo(campos, 20));
                    notaAtual.vlIcms = parseDouble(campo(campos, 22));
                    notaAtual.vlIpi = parseDouble(campo(campos, 25));
                    notaAtual.importacao = null;
                    notas.add(notaAtual);
                    break;

                case "C120":
                    if (notaAtual == null) break;
                    // Layout oficial C120 (ver SpedToSafx49): COD_DOC_IMP(1) NUM_DOC_IMP(2)
                    // PIS_IMP(3) COFINS_IMP(4). Não há data da DI neste registro.
                    Importacao imp = new Importacao();
                    imp.tipoDocImp = campo(campos, 2);
                    imp.numDi = campo(campos, 3);
                    imp.pisImp = parseDouble(campo(campos, 4));
                    imp.cofinsImp = parseDouble(campo(campos, 5));
                    notaAtual.importacao = imp;
                    break;

                case "C170":
                    if (notaAtual == null) break;
                    // Layout oficial C170 (ver SpedToSafx07): VL_ITEM é o campo 6 (campos[7]).
                    ItemNota item = new ItemNota();
                    item.vlItem = parseDouble(campo(campos, 7));
                    notaAtual.itens.add(item);
                    break;

                default:
                    break;
            }
        }

        try (BufferedWriter writer = new BufferedWriter(
                new OutputStreamWriter(new FileOutputStream(outputFile), Charset.forName("UTF-8")))) {
            // BOM UTF-8: garante acentuação correta ao abrir no Excel em português.
            writer.write('﻿');
            writer.write(linhaCsv(CABECALHO));
            writer.newLine();

            for (NotaFiscal nota : notas) {
                if (nota.importacao == null) continue;
                writer.write(linhaCsv(gerarLinha(nota, participantes)));
                writer.newLine();
            }
        }

        System.out.println("Arquivo IMPORTACAO.csv gerado: " + outputFile);
    }

    private static String[] gerarLinha(NotaFiscal nota, Map<String, Participante> participantes) {
        Participante fornecedor = participantes.get(nota.codPart);
        double vlProduto = nota.itens.stream().mapToDouble(i -> i.vlItem).sum();

        String[] linha = new String[CABECALHO.length];
        Arrays.fill(linha, "");

        linha[0] = nota.importacao.numDi;                                   // Número (NUM_DI)
        // Data (da DI): não disponível no registro C120 — só existe no XML da NF-e (detDI).
        linha[2] = fornecedor != null ? fornecedor.nome : "";               // Fornecedor
        linha[3] = nota.numDoc;                                             // NF-e
        linha[4] = status(nota.codSit);                                    // Status (COD_SIT)
        linha[5] = fdate(nota.dtES);                                       // Data de Entrada (DT_E_S)
        linha[6] = frete(nota.indFrete);                                   // Frete (IND_FRT)
        linha[7] = fnum(nota.vlFrete);                                     // Valor Frete (VL_FRT)
        // Valor Frete $, Valor FOB(+$), Valor CIF(+$): exigem XML da NF-e/DI — não inferidos.
        linha[9] = fnum(vlProduto);                                        // Valor dos Produtos (soma VL_ITEM)
        linha[14] = fnum(nota.vlSeg);                                      // Valor do seguro (VL_SEG)
        // II: não disponível no SPED (só no XML da DI).
        linha[16] = fnum(nota.vlIpi);                                      // IPI (VL_IPI)
        linha[17] = fnum(nota.importacao.pisImp);                          // PIS (PIS_IMP do C120)
        linha[18] = fnum(nota.importacao.cofinsImp);                       // COFINS (COFINS_IMP do C120)
        linha[19] = fnum(nota.vlIcms);                                     // ICMS (VL_ICMS)
        // Antidumping, Siscomex, Acréscimo/Capatazia, Multa, Taxa AFRMM, Taxa ATAERO:
        // nenhum desses valores existe no SPED — não inferidos.
        linha[26] = fnum(nota.vlOutr);                                     // Outras despesas acessórias (VL_OUT_DA)
        // UF, Local, Data (assinatura), Nome/CPF do Representante Legal: dados de
        // preenchimento manual do documento, não vêm do SPED — deixados em branco.

        return linha;
    }

    private static String status(String codSit) {
        if (codSit == null) return "";
        switch (codSit) {
            case "00": return "Documento Regular";
            case "01": return "Documento Cancelado";
            case "02": return "Documento Denegado";
            case "03": return "Numeração Inutilizada";
            case "04": return "NFe Complementar";
            case "05": return "Documento Complementar";
            case "06": return "Documento Complementar Cancelado";
            case "07": return "Documento Regular, emitido com base em regime especial ou norma específica, antecedendo a entrega da mercadoria e/ou início da prestação do serviço";
            case "08": return "Documento Regular, emitido em decorrência de regime especial ou norma específica antecedendo a entrega da mercadoria e/ou o início da prestação do serviço.";
            default: return codSit;
        }
    }

    private static String frete(String indFrt) {
        if (indFrt == null) return "";
        switch (indFrt) {
            case "0": return "CIF (por conta do remetente)";
            case "1": return "FOB (por conta do destinatário)";
            case "2": return "Por conta de terceiros";
            case "3": return "Transporte próprio (remetente)";
            case "4": return "Transporte próprio (destinatário)";
            case "9": return "Sem transporte";
            default: return indFrt;
        }
    }

    private static String campo(String[] campos, int idx) {
        return idx < campos.length ? campos[idx] : "";
    }

    private static double parseDouble(String s) {
        if (s == null || s.trim().isEmpty()) return 0.0;
        s = s.replace(",", ".");
        try { return Double.parseDouble(s); } catch (NumberFormatException e) { return 0.0; }
    }

    // Formatação de número no padrão brasileiro (vírgula decimal), sem separador de milhar,
    // consistente com a notação já usada nos arquivos de origem (SPED) e nos SAFX.
    private static String fnum(double v) {
        return String.format(Locale.forLanguageTag("pt-BR"), "%.2f", v);
    }

    // DDMMAAAA (formato de data do SPED) -> DD/MM/AAAA, mais legível num relatório CSV.
    private static String fdate(String ddmmyyyy) {
        if (ddmmyyyy == null || ddmmyyyy.length() != 8) return "";
        return ddmmyyyy.substring(0, 2) + "/" + ddmmyyyy.substring(2, 4) + "/" + ddmmyyyy.substring(4);
    }

    private static String linhaCsv(String[] campos) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < campos.length; i++) {
            if (i > 0) sb.append(';');
            sb.append(escapeCsv(campos[i]));
        }
        return sb.toString();
    }

    // CSV delimitado por ";" (não ",") porque o Excel em português usa vírgula como separador
    // decimal — com "," como delimitador de coluna, os valores monetários quebrariam ao abrir.
    private static String escapeCsv(String valor) {
        if (valor == null) valor = "";
        if (valor.contains(";") || valor.contains("\"") || valor.contains("\n")) {
            return "\"" + valor.replace("\"", "\"\"") + "\"";
        }
        return valor;
    }
}
