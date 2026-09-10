package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

/**
 * Conformidade genérica com o safx-layout.json: as regras saem do próprio
 * leiaute e valem para qualquer arquivo gerado, venha ele de SPED ou de XML.
 *
 * É o teste que pega, sozinho, a classe de defeito mais cara — registro
 * desalinhado, campo maior que o leiaute, data fora do formato, campo-chave em
 * branco — sem depender de alguém ter previsto o cenário.
 */
public final class TestesConformidadeLeiaute {

    /**
     * Exceções conhecidas e justificadas ao "campo-chave não pode ser nulo":
     * NFS-e nacional não tem modelo COTEPE (o campo 13 do SAFX07 só existe para
     * documento de mercadoria) e o SAFX49 sai vazio quando não há importação.
     */
    private static final String COD_MODELO_SAFX07 = "SAFX07:13";

    private TestesConformidadeLeiaute() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Conformidade com o leiaute (safx-layout.json)");

        Path casoXml = Fixtures.caso(base, "conformidade-xml");
        Fixtures.escrever(casoXml, "venda.xml",
                Fixtures.nfe("5001", true, "42105890000901", "46754545000194"));
        Fixtures.escrever(casoXml, "compra.xml",
                Fixtures.nfe("5002", false, "11222333000181", "42105890000901"));
        Fixtures.escrever(casoXml, "servico.xml",
                Fixtures.nfse("7001", "11056737000142", "42105890000901"));
        // documentos com grupo IBS/CBS, para os layouts 3007/3008/3009 saírem preenchidos
        Fixtures.escrever(casoXml, "venda-reforma.xml", Fixtures.nfeComIbsCbs());
        Fixtures.escrever(casoXml, "servico-reforma.xml", Fixtures.nfseComIbsCbs());
        System.setProperty("user.dir", casoXml.resolve("out").toString());
        Conversao.apartirDeXml(casoXml.resolve("xmls").toString());
        verificar("XML", casoXml.resolve("out"));
        verificarReforma("XML", casoXml.resolve("out"));

        Path casoSped = Fixtures.caso(base, "conformidade-sped");
        Path sped = Fixtures.escreverSped(casoSped, "sped.txt", Fixtures.sped(3, 2),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", casoSped.resolve("out").toString());
        Conversao.apartirDeSped(sped.toString());
        verificar("SPED", casoSped.resolve("out"));
    }

    private static void verificar(String origem, Path saida) throws IOException {
        for (String layout : new String[]{"SAFX04", "SAFX07", "SAFX08", "SAFX49"}) {
            Path arquivo = saida.resolve(layout + ".txt");
            if (!Files.exists(arquivo)) {
                Assercoes.check(origem + " " + layout + ": arquivo gerado", false, "não existe");
                continue;
            }
            List<String> linhas = new ArrayList<>(Files.readAllLines(arquivo, StandardCharsets.UTF_8));
            linhas.removeIf(String::isEmpty);
            if (linhas.isEmpty()) continue;

            List<Leiaute.Campo> campos = Leiaute.campos(layout);
            int esperado = Leiaute.quantidadeCampos(layout);

            List<String> erros = new ArrayList<>();
            for (int l = 0; l < linhas.size(); l++) {
                String[] valores = linhas.get(l).split("\t", -1);
                if (valores.length != esperado) {
                    erros.add("linha " + (l + 1) + ": " + valores.length + " campos (esperado " + esperado + ")");
                    continue;
                }
                for (int i = 0; i < valores.length; i++) {
                    Leiaute.Campo campo = campos.get(i);
                    String valor = valores[i];
                    String erro = validar(layout, campo, valor);
                    if (erro != null) {
                        erros.add("linha " + (l + 1) + " campo " + campo.posicao
                                + " (" + campo.nome + "): " + erro + " [" + valor + "]");
                    }
                }
            }
            Assercoes.check(origem + " " + layout + ": " + linhas.size()
                            + " linha(s) conformes ao leiaute", erros.isEmpty(),
                    erros.isEmpty() ? "" : erros.size() + " problema(s): " + erros.get(0));
        }
    }

