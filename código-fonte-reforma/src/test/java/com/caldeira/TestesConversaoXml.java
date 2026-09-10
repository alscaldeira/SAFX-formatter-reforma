package com.caldeira;

import com.caldeira.service.ConversorXml;

import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;

/**
 * Bateria de testes do módulo XML -> SAFX.
 *
 * Sem framework de teste: o pom do projeto não tem dependências. Para rodar:
 *
 * <pre>
 * javac -encoding UTF-8 -d /tmp/safx-classes $(find src/main/java src/test/java -name '*.java')
 * java -cp /tmp/safx-classes:src/main/resources com.caldeira.TestesConversaoXml /tmp/safx-casos
 * </pre>
 *
 * Cada cenário monta uma pasta própria com XMLs sintéticos, roda o
 * {@link ConversorXml} apontando user.dir para a saída do cenário e confere os
 * arquivos gerados. O processo termina com código 1 se algum caso falhar.
 */
public class TestesConversaoXml {

    static Path base;

    static final String PROPS =
            "42105890000901=002/0001\n" +
            "11222333000181=002/0001\n" +
            "99888777000166=0007\n";

    public static void main(String[] args) throws Exception {
        executar(args.length > 0 ? Paths.get(args[0])
                : Files.createTempDirectory("safx-testes-xml-"));
        System.out.println("\n================================");
        System.out.println("PASSOU: " + Assercoes.passou() + "   FALHOU: " + Assercoes.falhou());
        if (Assercoes.falhou() > 0) System.exit(1);
    }

    public static void executar(Path raiz) throws Exception {
        Assercoes.secao("Conversão XML -> SAFX (cenários funcionais)");
        base = raiz;
        Files.createDirectories(base);

        textoComQuebraDeLinhaETab();
        multiplosItens();
        destinatarioPessoaFisica();
        destinatarioExterior();
        mesmoCnpjEmDoisPapeis();
        notaCancelada();
        numeroDocumentoIrregular();
        valorMuitoGrande();
        xmlCorrompido();
        pastaSemXml();
        nfseSemTomador();
        estabelecimentoNaoCadastrado();
        nfeSemProtocolo();
        itemSemImposto();
        encodingLatin1();
        unidadeNormalizada();
        arquivoNaoXmlNaPasta();
        quantidadeZerada();
        valorQueEstoura();
        participanteRepetido();
        cstComOrigemDiferente();
        referenciaUnica();
        notaSemItens();
        rootDesconhecido();
        arquivoVazio();
        nfseCompleta();
        importacaoCompleta();
    }

    // ------------------------------------------------------------- cenários

    static void textoComQuebraDeLinhaETab() throws Exception {
        Path caso = caso("quebra-linha");
        escrever(caso, "nota.xml", nfe(cfg().comObs("Linha 1\nLinha 2\tcom tab\r\nLinha 3")
                .comDescricaoProduto("PRODUTO\tCOM TAB\nE QUEBRA")));
        rodar(caso);
        check("quebra de linha: SAFX07 tem 1 linha", linhas(caso, "SAFX07").size() == 1,
                "linhas=" + linhas(caso, "SAFX07").size());
        check("quebra de linha: SAFX07 com 302 campos", campos(caso, "SAFX07", 0).length == 302,
                "campos=" + campos(caso, "SAFX07", 0).length);
        check("quebra de linha: SAFX08 com 262 campos", campos(caso, "SAFX08", 0).length == 262,
                "campos=" + campos(caso, "SAFX08", 0).length);
        check("quebra de linha: CSV gerado", Files.exists(caso.resolve("out/SAFX07.csv")), "");
    }

    static void multiplosItens() throws Exception {
        Path caso = caso("multi-itens");
        escrever(caso, "nota.xml", nfe(cfg().comItens(3)));
        rodar(caso);
        List<String> itens = linhas(caso, "SAFX08");
        check("multi-itens: 3 linhas no SAFX08", itens.size() == 3, "linhas=" + itens.size());
        check("multi-itens: NUM_ITEM sequencial",
                campos(caso, "SAFX08", 0)[17].equals("00001")
                        && campos(caso, "SAFX08", 2)[17].equals("00003"),
                campos(caso, "SAFX08", 0)[17] + "/" + campos(caso, "SAFX08", 2)[17]);
        // VLR_PRODUTO da capa deve ser o vProd do total, não o do primeiro item
        check("multi-itens: VLR_PRODUTO da capa = total",
                campos(caso, "SAFX07", 0)[21].equals(valor16(3 * 25500.00, 2)),
                campos(caso, "SAFX07", 0)[21]);
    }

