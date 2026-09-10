package com.caldeira;

import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

/** Geradores de arquivos de entrada (XML e SPED) usados pelas suítes. */
public final class Fixtures {

    public static final String PROPS_ESTAB =
            "42105890000901=002/0001\n"
            + "11222333000181=002/0001\n"
            + "72977242000140=0001\n"
            + "72977242000302=0003\n"
            + "72977242000493=0005\n"
            + "99888777000166=0007\n";

    private Fixtures() {
    }

    /** Cria caso/xmls e caso/out, já com o cadastro de estabelecimentos. */
    public static Path caso(Path base, String nome) throws IOException {
        Path caso = base.resolve(nome);
        Files.createDirectories(caso.resolve("xmls"));
        Files.createDirectories(caso.resolve("out"));
        Files.write(caso.resolve("out/estabelecimentos.properties"),
                PROPS_ESTAB.getBytes(StandardCharsets.UTF_8));
        return caso;
    }

    public static void escrever(Path caso, String nome, String conteudo) throws IOException {
        Files.write(caso.resolve("xmls").resolve(nome), conteudo.getBytes(StandardCharsets.UTF_8));
    }

    // ------------------------------------------------------------------ NF-e

    public static String nfe(String numero, boolean saida, String cnpjEmit, String cnpjDest) {
        return nfe(numero, saida, cnpjEmit, cnpjDest, "PRODUTO DE TESTE", "TO", "10.0000", "1000.00");
    }

    /** NF-e mínima porém completa: 1 item, todos os grupos de imposto. */
    public static String nfe(String numero, boolean saida, String cnpjEmit, String cnpjDest,
                             String descricao, String unidade, String qtd, String valor) {
        double v = Double.parseDouble(valor);
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><NFe>"
                + "<infNFe Id=\"NFe" + chave(cnpjEmit, numero) + "\" versao=\"4.00\">"
                + "<ide><cUF>21</cUF><natOp>Operacao de teste</natOp><mod>55</mod><serie>1</serie>"
                + "<nNF>" + numero + "</nNF><dhEmi>2026-03-13T05:26:14-03:00</dhEmi>"
                + "<dhSaiEnt>2026-03-13T06:00:00-03:00</dhSaiEnt><tpNF>" + (saida ? "1" : "0") + "</tpNF>"
                + "<idDest>2</idDest><cDV>5</cDV><tpAmb>1</tpAmb><finNFe>1</finNFe></ide>"
                + "<emit><CNPJ>" + cnpjEmit + "</CNPJ><xNome>EMITENTE TESTE LTDA</xNome>"
                + "<enderEmit><xLgr>RUA A</xLgr><nro>1</nro><xBairro>CENTRO</xBairro>"
                + "<cMun>2111300</cMun><xMun>SAO LUIS</xMun><UF>MA</UF><CEP>65095603</CEP>"
                + "<cPais>1058</cPais></enderEmit><IE>120844931</IE><CRT>3</CRT></emit>"
                + "<dest><CNPJ>" + cnpjDest + "</CNPJ><xNome>DESTINATARIO TESTE LTDA</xNome>"
                + "<enderDest><xLgr>AV B</xLgr><nro>2</nro><xBairro>CENTRO</xBairro>"
                + "<cMun>3534302</cMun><xMun>ORLANDIA</xMun><UF>SP</UF><CEP>14620000</CEP>"
                + "<cPais>1058</cPais></enderDest><indIEDest>1</indIEDest><IE>491005087115</IE></dest>"
                + "<det nItem=\"1\"><prod><cProd>P1</cProd><xProd>" + escapar(descricao) + "</xProd>"
                + "<NCM>76011000</NCM><CFOP>" + (saida ? "6101" : "1101") + "</CFOP>"
                + "<uCom>" + unidade + "</uCom><qCom>" + qtd + "</qCom><vUnCom>100.0000000000</vUnCom>"
                + "<vProd>" + valor + "</vProd><uTrib>" + unidade + "</uTrib><qTrib>" + qtd + "</qTrib>"
                + "<vUnTrib>100.0000000000</vUnTrib><indTot>1</indTot></prod>"
                + "<imposto><ICMS><ICMS00><orig>0</orig><CST>00</CST><modBC>3</modBC>"
                + "<vBC>" + valor + "</vBC><pICMS>12.0000</pICMS><vICMS>"
                + moeda(v * 0.12) + "</vICMS></ICMS00></ICMS>"
                + "<IPI><cEnq>999</cEnq><IPITrib><CST>50</CST><vBC>" + valor + "</vBC>"
                + "<pIPI>2.6000</pIPI><vIPI>" + moeda(v * 0.026) + "</vIPI></IPITrib></IPI>"
                + "<PIS><PISAliq><CST>01</CST><vBC>" + valor + "</vBC><pPIS>1.6500</pPIS>"
                + "<vPIS>" + moeda(v * 0.0165) + "</vPIS></PISAliq></PIS>"
                + "<COFINS><COFINSAliq><CST>01</CST><vBC>" + valor + "</vBC><pCOFINS>7.6000</pCOFINS>"
                + "<vCOFINS>" + moeda(v * 0.076) + "</vCOFINS></COFINSAliq></COFINS></imposto>"
                + "<vItem>" + valor + "</vItem></det>"
                + "<total><ICMSTot><vBC>" + valor + "</vBC><vICMS>" + moeda(v * 0.12) + "</vICMS>"
                + "<vProd>" + valor + "</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg>"
                + "<vDesc>0.00</vDesc><vII>0.00</vII><vIPI>" + moeda(v * 0.026) + "</vIPI>"
                + "<vPIS>" + moeda(v * 0.0165) + "</vPIS><vCOFINS>" + moeda(v * 0.076) + "</vCOFINS>"
                + "<vOutro>0.00</vOutro><vNF>" + valor + "</vNF></ICMSTot></total>"
                + "<transp><modFrete>0</modFrete></transp>"
                + "<pag><detPag><tPag>15</tPag><vPag>" + valor + "</vPag></detPag></pag>"
                + "</infNFe></NFe><protNFe><infProt><chNFe>" + chave(cnpjEmit, numero) + "</chNFe>"
                + "<dhRecbto>2026-03-13T05:26:31-03:00</dhRecbto><nProt>421260009504426</nProt>"
                + "<cStat>100</cStat><xMotivo>Autorizado</xMotivo></infProt></protNFe></nfeProc>";
    }

