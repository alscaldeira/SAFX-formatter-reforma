package com.caldeira.service;

import java.io.IOException;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;

/**
 * De-para CNPJ/CPF -> COD_PART, usado para montar o COD_FIS_JUR dos arquivos
 * SAFX04/07/08/49 na conversão a partir de XML.
 *
 * Motivo: no fluxo SPED o COD_FIS_JUR sai de "M" + COD_PART do registro 0150,
 * um código interno do ERP que NÃO é derivável do CNPJ (ex.: 9003065, 8748811).
 * O XML fiscal não traz esse código, então sem um de-para os dois fluxos gerariam
 * chaves diferentes para o mesmo participante e o MasterSAF ficaria com cadastro
 * duplicado.
 *
 * O código sem de-para não leva o prefixo "M": com ele o CNPJ passaria de 14
 * posições, que é o tamanho do campo COD_FIS_JUR.
 *
 * Como em {@link Estabelecimentos}, a ausência de cadastro NÃO aborta a
 * conversão: cai-se para o próprio CNPJ como código e o caso entra na lista de
 * avisos. A chave continua íntegra dentro do lote porque o SAFX04 gerado na
 * mesma execução cria o cadastro com esse mesmo código.
 */
final class Participantes {

    static final String NOME_ARQUIVO = "participantes.properties";

    private static Map<String, String> bruto;
    private static Map<String, String> mapa;
    private static final Set<String> semCadastro = new LinkedHashSet<>();

    private Participantes() {
    }

    /** Tamanho do campo COD_FIS_JUR no leiaute (SAFX04 campo 2, SAFX07 campo 7). */
    private static final int TAMANHO_COD_FIS_JUR = 14;

    /**
     * COD_FIS_JUR pronto para gravação.
     *
     * Com de-para, vale a convenção do fluxo SPED: "M" + COD_PART (o código do
     * ERP é curto e cabe). Sem de-para, o código é o próprio CNPJ/CPF — com o
     * prefixo "M" ele teria 15 posições e estouraria o campo de 14, e um CNPJ
     * já é identificador suficiente. Nesse caso o IND_CONTEM_COD do SAFX04 sai
     * como "3" (o conteúdo do código é um CGC), que é exatamente a situação.
     *
     * O documento entra mascarado sempre que a máscara couber no campo: o CPF
     * (14 posições com máscara) sai como 000.000.000-00; o CNPJ (18) não cabe
     * nas 14 posições do COD_FIS_JUR e continua sem pontuação. Ver
     * {@link Documentos}.
     */
    static String codFisJur(String documento) throws IOException {
        String chave = CadastroProperties.chaveDocumento(documento);
        String bruto = documento == null ? "" : documento.trim();
        if (chave.isEmpty() && bruto.isEmpty()) return "";

        String cod = carregar().get(chave.isEmpty() ? bruto : chave);
        if (cod != null) {
            String comPrefixo = "M" + cod;
            if (comPrefixo.length() > TAMANHO_COD_FIS_JUR) {
                throw new IOException("COD_PART \"" + cod + "\" cadastrado em " + NOME_ARQUIVO
                        + " não cabe no COD_FIS_JUR (máximo " + (TAMANHO_COD_FIS_JUR - 1)
                        + " posições com o prefixo M).");
            }
            return comPrefixo;
        }

        semCadastro.add(chave.isEmpty() ? bruto : chave);
        // A máscara só entra quando cabe: o CPF mascarado tem 14 posições e
        // cabe, o CNPJ tem 18 e estouraria o campo — nesse caso vale o
        // documento sem pontuação, como antes.
        String semPrefixo = Documentos.mascararSeCouber(
                chave.isEmpty() ? bruto : chave, TAMANHO_COD_FIS_JUR);
        return semPrefixo.length() > TAMANHO_COD_FIS_JUR
                ? semPrefixo.substring(0, TAMANHO_COD_FIS_JUR)
                : semPrefixo;
    }

    /** COD_PART do de-para; na falta dele, o próprio documento. */
    static String codigo(String documento) throws IOException {
        String chave = CadastroProperties.chaveDocumento(documento);
        if (chave.isEmpty()) return "";
        String cod = carregar().get(chave);
        if (cod != null) return cod;
        semCadastro.add(chave);
        return chave;
    }

    /** CNPJs que caíram no fallback desde o início da execução. */
    static Set<String> semCadastro() {
        return semCadastro;
    }

    static void limparAvisos() {
        semCadastro.clear();
    }

    private static synchronized Map<String, String> carregar() throws IOException {
        Map<String, String> atual = CadastroProperties.carregar(NOME_ARQUIVO, false);
        if (atual == bruto) return mapa;

        java.util.Map<String, String> normalizado = new java.util.LinkedHashMap<>();
        for (Map.Entry<String, String> e : atual.entrySet()) {
            normalizado.put(CadastroProperties.chaveDocumento(e.getKey()), e.getValue());
        }
        bruto = atual;
        mapa = normalizado;
        return mapa;
    }
}
