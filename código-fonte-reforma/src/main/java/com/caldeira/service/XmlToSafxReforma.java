package com.caldeira.service;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Conversão XML -> SAFX da Reforma Tributária (SAFX3007 capa, SAFX3008 item de
 * mercadoria, SAFX3009 item de serviço).
 *
 * O mapeamento é por NOME de campo: cada registro é montado como
 * nome -> valor e só depois serializado na ordem declarada em
 * {@link LayoutReforma}. Consequência prática: a ordem oficial das planilhas do
 * cliente (130/88/64 campos) pode ser preenchida no JSON sem tocar em Java.
 *
 * A ordem e os nomes vêm do Manual_Layout_TAXONE_Reforma_Tributaria.xlsx
 * (130, 88 e 64 campos). Campo declarado no leiaute mas sem origem no XML sai
 * nulo — não se inventa valor. Enquanto um layout estiver com
 * "ordemConfirmada": false, gera-se apenas o CSV nomeado.
 */
final class XmlToSafxReforma {

    private XmlToSafxReforma() {
    }

    static void gerar(List<XmlNota> notas, List<String> avisos) throws IOException {
        List<Map<String, Object>> capas = new ArrayList<>();
        List<Map<String, Object>> itensMercadoria = new ArrayList<>();
        List<Map<String, Object>> itensServico = new ArrayList<>();
        int semReforma = 0;

        for (XmlNota nota : notas) {
            boolean temCapa = nota.ibsCbs != null && nota.ibsCbs.temDados();
            if (temCapa) {
                capas.add(capa(nota));
            } else {
                semReforma++;
            }

            if (nota.origem == XmlNota.Origem.NFE) {
                for (XmlItem item : nota.itens) {
                    if (item.ibsCbs != null && item.ibsCbs.temDados()) {
                        itensMercadoria.add(itemMercadoria(nota, item));
                    }
                }
            } else if (temCapa) {
                itensServico.add(itemServico(nota));
            }
        }

        escrever("SAFX3007", capas, avisos);
        escrever("SAFX3008", itensMercadoria, avisos);
        escrever("SAFX3009", itensServico, avisos);

        if (semReforma > 0) {
            avisos.add(semReforma + " documento(s) sem grupo IBS/CBS no XML não geraram registro de "
                    + "Reforma Tributária — nota anterior ao novo regime não tem esses valores.");
        }
    }

    // ------------------------------------------------------------- registros