    static void destinatarioPessoaFisica() throws Exception {
        Path caso = caso("dest-pf");
        escrever(caso, "nota.xml", nfe(cfg().comSaida().comDestinatarioCpf("12345678909")));
        rodar(caso);
        String[] p = campos(caso, "SAFX04", 0);
        check("dest PF: IND_CONTEM_COD = 2", p[3].equals("2"), p[3]);
        check("dest PF: CPF_CGC preenchido", p[5].equals("12345678909"), p[5]);
        check("dest PF: papel cliente", p[0].equals("2"), p[0]);
        check("dest PF: COD_FIS_JUR igual na capa",
                campos(caso, "SAFX07", 0)[6].equals(p[1]),
                campos(caso, "SAFX07", 0)[6] + " x " + p[1]);
    }

    static void destinatarioExterior() throws Exception {
        Path caso = caso("dest-exterior");
        escrever(caso, "nota.xml", nfe(cfg().comSaida().comDestinatarioExterior()));
        rodar(caso);
        String[] p = campos(caso, "SAFX04", 0);
        check("exterior: IND_CONTEM_COD = 4", p[3].equals("4"), p[3]);
        check("exterior: UF = EX", p[18].equals("EX"), p[18]);
        check("exterior: CPF_CGC nulo", p[5].equals("@"), p[5]);
    }

    static void mesmoCnpjEmDoisPapeis() throws Exception {
        Path caso = caso("papel-misto");
        // 55555555000155 é cliente na venda e fornecedor na entrada de terceiro
        escrever(caso, "venda.xml", nfe(cfg().comSaida().comDestinatario("55555555000155").comNumero("1001")));
        escrever(caso, "compra.xml", nfe(cfg().comEmitente("55555555000155").comNumero("1002")));
        rodar(caso);
        String[] p = campos(caso, "SAFX04", 0);
        check("papel misto: SAFX04 = 5", p[0].equals("5"), p[0]);
        String capa1 = campos(caso, "SAFX07", 0)[5];
        String capa2 = campos(caso, "SAFX07", 1)[5];
        check("papel misto: capas também com 5", capa1.equals("5") && capa2.equals("5"),
                capa1 + "/" + capa2);
        String item = campos(caso, "SAFX08", 0)[6];
        check("papel misto: item também com 5", item.equals("5"), item);
    }

    static void notaCancelada() throws Exception {
        Path caso = caso("cancelada");
        escrever(caso, "nota.xml", nfe(cfg().comCstat("101")));
        rodar(caso);
        check("cancelada: SITUACAO = S", campos(caso, "SAFX07", 0)[29].equals("S"),
                campos(caso, "SAFX07", 0)[29]);
    }

    static void numeroDocumentoIrregular() throws Exception {
        Path caso = caso("num-doc");
        escrever(caso, "nota.xml", nfe(cfg().comNumero("9999999999999")));  // 13 dígitos
        rodar(caso);
        String num = campos(caso, "SAFX07", 0)[7];
        check("num-doc: truncado em 12 posições", num.length() == 12, num);
        check("num-doc: item usa o mesmo número",
                campos(caso, "SAFX08", 0)[8].equals(num),
                campos(caso, "SAFX08", 0)[8] + " x " + num);
    }

    static void valorMuitoGrande() throws Exception {
        Path caso = caso("valor-grande");
        escrever(caso, "nota.xml", nfe(cfg().comValorProduto("99999999999999.99")));
        rodar(caso);
        String v = campos(caso, "SAFX08", 0)[27];
        check("valor grande: campo continua com 16 dígitos", v.length() == 16, v + " (len=" + v.length() + ")");
    }

    static void xmlCorrompido() throws Exception {
        Path caso = caso("corrompido");
        escrever(caso, "quebrado.xml", "<?xml version=\"1.0\"?><nfeProc><NFe><infNFe>");
        String erro = rodarEsperandoErro(caso);
        check("corrompido: erro cita o arquivo", erro != null && erro.contains("quebrado.xml"), erro);
    }

    static void pastaSemXml() throws Exception {
        Path caso = caso("sem-xml");
        escrever(caso, "leiame.txt", "nada aqui");
        String erro = rodarEsperandoErro(caso);
        check("sem xml: mensagem clara", erro != null && erro.contains("Nenhum arquivo .xml"), erro);
    }

    static void nfseSemTomador() throws Exception {
        Path caso = caso("nfse-sem-toma");
        escrever(caso, "servico.xml", nfseSemTomadorXml());
        String erro = rodarEsperandoErro(caso);
        check("nfse sem tomador: erro identifica o estabelecimento",
                erro != null && erro.contains("estabelecimento"), erro);
    }

    static void estabelecimentoNaoCadastrado() throws Exception {
        Path caso = caso("estab-nao-cadastrado");
        escrever(caso, "nota.xml", nfe(cfg().comSaida().comEmitente("12121212000112")));
        String erro = rodarEsperandoErro(caso);
        check("estab não cadastrado: erro lista o CNPJ",
                erro != null && erro.contains("12121212000112"), erro);
    }

