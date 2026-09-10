package com.caldeira;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Gera uma pasta de XMLs de exemplo para experimentar a conversão sem depender
 * de documento fiscal real.
 *
 * Os arquivos são SINTÉTICOS: não têm assinatura digital nem protocolo válido
 * de SEFAZ e não são documento fiscal. O estabelecimento usado é o CNPJ
 * 72977242000140, que já vem cadastrado no estabelecimentos.properties do
 * projeto, então a conversão roda sem cadastro adicional.
 *
 * <pre>
 * java -cp /tmp/safx-classes:src/main/resources com.caldeira.GerarXmlsExemplo
 * </pre>
 */
public final class GerarXmlsExemplo {

    /** Estabelecimento da empresa — já cadastrado como 0001. */
    private static final String ESTABELECIMENTO = "72977242000140";

    private static final String AVISO =
            "Os arquivos desta pasta sao SINTETICOS, gerados por\n"
            + "src/test/java/com/caldeira/GerarXmlsExemplo.java.\n\n"
            + "Nao possuem assinatura digital nem protocolo de autorizacao valido:\n"
            + "servem apenas para experimentar a conversao XML -> SAFX. Nao sao\n"
            + "documento fiscal e nao devem ser usados para carga em producao.\n\n"
            + "Estabelecimento: CNPJ " + ESTABELECIMENTO + " (ja cadastrado como 0001\n"
            + "em src/main/resources/estabelecimentos.properties).\n\n"
            + "Conteudo:\n"
            + "  Vendas/    2 NF-e de saida (CFOP 6101), com ICMS, IPI, PIS e COFINS\n"
            + "  Entradas/  2 NF-e de importacao (CFOP 3101 e 3102) com DI, II,\n"
            + "             despesas aduaneiras e desembaraco -> alimentam o SAFX49;\n"
            + "             2 NFS-e de servico tomado (a empresa e a tomadora),\n"
            + "             que alimentam o SAFX3009\n"
            + "  Servicos/  1 NFS-e nacional, com ISS e retencoes federais\n\n"
            + "Todos os documentos trazem o grupo IBS/CBS da Reforma Tributaria,\n"
            + "entao a conversao tambem gera SAFX3007, SAFX3008 e SAFX3009.\n";

    public static void main(String[] args) throws IOException {
        Path destino = Paths.get(args.length > 0 ? args[0] : "src/test/resources/XMLs-exemplo");
        gerar(destino);
        System.out.println("Pasta de exemplo gerada em: " + destino.toAbsolutePath());
    }