    private static Map<String, Object> capa(XmlNota nota) throws IOException {
        XmlIbsCbs ibs = nota.ibsCbs;
        Map<String, Object> r = new LinkedHashMap<>();
        chave(r, nota);
        // Lista do manual: 1 = NFS-e, 2 = NF-e (não é a ordem que a intuição sugere)
        r.put("TIPO_CHAVE_DFE", nota.origem == XmlNota.Origem.NFE ? "2" : "1");
        r.put("CHAVE_DFE_REF", nota.chaveReferenciada);

        if (nota.origem == XmlNota.Origem.NFE) {
            r.put("FINALIDADE_EMISSAO_NFE", nota.finalidade);
            r.put("IND_OPER_FINAL", nota.indicadorConsumidorFinal);
            r.put("IND_COMPRA_MOMENTO_OPER", nota.indicadorPresenca);
            r.put("IND_INTERM", nota.indicadorIntermediador);
            // A NF-e traz CST, classificação e alíquotas por item, não na capa.
            // Quando todos os itens compartilham o mesmo valor ele vale para o
            // documento e a capa é preenchida; com itens divergentes o campo
            // fica nulo, porque não existe valor único que represente a nota.
            r.put("CST_IBS_CBS", comum(nota, i -> i.cst));
            r.put("CCLASS_IBS_CBS", comum(nota, i -> i.classificacaoTributaria));
            r.put("BC_IBS_CBS", valorNumerico(ibs.baseCalculo));
            r.put("ALIQ_IBS_UF", comum(nota, i -> i.aliqIbsUf));
            r.put("ALIQ_IBS_MUN", comum(nota, i -> i.aliqIbsMun));
            r.put("ALIQ_CBS", comum(nota, i -> i.aliqCbs));
            // Valores do documento (campos 42 e 57): o XML só traz vIBSUF/vCBS
            // por item e no total; na capa vale o total, que é a soma dos itens.
            r.put("VLR_IBS_UF", valorNumerico(ibs.vlIbsUf));
            r.put("VLR_CBS", valorNumerico(ibs.vlCbs));
        } else {
            r.put("FINALIDADE_EMISSAO_NFSE", ibs.finalidadeNfse);
            r.put("IND_DESTINATARIO_SERVICO", ibs.indicadorDestinatario);
            r.put("IND_OPER_FORNECIMENTO", ibs.codIndicadorOperacao);
            // a NFS-e traz CST, classificação e alíquotas já na capa
            r.put("CST_IBS_CBS", ibs.cst);
            r.put("CCLASS_IBS_CBS", ibs.classificacaoTributaria);
            r.put("BC_IBS_CBS", valorNumerico(ibs.baseCalculo));
            r.put("ALIQ_IBS_UF", valorNumerico(ibs.aliqIbsUf));
            r.put("PERC_RED_ALIQ_IBS_UF", valorNumerico(ibs.aliqReducaoIbsUf));
            r.put("ALIQ_EFET_IBS_UF", valorNumerico(ibs.aliqEfetivaIbsUf));
            r.put("ALIQ_IBS_MUN", valorNumerico(ibs.aliqIbsMun));
            r.put("PERC_RED_ALIQ_IBS_MUN", valorNumerico(ibs.aliqReducaoIbsMun));
            r.put("ALIQ_EFET_IBS_MUN", valorNumerico(ibs.aliqEfetivaIbsMun));
            r.put("ALIQ_CBS", valorNumerico(ibs.aliqCbs));
            r.put("PERC_RED_ALIQ_CBS", valorNumerico(ibs.aliqReducaoCbs));
            r.put("ALIQ_EFET_CBS", valorNumerico(ibs.aliqEfetivaCbs));
            // campos 42 e 57: totCIBS/gIBS/gIBSUFTot/vIBSUF e totCIBS/gCBS/vCBS
            r.put("VLR_IBS_UF", valorNumerico(ibs.vlIbsUf));
            r.put("VLR_CBS", valorNumerico(ibs.vlCbs));
        }

        // Totais do documento (campos 109 a 130 do leiaute)
        r.put("VLR_TOT_BC_IBS_CBS", valorNumerico(ibs.baseCalculo));
        r.put("VLR_TOT_DIF_IBS_UF", valorNumerico(ibs.vlDiferimentoIbsUf));
        r.put("VLR_TOT_DEV_TRIB_IBS_UF", valorNumerico(ibs.vlDevolucaoTributoIbsUf));
        r.put("VLR_TOT_IBS_UF", valorNumerico(ibs.vlIbsUf));
        r.put("VLR_TOT_DIF_IBS_MUN", valorNumerico(ibs.vlDiferimentoIbsMun));
        r.put("VLR_TOT_DEV_TRIB_IBS_MUN", valorNumerico(ibs.vlDevolucaoTributoIbsMun));
        r.put("VLR_TOT_IBS_MUN", valorNumerico(ibs.vlIbsMun));
        r.put("VLR_TOT_IBS", valorNumerico(ibs.vlIbs));
        r.put("VLR_TOT_CRED_PRES_IBS", valorNumerico(ibs.vlCreditoPresumidoIbs));
        r.put("VLR_TOT_CRED_PRES_C_SUS_IBS", valorNumerico(ibs.vlCreditoPresumidoCondSuspensaoIbs));
        r.put("VLR_TOT_CRED_PRES_CBS", valorNumerico(ibs.vlCreditoPresumidoCbs));
        r.put("VLR_TOT_CRED_PRES_C_SUS_CBS", valorNumerico(ibs.vlCreditoPresumidoCondSuspensaoCbs));
        r.put("VLR_TOT_DIF_CBS", valorNumerico(ibs.vlDiferimentoCbs));
        r.put("VLR_TOT_DEV_TRIB_CBS", valorNumerico(ibs.vlDevolucaoTributoCbs));
        r.put("VLR_TOT_CBS", valorNumerico(ibs.vlCbs));
        r.put("VLR_TOT_NF_IBS_CBS_IS", valorNumerico(ibs.vlTotalDocumento));
        return r;
    }

    private static Map<String, Object> itemMercadoria(XmlNota nota, XmlItem item) throws IOException {
        XmlIbsCbs ibs = item.ibsCbs;
        Map<String, Object> r = new LinkedHashMap<>();
        chave(r, nota);
        r.put("IND_BEM_PATR", "N");
        r.put("IND_PRODUTO", "5");
        r.put("COD_PRODUTO", item.codProduto);
        r.put("COD_UND_PADRAO", Unidades.normalizar(item.unidadeComercial));
        r.put("NUM_ITEM", String.format("%05d", item.numItem));
        r.put("CST_IBS_CBS", ibs.cst);
        r.put("CCLASS_IBS_CBS", ibs.classificacaoTributaria);
        r.put("BC_IBS_CBS", valorNumerico(ibs.baseCalculo));
        r.put("ALIQ_IBS_UF", valorNumerico(ibs.aliqIbsUf));
        r.put("VLR_IBS_UF", valorNumerico(ibs.vlIbsUf));
        r.put("ALIQ_CBS", valorNumerico(ibs.aliqCbs));
        r.put("VLR_CBS", valorNumerico(ibs.vlCbs));
        r.put("VLR_TOT_ITEM", valorNumerico(item.vlContabil));
        return r;
    }