    static void nfeSemProtocolo() throws Exception {
        Path caso = caso("sem-protocolo");
        escrever(caso, "nota.xml", nfe(cfg().semProtocolo()));
        rodar(caso);
        check("sem protocolo: SITUACAO = N", campos(caso, "SAFX07", 0)[29].equals("N"),
                campos(caso, "SAFX07", 0)[29]);
        check("sem protocolo: chave veio do Id",
                campos(caso, "SAFX07", 0)[225].length() == 44,
                campos(caso, "SAFX07", 0)[225]);
    }

    static void itemSemImposto() throws Exception {
        Path caso = caso("sem-imposto");
        escrever(caso, "nota.xml", nfe(cfg().semImposto()));
        rodar(caso);
        String[] item = campos(caso, "SAFX08", 0);
        check("sem imposto: linha gerada com 262 campos", item.length == 262, "" + item.length);
        check("sem imposto: CST fica nulo", item[29].equals("@") && item[30].equals("@"),
                item[29] + "/" + item[30]);
        check("sem imposto: TRIB_ICMS preenchido", !item[54].equals("@"), item[54]);
    }

    static void encodingLatin1() throws Exception {
        Path caso = caso("latin1");
        String xml = nfe(cfg().comDescricaoProduto("AÇÚCAR REFINADO ESPECIAL"))
                .replace("encoding=\"utf-8\"", "encoding=\"ISO-8859-1\"");
        Files.write(caso.resolve("xmls/nota.xml"), xml.getBytes(Charset.forName("ISO-8859-1")));
        rodar(caso);
        String descr = campos(caso, "SAFX08", 0)[20];
        check("latin1: acentuação preservada", descr.startsWith("AÇÚCAR"), descr);
    }

    static void unidadeNormalizada() throws Exception {
        Path caso = caso("unidade");
        escrever(caso, "nota.xml", nfe(cfg().comUnidade("TON", "TON")));
        rodar(caso);
        String[] item = campos(caso, "SAFX08", 0);
        check("unidade: TON normalizada para TO nos campos 17 e 25",
                item[16].equals("TO") && item[24].equals("TO"), item[16] + "/" + item[24]);
    }

    static void arquivoNaoXmlNaPasta() throws Exception {
        Path caso = caso("lixo-na-pasta");
        escrever(caso, "nota.xml", nfe(cfg()));
        escrever(caso, ".DS_Store", "\0\0lixo");
        escrever(caso, "planilha.csv", "a;b;c");
        rodar(caso);
        check("lixo na pasta: só o XML foi processado", linhas(caso, "SAFX07").size() == 1,
                "linhas=" + linhas(caso, "SAFX07").size());
    }

    static void quantidadeZerada() throws Exception {
        Path caso = caso("qtd-zero");
        escrever(caso, "nota.xml", nfe(cfg().comQuantidade("0.0000", "0.00")));
        rodar(caso);
        String[] item = campos(caso, "SAFX08", 0);
        check("qtd zero: quantidade formatada", item[23].length() == 16, item[23]);
        check("qtd zero: sem NaN/Infinity na linha",
                !String.join("", item).contains("NaN") && !String.join("", item).contains("Infinity"), "");
    }

    static void valorQueEstoura() throws Exception {
        Path caso = caso("valor-estoura");
        escrever(caso, "nota.xml", nfe(cfg().comValorProduto("999999999999999.99")));
        String erro = rodarEsperandoErro(caso);
        check("valor que estoura: falha em vez de desalinhar o registro",
                erro != null && erro.contains("não cabe"), String.valueOf(erro));
    }

    static void participanteRepetido() throws Exception {
        Path caso = caso("participante-repetido");
        escrever(caso, "n1.xml", nfe(cfg().comSaida().comNumero("2001")));
        escrever(caso, "n2.xml", nfe(cfg().comSaida().comNumero("2002")));
        rodar(caso);
        check("participante repetido: uma única linha no SAFX04",
                linhas(caso, "SAFX04").size() == 1, "linhas=" + linhas(caso, "SAFX04").size());
        check("participante repetido: duas capas", linhas(caso, "SAFX07").size() == 2,
                "linhas=" + linhas(caso, "SAFX07").size());
    }

    static void cstComOrigemDiferente() throws Exception {
        Path caso = caso("cst-origem");
        String xml = nfe(cfg()).replace("<orig>0</orig><CST>00</CST>", "<orig>2</orig><CST>40</CST>");
        escrever(caso, "nota.xml", xml);
        rodar(caso);
        String[] item = campos(caso, "SAFX08", 0);
        check("cst: COD_SITUACAO_A = origem", item[29].equals("2"), item[29]);
        check("cst: COD_SITUACAO_B = CST", item[30].equals("40"), item[30]);
    }