    /** Parâmetros de uma NF-e de importação (grupo det/prod/DI + imposto/II). */
    public static final class Importacao {
        public String numero = "80001";
        public String cnpjEmitente = "11222333000181";
        public String cnpjDestinatario = "72977242000140";
        public String produto = "INSUMO IMPORTADO";
        public String ncm = "28182000";
        public String exTipi = "01";
        public String cfop = "3101";
        public String unidade = "KG";
        public String quantidade = "1000.0000";
        public double valorProduto = 25500.00;
        public double frete = 1200.00;
        public double seguro = 300.00;
        public double despesasAduaneiras = 800.00;
        public double afrmm = 450.00;
        public double aliqIi = 12.0;
        public double aliqIpi = 5.0;
        public double aliqIcms = 18.0;
        public double baseIcms = 40000.00;
        public String numeroDi = "26/1234567-8";
        public String dataDi = "2026-03-15";
        public String dataDesembaraco = "2026-03-18";
        public String ufDesembaraco = "MA";
        public String localDesembaraco = "PORTO DO ITAQUI";
        public String viaTransporte = "01";
        /** nDraw de 11 posições (ano + número + dígito); vazio quando não há drawback. */
        public String drawback = "";
        public String fabricante = "FAB123";
    }

    /**
     * NF-e de entrada por importação: traz o grupo det/prod/DI (obrigatório para
     * CFOP 3xxx) e o grupo imposto/II. É essa nota que alimenta o SAFX49.
     */
    public static String nfeImportacao(Importacao imp) {
        double valorAduaneiro = imp.valorProduto + imp.frete + imp.seguro;
        double vIi = arredondar(valorAduaneiro * imp.aliqIi / 100);
        double baseIpi = valorAduaneiro + vIi;
        double vIpi = arredondar(baseIpi * imp.aliqIpi / 100);
        double vIcms = arredondar(imp.baseIcms * imp.aliqIcms / 100);
        double vPis = arredondar(valorAduaneiro * 2.10 / 100);
        double vCofins = arredondar(valorAduaneiro * 9.65 / 100);
        double outras = imp.despesasAduaneiras + imp.afrmm;
        double vNf = arredondar(imp.valorProduto + imp.frete + imp.seguro + vIi + vIpi + outras);
        double vItem = arredondar(imp.valorProduto + vIpi);

        StringBuilder di = new StringBuilder();
        di.append("<DI><nDI>").append(imp.numeroDi).append("</nDI>")
          .append("<dDI>").append(imp.dataDi).append("</dDI>")
          .append("<xLocDesemb>").append(imp.localDesembaraco).append("</xLocDesemb>")
          .append("<UFDesemb>").append(imp.ufDesembaraco).append("</UFDesemb>")
          .append("<dDesemb>").append(imp.dataDesembaraco).append("</dDesemb>")
          .append("<tpViaTransp>").append(imp.viaTransporte).append("</tpViaTransp>")
          .append("<vAFRMM>").append(moeda(imp.afrmm)).append("</vAFRMM>")
          .append("<tpIntermedio>1</tpIntermedio>")
          .append("<adi><nAdicao>1</nAdicao><nSeqAdic>1</nSeqAdic>")
          .append("<cFabricante>").append(imp.fabricante).append("</cFabricante>")
          .append("<vDescDI>0.00</vDescDI>");
        if (!imp.drawback.isEmpty()) {
            di.append("<nDraw>").append(imp.drawback).append("</nDraw>");
        }
        di.append("</adi></DI>");

        double unitario = imp.valorProduto / Double.parseDouble(imp.quantidade);

        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><NFe>"
                + "<infNFe Id=\"NFe" + chave(imp.cnpjEmitente, imp.numero) + "\" versao=\"4.00\">"
                + "<ide><cUF>21</cUF><natOp>Importacao do exterior</natOp><mod>55</mod><serie>1</serie>"
                + "<nNF>" + imp.numero + "</nNF><dhEmi>2026-03-20T10:00:00-03:00</dhEmi>"
                + "<dhSaiEnt>2026-03-21T08:00:00-03:00</dhSaiEnt><tpNF>0</tpNF><idDest>1</idDest>"
                + "<cMunFG>2111300</cMunFG><cDV>5</cDV><tpAmb>1</tpAmb><finNFe>1</finNFe></ide>"
                + "<emit><CNPJ>" + imp.cnpjEmitente + "</CNPJ><xNome>IMPORTADORA TESTE LTDA</xNome>"
                + "<enderEmit><xLgr>AV PORTUARIA</xLgr><nro>500</nro><xBairro>PORTO</xBairro>"
                + "<cMun>2111300</cMun><xMun>SAO LUIS</xMun><UF>MA</UF><CEP>65085000</CEP>"
                + "<cPais>1058</cPais></enderEmit><IE>121212121</IE><CRT>3</CRT></emit>"
                + "<dest><CNPJ>" + imp.cnpjDestinatario + "</CNPJ><xNome>DESTINATARIO TESTE LTDA</xNome>"
                + "<enderDest><xLgr>ROD BR 135</xLgr><nro>04</nro><xBairro>PEDRINHAS</xBairro>"
                + "<cMun>2111300</cMun><xMun>SAO LUIS</xMun><UF>MA</UF><CEP>65095603</CEP>"
                + "<cPais>1058</cPais></enderDest><indIEDest>1</indIEDest><IE>120844931</IE></dest>"
                + "<det nItem=\"1\"><prod><cProd>IMP-" + imp.numero + "</cProd>"
                + "<xProd>" + escapar(imp.produto) + "</xProd><NCM>" + imp.ncm + "</NCM>"
                + "<EXTIPI>" + imp.exTipi + "</EXTIPI><CFOP>" + imp.cfop + "</CFOP>"
                + "<uCom>" + imp.unidade + "</uCom><qCom>" + imp.quantidade + "</qCom>"
                + "<vUnCom>" + String.format(Locale.US, "%.10f", unitario) + "</vUnCom>"
                + "<vProd>" + moeda(imp.valorProduto) + "</vProd>"
                + "<uTrib>" + imp.unidade + "</uTrib><qTrib>" + imp.quantidade + "</qTrib>"
                + "<vUnTrib>" + String.format(Locale.US, "%.10f", unitario) + "</vUnTrib>"
                + "<vFrete>" + moeda(imp.frete) + "</vFrete><vSeg>" + moeda(imp.seguro) + "</vSeg>"
                + "<vOutro>" + moeda(outras) + "</vOutro><indTot>1</indTot>"
                + di
                + "</prod><imposto>"
                + "<II><vBC>" + moeda(valorAduaneiro) + "</vBC>"
                + "<vDespAdu>" + moeda(imp.despesasAduaneiras) + "</vDespAdu>"
                + "<vII>" + moeda(vIi) + "</vII><vIOF>0.00</vIOF></II>"
                + "<ICMS><ICMS00><orig>1</orig><CST>00</CST><modBC>3</modBC>"
                + "<vBC>" + moeda(imp.baseIcms) + "</vBC>"
                + "<pICMS>" + String.format(Locale.US, "%.4f", imp.aliqIcms) + "</pICMS>"
                + "<vICMS>" + moeda(vIcms) + "</vICMS></ICMS00></ICMS>"
                + "<IPI><cEnq>999</cEnq><IPITrib><CST>50</CST><vBC>" + moeda(baseIpi) + "</vBC>"
                + "<pIPI>" + String.format(Locale.US, "%.4f", imp.aliqIpi) + "</pIPI>"
                + "<vIPI>" + moeda(vIpi) + "</vIPI></IPITrib></IPI>"
                + "<PIS><PISAliq><CST>01</CST><vBC>" + moeda(valorAduaneiro) + "</vBC>"
                + "<pPIS>2.1000</pPIS><vPIS>" + moeda(vPis) + "</vPIS></PISAliq></PIS>"
                + "<COFINS><COFINSAliq><CST>01</CST><vBC>" + moeda(valorAduaneiro) + "</vBC>"
                + "<pCOFINS>9.6500</pCOFINS><vCOFINS>" + moeda(vCofins) + "</vCOFINS>"
                + "</COFINSAliq></COFINS></imposto>"
                + "<vItem>" + moeda(vItem) + "</vItem></det>"
                + "<total><ICMSTot><vBC>" + moeda(imp.baseIcms) + "</vBC>"
                + "<vICMS>" + moeda(vIcms) + "</vICMS><vProd>" + moeda(imp.valorProduto) + "</vProd>"
                + "<vFrete>" + moeda(imp.frete) + "</vFrete><vSeg>" + moeda(imp.seguro) + "</vSeg>"
                + "<vDesc>0.00</vDesc><vII>" + moeda(vIi) + "</vII>"
                + "<vIPI>" + moeda(vIpi) + "</vIPI><vPIS>" + moeda(vPis) + "</vPIS>"
                + "<vCOFINS>" + moeda(vCofins) + "</vCOFINS><vOutro>" + moeda(outras) + "</vOutro>"
                + "<vNF>" + moeda(vNf) + "</vNF></ICMSTot></total>"
                + "<transp><modFrete>1</modFrete></transp>"
                + "<pag><detPag><tPag>15</tPag><vPag>" + moeda(vNf) + "</vPag></detPag></pag>"
                + "</infNFe></NFe><protNFe><infProt>"
                + "<chNFe>" + chave(imp.cnpjEmitente, imp.numero) + "</chNFe>"
                + "<dhRecbto>2026-03-20T10:05:00-03:00</dhRecbto><nProt>421260009519999</nProt>"
                + "<cStat>100</cStat><xMotivo>Autorizado</xMotivo></infProt></protNFe></nfeProc>";
    }

