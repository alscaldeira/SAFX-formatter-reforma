package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * Vínculos entre os quatro arquivos, conforme o bloco "relationships" do
 * safx-layout.json:
 *
 * <ul>
 *   <li>COD_FIS_JUR do 07/08/49 tem de existir no SAFX04, com o mesmo IND_FIS_JUR;</li>
 *   <li>todo item do SAFX08 pertence a uma capa do SAFX07 (chave completa);</li>
 *   <li>todo registro do SAFX49 aponta para um item do SAFX08.</li>
 * </ul>
 *
 * Foi a quebra dessa regra que escondeu o bug do participante do exterior.
 */
public final class TestesIntegridadeReferencial {

    private TestesIntegridadeReferencial() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Integridade referencial entre SAFX04/07/08/49");

        Path caso = Fixtures.caso(base, "integridade-xml");
        Fixtures.escrever(caso, "venda1.xml",
                Fixtures.nfe("6001", true, "42105890000901", "46754545000194"));
        Fixtures.escrever(caso, "venda2.xml",
                Fixtures.nfe("6002", true, "42105890000901", "29508058001447"));
        Fixtures.escrever(caso, "compra.xml",
                Fixtures.nfe("6003", false, "46754545000194", "42105890000901"));
        Fixtures.escrever(caso, "servico.xml",
                Fixtures.nfse("6004", "11056737000142", "42105890000901"));
        System.setProperty("user.dir", caso.resolve("out").toString());
        Conversao.apartirDeXml(caso.resolve("xmls").toString());
        verificar("XML", caso);

        Path casoSped = Fixtures.caso(base, "integridade-sped");
        Path sped = Fixtures.escreverSped(casoSped, "sped.txt", Fixtures.sped(2, 3),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", casoSped.resolve("out").toString());
        Conversao.apartirDeSped(sped.toString());
        verificar("SPED", casoSped);
    }

    private static void verificar(String origem, Path caso) throws IOException {
        // SAFX04: IND_FIS_JUR (1) + COD_FIS_JUR (2)
        Set<String> cadastro = new HashSet<>();
        Set<String> codigosCadastrados = new HashSet<>();
        for (String linha : Fixtures.linhas(caso, "SAFX04")) {
            String[] c = linha.split("\t", -1);
            cadastro.add(c[0] + "|" + c[1]);
            codigosCadastrados.add(c[1]);
        }
        Assercoes.check(origem + ": SAFX04 sem participante duplicado",
                codigosCadastrados.size() == Fixtures.linhas(caso, "SAFX04").size(),
                codigosCadastrados.size() + " códigos em " + Fixtures.linhas(caso, "SAFX04").size() + " linhas");

        // SAFX07: chave da capa
        Set<String> capas = new HashSet<>();
        List<String> semCadastro = new ArrayList<>();
        for (String linha : Fixtures.linhas(caso, "SAFX07")) {
            String[] c = linha.split("\t", -1);
            capas.add(chaveDocumento(c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7], c[8]));
            if (!cadastro.contains(c[5] + "|" + c[6])) semCadastro.add(c[5] + "|" + c[6]);
        }
        Assercoes.check(origem + ": todo COD_FIS_JUR do SAFX07 existe no SAFX04 com o mesmo papel",
                semCadastro.isEmpty(), String.valueOf(semCadastro));

        // SAFX08: cada item pertence a uma capa
        List<String> orfaos = new ArrayList<>();
        Set<String> itens = new HashSet<>();
        for (String linha : Fixtures.linhas(caso, "SAFX08")) {
            String[] c = linha.split("\t", -1);
            String chave = chaveDocumento(c[0], c[1], c[3], c[4], c[5], c[6], c[7], c[8], c[9]);
            if (!capas.contains(chave)) orfaos.add(chave);
            itens.add(c[0] + "|" + c[1] + "|" + c[8] + "|" + c[13] + "|" + c[17]);
        }
        Assercoes.check(origem + ": todo item do SAFX08 tem capa no SAFX07",
                orfaos.isEmpty(), orfaos.isEmpty() ? "" : orfaos.size() + " órfão(s): " + orfaos.get(0));

        List<String> semItem = new ArrayList<>();
        for (String linha : Fixtures.linhas(caso, "SAFX49")) {
            String[] c = linha.split("\t", -1);
            // SAFX49: empresa(1) estab(2) num_nf(8) cod_produto(12) num_item(13)
            String chave = c[0] + "|" + c[1] + "|" + c[7] + "|" + c[11] + "|" + c[12];
            if (!itens.contains(chave)) semItem.add(chave);
        }
        Assercoes.check(origem + ": todo registro do SAFX49 aponta para um item do SAFX08",
                semItem.isEmpty(), semItem.isEmpty() ? "" : semItem.size() + " sem item: " + semItem.get(0));
    }

    /** COD_EMPRESA, COD_ESTAB, MOVTO_E_S, NORM_DEV, COD_DOCTO, IND_FIS_JUR, COD_FIS_JUR, NUM, SERIE. */
    private static String chaveDocumento(String... partes) {
        return String.join("|", partes);
    }
}