    static void referenciaUnica() throws Exception {
        Path caso = caso("ref-unica");
        String xml = nfe(cfg()).replace("</ide>",
                "<NFref><refNFe>21260242105890000901550010000549881096247175</refNFe></NFref></ide>");
        escrever(caso, "nota.xml", xml);
        List<String> avisos = rodarComAvisos(caso);
        String[] capa = campos(caso, "SAFX07", 0);
        check("ref única: NUM_DOCFIS_REF = 000054988", capa[15].equals("000054988"), capa[15]);
        check("ref única: SERIE_DOCFIS_REF = 001", capa[16].equals("001"), capa[16]);
        check("ref única: sem aviso de referência múltipla",
                avisos.stream().noneMatch(a -> a.contains("documentos referenciados")),
                String.valueOf(avisos));
    }

    static void notaSemItens() throws Exception {
        Path caso = caso("sem-itens");
        escrever(caso, "nota.xml", nfe(cfg().comItens(0)));
        rodar(caso);
        check("sem itens: capa gerada", linhas(caso, "SAFX07").size() == 1,
                "linhas=" + linhas(caso, "SAFX07").size());
        check("sem itens: SAFX08 vazio", linhas(caso, "SAFX08").isEmpty(),
                "linhas=" + linhas(caso, "SAFX08").size());
    }

    static void rootDesconhecido() throws Exception {
        Path caso = caso("root-desconhecido");
        escrever(caso, "evento.xml",
                "<?xml version=\"1.0\"?><procEventoNFe versao=\"1.00\"><evento><infEvento>"
                        + "<chNFe>21260342105890000901550010000553131155885785</chNFe>"
                        + "</infEvento></evento></procEventoNFe>");
        escrever(caso, "nota.xml", nfe(cfg()));
        List<String> avisos = rodarComAvisos(caso);
        check("root desconhecido: nota válida processada", linhas(caso, "SAFX07").size() == 1,
                "linhas=" + linhas(caso, "SAFX07").size());
        check("root desconhecido: avisa o arquivo ignorado",
                avisos.stream().anyMatch(a -> a.contains("evento.xml")), String.valueOf(avisos));
    }

    static void arquivoVazio() throws Exception {
        Path caso = caso("arquivo-vazio");
        escrever(caso, "vazio.xml", "");
        String erro = rodarEsperandoErro(caso);
        check("arquivo vazio: erro cita o arquivo",
                erro != null && erro.contains("vazio.xml"), String.valueOf(erro));
    }

    static void nfseCompleta() throws Exception {
        Path caso = caso("nfse-completa");
        escrever(caso, "servico.xml", nfseCompletaXml());
        rodar(caso);
        String[] capa = campos(caso, "SAFX07", 0);
        check("nfse: MOVTO_E_S = 1", capa[2].equals("1"), capa[2]);
        check("nfse: COD_CLASS_DOC_FIS = 2", capa[11].equals("2"), capa[11]);
        // SAFX07 campo 45 (VLR_ALIQ_ISS) é 003V004: 3 inteiros + 4 decimais = 7 posições
        check("nfse: alíquota ISS 5,0000 em 7 posições", capa[44].equals("0050000"), capa[44]);
        check("nfse: valor ISS 4475,00", capa[45].equals(valor16(4475.00, 2)), capa[45]);
        check("nfse: base ISS 89500,00", capa[60].equals(valor16(89500.00, 2)), capa[60]);
        check("nfse: município do ISS", capa[72].equals("2111300"), capa[72]);
        check("nfse: não gera item no SAFX08", linhas(caso, "SAFX08").isEmpty(),
                "linhas=" + linhas(caso, "SAFX08").size());
        check("nfse: prestador é fornecedor no SAFX04",
                campos(caso, "SAFX04", 0)[0].equals("1"), campos(caso, "SAFX04", 0)[0]);
        check("nfse: inscrição municipal do prestador",
                campos(caso, "SAFX04", 0)[8].equals("70346001"), campos(caso, "SAFX04", 0)[8]);
    }

    static void importacaoCompleta() throws Exception {
        Path caso = caso("importacao");
        escrever(caso, "imp.xml", importacaoXml());
        rodar(caso);
        List<String> l = linhas(caso, "SAFX49");
        check("importação: 1 linha no SAFX49", l.size() == 1, "linhas=" + l.size());
        if (l.isEmpty()) return;
        String[] c = campos(caso, "SAFX49", 0);
        check("importação: 72 campos", c.length == 72, "" + c.length);
        check("importação: DAT_DI", c[2].equals("20260315"), c[2]);
        check("importação: NUM_DI", c[3].equals("26/1234567-8"), c[3]);
        check("importação: ato concessório quebrado",
                c[20].equals("2026") && c[21].equals("001234") && c[22].equals("5"),
                c[20] + "/" + c[21] + "/" + c[22]);
        check("importação: despesas aduaneiras = vDespAdu + vAFRMM",
                c[31].equals(valor16(1250.00, 2)), c[31]);
        check("importação: valor do II", c[47].equals(valor16(2700.00, 2)), c[47]);
        check("importação: data de desembaraço", c[70].equals("20260318"), c[70]);
        check("importação: capa marcada como entrada de terceiro",
                campos(caso, "SAFX07", 0)[2].equals("1"), campos(caso, "SAFX07", 0)[2]);
    }