    private static Map<String, Object> itemServico(XmlNota nota) throws IOException {
        XmlIbsCbs ibs = nota.ibsCbs;
        Map<String, Object> r = new LinkedHashMap<>();
        chave(r, nota);
        // a NFS-e nacional descreve um serviço por documento
        r.put("NUM_ITEM", "00001");
        r.put("COD_SERVICO", codServico(nota.codTributacaoNacional));
        r.put("COD_MUN_FT_GER_IBS_CBS", ibs.codLocalidadeIncidencia);
        r.put("CST_IBS_CBS", ibs.cst);
        r.put("CCLASS_IBS_CBS", ibs.classificacaoTributaria);
        r.put("BC_IBS_CBS", valorNumerico(ibs.baseCalculo));
        // o leiaute do item de serviço só tem IBS municipal — o IBS estadual da
        // NFS-e vai na capa (SAFX3007, campos 41/46/47 e 112)
        r.put("ALIQ_IBS_MUN", valorNumerico(ibs.aliqIbsMun));
        r.put("PERC_RED_ALIQ_IBS_MUN", valorNumerico(ibs.aliqReducaoIbsMun));
        r.put("ALIQ_EFET_IBS_MUN", valorNumerico(ibs.aliqEfetivaIbsMun));
        r.put("VLR_IBS_MUN", valorNumerico(ibs.vlIbsMun));
        r.put("ALIQ_CBS", valorNumerico(ibs.aliqCbs));
        r.put("PERC_RED_ALIQ_CBS", valorNumerico(ibs.aliqReducaoCbs));
        r.put("ALIQ_EFET_CBS", valorNumerico(ibs.aliqEfetivaCbs));
        r.put("VLR_CBS", valorNumerico(ibs.vlCbs));
        r.put("VLR_TOT_ITEM", valorNumerico(nota.vlServico));
        return r;
    }

    /**
     * Valor do grupo IBS/CBS repetido em todos os itens da nota, ou null quando
     * os itens divergem — aí não há valor único que represente o documento e o
     * campo da capa fica nulo, em vez de eleger o de um item qualquer.
     */
    private static <T> T comum(XmlNota nota, java.util.function.Function<XmlIbsCbs, T> campo) {
        T comum = null;
        for (XmlItem item : nota.itens) {
            if (item.ibsCbs == null) continue;
            T v = campo.apply(item.ibsCbs);
            if (comum == null) comum = v;
            else if (!comum.equals(v)) return null;
        }
        return comum;
    }

    /** Chave compartilhada com a tabela-base (SAFX07/08/09), campo a campo. */
    private static void chave(Map<String, Object> r, XmlNota nota) throws IOException {
        // sem fórmula de Excel aqui: o valor gravado é o do leiaute (o .txt
        // posicional usa o mesmo registro); a proteção contra o Excel comer o
        // zero à esquerda fica na gravação do CSV, em SafxCsvSupport.celulaCsv
        r.put("COD_EMPRESA", Estabelecimentos.empresa(nota.cnpjEstab));
        r.put("COD_ESTAB", Estabelecimentos.codigo(nota.cnpjEstab));
        // Leiaute campo 3: saída = data de emissão; entrada = data de saída/
        // recebimento (dhSaiEnt, com dhEmi como fallback na leitura do XML)
        r.put("DATA_FISCAL", "9".equals(nota.movtoES) ? nota.dtEmissao : nota.dtSaidaEnt);
        r.put("MOVTO_E_S", nota.movtoES);
        r.put("NORM_DEV", nota.normDev);
        r.put("COD_DOCTO", nota.codDocto);
        r.put("IND_FIS_JUR", nota.participante.papel);
        r.put("COD_FIS_JUR", Participantes.codFisJur(nota.participante.documento()));
        r.put("NUM_DOCFIS", numeroDocumento(nota.numDoc));
        r.put("SERIE_DOCFIS", nota.serie);
        r.put("SUB_SERIE_DOCFIS", "");
    }

    // ----------------------------------------------------------- gravação