    private static double arredondar(double v) {
        return Math.round(v * 100.0) / 100.0;
    }

    public static String nfse(String numero, String cnpjPrestador, String cnpjTomador) {
        return nfse(numero, cnpjPrestador, cnpjTomador, 10000.00);
    }

    /** NFS-e nacional com ISS e retenções federais. */
    public static String nfse(String numero, String cnpjPrestador, String cnpjTomador,
                              double valorServico) {
        double iss = arredondar(valorServico * 0.05);
        double pis = arredondar(valorServico * 0.0065);
        double cofins = arredondar(valorServico * 0.03);
        double irrf = arredondar(valorServico * 0.015);
        double csll = arredondar(valorServico * 0.01);
        double inss = arredondar(valorServico * 0.011);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<NFSe xmlns=\"http://www.sped.fazenda.gov.br/nfse\" versao=\"1.01\">"
                + "<infNFSe Id=\"NFS2111300121105673700014200000000" + numero + "\">"
                + "<nNFSe>" + numero + "</nNFSe><cLocIncid>2111300</cLocIncid>"
                + "<xLocIncid>SAO LUIS</xLocIncid><cStat>100</cStat>"
                + "<dhProc>2026-06-25T12:44:50-03:00</dhProc>"
                + "<emit><CNPJ>" + cnpjPrestador + "</CNPJ><IM>70346001</IM>"
                + "<xNome>PRESTADOR DE SERVICOS LTDA</xNome>"
                + "<enderNac><xLgr>R PRINCIPAL</xLgr><nro>1000</nro><xBairro>VILA</xBairro>"
                + "<cMun>2111300</cMun><UF>MA</UF><CEP>65091100</CEP></enderNac>"
                + "<email>contato@prestador.com.br</email></emit>"
                + "<valores><vBC>" + moeda(valorServico) + "</vBC><pAliqAplic>5.00</pAliqAplic>"
                + "<vISSQN>" + moeda(iss) + "</vISSQN>"
                + "<vTotalRet>0.00</vTotalRet><vLiq>" + moeda(valorServico) + "</vLiq></valores>"
                + "<DPS versao=\"1.01\"><infDPS Id=\"DPS" + numero + "\">"
                + "<dhEmi>2026-06-25T12:44:50-03:00</dhEmi><serie>1</serie><nDPS>" + numero + "</nDPS>"
                + "<dCompet>2026-06-25</dCompet>"
                + "<prest><CNPJ>" + cnpjPrestador + "</CNPJ><IM>70346001</IM>"
                + "<regTrib><opSimpNac>1</opSimpNac></regTrib></prest>"
                + "<toma><CNPJ>" + cnpjTomador + "</CNPJ><xNome>TOMADORA SA</xNome></toma>"
                + "<serv><cServ><cTribNac>200101</cTribNac>"
                + "<xDescServ>SERVICOS DE TESTE</xDescServ></cServ></serv>"
                + "<valores><vServPrest><vServ>" + moeda(valorServico) + "</vServ></vServPrest>"
                + "<trib><tribMun><tribISSQN>1</tribISSQN><tpRetISSQN>1</tpRetISSQN></tribMun>"
                + "<tribFed><piscofins><CST>01</CST>"
                + "<vBCPisCofins>" + moeda(valorServico) + "</vBCPisCofins>"
                + "<pAliqPis>0.65</pAliqPis><pAliqCofins>3.00</pAliqCofins>"
                + "<vPis>" + moeda(pis) + "</vPis><vCofins>" + moeda(cofins) + "</vCofins></piscofins>"
                + "<vRetIRRF>" + moeda(irrf) + "</vRetIRRF>"
                + "<vRetCSLL>" + moeda(csll) + "</vRetCSLL>"
                + "<vRetCP>" + moeda(inss) + "</vRetCP></tribFed></trib>"
                + "</valores></infDPS></DPS></infNFSe></NFSe>";
    }

