package com.caldeira.service;

import java.io.*;
import java.nio.charset.Charset;
import java.nio.file.*;
import java.nio.file.attribute.FileTime;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Properties;

/**
 * Carregamento dos arquivos de de-para usados pelos conversores
 * (estabelecimentos, participantes, unidades).
 *
 * Regra comum a todos: procura-se primeiro um arquivo ao lado do programa
 * (user.dir), o que permite cadastrar sem recompilar; se não existir, usa-se o
 * arquivo embutido em resources.
 *
 * O conteúdo fica em cache, mas o cache é invalidado quando o arquivo externo
 * muda (caminho, data de modificação ou tamanho). Sem isso, o usuário que
 * recebesse "estabelecimento não cadastrado", corrigisse o .properties e
 * convertesse de novo continuaria vendo o erro até fechar o programa — a tela
 * é um processo longo e a conversão roda várias vezes na mesma JVM.
 */
final class CadastroProperties {

    private CadastroProperties() {
    }

    /**
     * @param obrigatorio quando true, a ausência do arquivo embutido é erro;
     *                    quando false, devolve um cadastro vazio.
     */
    private static final class Cache {
        final Map<String, String> dados;
        final String origem;
        final long modificadoEm;
        final long tamanho;

        Cache(Map<String, String> dados, String origem, long modificadoEm, long tamanho) {
            this.dados = dados;
            this.origem = origem;
            this.modificadoEm = modificadoEm;
            this.tamanho = tamanho;
        }
    }

    private static final Map<String, Cache> caches = new HashMap<>();

    static synchronized Map<String, String> carregar(String nomeArquivo, boolean obrigatorio) throws IOException {
        Path externo = Paths.get(System.getProperty("user.dir"), nomeArquivo);
        boolean existe = Files.exists(externo);
        long modificadoEm = -1;
        long tamanho = -1;
        if (existe) {
            FileTime ft = Files.getLastModifiedTime(externo);
            modificadoEm = ft.toMillis();
            tamanho = Files.size(externo);
        }

        Cache cache = caches.get(nomeArquivo);
        if (cache != null && cache.origem.equals(externo.toString())
                && cache.modificadoEm == modificadoEm && cache.tamanho == tamanho) {
            return cache.dados;
        }

        Map<String, String> carregado = ler(nomeArquivo, obrigatorio, externo, existe);
        caches.put(nomeArquivo, new Cache(carregado, externo.toString(), modificadoEm, tamanho));
        return carregado;
    }

    private static Map<String, String> ler(String nomeArquivo, boolean obrigatorio,
                                           Path externo, boolean existe) throws IOException {
        Properties props = new Properties();

        if (existe) {
            try (Reader r = Files.newBufferedReader(externo, Charset.forName("UTF-8"))) {
                props.load(r);
            }
        } else {
            try (InputStream in = CadastroProperties.class.getResourceAsStream("/" + nomeArquivo)) {
                if (in == null) {
                    if (obrigatorio) {
                        throw new FileNotFoundException("Arquivo " + nomeArquivo + " não encontrado.");
                    }
                    return new LinkedHashMap<>();
                }
                try (Reader r = new InputStreamReader(in, Charset.forName("UTF-8"))) {
                    props.load(r);
                }
            }
        }

        Map<String, String> carregado = new LinkedHashMap<>();
        for (String nome : props.stringPropertyNames()) {
            String chave = nome.trim();
            String valor = props.getProperty(nome).trim();
            if (!chave.isEmpty() && !valor.isEmpty()) {
                carregado.put(chave, valor);
            }
        }
        return carregado;
    }

    /**
     * Chave de consulta de um CNPJ/CPF nos cadastros: só letras e dígitos, em
     * maiúsculas, para que "72.977.242/0001-40" no properties case com
     * "72977242000140" no XML.
     *
     * NÃO usar para gravar: o CNPJ vai para o arquivo SAFX exatamente como veio
     * do XML. Desde 2026 o CNPJ é alfanumérico (as 8 primeiras posições podem
     * ser letras), então as letras são preservadas aqui — só a pontuação sai.
     */
    static String chaveDocumento(String s) {
        if (s == null) return "";
        StringBuilder sb = new StringBuilder();
        for (char c : s.toUpperCase().toCharArray()) {
            if (Character.isLetterOrDigit(c)) sb.append(c);
        }
        return sb.toString();
    }
}
