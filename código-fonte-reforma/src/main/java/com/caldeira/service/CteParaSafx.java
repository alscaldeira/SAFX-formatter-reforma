package com.caldeira.service;

import org.w3c.dom.Element;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.nio.file.Path;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;

/**
 * CT-e (modelo 57) e CT-e OS (67) -> SAFX07, SAFX08 e SAFX04.
 *
 * Porte de HomeController.LerCTes do sistema original. O CT-e não passa pelos
 * scripts SQL de NF-e: as regras abaixo são as do próprio LerCTes, com os campos
 * fixos que ele grava em comum com os scripts (ver FIXOS_*).
 */
final class CteParaSafx {

    static final class Resultado {
        final List<Map<String, String>> safx07 = new ArrayList<>();
        final List<Map<String, String>> safx08 = new ArrayList<>();
        final List<Map<String, String>> safx04 = new ArrayList<>();
    }

    private static final String SEVEN_ZEROS = "0000000";
    private static final String SEVENTEEN_ZEROS = "00000000000000000";

    // Campos sem origem no XML, com o mesmo valor dos scripts de NF-e (SAFX07.txt/SAFX08.txt).
    private static final Map<String, String> FIXOS_SAFX07 = mapa(
            "NORM_DEV", "1", "IDENT_FIS_JUR", "2", "COD_CLASS_DOC_FIS", "1",
            "SITUACAO", "N", "VLR_ALIQ_ICMS", "0", "VLR_ICMS", "0",
            "DIF_ALIQ_ICMS", "0", "VLR_IPI", "0", "VLR_SUBST_ICMS", "0",
            "IND_NF_ESPECIAL", "N", "IND_TRANSF_CRED", "0", "VLR_ICMS_NDESTAC", "0",
            "VLR_INSS_RETIDO", "0", "IND_FATURA", "1", "BASE_ICMS_ORIGDEST", "0",
            "VLR_ICMS_ORIGDEST", "0", "IND_COMPRA_VENDA", "CP", "COD_SITUACAO_A", "0",
            "IND_NAT_FRETE", "9");

    private static final Map<String, String> FIXOS_SAFX08 = mapa(
            "NORM_DEV", "1", "IND_FIS_JUR", "2", "IND_BEM_PATR", "N", "IND_PRODUTO", "4",
            "COD_PRODUTO", "FRETES", "COD_UND_PADRAO", "UN", "COD_MEDIDA", "UN",
            "QUANTIDADE", "00000000001000000", "COD_NBM", "00000000", "VLR_DESCONTO", "0",
            "COD_SITUACAO_A", "0", "COD_FEDERAL", "2", "IND_IPI_INCLUSO", "N",
            "PESO_LIQUIDO", "00000000001000", "VLR_FRETE", "0", "VLR_SEGURO", "00000000000000000",
            "VLR_OUTRAS", "0", "VLR_ALIQ_IPI", "0", "VLR_IPI", "0", "VLR_OUTROS1", "0",
            "ALIQ_ICMS_ORIGDEST", "0", "VLR_DESC_CONDIC", "0", "TRIB_ICMSS", "2",
            "BASE_REDU_ICMSS", "0", "VLR_CUSTO_UNIT", "0", "IND_NATUREZA_FRETE", "9");

    private static final Map<String, String> FIXOS_SAFX04 = mapa("IND_FIS_JUR", "2");

    private CteParaSafx() {
    }

    private static Map<String, String> mapa(String... pares) {
        Map<String, String> m = new LinkedHashMap<>();
        for (int i = 0; i < pares.length; i += 2) m.put(pares[i], pares[i + 1]);
        return m;
    }

    // ------------------------------------------------------------------- API