    /**
     * Chave de acesso de 44 posições no layout oficial:
     * cUF(2) AAMM(4) CNPJ(14) mod(2) serie(3) nNF(9) tpEmis(1) cNF(8) cDV(1).
     * O CNPJ é o do emitente, como na NF-e real.
     */
    public static String chave(String cnpjEmitente, String numero) {
        String n = numero.replaceAll("\\D", "");
        String cnpj = String.format("%-14s", cnpjEmitente.replaceAll("\\D", ""))
                .replace(' ', '0').substring(0, 14);
        return "21" + "2603" + cnpj + "55" + "001"
                + String.format("%09d", Long.parseLong(n.isEmpty() ? "0" : n))
                + "1" + "11558857" + "8";
    }

    // ------------------------------------------------------------------ SPED

    /** Arquivo SPED com N notas, cada uma com M itens e um C120 de importação. */
    public static String sped(int notas, int itensPorNota) {
        StringBuilder sb = new StringBuilder();
        sb.append("|0000|006|0|||01062026|30062026|EMPRESA TESTE LTDA|72977242000140|SP|3535309||00|0|\n");
        sb.append("|0001|0|\n");
        sb.append("|0150|9003065|FORNECEDOR TESTE|1058|11222333000181||111222333|3550308|||RUA X|10||CENTRO|\n");
        sb.append("|0200|ITEM1|PRODUTO DE TESTE|||UN|00|76011000||||\n");
        sb.append("|C001|0|\n");
        sb.append("|C010|72977242000140|0|\n");
        for (int n = 1; n <= notas; n++) {
            sb.append("|C100|0|1|9003065|55|00|1|").append(n).append('|').append(chaveSped(n))
              .append("|13032026|13032026|1000,00|0|0,00|0,00|1000,00|9|0,00|0,00|0,00|1000,00|120,00|0,00|0,00|26,00|16,50|76,00|\n");
            sb.append("|C120|0|26/1234567-8|0,00|0,00||\n");
            for (int i = 1; i <= itensPorNota; i++) {
                sb.append("|C170|").append(i)
                  .append("|ITEM1|DESCRICAO COMPLEMENTAR|10,000|UN|100,00|0,00|0|000|1101||100,00|12,00|12,00|0,00|0,00|0,00|0|50||100,00|2,60|2,60|01|100,00|1,6500|0,00|0,0000|1,65|01|100,00|7,6000|0,00|0,0000|7,60||0,00|\n");
            }
        }
        sb.append("|C990|").append(notas * (2 + itensPorNota) + 2).append("|\n");
        sb.append("|9999|999|\n");
        return sb.toString();
    }