    public static void gerar(Path destino) throws IOException {
        Path vendas = destino.resolve("Vendas");
        Path entradas = destino.resolve("Entradas");
        Path servicos = destino.resolve("Servicos");
        for (Path p : new Path[]{vendas, entradas, servicos}) {
            Files.createDirectories(p);
        }

        // a chave de acesso carrega o CNPJ do emitente, como na NF-e real;
        // todas as notas trazem o grupo IBS/CBS, para alimentar o SAFX3007/3008
        gravar(vendas, ESTABELECIMENTO, "70001", Fixtures.comIbsCbs(
                Fixtures.nfe("70001", true, ESTABELECIMENTO, "46754545000194",
                        "CHAPA DE ALUMINIO 3MM", "KG", "1500.0000", "45000.00"),
                "000", "000001", 40500.00, 0.10, 0.00, 0.90, 46170.00));
        gravar(vendas, ESTABELECIMENTO, "70002", Fixtures.comIbsCbs(
                Fixtures.nfe("70002", true, ESTABELECIMENTO, "29508058001447",
                        "LINGOTE DE ALUMINIO P1020", "TON", "12.5000", "128750.00"),
                "000", "000001", 115875.00, 0.10, 0.00, 0.90, 132097.50));

        // Entradas por importação (CFOP 3xxx com grupo DI): é o que alimenta o SAFX49
        Fixtures.Importacao eletrodo = new Fixtures.Importacao();
        eletrodo.numero = "80001";
        eletrodo.cnpjEmitente = "11222333000181";
        eletrodo.cnpjDestinatario = ESTABELECIMENTO;
        eletrodo.produto = "ELETRODO DE GRAFITE UHP 600MM";
        eletrodo.ncm = "85451100";
        eletrodo.exTipi = "00";
        eletrodo.cfop = "3101";
        eletrodo.unidade = "KG";
        eletrodo.quantidade = "800.0000";
        eletrodo.valorProduto = 96000.00;
        eletrodo.frete = 4200.00;
        eletrodo.seguro = 380.00;
        eletrodo.despesasAduaneiras = 1250.00;
        eletrodo.afrmm = 630.00;
        eletrodo.baseIcms = 160000.00;
        eletrodo.numeroDi = "26/0451233-7";
        eletrodo.dataDi = "2026-03-15";
        eletrodo.dataDesembaraco = "2026-03-18";
        eletrodo.drawback = "20260012345";
        eletrodo.fabricante = "GRAFTECH-US";
        gravar(entradas, eletrodo.cnpjEmitente, eletrodo.numero, Fixtures.comIbsCbs(
                Fixtures.nfeImportacao(eletrodo), "000", "000001",
                100580.00, 0.10, 0.00, 0.90, 120162.08));

        Fixtures.Importacao oleo = new Fixtures.Importacao();
        oleo.numero = "80002";
        oleo.cnpjEmitente = "12514972000183";
        oleo.cnpjDestinatario = ESTABELECIMENTO;
        oleo.produto = "OLEO LUBRIFICANTE INDUSTRIAL ISO 220";
        oleo.ncm = "27101932";
        oleo.exTipi = "01";
        oleo.cfop = "3102";
        oleo.unidade = "UN";
        oleo.quantidade = "40.0000";
        oleo.valorProduto = 7200.00;
        oleo.frete = 900.00;
        oleo.seguro = 60.00;
        oleo.despesasAduaneiras = 340.00;
        oleo.afrmm = 180.00;
        oleo.aliqIi = 14.0;
        oleo.baseIcms = 12000.00;
        oleo.numeroDi = "26/0459871-2";
        oleo.dataDi = "2026-03-11";
        oleo.dataDesembaraco = "2026-03-14";
        oleo.drawback = "";                  // sem ato concessório
        oleo.fabricante = "SHELL-NL";
        gravar(entradas, oleo.cnpjEmitente, oleo.numero, Fixtures.comIbsCbs(
                Fixtures.nfeImportacao(oleo), "000", "000001",
                8160.00, 0.10, 0.00, 0.90, 10287.52));

        // Serviço tomado é entrada: a NFS-e onde a empresa é tomadora entra aqui
        // e alimenta o SAFX3009 (itens de serviço). NF-e de mercadoria nunca
        // gera SAFX3009 — pelo leiaute, item de mercadoria é SAFX3008.
        Files.write(entradas.resolve("07060718000112_2026-06-22_80003.xml"),
                Fixtures.comIbsCbsNfse(
                        Fixtures.nfse("80003", "07060718000112", ESTABELECIMENTO, 24500.00),
                        "000", "000001", 24500.00, 0.10, 0.00, 0.90, 24500.00)
                        .getBytes(StandardCharsets.UTF_8));
        Files.write(entradas.resolve("12514972000183_2026-06-24_80004.xml"),
                Fixtures.comIbsCbsNfse(
                        Fixtures.nfse("80004", "12514972000183", ESTABELECIMENTO, 8300.00),
                        "200", "200052", 8300.00, 0.10, 30.00, 0.90, 8300.00)
                        .getBytes(StandardCharsets.UTF_8));

        Path nfse = servicos.resolve("11056737000142_2026-06-25_90001.xml");
        Files.write(nfse, Fixtures.comIbsCbsNfse(
                        Fixtures.nfse("90001", "11056737000142", ESTABELECIMENTO),
                        "000", "000001", 10000.00, 0.10, 30.00, 0.90, 10000.00)
                .getBytes(StandardCharsets.UTF_8));

        Files.write(destino.resolve("LEIA-ME.txt"), AVISO.getBytes(StandardCharsets.UTF_8));
    }

    private static void gravar(Path pasta, String cnpjEmitente, String numero, String xml)
            throws IOException {
        Files.write(pasta.resolve(Fixtures.chave(cnpjEmitente, numero) + ".xml"),
                xml.getBytes(StandardCharsets.UTF_8));
    }
}