    static Resultado converter(List<Path> arquivos, boolean calcPisCofins, boolean calcIcms,
                               List<String> avisos) throws IOException {
        Resultado r = new Resultado();
        String aliqPis = formatar(new BigDecimal("16500.00"), SEVEN_ZEROS);
        String aliqCofins = formatar(new BigDecimal("76000.0"), SEVEN_ZEROS);

        for (Path arquivo : arquivos) {
            String nome = arquivo.getFileName().toString();
            Element raiz = Dom.ler(arquivo.toFile()).getDocumentElement();

            // A raiz varia: cteProc (CT-e autorizado), cteOSProc (CT-e OS) e, quando o ERP
            // remonta o arquivo, o próprio CTe/CTeOS sem protocolo.
            Element infCte = primeiro(Dom.no(raiz, "CTe", "infCte"), Dom.no(raiz, "CTeOS", "infCte"),
                    Dom.no(raiz, "infCte"));
            if (infCte == null) infCte = Dom.buscar(raiz, "infCte");
            if (infCte == null) {
                avisos.add(nome + ": nao foi possivel localizar o grupo infCte; documento ignorado.");
                continue;
            }

            Element ide = Dom.no(infCte, "ide");
            Element emit = Dom.no(infCte, "emit");
            Element enderEmit = Dom.no(emit, "enderEmit");
            Element vPrest = Dom.no(infCte, "vPrest");
            Element infProt = primeiro(Dom.no(raiz, "protCTe", "infProt"), Dom.buscar(raiz, "infProt"));

            // Grupos sem os quais não dá para montar nenhuma das três saídas
            String faltando = null;
            if (ide == null) faltando = "ide";
            else if (emit == null) faltando = "emit";
            else if (enderEmit == null) faltando = "emit/enderEmit";
            else if (vPrest == null) faltando = "vPrest";
            else if (infProt == null) faltando = "protCTe/infProt";
            if (faltando != null) {
                avisos.add(nome + ": grupo obrigatorio <" + faltando + "> ausente; documento ignorado.");
                continue;
            }

            boolean proprio = EmpresasFixas.MEU_CNPJ.equals(txt(emit, "CNPJ"));

            int cfop;
            try {
                cfop = Integer.parseInt(txt(ide, "CFOP"));
            } catch (NumberFormatException e) {
                avisos.add(nome + ": CFOP ausente ou invalido em <ide>; documento ignorado.");
                continue;
            }
            // CT-e de terceiro inverte o CFOP: 1xxx-3xxx (entrada) +4000, 5xxx-7xxx (saída) -4000
            if (!proprio) {
                if (cfop >= 1000 && cfop < 4000) cfop += 4000;
                else if (cfop >= 5000 && cfop < 8000) cfop -= 4000;
            }

            // ICMS: o grupo depende do CST. Ler sempre ICMS00 quebraria em qualquer CT-e
            // que não fosse tributado integralmente — ICMS45 (isento) é comum em frete.
            Element icmsGrupo = Dom.primeiroFilhoElemento(Dom.no(infCte, "imp", "ICMS"));
            if (icmsGrupo == null) {
                avisos.add(nome + ": grupo <imp/ICMS> sem detalhamento de CST; documento ignorado.");
                continue;
            }
            if (!"ICMS00".equals(Dom.nomeLocal(icmsGrupo))) {
                avisos.add(nome + ": ICMS informado como <" + Dom.nomeLocal(icmsGrupo) + ">, e nao ICMS00. "
                        + "Base, aliquota e valor saem do que o grupo traz — confira o lancamento.");
            }

            String vTPrest = txt(vPrest, "vTPrest");
            String vBC = txt(icmsGrupo, "vBC");
            String vICMS = txt(icmsGrupo, "vICMS");
            String cst = txt(icmsGrupo, "CST");
            String dhRecbto = txt(infProt, "dhRecbto");
            String modelo = txt(ide, "mod");
            String codDocto = "57".equals(modelo) ? "CT-E" : "CTE-OS";
            String cnpjEmit = txt(emit, "CNPJ");
            String cnpjDest = txt(Dom.no(infCte, "dest"), "CNPJ");

            // ------------------------------------------------- SAFX04
            Map<String, String> l04 = new LinkedHashMap<>();
            l04.put("DATA_X04", paraData(txt(ide, "dhEmi")));
            l04.put("COD_FIS_JUR", cnpjEmit);
            l04.put("RAZAO_SOCIAL", txt(emit, "xNome"));
            l04.put("CPF_CGC", cnpjEmit);
            l04.put("INSC_ESTADUAL", txt(emit, "IE"));
            l04.put("NOME_FANTASIA", txt(emit, "xFant"));
            l04.put("ENDERECO", txt(enderEmit, "xLgr"));
            l04.put("NUM_ENDERECO", txt(enderEmit, "nro"));
            l04.put("COMPL_ENDERECO", txt(enderEmit, "xCpl"));
            l04.put("BAIRRO", txt(enderEmit, "xBairro"));
            l04.put("CIDADE", txt(enderEmit, "xMun"));
            l04.put("UF", txt(enderEmit, "UF"));
            l04.put("CEP", txt(enderEmit, "CEP"));
            String fone = txt(enderEmit, "fone");
            l04.put("TELEFONE", fone.length() > 10 ? fone.substring(fone.length() - 10) : fone);
            String cPais = txt(enderEmit, "cPais");
            l04.put("COD_PAIS", (cPais.length() >= 3 ? cPais : "105").substring(0, 3));
            String cMun = txt(enderEmit, "cMun");
            l04.put("COD_MUNICIPIO", cMun.length() > 5 ? cMun.substring(cMun.length() - 5) : cMun);
            // o original decide pelo CNPJ do remetente (rem), não do emitente
            l04.put("IND_CONTEM_COD", txt(Dom.no(infCte, "rem"), "CNPJ").isEmpty() ? "4" : "1");
            l04.putAll(FIXOS_SAFX04);
            r.safx04.add(l04);

            // ------------------------------------------------- SAFX07
            Map<String, String> l07 = new LinkedHashMap<>(FIXOS_SAFX07);
            l07.put("COD_EMPRESA", EmpresasFixas.codEmpresa(cnpjDest));
            l07.put("COD_ESTAB", EmpresasFixas.codEstab(cnpjDest));
            l07.put("COD_FIS_JUR", cnpjEmit);
            // Entrada ('1') para CT-e de terceiro; CT-e próprio é saída ('9')
            l07.put("MOVTO_E_S", proprio ? "9" : "1");
            l07.put("COD_MODELO_COTEPE", modelo);
            l07.put("COD_MODELO", modelo);
            l07.put("COD_DOCTO", codDocto);
            l07.put("NUM_DOCFIS", pad(txt(ide, "nCT"), 12));
            l07.put("SERIE_DOCFIS", txt(ide, "serie"));
            l07.put("DATA_EMISSAO", paraData(txt(ide, "dhEmi")));
            l07.put("DAT_FATO_GERADOR", paraData(txt(ide, "dhEmi")));
            l07.put("UF_ORIG_DEST", txt(ide, "UFEnv"));
            l07.put("UF_DESTINO", txt(ide, "UFFim"));
            l07.put("NUM_DV_NFE", txt(ide, "cDV"));
            l07.put("COD_CFO", String.valueOf(cfop));
            l07.put("COD_MUNICIPIO_ORIG", sub2(txt(Dom.no(infCte, "exped", "enderExped"), "cMun")));
            l07.put("COD_MUNICIPIO_DEST", sub2(txt(Dom.no(infCte, "dest", "enderDest"), "cMun")));
            l07.put("VLR_ALIQ_PIS", calcPisCofins ? aliqPis : SEVEN_ZEROS);
            l07.put("VLR_ALIQ_COFINS", calcPisCofins ? aliqCofins : SEVEN_ZEROS);
            l07.put("COD_SIT_PIS", calcPisCofins ? "56" : "70");
            l07.put("COD_SIT_COFINS", calcPisCofins ? "56" : "70");

            // "FRETE VALOR" fatura o componente (vComp), não o total da prestação
            String valorProduto = txt(emit, "xNome").toUpperCase(Locale.ROOT).equals("FRETE VALOR")
                    ? txt(Dom.no(vPrest, "Comp"), "vComp") : vTPrest;
            l07.put("VLR_PRODUTO", to17z(valorProduto));
            l07.put("NUM_AUTENTIC_NFE", txt(infProt, "chCTe"));
            l07.put("DATA_SAIDA_REC", paraData(dhRecbto));
            l07.put("DAT_LANC_PIS_COFINS", paraData(dhRecbto));
            l07.put("VLR_TOT_NOTA", to17z(vTPrest));

            BigDecimal baseCalculo = proprio ? dec(vTPrest) : dec(vTPrest).subtract(dec(vICMS));
            if (calcPisCofins) {
                // Base do PIS/COFINS: o total da prestação no CT-e próprio; o total menos o ICMS no de terceiro
                l07.put("VLR_BASE_PIS", z17(baseCalculo));
                l07.put("VLR_BASE_COFINS", z17(baseCalculo));
                l07.put("VLR_PIS", z17(baseCalculo.multiply(new BigDecimal("1.65"))
                        .divide(new BigDecimal("100"), 2, RoundingMode.HALF_EVEN)));
                l07.put("VLR_COFINS", z17(baseCalculo.multiply(new BigDecimal("7.6"))
                        .divide(new BigDecimal("100"), 2, RoundingMode.HALF_EVEN)));
            } else {
                l07.put("VLR_PIS", SEVENTEEN_ZEROS);
                l07.put("VLR_BASE_PIS", SEVENTEEN_ZEROS);
                l07.put("VLR_BASE_COFINS", SEVENTEEN_ZEROS);
                l07.put("VLR_COFINS", SEVENTEEN_ZEROS);
            }

            // Tomador: toma3 quando é um dos participantes do próprio CT-e, toma4 quando é um terceiro
            String toma = txt(Dom.no(ide, "toma3"), "toma");
            if (toma.isEmpty()) toma = txt(Dom.no(ide, "toma4"), "toma");
            if (toma.equals("0")) l07.put("IND_TP_FRETE", "1");
            else if (toma.equals("3")) l07.put("IND_TP_FRETE", "2");
            else if (!toma.isEmpty()) l07.put("IND_TP_FRETE", toma);

            l07.put("COD_SITUACAO_B", cst);
            if (!calcIcms) l07.put("BASE_OUTR_ICMS", to17z(vBC));
            r.safx07.add(l07);

            // ------------------------------------------------- SAFX08
            Map<String, String> l08 = new LinkedHashMap<>(FIXOS_SAFX08);
            l08.put("COD_EMPRESA", EmpresasFixas.codEmpresa(cnpjDest));
            l08.put("COD_ESTAB", EmpresasFixas.codEstab(cnpjDest));
            // CT-e de terceiro: entrada e CST de IPI '03'. CT-e próprio segue a NF-e: saída e '53'.
            l08.put("MOVTO_E_S", proprio ? "9" : "1");
            l08.put("COD_TRIB_IPI", proprio ? "53" : "03");
            l08.put("DATA_FISCAL", paraData(dhRecbto));
            l08.put("DAT_LANC_PIS_COFINS", paraData(dhRecbto));
            l08.put("COD_DOCTO", codDocto);
            l08.put("COD_FIS_JUR", cnpjEmit);
            l08.put("NUM_DOCFIS", pad(txt(ide, "nCT"), 12));
            l08.put("SERIE_DOCFIS", txt(ide, "serie"));
            l08.put("DESCRICAO_COMPL", txt(Dom.no(infCte, "compl"), "xObs"));
            l08.put("COD_CFO", String.valueOf(cfop));
            l08.put("VLR_UNIT", formatar(dec(vTPrest).multiply(new BigDecimal("10000")), "0000000000000000000"));
            l08.put("VLR_ITEM", to17z(vTPrest));
            l08.put("COD_SITUACAO_B", calcIcms ? cst : "90");
            l08.put("BASE_ICMS", to17z(vBC));
            if (calcIcms) {
                l08.put("VLR_ALIQ_ICMS", formatar(dec(txt(icmsGrupo, "pICMS")).multiply(new BigDecimal("10000")),
                        SEVEN_ZEROS));
                l08.put("VLR_ICMS", to17z(vICMS));
                l08.put("TRIB_ICMS", "1");
            } else {
                l08.put("VLR_ALIQ_ICMS", SEVEN_ZEROS);
                l08.put("VLR_ICMS", SEVENTEEN_ZEROS);
                l08.put("TRIB_ICMS", "3");
            }
            l08.put("VLR_CONTAB_ITEM", to17z(vTPrest));
            // O CT-e tem um único item (a prestação). Item 0 não existe no SAFX.
            l08.put("NUM_ITEM", "1");

            if (calcPisCofins) {
                l08.put("VLR_ALIQ_PIS", aliqPis);
                l08.put("VLR_ALIQ_COFINS", aliqCofins);
                l08.put("COD_SITUACAO_PIS", "56");
                l08.put("COD_SITUACAO_COFINS", "56");
                l08.put("VLR_BASE_PIS", z17(baseCalculo));
                l08.put("VLR_BASE_COFINS", z17(baseCalculo));
                l08.put("VLR_PIS", l07.get("VLR_PIS"));
                l08.put("VLR_COFINS", l07.get("VLR_COFINS"));
            } else {
                l08.put("VLR_BASE_PIS", SEVENTEEN_ZEROS);
                l08.put("VLR_PIS", SEVENTEEN_ZEROS);
                l08.put("VLR_BASE_COFINS", SEVENTEEN_ZEROS);
                l08.put("VLR_COFINS", SEVENTEEN_ZEROS);
                l08.put("VLR_ALIQ_PIS", SEVEN_ZEROS);
                l08.put("VLR_ALIQ_COFINS", SEVEN_ZEROS);
                l08.put("COD_SITUACAO_PIS", "70");
                l08.put("COD_SITUACAO_COFINS", "70");
            }
            r.safx08.add(l08);
        }

        removerParticipantesRepetidos(r.safx04);
        return r;
    }