    public static String chaveSped(int n) {
        return "212603" + "72977242000140" + "55" + "001" + String.format("%09d", n) + "1" + "12345678" + "9";
    }

    public static Path escreverSped(Path caso, String nome, String conteudo, Charset charset)
            throws IOException {
        Path arquivo = caso.resolve("xmls").resolve(nome);
        Files.write(arquivo, conteudo.getBytes(charset));
        return arquivo;
    }

    /** NF-e com o grupo IBSCBS completo, no item e no total. */
    public static String nfeComIbsCbs() {
        return nfe("55313", true, "42105890000901", "46754545000194",
                        "AL.PRIM. P1020A", "TO", "37.4530", "770807.69")
                .replace("</imposto>",
                        "<IBSCBS><CST>000</CST><cClassTrib>000001</cClassTrib><gIBSCBS>"
                        + "<vBC>615567.03</vBC>"
                        + "<gIBSUF><pIBSUF>0.1000</pIBSUF><vIBSUF>615.57</vIBSUF></gIBSUF>"
                        + "<gIBSMun><pIBSMun>0.0000</pIBSMun><vIBSMun>0.00</vIBSMun></gIBSMun>"
                        + "<vIBS>615.57</vIBS>"
                        + "<gCBS><pCBS>0.9000</pCBS><vCBS>5540.10</vCBS></gCBS>"
                        + "</gIBSCBS></IBSCBS></imposto>")
                .replace("</ICMSTot></total>",
                        "</ICMSTot><IBSCBSTot><vBCIBSCBS>615567.03</vBCIBSCBS>"
                        + "<gIBS><gIBSUF><vDif>1.11</vDif><vDevTrib>2.22</vDevTrib>"
                        + "<vIBSUF>615.57</vIBSUF></gIBSUF>"
                        + "<gIBSMun><vDif>0.00</vDif><vDevTrib>0.00</vDevTrib>"
                        + "<vIBSMun>0.00</vIBSMun></gIBSMun>"
                        + "<vIBS>615.57</vIBS><vCredPres>12.34</vCredPres>"
                        + "<vCredPresCondSus>0.00</vCredPresCondSus></gIBS>"
                        + "<gCBS><vDif>0.00</vDif><vDevTrib>0.00</vDevTrib><vCBS>5540.10</vCBS>"
                        + "<vCredPres>0.00</vCredPres><vCredPresCondSus>0.00</vCredPresCondSus></gCBS>"
                        + "</IBSCBSTot><vNFTot>790848.69</vNFTot></total>");
    }

