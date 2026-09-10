package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.util.*;

/**
 * De-para CNPJ -> COD_EMPRESA/COD_ESTAB usado por SAFX07/08/49.
 *
 * O código do estabelecimento é um cadastro do MasterSAF e NÃO é derivável do
 * CNPJ (o CNPJ 72977242000493 tem ordem 0004, mas o estabelecimento é 0005),
 * por isso a tabela é externa: procura-se primeiro um arquivo
 * "estabelecimentos.properties" ao lado do programa (user.dir), o que permite
 * cadastrar novos estabelecimentos sem recompilar; se não existir, usa-se o
 * arquivo embutido em resources.
 *
 * Como em {@link Participantes}, a ausência de cadastro não aborta a conversão:
 * cai-se para a ordem do estabelecimento no próprio CNPJ (posições 9 a 12) com
 * COD_EMPRESA "001", e o caso entra na lista de avisos — esse código é um chute
 * razoável, não o cadastro do MasterSAF, e precisa ser conferido.
 *
 * O valor aceita duas formas, para que arquivos já existentes continuem válidos:
 * <pre>
 *   72977242000140=0001        -> COD_EMPRESA "001" (padrão), COD_ESTAB "0001"
 *   42105890000901=002/0001    -> COD_EMPRESA "002",          COD_ESTAB "0001"
 * </pre>
 * Isso é necessário porque um mesmo arquivo pode conter estabelecimentos de
 * empresas diferentes — o COD_EMPRESA não é constante como era no fluxo SPED.
 */
final class Estabelecimentos {

    static final String NOME_ARQUIVO = "estabelecimentos.properties";

    private static Map<String, String> bruto;
    private static Map<String, String> mapa;
    private static final Set<String> semCadastro = new LinkedHashSet<>();

    private Estabelecimentos() {
    }

    /**
     * Descobre o CNPJ do estabelecimento a que um documento pertence.
     * Para nota de emissão própria (IND_EMIT = 0) o CNPJ está na própria chave
     * da NF-e (posições 7 a 20), que é a informação mais confiável por documento;
     * caso contrário vale o bloco C010 aberto e, por último, o CNPJ do registro 0000.
     */
    static String cnpjDoDocumento(String chaveNFe, String indEmit, String cnpjC010, String cnpjArquivo) {
        String chave = CadastroProperties.chaveDocumento(chaveNFe);
        if ("0".equals(indEmit) && chave.length() == 44) {
            return chave.substring(6, 20);
        }
        if (!CadastroProperties.chaveDocumento(cnpjC010).isEmpty()) {
            return cnpjC010.trim();
        }
        return cnpjArquivo == null ? "" : cnpjArquivo.trim();
    }

    /** COD_ESTAB (campo 2 do SAFX07/08/49). */
    static String codigo(String cnpj) throws IOException {
        return partes(cnpj)[1];
    }

    /** COD_EMPRESA (campo 1 do SAFX07/08/49); "001" quando o cadastro não informa. */
    static String empresa(String cnpj) throws IOException {
        return partes(cnpj)[0];
    }

    private static final String EMPRESA_PADRAO = "001";

    private static String[] partes(String cnpj) throws IOException {
        String chave = CadastroProperties.chaveDocumento(cnpj);
        String cod = extrairCodigoEstabelecimento(chave);
        if (cod == null) {
            semCadastro.add(chave);
            return new String[]{EMPRESA_PADRAO, ordemNoCnpj(chave)};
        }
        int barra = cod.indexOf('/');
        if (barra < 0) {
            return new String[]{EMPRESA_PADRAO, cod};
        }
        return new String[]{cod.substring(0, barra).trim(), cod.substring(barra + 1).trim()};
    }

    /**
     * Devolve o valor cadastrado para o CNPJ ("0001" ou "002/0001"), ou null
     * quando o CNPJ não está no cadastro. O código NÃO é derivado do CNPJ: a
     * ordem do estabelecimento no CNPJ e o código do MasterSAF são coisas
     * diferentes (72977242000493 tem ordem 0004 e estabelecimento 0005).
     */
    private static String extrairCodigoEstabelecimento(String cnpj) throws IOException {
        if (cnpj == null || cnpj.isEmpty()) {
            return null;
        }
        return carregar().get(cnpj);
    }

    /**
     * COD_ESTAB deduzido da ordem do estabelecimento no CNPJ (posições 9 a 12:
     * 42105890000901 -> "0009"). Usado só quando o CNPJ não está cadastrado —
     * a ordem do CNPJ e o código do MasterSAF coincidem com frequência, mas não
     * sempre, e é por isso que o caso vira aviso.
     */
    private static String ordemNoCnpj(String cnpj) {
        return cnpj != null && cnpj.length() == 14 ? cnpj.substring(8, 12) : "0001";
    }

    /** CNPJs de estabelecimento que caíram no fallback desde o início da execução. */
    static Set<String> semCadastro() {
        return semCadastro;
    }

    static void limparAvisos() {
        semCadastro.clear();
    }

    private static synchronized Map<String, String> carregar() throws IOException {
        // Comparação por identidade: CadastroProperties devolve a mesma instância
        // enquanto o arquivo não muda, e outra quando ele é editado.
        Map<String, String> atual = CadastroProperties.carregar(NOME_ARQUIVO, true);
        if (atual == bruto) return mapa;

        Map<String, String> carregado = new LinkedHashMap<>();
        for (Map.Entry<String, String> e : atual.entrySet()) {
            String cnpj = CadastroProperties.chaveDocumento(e.getKey());
            if (!cnpj.isEmpty()) {
                carregado.put(cnpj, e.getValue());
            }
        }
        bruto = atual;
        mapa = carregado;
        return mapa;
    }

}