    /** Um participante por CNPJ, como o 'distinct' do script de NF-e. */
    static void removerParticipantesRepetidos(List<Map<String, String>> linhas) {
        Set<String> vistos = new HashSet<>();
        linhas.removeIf(l -> !vistos.add(l.get("COD_FIS_JUR").trim()));
    }

    // ------------------------------------------------------------ utilitários

    private static Element primeiro(Element... candidatos) {
        for (Element e : candidatos) {
            if (e != null) return e;
        }
        return null;
    }

    /** Texto do nó com os acentos tirados (o original passa o XML inteiro pelo TiraAcento). */
    private static String txt(org.w3c.dom.Node pai, String... caminho) {
        return Valores.tiraAcento(Dom.txt(pai, caminho));
    }

    private static BigDecimal dec(String valor) {
        return Valores.decOuZero(valor);
    }

    /** Valor em centavos, 17 posições com zeros à esquerda. */
    private static String z17(BigDecimal valor) {
        return formatar(valor.multiply(new BigDecimal("100")), SEVENTEEN_ZEROS);
    }

    private static String to17z(String valor) {
        return z17(dec(valor));
    }

    /** ToString("000...") do .NET: arredonda para inteiro (meio para longe do zero) e completa com zeros. */
    private static String formatar(BigDecimal valor, String mascara) {
        BigDecimal inteiro = valor.setScale(0, RoundingMode.HALF_UP);
        String digitos = inteiro.abs().toPlainString();
        String preenchido = digitos.length() >= mascara.length() ? digitos
                : mascara.substring(0, mascara.length() - digitos.length()) + digitos;
        return (inteiro.signum() < 0 ? "-" : "") + preenchido;
    }