    /** NFS-e com o grupo IBSCBS completo (alíquotas de redução e efetivas). */
    public static String nfseComIbsCbs() {
        return nfse("90100", "11056737000142", "42105890000901")
                .replace("</valores><DPS",
                        "</valores>"
                        + "<IBSCBS><cLocalidadeIncid>2111300</cLocalidadeIncid>"
                        + "<xLocalidadeIncid>SAO LUIS</xLocalidadeIncid>"
                        + "<valores><vBC>10000.00</vBC>"
                        + "<uf><pIBSUF>0.10</pIBSUF><pRedAliqUF>30.00</pRedAliqUF>"
                        + "<pAliqEfetUF>0.07</pAliqEfetUF></uf>"
                        + "<mun><pIBSMun>0.00</pIBSMun><pRedAliqMun>30.00</pRedAliqMun>"
                        + "<pAliqEfetMun>0.00</pAliqEfetMun></mun>"
                        + "<fed><pCBS>0.90</pCBS><pRedAliqCBS>30.00</pRedAliqCBS>"
                        + "<pAliqEfetCBS>0.63</pAliqEfetCBS></fed></valores>"
                        + "<totCIBS><vTotNF>10000.00</vTotNF>"
                        + "<gIBS><vIBSTot>7.00</vIBSTot>"
                        + "<gIBSUFTot><vIBSUF>7.00</vIBSUF></gIBSUFTot>"
                        + "<gIBSMunTot><vIBSMun>0.00</vIBSMun></gIBSMunTot></gIBS>"
                        + "<gCBS><vCBS>63.00</vCBS></gCBS></totCIBS></IBSCBS>"
                        + "<DPS")
                .replace("</infDPS>",
                        "<IBSCBS><finNFSe>0</finNFSe><cIndOp>100301</cIndOp><indDest>0</indDest>"
                        + "<valores><trib><gIBSCBS><CST>200</CST>"
                        + "<cClassTrib>200052</cClassTrib></gIBSCBS></trib></valores>"
                        + "</IBSCBS></infDPS>");
    }