    // ------------------------------------------------------------ infraestrutura

    static Path caso(String nome) throws IOException {
        Path caso = base.resolve(nome);
        Files.createDirectories(caso.resolve("xmls"));
        Files.createDirectories(caso.resolve("out"));
        Files.write(caso.resolve("out/estabelecimentos.properties"), PROPS.getBytes(StandardCharsets.UTF_8));
        return caso;
    }

    static void escrever(Path caso, String nome, String conteudo) throws IOException {
        Files.write(caso.resolve("xmls").resolve(nome), conteudo.getBytes(StandardCharsets.UTF_8));
    }

    static void rodar(Path caso) throws IOException {
        System.setProperty("user.dir", caso.resolve("out").toString());
        ConversorXml.converter(caso.resolve("xmls").toString());
    }

    static List<String> rodarComAvisos(Path caso) throws IOException {
        System.setProperty("user.dir", caso.resolve("out").toString());
        return ConversorXml.converter(caso.resolve("xmls").toString());
    }

    static String rodarEsperandoErro(Path caso) {
        try {
            rodar(caso);
            return null;
        } catch (Exception e) {
            return e.getMessage();
        }
    }

    static List<String> linhas(Path caso, String arquivo) throws IOException {
        Path p = caso.resolve("out").resolve(arquivo + ".txt");
        if (!Files.exists(p)) return List.of();
        List<String> todas = Files.readAllLines(p, StandardCharsets.UTF_8);
        todas.removeIf(String::isEmpty);
        return todas;
    }

    static String[] campos(Path caso, String arquivo, int linha) throws IOException {
        List<String> l = linhas(caso, arquivo);
        if (linha >= l.size()) return new String[0];
        return l.get(linha).split("\t", -1);
    }

    static String valor16(double v, int decimais) {
        return String.format(Locale.US, "%0" + (16 + 1) + "." + decimais + "f", v).replace(".", "");
    }

    static void check(String nome, boolean ok, String detalhe) {
        Assercoes.check(nome, ok, detalhe);
    }

    // ------------------------------------------------------------- fixtures

    static Cfg cfg() {
        return new Cfg();
    }

    static class Cfg {
        String emitente = "11222333000181";
        String destinatario = "42105890000901";
        String cpfDest;
        boolean exterior;
        boolean saida;
        String numero = "55313";
        String cStat = "100";
        boolean protocolo = true;
        boolean imposto = true;
        int itens = 1;
        String obs;
        String descricao = "AL.PRIM. P1020A PUR.MIN. 99.7%-LINGOTES";
        String uCom = "TO";
        String uTrib = "TO";
        String qCom = "1000.0000";
        String vProd = "25500.00";

        Cfg comSaida() { saida = true; emitente = "42105890000901"; destinatario = "46754545000194"; return this; }
        Cfg comEmitente(String c) { emitente = c; return this; }
        Cfg comDestinatario(String c) { destinatario = c; return this; }
        Cfg comDestinatarioCpf(String c) { cpfDest = c; destinatario = null; return this; }
        Cfg comDestinatarioExterior() { exterior = true; destinatario = null; return this; }
        Cfg comNumero(String n) { numero = n; return this; }
        Cfg comCstat(String c) { cStat = c; return this; }
        Cfg semProtocolo() { protocolo = false; return this; }
        Cfg semImposto() { imposto = false; return this; }
        Cfg comItens(int n) { itens = n; return this; }
        Cfg comObs(String o) { obs = o; return this; }
        Cfg comDescricaoProduto(String d) { descricao = d; return this; }
        Cfg comUnidade(String c, String t) { uCom = c; uTrib = t; return this; }
        Cfg comQuantidade(String q, String v) { qCom = q; vProd = v; return this; }
        Cfg comValorProduto(String v) { vProd = v; return this; }
    }