    /** Mesmas regras, contra o leiaute do Manual da Reforma Tributária. */
    private static void verificarReforma(String origem, Path saida) throws IOException {
        for (String layout : new String[]{"SAFX3007", "SAFX3008", "SAFX3009"}) {
            Path arquivo = saida.resolve(layout + ".txt");
            if (!Files.exists(arquivo)) continue;
            List<String> linhas = new ArrayList<>(Files.readAllLines(arquivo, StandardCharsets.UTF_8));
            linhas.removeIf(String::isEmpty);
            if (linhas.isEmpty()) continue;

            List<Leiaute.Campo> campos = Leiaute.camposReforma(layout);
            List<String> erros = new ArrayList<>();
            for (int l = 0; l < linhas.size(); l++) {
                String[] valores = linhas.get(l).split("\t", -1);
                if (valores.length != campos.size()) {
                    erros.add("linha " + (l + 1) + ": " + valores.length + " campos (esperado "
                            + campos.size() + ")");
                    continue;
                }
                for (int i = 0; i < valores.length; i++) {
                    String erro = validar(layout, campos.get(i), valores[i]);
                    if (erro != null) {
                        erros.add("linha " + (l + 1) + " campo " + campos.get(i).posicao
                                + " (" + campos.get(i).nome + "): " + erro + " [" + valores[i] + "]");
                    }
                }
            }
            Assercoes.check(origem + " " + layout + ": " + linhas.size()
                            + " linha(s) conformes ao Manual da Reforma", erros.isEmpty(),
                    erros.isEmpty() ? "" : erros.size() + " problema(s): " + erros.get(0));
        }
    }

    private static String validar(String layout, Leiaute.Campo campo, String valor) {
        if (valor.indexOf('\t') >= 0 || valor.indexOf('\n') >= 0 || valor.indexOf('\r') >= 0) {
            return "contém TAB ou quebra de linha";
        }
        boolean nulo = "@".equals(valor);

        if (campo.chave && nulo && !(layout + ":" + campo.posicao).equals(COD_MODELO_SAFX07)) {
            return "campo-chave em branco";
        }
        if (nulo) return null;

        if (campo.decimal()) {
            if (!valor.chars().allMatch(Character::isDigit)) return "campo numérico com caractere inválido";
            // A largura vem do leiaute (015V002 = 17 dígitos, 003V004 = 7), não de
            // uma constante: estourar o tamanho declarado é erro. Gravar menos que
            // o declarado mantém o valor correto (as casas decimais são implícitas
            // e o arquivo é delimitado por TAB), então só os layouts da Reforma —
            // cujo manual está no repositório — exigem a largura exata.
            int declarado = campo.tamanho + campo.decimais;
            if (valor.length() > declarado) {
                return "campo numérico com " + valor.length() + " dígitos estoura o tamanho declarado ("
                        + declarado + ")";
            }
            if (layout.startsWith("SAFX30") && valor.length() != declarado) {
                return "campo numérico deveria ter " + declarado + " dígitos (manual da Reforma), tem "
                        + valor.length();
            }
            return null;
        }
        if (valor.length() > campo.tamanho) {
            return "tamanho " + valor.length() + " maior que o leiaute (" + campo.tamanho + ")";
        }
        if (campo.data()) {
            if (!valor.matches("\\d{8}")) return "data fora do formato YYYYMMDD";
            int ano = Integer.parseInt(valor.substring(0, 4));
            int mes = Integer.parseInt(valor.substring(4, 6));
            int dia = Integer.parseInt(valor.substring(6, 8));
            if (ano < 1900 || ano > 2200 || mes < 1 || mes > 12 || dia < 1 || dia > 31) {
                return "data inválida";
            }
        }
        return null;
    }
}