    // -------------------------------------------------- Reforma Tributária

    /**
     * Injeta o grupo IBS/CBS numa NF-e já montada: no item
     * ({@code det/imposto/IBSCBS}) e no total ({@code total/IBSCBSTot}).
     * Os valores são calculados da base e das alíquotas, para o exemplo fechar.
     */
    public static String comIbsCbs(String xmlNfe, String cst, String classTrib,
                                   double base, double aliqUf, double aliqMun, double aliqCbs,
                                   double totalNota) {
        double vIbsUf = arredondar(base * aliqUf / 100);
        double vIbsMun = arredondar(base * aliqMun / 100);
        double vIbs = arredondar(vIbsUf + vIbsMun);
        double vCbs = arredondar(base * aliqCbs / 100);

        String item = "<IBSCBS><CST>" + cst + "</CST><cClassTrib>" + classTrib + "</cClassTrib>"
                + "<gIBSCBS><vBC>" + moeda(base) + "</vBC>"
                + "<gIBSUF><pIBSUF>" + aliquota(aliqUf) + "</pIBSUF>"
                + "<vIBSUF>" + moeda(vIbsUf) + "</vIBSUF></gIBSUF>"
                + "<gIBSMun><pIBSMun>" + aliquota(aliqMun) + "</pIBSMun>"
                + "<vIBSMun>" + moeda(vIbsMun) + "</vIBSMun></gIBSMun>"
                + "<vIBS>" + moeda(vIbs) + "</vIBS>"
                + "<gCBS><pCBS>" + aliquota(aliqCbs) + "</pCBS>"
                + "<vCBS>" + moeda(vCbs) + "</vCBS></gCBS></gIBSCBS></IBSCBS></imposto>";

        String total = "</ICMSTot><IBSCBSTot><vBCIBSCBS>" + moeda(base) + "</vBCIBSCBS>"
                + "<gIBS><gIBSUF><vDif>0.00</vDif><vDevTrib>0.00</vDevTrib>"
                + "<vIBSUF>" + moeda(vIbsUf) + "</vIBSUF></gIBSUF>"
                + "<gIBSMun><vDif>0.00</vDif><vDevTrib>0.00</vDevTrib>"
                + "<vIBSMun>" + moeda(vIbsMun) + "</vIBSMun></gIBSMun>"
                + "<vIBS>" + moeda(vIbs) + "</vIBS><vCredPres>0.00</vCredPres>"
                + "<vCredPresCondSus>0.00</vCredPresCondSus></gIBS>"
                + "<gCBS><vDif>0.00</vDif><vDevTrib>0.00</vDevTrib><vCBS>" + moeda(vCbs) + "</vCBS>"
                + "<vCredPres>0.00</vCredPres><vCredPresCondSus>0.00</vCredPresCondSus></gCBS>"
                + "</IBSCBSTot><vNFTot>" + moeda(totalNota) + "</vNFTot></total>";

        return xmlNfe.replace("</imposto>", item).replace("</ICMSTot></total>", total);
    }