    static String nfe(Cfg c) {
        StringBuilder sb = new StringBuilder();
        sb.append("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
          .append("<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><NFe>")
          .append("<infNFe Id=\"NFe21260342105890000901550010000553131155885785\" versao=\"4.00\">")
          .append("<ide><cUF>21</cUF><natOp>Operacao</natOp><mod>55</mod><serie>1</serie><nNF>")
          .append(c.numero).append("</nNF><dhEmi>2026-03-13T05:26:14-03:00</dhEmi>")
          .append("<dhSaiEnt>2026-03-13T05:26:14-03:00</dhSaiEnt><tpNF>").append(c.saida ? "1" : "0")
          .append("</tpNF><idDest>").append(c.exterior ? "3" : "2").append("</idDest><cDV>5</cDV>")
          .append("<tpAmb>1</tpAmb><finNFe>1</finNFe></ide>")
          .append("<emit><CNPJ>").append(c.emitente).append("</CNPJ><xNome>EMITENTE TESTE LTDA</xNome>")
          .append("<xFant>Emitente</xFant><enderEmit><xLgr>RUA A</xLgr><nro>1</nro>")
          .append("<xBairro>CENTRO</xBairro><cMun>2111300</cMun><xMun>SAO LUIS</xMun><UF>MA</UF>")
          .append("<CEP>65095603</CEP><cPais>1058</cPais><fone>9832181656</fone></enderEmit>")
          .append("<IE>120844931</IE><CRT>3</CRT></emit><dest>");
        if (c.cpfDest != null) {
            sb.append("<CPF>").append(c.cpfDest).append("</CPF>");
        } else if (c.destinatario != null) {
            sb.append("<CNPJ>").append(c.destinatario).append("</CNPJ>");
        }
        sb.append("<xNome>DESTINATARIO TESTE</xNome><enderDest><xLgr>AV B</xLgr><nro>2</nro>")
          .append("<xBairro>CENTRO</xBairro>");
        if (!c.exterior) {
            sb.append("<cMun>3534302</cMun><xMun>ORLANDIA</xMun><UF>SP</UF><CEP>14620000</CEP>")
              .append("<cPais>1058</cPais><xPais>Brasil</xPais>");
        } else {
            sb.append("<cMun>9999999</cMun><xMun>EXTERIOR</xMun><cPais>2496</cPais><xPais>ESTADOS UNIDOS</xPais>");
        }
        sb.append("</enderDest><indIEDest>").append(c.exterior ? "9" : "1").append("</indIEDest></dest>");

        for (int i = 1; i <= c.itens; i++) {
            sb.append("<det nItem=\"").append(i).append("\"><prod><cProd>P").append(i).append("</cProd>")
              .append("<xProd>").append(escapar(c.descricao)).append("</xProd><NCM>76011000</NCM>")
              .append("<CFOP>").append(c.saida ? "6101" : "1505").append("</CFOP>")
              .append("<uCom>").append(c.uCom).append("</uCom><qCom>").append(c.qCom).append("</qCom>")
              .append("<vUnCom>25.5000000000</vUnCom><vProd>").append(c.vProd).append("</vProd>")
              .append("<uTrib>").append(c.uTrib).append("</uTrib><qTrib>").append(c.qCom).append("</qTrib>")
              .append("<vUnTrib>25.5000000000</vUnTrib><indTot>1</indTot></prod>");
            if (c.imposto) {
                sb.append("<imposto><ICMS><ICMS00><orig>0</orig><CST>00</CST><modBC>3</modBC>")
                  .append("<vBC>").append(c.vProd).append("</vBC><pICMS>12.0000</pICMS><vICMS>3060.00</vICMS>")
                  .append("</ICMS00></ICMS><IPI><cEnq>999</cEnq><IPITrib><CST>50</CST><vBC>")
                  .append(c.vProd).append("</vBC><pIPI>2.6000</pIPI><vIPI>663.00</vIPI></IPITrib></IPI>")
                  .append("<PIS><PISAliq><CST>01</CST><vBC>").append(c.vProd)
                  .append("</vBC><pPIS>1.6500</pPIS><vPIS>420.75</vPIS></PISAliq></PIS>")
                  .append("<COFINS><COFINSAliq><CST>01</CST><vBC>").append(c.vProd)
                  .append("</vBC><pCOFINS>7.6000</pCOFINS><vCOFINS>1938.00</vCOFINS></COFINSAliq></COFINS>")
                  .append("</imposto>");
            } else {
                sb.append("<imposto></imposto>");
            }
            sb.append("<vItem>").append(c.vProd).append("</vItem></det>");
        }

        double totalProd = Double.parseDouble(c.vProd) * c.itens;
        sb.append("<total><ICMSTot><vBC>0.00</vBC><vICMS>0.00</vICMS><vProd>")
          .append(String.format(Locale.US, "%.2f", totalProd))
          .append("</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg><vDesc>0.00</vDesc><vII>0.00</vII>")
          .append("<vIPI>0.00</vIPI><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro>")
          .append("<vNF>").append(String.format(Locale.US, "%.2f", totalProd)).append("</vNF></ICMSTot></total>")
          .append("<transp><modFrete>0</modFrete></transp>")
          .append("<pag><detPag><tPag>15</tPag><vPag>0.00</vPag></detPag></pag>");
        if (c.obs != null) {
            sb.append("<infAdic><infCpl>").append(escapar(c.obs)).append("</infCpl></infAdic>");
        }
        sb.append("</infNFe></NFe>");
        if (c.protocolo) {
            sb.append("<protNFe><infProt><chNFe>21260342105890000901550010000553131155885785</chNFe>")
              .append("<dhRecbto>2026-03-13T05:26:31-03:00</dhRecbto><nProt>421260009504426</nProt>")
              .append("<cStat>").append(c.cStat).append("</cStat><xMotivo>Teste</xMotivo></infProt></protNFe>");
        }
        sb.append("</nfeProc>");
        return sb.toString();
    }

    static String nfseSemTomadorXml() {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<NFSe xmlns=\"http://www.sped.fazenda.gov.br/nfse\" versao=\"1.01\">"
                + "<infNFSe Id=\"NFS211130012070607180001120000000014407\"><nNFSe>14407</nNFSe>"
                + "<cStat>100</cStat><dhProc>2026-06-22T10:49:27-03:00</dhProc>"
                + "<emit><CNPJ>07060718000112</CNPJ><IM>11107001</IM><xNome>PRESTADOR</xNome>"
                + "<enderNac><xLgr>AV X</xLgr><nro>1</nro><xBairro>C</xBairro><cMun>2111300</cMun>"
                + "<UF>MA</UF><CEP>65075230</CEP></enderNac></emit>"
                + "<valores><vLiq>2340.00</vLiq></valores>"
                + "<DPS versao=\"1.01\"><infDPS Id=\"DPS1\"><dhEmi>2026-06-22T10:49:27-03:00</dhEmi>"
                + "<serie>1</serie><nDPS>14407</nDPS><dCompet>2026-06-22</dCompet>"
                + "<prest><CNPJ>07060718000112</CNPJ></prest>"
                + "<valores><vServPrest><vServ>2340.00</vServ></vServPrest></valores>"
                + "</infDPS></DPS></infNFSe></NFSe>";
    }

    static String nfseCompletaXml() {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<NFSe xmlns=\"http://www.sped.fazenda.gov.br/nfse\" versao=\"1.01\">"
                + "<infNFSe Id=\"NFS21113001211056737000142000000000253226062986532670\">"
                + "<nNFSe>2532</nNFSe><cLocIncid>2111300</cLocIncid><xLocIncid>SAO LUIS</xLocIncid>"
                + "<cStat>100</cStat><dhProc>2026-06-25T12:44:50-03:00</dhProc>"
                + "<emit><CNPJ>11056737000142</CNPJ><IM>70346001</IM><xNome>PRESTADOR LTDA</xNome>"
                + "<enderNac><xLgr>R PRINCIPAL</xLgr><nro>1000</nro><xBairro>VILA</xBairro>"
                + "<cMun>2111300</cMun><UF>MA</UF><CEP>65091100</CEP></enderNac>"
                + "<email>contato@teste.com.br</email></emit>"
                + "<valores><vBC>89500.00</vBC><pAliqAplic>5.00</pAliqAplic><vISSQN>4475.00</vISSQN>"
                + "<vTotalRet>0.00</vTotalRet><vLiq>89500.00</vLiq></valores>"
                + "<DPS versao=\"1.01\"><infDPS Id=\"DPS1\"><dhEmi>2026-06-25T12:44:50-03:00</dhEmi>"
                + "<serie>1</serie><nDPS>2532</nDPS><dCompet>2026-06-25</dCompet>"
                + "<prest><CNPJ>11056737000142</CNPJ><IM>70346001</IM>"
                + "<regTrib><opSimpNac>1</opSimpNac></regTrib></prest>"
                + "<toma><CNPJ>42105890000901</CNPJ><xNome>TOMADORA SA</xNome></toma>"
                + "<serv><cServ><cTribNac>200101</cTribNac>"
                + "<xDescServ>SERVICOS PORTUARIOS</xDescServ></cServ></serv>"
                + "<valores><vServPrest><vServ>89500.00</vServ></vServPrest>"
                + "<trib><tribMun><tribISSQN>1</tribISSQN><tpRetISSQN>1</tpRetISSQN></tribMun>"
                + "<tribFed><piscofins><CST>01</CST><vBCPisCofins>89500.00</vBCPisCofins>"
                + "<pAliqPis>0.65</pAliqPis><pAliqCofins>3.00</pAliqCofins>"
                + "<vPis>581.75</vPis><vCofins>2685.00</vCofins></piscofins>"
                + "<vRetIRRF>1342.50</vRetIRRF><vRetCSLL>895.00</vRetCSLL></tribFed></trib>"
                + "</valores></infDPS></DPS></infNFSe></NFSe>";
    }

    static String importacaoXml() {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><NFe>"
                + "<infNFe Id=\"NFe21260342105890000901550010000900011155885785\" versao=\"4.00\">"
                + "<ide><cUF>21</cUF><natOp>Importacao</natOp><mod>55</mod><serie>1</serie>"
                + "<nNF>90001</nNF><dhEmi>2026-03-20T10:00:00-03:00</dhEmi>"
                + "<dhSaiEnt>2026-03-21T08:00:00-03:00</dhSaiEnt><tpNF>0</tpNF><finNFe>1</finNFe>"
                + "<cDV>5</cDV></ide>"
                + "<emit><CNPJ>11222333000181</CNPJ><xNome>IMPORTADORA LTDA</xNome>"
                + "<enderEmit><xLgr>RUA A</xLgr><nro>10</nro><xMun>SANTOS</xMun><UF>SP</UF>"
                + "<CEP>11010000</CEP><cPais>1058</cPais></enderEmit><IE>111222333</IE></emit>"
                + "<dest><CNPJ>42105890000901</CNPJ><xNome>DESTINATARIA SA</xNome>"
                + "<enderDest><xLgr>ROD BR 135</xLgr><nro>04</nro><xMun>Sao Luis</xMun><UF>MA</UF>"
                + "<CEP>65099110</CEP><cPais>1058</cPais></enderDest><indIEDest>1</indIEDest></dest>"
                + "<det nItem=\"1\"><prod><cProd>INS-001</cProd><xProd>INSUMO IMPORTADO</xProd>"
                + "<NCM>28182000</NCM><EXTIPI>01</EXTIPI><CFOP>3101</CFOP><uCom>KG</uCom>"
                + "<qCom>1000.0000</qCom><vUnCom>25.5000</vUnCom><vProd>25500.00</vProd>"
                + "<uTrib>KG</uTrib><qTrib>1000.0000</qTrib><vFrete>1200.00</vFrete>"
                + "<vSeg>300.00</vSeg><indTot>1</indTot>"
                + "<DI><nDI>26/1234567-8</nDI><dDI>2026-03-15</dDI><UFDesemb>MA</UFDesemb>"
                + "<dDesemb>2026-03-18</dDesemb><tpViaTransp>01</tpViaTransp><vAFRMM>450.00</vAFRMM>"
                + "<tpIntermedio>1</tpIntermedio><adi><nAdicao>1</nAdicao><nSeqAdic>1</nSeqAdic>"
                + "<cFabricante>FAB123</cFabricante><vDescDI>100.00</vDescDI>"
                + "<nDraw>20260012345</nDraw></adi></DI></prod>"
                + "<imposto><II><vBC>27000.00</vBC><vDespAdu>800.00</vDespAdu><vII>2700.00</vII>"
                + "<vIOF>50.00</vIOF></II>"
                + "<ICMS><ICMS00><orig>1</orig><CST>00</CST><vBC>35000.00</vBC>"
                + "<pICMS>18.0000</pICMS><vICMS>6300.00</vICMS></ICMS00></ICMS>"
                + "<IPI><cEnq>999</cEnq><IPITrib><CST>50</CST><vBC>28200.00</vBC>"
                + "<pIPI>5.0000</pIPI><vIPI>1410.00</vIPI></IPITrib></IPI>"
                + "<PIS><PISAliq><CST>01</CST><vBC>28200.00</vBC><pPIS>2.1000</pPIS>"
                + "<vPIS>592.20</vPIS></PISAliq></PIS>"
                + "<COFINS><COFINSAliq><CST>01</CST><vBC>28200.00</vBC><pCOFINS>9.6500</pCOFINS>"
                + "<vCOFINS>2721.30</vCOFINS></COFINSAliq></COFINS></imposto>"
                + "<vItem>36502.20</vItem></det>"
                + "<total><ICMSTot><vBC>35000.00</vBC><vICMS>6300.00</vICMS><vProd>25500.00</vProd>"
                + "<vFrete>1200.00</vFrete><vSeg>300.00</vSeg><vDesc>0.00</vDesc><vII>2700.00</vII>"
                + "<vIPI>1410.00</vIPI><vPIS>592.20</vPIS><vCOFINS>2721.30</vCOFINS>"
                + "<vOutro>800.00</vOutro><vNF>38210.00</vNF></ICMSTot></total>"
                + "<transp><modFrete>1</modFrete></transp></infNFe></NFe>"
                + "<protNFe><infProt><chNFe>21260342105890000901550010000900011155885785</chNFe>"
                + "<dhRecbto>2026-03-20T10:05:00-03:00</dhRecbto><nProt>421260009519999</nProt>"
                + "<cStat>100</cStat><xMotivo>Autorizado</xMotivo></infProt></protNFe></nfeProc>";
    }

    static String escapar(String s) {
        return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;");
    }
}