    private static void escrever(String layout, List<Map<String, Object>> registros,
                                 List<String> avisos) throws IOException {
        LayoutReforma.Layout declarado = LayoutReforma.de(layout);
        String dir = System.getProperty("user.dir");

        // CSV nomeado: sempre gerado, não depende da ordem oficial
        String csv = dir + "/" + layout + ".csv";
        try (BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(
                new FileOutputStream(csv), StandardCharsets.UTF_8))) {
            writer.write('﻿');
            writer.write(linhaCsv(nomes(declarado)));
            writer.newLine();
            for (Map<String, Object> registro : registros) {
                writer.write(linhaCsv(valores(declarado, registro)));
                writer.newLine();
            }
        }

        if (declarado.ordemConfirmada) {
            String txt = dir + "/" + layout + ".txt";
            try (BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(
                    new FileOutputStream(txt), StandardCharsets.UTF_8))) {
                for (Map<String, Object> registro : registros) {
                    writer.write(String.join("\t", valores(declarado, registro)));
                    writer.newLine();
                }
            }
            System.out.println("Arquivo " + layout + " gerado: " + txt
                    + " (" + registros.size() + " registros)");
        } else {
            System.out.println("Arquivo " + layout + ".csv gerado: " + csv
                    + " (" + registros.size() + " registros; .txt não gerado)");
            if (!registros.isEmpty()) {
                avisos.add(layout + ": " + registros.size() + " registro(s) gerados apenas em CSV. "
                        + "O leiaute oficial tem " + declarado.fieldCountOficial + " campos e o projeto "
                        + "só tem " + declarado.campos.size() + " mapeados, sem a ordem confirmada. "
                        + "Preencha " + LayoutReforma.NOME_ARQUIVO + " com a planilha do cliente e marque "
                        + "\"ordemConfirmada\": true para o .txt posicional ser gerado.");
            }
        }
    }

    private static String[] nomes(LayoutReforma.Layout layout) {
        String[] nomes = new String[layout.campos.size()];
        for (int i = 0; i < nomes.length; i++) nomes[i] = layout.campos.get(i).nome;
        return nomes;
    }

    private static String[] valores(LayoutReforma.Layout layout, Map<String, Object> registro) {
        String[] valores = new String[layout.campos.size()];
        for (int i = 0; i < valores.length; i++) {
            LayoutReforma.Campo campo = layout.campos.get(i);
            Object valor = registro.get(campo.nome);
            if (valor == null) {
                valores[i] = "@";
            } else if (valor instanceof Double) {
                // largura e escala exatamente como o manual declara; se o
                // descritor não trouxer o tamanho, cai na convenção da família
                // SAFX (16 dígitos com 2 decimais)
                int digitos = campo.digitos > 0 ? campo.digitos : 16;
                int decimais = campo.decimal() ? campo.decimais : 2;
                valores[i] = SafxFormat.numero((Double) valor, decimais, digitos);
            } else {
                String texto = SafxLinha.limpar(String.valueOf(valor));
                // SUB_SERIE_DOCFIS não usa o sentinela "@": o layout exige
                // que o campo saia realmente vazio quando não há subsérie.
                boolean vazioIntencional = "SUB_SERIE_DOCFIS".equals(campo.nome);
                valores[i] = texto.isEmpty() && !vazioIntencional ? "@" : texto;
            }
        }
        return valores;
    }

    private static String linhaCsv(String[] campos) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < campos.length; i++) {
            if (i > 0) sb.append(';');
            sb.append(SafxCsvSupport.celulaCsv(campos[i]));
        }
        return sb.toString();
    }

    /**
     * COD_SERVICO (4 posições) a partir do código de tributação nacional da
     * NFS-e (cTribNac/xTribNac, 6 dígitos): valem os 4 últimos, que são o item
     * e o subitem da lista de serviços — "310103" -> "0103". Os 2 primeiros são
     * o grupo, que o campo do TAX ONE não carrega.
     */
    private static String codServico(String codTributacaoNacional) {
        String digitos = codTributacaoNacional == null ? ""
                : codTributacaoNacional.replaceAll("\\D", "");
        if (digitos.isEmpty()) return null;
        return digitos.length() > 4 ? digitos.substring(digitos.length() - 4) : digitos;
    }

    private static String numeroDocumento(String numero) {
        String digitos = numero == null ? "" : numero.replaceAll("\\D", "");
        if (digitos.isEmpty()) return "000000000000";
        if (digitos.length() > 12) digitos = digitos.substring(digitos.length() - 12);
        return String.format("%012d", Long.parseLong(digitos));
    }

    /**
     * O valor numérico entra cru no registro: a escala e a largura saem do
     * leiaute na hora de gravar, porque o manual declara tamanhos diferentes
     * (015V002 = 17 dígitos, 003V004 = 7, 011V004 = 15) e não um tamanho único.
     */
    private static Double valorNumerico(double v) {
        return v;
    }
}