    /** Injeta o grupo IBS/CBS numa NFS-e já montada (capa + DPS). */
    public static String comIbsCbsNfse(String xmlNfse, String cst, String classTrib,
                                       double base, double aliqUf, double reducao, double aliqCbs,
                                       double totalNota) {
        double efetUf = arredondar(aliqUf * (100 - reducao) / 100);
        double efetCbs = arredondar(aliqCbs * (100 - reducao) / 100);
        double vIbsUf = arredondar(base * efetUf / 100);
        double vCbs = arredondar(base * efetCbs / 100);

        String capa = "</valores>"
                + "<IBSCBS><cLocalidadeIncid>2111300</cLocalidadeIncid>"
                + "<xLocalidadeIncid>SAO LUIS</xLocalidadeIncid>"
                + "<valores><vBC>" + moeda(base) + "</vBC>"
                + "<uf><pIBSUF>" + moeda(aliqUf) + "</pIBSUF>"
                + "<pRedAliqUF>" + moeda(reducao) + "</pRedAliqUF>"
                + "<pAliqEfetUF>" + moeda(efetUf) + "</pAliqEfetUF></uf>"
                + "<mun><pIBSMun>0.00</pIBSMun><pRedAliqMun>" + moeda(reducao) + "</pRedAliqMun>"
                + "<pAliqEfetMun>0.00</pAliqEfetMun></mun>"
                + "<fed><pCBS>" + moeda(aliqCbs) + "</pCBS>"
                + "<pRedAliqCBS>" + moeda(reducao) + "</pRedAliqCBS>"
                + "<pAliqEfetCBS>" + moeda(efetCbs) + "</pAliqEfetCBS></fed></valores>"
                + "<totCIBS><vTotNF>" + moeda(totalNota) + "</vTotNF>"
                + "<gIBS><vIBSTot>" + moeda(vIbsUf) + "</vIBSTot>"
                + "<gIBSUFTot><vIBSUF>" + moeda(vIbsUf) + "</vIBSUF></gIBSUFTot>"
                + "<gIBSMunTot><vIBSMun>0.00</vIBSMun></gIBSMunTot></gIBS>"
                + "<gCBS><vCBS>" + moeda(vCbs) + "</vCBS></gCBS></totCIBS></IBSCBS>"
                + "<DPS";

        String dps = "<IBSCBS><finNFSe>0</finNFSe><cIndOp>100301</cIndOp><indDest>0</indDest>"
                + "<valores><trib><gIBSCBS><CST>" + cst + "</CST>"
                + "<cClassTrib>" + classTrib + "</cClassTrib></gIBSCBS></trib></valores>"
                + "</IBSCBS></infDPS>";

        return xmlNfse.replace("</valores><DPS", capa).replace("</infDPS>", dps);
    }

    private static String aliquota(double v) {
        return String.format(Locale.US, "%.4f", v);
    }

    // ------------------------------------------------------------ utilidades

    public static List<String> linhas(Path caso, String arquivo) throws IOException {
        Path p = caso.resolve("out").resolve(arquivo + ".txt");
        if (!Files.exists(p)) return new ArrayList<>();
        List<String> todas = new ArrayList<>(Files.readAllLines(p, StandardCharsets.UTF_8));
        todas.removeIf(String::isEmpty);
        return todas;
    }

    public static String[] campos(Path caso, String arquivo, int linha) throws IOException {
        List<String> l = linhas(caso, arquivo);
        if (linha >= l.size()) return new String[0];
        return l.get(linha).split("\t", -1);
    }

    public static String moeda(double v) {
        return String.format(Locale.US, "%.2f", v);
    }

    public static String escapar(String s) {
        return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;");
    }
}