    private static String pad(String valor, int n) {
        String v = valor == null ? "" : valor;
        return v.length() >= n ? v : "0".repeat(n - v.length()) + v;
    }

    private static String sub2(String valor) {
        String texto = valor == null || valor.isEmpty() ? "   " : valor;
        return texto.length() > 2 ? texto.substring(2) : texto;
    }

    /**
     * dhEmi/dhRecbto chegam com offset (2026-09-01T23:30:00-03:00). A data é a da hora
     * de parede do próprio documento — converter para o fuso do servidor faria um CT-e
     * emitido depois das 21h sair com a data do dia seguinte num servidor em UTC.
     */
    static String paraData(String valor) {
        if (valor == null || valor.isEmpty()) return "";
        DateTimeFormatter saida = DateTimeFormatter.ofPattern("yyyyMMdd", Locale.ROOT);
        try {
            return OffsetDateTime.parse(valor).toLocalDate().format(saida);
        } catch (DateTimeParseException e) {
            // sem offset
        }
        try {
            return LocalDateTime.parse(valor).toLocalDate().format(saida);
        } catch (DateTimeParseException e) {
            // só a data
        }
        try {
            return LocalDate.parse(valor).format(saida);
        } catch (DateTimeParseException e) {
            return "";
        }
    }
}
