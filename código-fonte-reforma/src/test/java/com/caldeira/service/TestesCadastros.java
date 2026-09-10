package com.caldeira.service;

import com.caldeira.Assercoes;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

/**
 * Unidades de cadastro e formatação. Fica no pacote com.caldeira.service porque
 * Estabelecimentos, Participantes, Unidades e SafxFormat são package-private —
 * são as classes das quais todo o resto depende.
 */
public final class TestesCadastros {

    private TestesCadastros() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Cadastros e formatação numérica");
        safxFormat();
        estabelecimentos(base);
        cacheInvalidado(base);
        participantes(base);
        unidades(base);
    }

    private static void safxFormat() {
        Assercoes.check("SafxFormat: 2 casas",
                SafxFormat.numero(1234.56, 2).equals("0000000000123456"),
                SafxFormat.numero(1234.56, 2));
        Assercoes.check("SafxFormat: 4 casas",
                SafxFormat.numero(1.65, 4).equals("0000000000016500"),
                SafxFormat.numero(1.65, 4));
        Assercoes.check("SafxFormat: 6 casas",
                SafxFormat.numero(10.0, 6).equals("0000000010000000"),
                SafxFormat.numero(10.0, 6));
        Assercoes.check("SafxFormat: zero",
                SafxFormat.numero(0.0, 2).equals("0000000000000000"),
                SafxFormat.numero(0.0, 2));
        Assercoes.check("SafxFormat: arredondamento de meio centavo (HALF_UP)",
                SafxFormat.numero(0.125, 2).equals("0000000000000013"),
                SafxFormat.numero(0.125, 2));
        Assercoes.check("SafxFormat: sempre 16 dígitos",
                SafxFormat.numero(999999999999.99, 2).length() == 16,
                SafxFormat.numero(999999999999.99, 2));

        String erro = null;
        try {
            SafxFormat.numero(99999999999999999.0, 2);
        } catch (IllegalArgumentException e) {
            erro = e.getMessage();
        }
        Assercoes.check("SafxFormat: recusa valor que não cabe",
                erro != null && erro.contains("não cabe"), String.valueOf(erro));

        // Locale: número não pode sair com vírgula decimal
        java.util.Locale original = java.util.Locale.getDefault();
        try {
            java.util.Locale.setDefault(java.util.Locale.GERMANY);
            Assercoes.check("SafxFormat: imune ao Locale do sistema",
                    SafxFormat.numero(1234.56, 2).equals("0000000000123456"),
                    SafxFormat.numero(1234.56, 2));
        } finally {
            java.util.Locale.setDefault(original);
        }
    }

    private static void estabelecimentos(Path base) throws IOException {
        Path dir = base.resolve("cadastro-estab");
        Files.createDirectories(dir);
        Files.write(dir.resolve("estabelecimentos.properties"),
                ("72977242000140=0001\n"
                        + "42.105.890/0009-01=002/0004\n").getBytes(StandardCharsets.UTF_8));
        System.setProperty("user.dir", dir.toString());

        Assercoes.check("Estabelecimentos: valor sem barra usa empresa padrão 001",
                Estabelecimentos.empresa("72977242000140").equals("001"),
                Estabelecimentos.empresa("72977242000140"));
        Assercoes.check("Estabelecimentos: COD_ESTAB sem barra",
                Estabelecimentos.codigo("72977242000140").equals("0001"),
                Estabelecimentos.codigo("72977242000140"));
        Assercoes.check("Estabelecimentos: empresa/estabelecimento com barra",
                Estabelecimentos.empresa("42105890000901").equals("002")
                        && Estabelecimentos.codigo("42105890000901").equals("0004"),
                Estabelecimentos.empresa("42105890000901") + "/"
                        + Estabelecimentos.codigo("42105890000901"));
        Assercoes.check("Estabelecimentos: chave com máscara é normalizada",
                Estabelecimentos.codigo("42.105.890/0009-01").equals("0004"),
                Estabelecimentos.codigo("42.105.890/0009-01"));

        String erro = null;
        try {
            Estabelecimentos.validar(java.util.List.of("11111111000111", "22222222000122"));
        } catch (IOException e) {
            erro = e.getMessage();
        }
        Assercoes.check("Estabelecimentos: validação lista todos os faltantes de uma vez",
                erro != null && erro.contains("11111111000111") && erro.contains("22222222000122"),
                String.valueOf(erro));

        // CNPJ do documento: chave da NF-e vence o C010 em emissão própria
        Assercoes.check("Estabelecimentos: emissão própria usa o CNPJ da chave da NF-e",
                Estabelecimentos.cnpjDoDocumento(
                        "21260372977242000302550010000553131155885785", "0", "72977242000140", "72977242000140")
                        .equals("72977242000302"),
                Estabelecimentos.cnpjDoDocumento(
                        "21260372977242000302550010000553131155885785", "0", "72977242000140", "72977242000140"));
        Assercoes.check("Estabelecimentos: documento de terceiro usa o C010 aberto",
                Estabelecimentos.cnpjDoDocumento(
                        "21260372977242000302550010000553131155885785", "1", "72977242000493", "72977242000140")
                        .equals("72977242000493"),
                Estabelecimentos.cnpjDoDocumento(
                        "21260372977242000302550010000553131155885785", "1", "72977242000493", "72977242000140"));
    }

    /**
     * A tela é um processo longo: quem corrige o .properties depois de um erro
     * precisa ver o cadastro novo sem reiniciar o programa.
     */
    private static void cacheInvalidado(Path base) throws IOException {
        Path dir = base.resolve("cadastro-cache");
        Files.createDirectories(dir);
        Path arquivo = dir.resolve("estabelecimentos.properties");
        Files.write(arquivo, "72977242000140=0001\n".getBytes(StandardCharsets.UTF_8));
        System.setProperty("user.dir", dir.toString());

        String erroAntes = null;
        try {
            Estabelecimentos.codigo("99999999000199");
        } catch (IOException e) {
            erroAntes = e.getMessage();
        }
        Assercoes.check("cache: CNPJ ausente falha na primeira conversão", erroAntes != null, "");

        Files.write(arquivo,
                "72977242000140=0001\n99999999000199=009/0002\n".getBytes(StandardCharsets.UTF_8));
        Files.setLastModifiedTime(arquivo,
                java.nio.file.attribute.FileTime.fromMillis(System.currentTimeMillis() + 2000));

        String codigo = null;
        String erroDepois = null;
        try {
            codigo = Estabelecimentos.codigo("99999999000199");
        } catch (IOException e) {
            erroDepois = e.getMessage();
        }
        Assercoes.check("cache: cadastro recém-editado é enxergado sem reiniciar",
                "0002".equals(codigo), codigo + " / erro=" + erroDepois);
        Assercoes.check("cache: empresa do cadastro recém-editado",
                "009".equals(Estabelecimentos.empresa("99999999000199")),
                Estabelecimentos.empresa("99999999000199"));
    }

    private static void participantes(Path base) throws IOException {
        Path dir = base.resolve("cadastro-participantes");
        Files.createDirectories(dir);
        Files.write(dir.resolve("participantes.properties"),
                "46754545000194=9003065\n".getBytes(StandardCharsets.UTF_8));
        System.setProperty("user.dir", dir.toString());
        Participantes.limparAvisos();

        Assercoes.check("Participantes: usa o COD_PART do de-para",
                Participantes.codFisJur("46754545000194").equals("M9003065"),
                Participantes.codFisJur("46754545000194"));
        // sem de-para o código é o próprio CNPJ, sem o prefixo "M": com ele
        // seriam 15 posições e o campo COD_FIS_JUR tem 14
        Assercoes.check("Participantes: sem de-para usa o CNPJ e cabe em 14 posições",
                Participantes.codFisJur("29508058001447").equals("29508058001447"),
                Participantes.codFisJur("29508058001447"));

        String erroLongo = null;
        try {
            Path dirLongo = base.resolve("cadastro-participantes-longo");
            Files.createDirectories(dirLongo);
            Files.write(dirLongo.resolve("participantes.properties"),
                    "46754545000194=CODIGOMUITOLONGO\n".getBytes(StandardCharsets.UTF_8));
            System.setProperty("user.dir", dirLongo.toString());
            Participantes.codFisJur("46754545000194");
        } catch (IOException e) {
            erroLongo = e.getMessage();
        } finally {
            System.setProperty("user.dir", dir.toString());
        }
        Assercoes.check("Participantes: COD_PART longo demais é recusado com mensagem clara",
                erroLongo != null && erroLongo.contains("não cabe"), String.valueOf(erroLongo));
        Assercoes.check("Participantes: CNPJ sem cadastro entra na lista de avisos",
                Participantes.semCadastro().contains("29508058001447"),
                String.valueOf(Participantes.semCadastro()));
        Assercoes.check("Participantes: quem tem de-para não vira aviso",
                !Participantes.semCadastro().contains("46754545000194"),
                String.valueOf(Participantes.semCadastro()));
        Participantes.limparAvisos();
        Assercoes.check("Participantes: avisos são zerados entre conversões",
                Participantes.semCadastro().isEmpty(), String.valueOf(Participantes.semCadastro()));
    }

    private static void unidades(Path base) throws IOException {
        Path dir = base.resolve("cadastro-unidades");
        Files.createDirectories(dir);
        Files.write(dir.resolve("unidades.properties"),
                "TON=TO\nCX=CAIXA\n".getBytes(StandardCharsets.UTF_8));
        System.setProperty("user.dir", dir.toString());

        Assercoes.check("Unidades: converte pelo de-para",
                Unidades.normalizar("TON").equals("TO"), Unidades.normalizar("TON"));
        Assercoes.check("Unidades: de-para é insensível a caixa e espaços",
                Unidades.normalizar(" ton ").equals("TO"), Unidades.normalizar(" ton "));
        Assercoes.check("Unidades: unidade fora do de-para volta em maiúsculas",
                Unidades.normalizar("kg").equals("KG"), Unidades.normalizar("kg"));
        Assercoes.check("Unidades: nulo e vazio não quebram",
                Unidades.normalizar(null).isEmpty() && Unidades.normalizar("  ").isEmpty(), "");
    }
}
