package com.caldeira.service;

import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

import java.io.IOException;
import java.math.BigDecimal;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * NF-e -> SAFX07 (capa), SAFX08 (itens), SAFX04 (destinatário), SAFX2013
 * (produto) e SAFX2043 (NBM).
 *
 * Reproduz, direto do XML, o que o sistema original faz em duas etapas: a carga
 * do XML nas tabelas INT_NFE_* (Funcoes.GravarXml310 / NormalizarXMLNFe) e os
 * scripts ScriptSQL/SAFX07, SAFX08, SAFX04, SAFX2013 e SAFX2043. Cada regra
 * abaixo aponta o trecho do original de onde veio. Onde o original depende de
 * um acidente do banco (join cartesiano, coluna int que perde o zero à
 * esquerda), o comportamento seguido está registrado em
 * MAPEAMENTO-REGRAS-CSHARP-PARA-JAVA.md.
 */
final class NfeParaSafx {

    static final class Resultado {
        final List<Map<String, String>> safx07 = new ArrayList<>();
        final List<Map<String, String>> safx08 = new ArrayList<>();
        final List<Map<String, String>> safx04 = new ArrayList<>();
        final List<Map<String, String>> safx2013 = new ArrayList<>();
        final List<Map<String, String>> safx2043 = new ArrayList<>();
        int notas;
    }

    // ------------------------------------------------------------ modelo lido

    private static final class Icms {
        Integer orig;
        Integer cst;
        BigDecimal vbc;
        BigDecimal picms;
        BigDecimal vicms;
        BigDecimal picmsst;
        BigDecimal vicmsst;
    }

    private static final class Pis {
        Integer cstAliq;
        BigDecimal vbc;
        BigDecimal ppis;
        BigDecimal vpis;
        Integer cstNt;
        Integer cstOutr;
    }

    private static final class Item {
        int nItem;
        String cProd;
        String xProd;
        String ncm;
        int cfop;
        String uCom;
        String uTrib;
        BigDecimal qCom;
        BigDecimal vUnCom;
        BigDecimal vProd;
        BigDecimal vFrete;
        BigDecimal vOutro;
        Icms icms;
        BigDecimal pIpi;
        BigDecimal vIpi;
        Pis pis;
        Pis cofins;
    }

    private static final class Contraparte {
        String cnpj;
        String nome;
        long ie;
        String indIeDest = "";
        String xLgr;
        String nro;
        String xCpl;
        String xBairro;
        Integer cMun;
        String xMun;
        String uf;
        Integer cep;
        String cPais;
        int fone;
    }

    private static final Set<String> GRUPOS_CST = Set.of("ICMS00", "ICMS10", "ICMS20", "ICMS30", "ICMS40",
            "ICMS51", "ICMS60", "ICMS70", "ICMS90", "ICMSPart", "ICMSST");
    private static final Set<String> GRUPOS_SN = Set.of("ICMSSN101", "ICMSSN102", "ICMSSN201", "ICMSSN202",
            "ICMSSN500", "ICMSSN900");
    private static final Set<String> GRUPOS_BASE = Set.of("ICMS00", "ICMS10", "ICMS20", "ICMS51", "ICMS70",
            "ICMS90", "ICMSPart", "ICMSSN900");
    private static final Set<String> GRUPOS_ST = Set.of("ICMS10", "ICMS30", "ICMS70", "ICMS90", "ICMSPart",
            "ICMSSN201", "ICMSSN202", "ICMSSN900");

    private NfeParaSafx() {
    }

    // ------------------------------------------------------------------- API

    static Resultado converter(List<Path> arquivos, List<String> avisos) throws IOException {
        Resultado r = new Resultado();
        Set<String> chaves = new HashSet<>();

        for (Path arquivo : arquivos) {
            String nome = arquivo.getFileName().toString();
            Element raiz = Dom.ler(arquivo.toFile()).getDocumentElement();
            Element inf = Dom.buscar(raiz, "infNFe");
            if (inf == null) {
                avisos.add(nome + ": grupo infNFe não encontrado; documento ignorado.");
                continue;
            }
            String id = inf.getAttribute("Id");
            String chave = id.length() >= 47 ? id.substring(3, 47) : id;
            if (!chaves.add(chave)) {
                avisos.add(nome + ": NF-e em duplicidade no lote (chave " + chave + "); mantida a primeira.");
                continue;
            }
            try {
                nota(r, inf, chave, nome, avisos);
            } catch (RuntimeException e) {
                avisos.add(nome + ": NF-e ignorada (" + e.getMessage() + ").");
            }
        }

        // ORDER BY NUM_DOCFIS, NUM_ITEM do script SAFX08
        r.safx08.sort((a, b) -> {
            int c = a.get("NUM_DOCFIS").compareTo(b.get("NUM_DOCFIS"));
            return c != 0 ? c : Integer.compare(Integer.parseInt(a.get("NUM_ITEM")),
                    Integer.parseInt(b.get("NUM_ITEM")));
        });
        return r;
    }

    // ------------------------------------------------------------------ nota

    private static void nota(Resultado r, Element inf, String chave, String arquivo, List<String> avisos) {
        Element ide = Dom.no(inf, "ide");
        Element emit = Dom.no(inf, "emit");
        Element dest = Dom.no(inf, "dest");
        Element tot = Dom.no(inf, "total", "ICMSTot");
        if (ide == null) throw new IllegalStateException("grupo ide ausente");
        if (tot == null) throw new IllegalStateException("grupo total/ICMSTot ausente");

        // Funcoes.GravarXml310: Convert.ToInt32(versao) — 2.00 -> 2, 3.10 -> 3, 4.00 -> 4
        int versao = Valores.inteiro(inf.getAttribute("versao"));
        int tpNf = Valores.inteiro(Dom.txt(ide, "tpNF"));
        int finNfe = Valores.inteiro(Dom.txt(ide, "finNFe"));
        // indPag saiu da NF-e 4.00 (virou pag/detPag); ausente vale 0 como no original
        int indPag = Valores.inteiro(Dom.txt(ide, "indPag"));
        int nNf = Valores.inteiro(Dom.txt(ide, "nNF"));

        // NF-e 2.00 traz dEmi + hSaiEnt; as demais, dhEmi/dhSaiEnt. A data é a do
        // próprio documento (hora de parede), não a convertida para o fuso do servidor.
        String dtEmissao = versao == 2 ? Valores.data(Dom.txt(ide, "dEmi")) : Valores.data(Dom.txt(ide, "dhEmi"));
        String dtSaidaEnt = versao == 2 ? "" : Valores.data(Dom.txt(ide, "dhSaiEnt"));

        // tpNF=1 (saída): contraparte = destinatário. Demais (entrada): o emitente
        // vira a "contraparte" e o destinatário ocupa o lugar do emitente.
        boolean saida = tpNf == 1;
        Contraparte cp = contraparte(saida ? dest : emit, saida ? Dom.no(dest, "enderDest")
                : Dom.no(emit, "enderEmit"), saida);
        Element outro = saida ? emit : dest;
        String ufOutro = Dom.txtOuNull(outro, saida ? "enderEmit" : "enderDest", "UF");

        List<Item> itens = itens(inf, avisos, arquivo);
        List<Item> comIcms = new ArrayList<>();
        for (Item i : itens) {
            if (i.icms != null) comIcms.add(i);
        }
        if (comIcms.isEmpty()) throw new IllegalStateException("nenhum item com grupo ICMS");

        BigDecimal vProdTot = Valores.decOuZero(Dom.txt(tot, "vProd"));
        BigDecimal vNf = Valores.decOuZero(Dom.txt(tot, "vNF"));
        BigDecimal vBcTot = Valores.decOuZero(Dom.txt(tot, "vBC"));
        BigDecimal vIcmsTot = Valores.decOuZero(Dom.txt(tot, "vICMS"));
        BigDecimal vFreteTot = Valores.decOuZero(Dom.txt(tot, "vFrete"));
        BigDecimal vSegTot = Valores.decOuZero(Dom.txt(tot, "vSeg"));
        BigDecimal vOutroTot = Valores.decOuZero(Dom.txt(tot, "vOutro"));
        BigDecimal vIpiTot = Valores.decOuZero(Dom.txt(tot, "vIPI"));
        BigDecimal vStTot = Valores.decOuZero(Dom.txt(tot, "vST"));
        BigDecimal vDescTot = Valores.decOuZero(Dom.txt(tot, "vDesc"));

        String codEmpresa = EmpresasFixas.codEmpresa(cp.cnpj);
        String codEstab = EmpresasFixas.codEstab(cp.cnpj);
        String movto = tpNf == 0 ? "4" : "9";
        String normDev = finNfe == 4 ? "2" : "1";
        String numDocfis = Valores.direita(String.valueOf(nNf), 12);
        boolean ex = "EX".equals(cp.uf);

        Element transp = Dom.no(inf, "transp");
        Integer modFrete = transp == null ? null : Valores.inteiro(Dom.txt(transp, "modFrete"));
        Element vol = Dom.no(transp, "vol");
        BigDecimal pesoL = vol == null ? null : Valores.decOuZero(Dom.txt(vol, "pesoL"));

        // ---------------------------------------------------------- SAFX07
        Item primeiro = comIcms.get(0);
        Map<String, String> c = new LinkedHashMap<>();
        c.put("COD_EMPRESA", codEmpresa);
        c.put("COD_ESTAB", codEstab);
        c.put("MOVTO_E_S", movto);
        c.put("NORM_DEV", normDev);
        c.put("COD_DOCTO", "NFE");
        c.put("IDENT_FIS_JUR", "2");
        c.put("COD_FIS_JUR", cp.cnpj);
        c.put("NUM_DOCFIS", numDocfis);
        c.put("SERIE_DOCFIS", "1");
        c.put("DATA_EMISSAO", dtEmissao);
        c.put("COD_CLASS_DOC_FIS", "1");
        c.put("COD_MODELO", "55");
        c.put("COD_CFO", cfop(tpNf, primeiro.cfop));
        c.put("DATA_SAIDA_REC", dtEmissao);
        c.put("VLR_PRODUTO", Valores.z17(vProdTot));
        c.put("VLR_TOT_NOTA", Valores.z17(vNf));
        c.put("VLR_FRETE", Valores.bruto(vFreteTot));
        c.put("VLR_SEGURO", Valores.bruto(vSegTot));
        c.put("VLR_OUTRAS", Valores.bruto(vOutroTot));
        c.put("CONTRIB_FINAL", "1".equals(cp.indIeDest) ? "S" : "N");
        c.put("SITUACAO", "N");
        c.put("VLR_ALIQ_ICMS", "0");
        c.put("VLR_ICMS", "0");
        c.put("DIF_ALIQ_ICMS", "0");
        c.put("VLR_ALIQ_IPI", Valores.bruto(vIpiTot));
        c.put("VLR_IPI", "0");
        c.put("VLR_SUBST_ICMS", Valores.bruto(vStTot));
        c.put("BASE_TRIB_ICMS", Valores.z17(vBcTot));
        c.put("BASE_OUTR_ICMS", Valores.z17(vProdTot));
        c.put("BASE_OUTR_IPI", Valores.z17(vProdTot));
        c.put("NUM_CONTROLE_DOCTO", "R" + nNf);
        c.put("IND_TP_FRETE", indTpFrete(cp.cnpj, versao, modFrete));
        c.put("IND_FATURA", indPag == 0 ? "1" : indPag == 1 ? "2" : null);
        // Base e valor do PIS/COFINS da capa: soma dos itens (o original cruza a capa com
        // todos os itens por id_controle e deixa o 'distinct' decidir; ver o mapeamento)
        c.put("VLR_BASE_PIS", somaZ17(itens, i -> i.pis == null ? null : i.pis.vbc));
        c.put("VLR_PIS", somaZ17(itens, i -> i.pis == null ? null : i.pis.vpis));
        c.put("VLR_BASE_COFINS", somaZ17(itens, i -> i.cofins == null ? null : i.cofins.vbc));
        c.put("VLR_COFINS", somaZ17(itens, i -> i.cofins == null ? null : i.cofins.vpis));
        c.put("BASE_ICMS_ORIGDEST", Valores.z17(vBcTot));
        c.put("VLR_ICMS_ORIGDEST", Valores.z17(vIcmsTot));
        c.put("UF_ORIG_DEST", ufOutro);
        c.put("IND_COMPRA_VENDA", "CP");
        c.put("UF_DESTINO", cp.uf);
        c.put("IND_SITUACAO_ESP", finNfe == 2 ? "5" : "");
        c.put("UF_ARMAZ_ORIG", ufOutro);
        c.put("UF_ARMAZ_DEST", cp.uf);
        Item pisRef = primeiroCom(itens, i -> i.pis != null && i.pis.ppis != null);
        Item cofRef = primeiroCom(itens, i -> i.cofins != null && i.cofins.ppis != null);
        c.put("VLR_ALIQ_PIS", pisRef == null ? null : aliquota(pisRef.pis.ppis));
        c.put("VLR_ALIQ_COFINS", cofRef == null ? null : aliquota(cofRef.cofins.ppis));
        c.put("COD_SITUACAO_A", texto(primeiro.icms.orig));
        c.put("COD_SITUACAO_B", texto(primeiro.icms.cst));
        c.put("NUM_AUTENTIC_NFE", chave);
        c.put("NUM_DV_NFE", String.valueOf(Valores.inteiro(Dom.txt(ide, "cDV"))));
        c.put("COD_MODELO_COTEPE", "55");
        c.put("COD_SIT_PIS", ex ? doisDigitos(item(itens, i -> i.pis == null ? null : i.pis.cstNt))
                : doisDigitos(item(itens, i -> i.pis == null ? null : i.pis.cstAliq)));
        c.put("COD_SIT_COFINS", ex ? doisDigitos(item(itens, i -> i.cofins == null ? null : i.cofins.cstNt))
                : doisDigitos(item(itens, i -> i.cofins == null ? null : i.cofins.cstAliq)));
        c.put("IND_NAT_FRETE", "9");
        r.safx07.add(c);

        // ---------------------------------------------------------- SAFX08
        String dtLanc = !dtSaidaEnt.isEmpty() ? dtSaidaEnt : dtEmissao;
        for (Item it : comIcms) {
            Map<String, String> l = new LinkedHashMap<>();
            Icms icms = it.icms;
            l.put("COD_EMPRESA", codEmpresa);
            l.put("COD_ESTAB", codEstab);
            l.put("DATA_FISCAL", dtEmissao);
            l.put("MOVTO_E_S", movto);
            l.put("NORM_DEV", normDev);
            l.put("COD_DOCTO", "NFE");
            l.put("IND_FIS_JUR", "2");
            l.put("COD_FIS_JUR", cp.cnpj);
            l.put("NUM_DOCFIS", numDocfis);
            l.put("SERIE_DOCFIS", "1");
            l.put("IND_BEM_PATR", "N");
            l.put("IND_PRODUTO", "1");
            l.put("COD_PRODUTO", it.cProd);
            l.put("COD_UND_PADRAO", it.uTrib);
            l.put("NUM_ITEM", String.valueOf(it.nItem));
            l.put("DESCRICAO_COMPL", Valores.tiraAcento(limitar(it.xProd, 50)));
            l.put("COD_CFO", cfop(tpNf, it.cfop));
            l.put("QUANTIDADE", Valores.direita(concat(Valores.digitos(it.qCom, 4), "00"), 17));
            l.put("COD_MEDIDA", it.uCom);
            l.put("COD_NBM", it.ncm);
            l.put("VLR_UNIT", Valores.z(it.vUnCom, 4, 19));
            l.put("VLR_ITEM", Valores.z17(it.vProd));
            // VLR_DESCONTO e VLR_SEGURO saem do TOTAL da nota, repetidos em cada item (script SQL)
            l.put("VLR_DESCONTO", Valores.z17(vDescTot));
            l.put("COD_SITUACAO_A", texto(icms.orig));
            l.put("COD_SITUACAO_B", texto(icms.cst));
            l.put("COD_FEDERAL", icms.orig == null ? null : icms.orig == 0 ? "1" : icms.orig == 1 ? "2"
                    : icms.orig == 2 ? "3" : null);
            l.put("IND_IPI_INCLUSO", "N");
            l.put("PESO_LIQUIDO", Valores.z(pesoL, 3, 14));
            l.put("VLR_FRETE", Valores.bruto(it.vFrete));
            l.put("VLR_SEGURO", Valores.z17(vSegTot));
            l.put("VLR_OUTRAS", Valores.bruto(it.vOutro));
            l.put("VLR_ALIQ_ICMS", Valores.z(icms.picms, 2, 7));
            l.put("VLR_ICMS", Valores.z17(icms.vicms));
            l.put("VLR_ALIQ_IPI", Valores.z(it.pIpi, 2, 7));
            l.put("VLR_IPI", Valores.z17(it.vIpi));
            l.put("VLR_ALIQ_SUB_ICMS", Valores.bruto(icms.picmsst));
            l.put("VLR_SUBST_ICMS", Valores.bruto(icms.vicmsst));
            l.put("TRIB_ICMS", tribIcms(icms.cst));
            l.put("BASE_ICMS", Valores.z17(icms.vbc != null ? icms.vbc : it.vProd));
            l.put("TRIB_IPI", tribIpi(icms.cst));
            l.put("BASE_IPI", Valores.z17(it.vProd));
            l.put("VLR_CONTAB_ITEM", Valores.z17(it.vProd));
            l.put("VLR_OUTROS1", Valores.bruto(it.vOutro));
            l.put("VLR_BASE_PIS", Valores.z17(it.pis == null ? null : it.pis.vbc));
            l.put("VLR_PIS", Valores.z17(it.pis == null ? null : it.pis.vpis));
            l.put("VLR_BASE_COFINS", Valores.z17(it.cofins == null ? null : it.cofins.vbc));
            l.put("VLR_COFINS", Valores.z17(it.cofins == null ? null : it.cofins.vpis));
            l.put("VLR_ALIQ_PIS", it.pis == null || it.pis.ppis == null ? null : aliquota(it.pis.ppis));
            l.put("VLR_ALIQ_COFINS", it.cofins == null || it.cofins.ppis == null ? null
                    : aliquota(it.cofins.ppis));
            // case when H.TAG_IPINT_CST != null ... else 53: comparação com NULL nunca é
            // verdadeira no SQL, então o resultado é sempre 53
            l.put("COD_TRIB_IPI", "53");
            l.put("IND_VLR_PIS_COFINS", "S");
            l.put("COD_SITUACAO_PIS", ex ? doisDigitos(it.pis == null ? null : coalesce(it.pis.cstNt, it.pis.cstOutr))
                    : doisDigitos(it.pis == null ? null : coalesce(it.pis.cstAliq, it.pis.cstOutr)));
            l.put("COD_SITUACAO_COFINS", ex ? doisDigitos(it.cofins == null ? null
                    : coalesce(it.cofins.cstNt, it.cofins.cstOutr))
                    : doisDigitos(it.cofins == null ? null : coalesce(it.cofins.cstAliq, it.cofins.cstOutr)));
            l.put("DAT_LANC_PIS_COFINS", dtLanc);
            l.put("IND_NATUREZA_FRETE", "9");
            r.safx08.add(l);
        }

        // -------------------------------------------- SAFX2013 / SAFX2043
        String dataProduto = dtEmissao.length() == 8 ? dtEmissao.substring(0, 6) + "01" : null;
        for (Item it : itens) {
            Map<String, String> p = new LinkedHashMap<>();
            p.put("IND_PRODUTO", "1");
            p.put("COD_PRODUTO", it.cProd);
            p.put("DATA_PRODUTO", dataProduto);
            p.put("DESCRICAO", limitar(it.xProd, 50));
            p.put("COD_NBM", it.ncm);
            p.put("COD_MEDIDA", it.uCom);
            p.put("COD_UND_PADRAO", it.uTrib);
            p.put("CLAS_ITEM", "99");
            r.safx2013.add(p);

            Map<String, String> n = new LinkedHashMap<>();
            n.put("COD_NBM", it.ncm);
            n.put("DATA_NBM", "0");
            n.put("DESCRICAO", it.xProd);
            n.put("SECAO_IPI", "0");
            n.put("NOTA_SECAO_IPI", "0");
            r.safx2043.add(n);
        }

        // ---------------------------------------------------------- SAFX04
        Map<String, String> pf = new LinkedHashMap<>();
        pf.put("IND_FIS_JUR", "2");
        pf.put("COD_FIS_JUR", cp.cnpj);
        pf.put("DATA_X04", dtEmissao.length() == 8 ? dtEmissao.substring(0, 6) + "01" : null);
        pf.put("IND_CONTEM_COD", ex ? "4" : "1");
        pf.put("RAZAO_SOCIAL", limitar(cp.nome, 50));
        pf.put("CPF_CGC", ex ? "" : cp.cnpj);
        pf.put("INSC_ESTADUAL", ex ? "ISENTO" : String.valueOf(cp.ie));
        pf.put("NOME_FANTASIA", limitar(cp.nome, 50));
        pf.put("ENDERECO", cp.xLgr);
        pf.put("NUM_ENDERECO", cp.nro);
        pf.put("COMPL_ENDERECO", cp.xCpl);
        pf.put("BAIRRO", limitar(cp.xBairro, 20));
        pf.put("CIDADE", cp.xMun);
        pf.put("UF", cp.uf);
        pf.put("CEP", cp.cep == null ? null : Valores.direita(String.valueOf(cp.cep), 8));
        pf.put("COD_PAIS", limitar(cp.cPais, 3));
        pf.put("TELEFONE", String.valueOf(cp.fone));
        pf.put("COD_MUNICIPIO", cp.cMun == null ? null : substringSql(String.valueOf(cp.cMun), 3, 5));
        r.safx04.add(pf);
        r.notas++;
    }

    // ----------------------------------------------------------- contraparte

    private static Contraparte contraparte(Element el, Element ender, boolean saida) {
        Contraparte cp = new Contraparte();
        if (el != null) {
            String cnpj = Dom.txtOuNull(el, "CNPJ");
            if (saida && cnpj == null) {
                // dest sem CNPJ: idEstrangeiro, e só na falta dele o CPF
                String idEstr = Dom.txtOuNull(el, "idEstrangeiro");
                cnpj = idEstr != null ? idEstr : Dom.txtOuNull(el, "CPF");
            }
            cp.cnpj = cnpj;
            cp.nome = Dom.txtOuNull(el, "xNome");
            String ie = Dom.txt(el, "IE");
            if (!ie.isEmpty()) {
                if (saida && ie.equalsIgnoreCase("isento")) {
                    cp.ie = 99;
                } else {
                    try {
                        cp.ie = Long.parseLong(ie);
                    } catch (NumberFormatException e) {
                        // Convert.ToInt64 estoura e o original descarta a nota inteira
                        throw new IllegalStateException("IE \"" + ie + "\" não numérica");
                    }
                }
            }
            if (saida) cp.indIeDest = Dom.txt(el, "indIEDest");
        }
        if (ender != null) {
            cp.xLgr = Dom.txtOuNull(ender, "xLgr");
            cp.nro = Dom.txtOuNull(ender, "nro");
            cp.xCpl = Dom.txt(ender, "xCpl");
            cp.xBairro = Dom.txtOuNull(ender, "xBairro");
            cp.cMun = Valores.inteiro(Dom.txt(ender, "cMun"));
            cp.xMun = Dom.txtOuNull(ender, "xMun");
            cp.uf = Dom.txtOuNull(ender, "UF");
            cp.cep = Valores.inteiro(Dom.txt(ender, "CEP"));
            cp.cPais = Dom.txtOuNull(ender, "cPais");
            // Int32.TryParse: telefone que não cabe em int (11 dígitos) ou com símbolo vira 0
            try {
                cp.fone = Integer.parseInt(Dom.txt(ender, "fone"));
            } catch (NumberFormatException e) {
                cp.fone = 0;
            }
        }
        return cp;
    }

    // ----------------------------------------------------------------- itens

    private static List<Item> itens(Element inf, List<String> avisos, String arquivo) {
        List<Item> itens = new ArrayList<>();
        NodeList filhos = inf.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() != Node.ELEMENT_NODE || !Dom.nomeLocal(n).equals("det")) continue;
            Element det = (Element) n;
            Element prod = Dom.no(det, "prod");
            Item it = new Item();
            it.nItem = Valores.inteiro(det.getAttribute("nItem"));
            it.cProd = Dom.txtOuNull(prod, "cProd");
            it.xProd = Dom.txtOuNull(prod, "xProd");
            it.ncm = Dom.txtOuNull(prod, "NCM");
            it.cfop = Valores.inteiro(Dom.txt(prod, "CFOP"));
            it.uCom = Dom.txtOuNull(prod, "uCom");
            it.uTrib = Dom.txtOuNull(prod, "uTrib");
            it.qCom = Valores.decOuZero(Dom.txt(prod, "qCom"));
            it.vUnCom = Valores.decOuZero(Dom.txt(prod, "vUnCom"));
            it.vProd = Valores.decOuZero(Dom.txt(prod, "vProd"));
            it.vFrete = Valores.decOuZero(Dom.txt(prod, "vFrete"));
            it.vOutro = Valores.decOuZero(Dom.txt(prod, "vOutro"));

            Element imposto = Dom.no(det, "imposto");
            Element icmsPai = Dom.no(imposto, "ICMS");
            if (icmsPai != null) {
                it.icms = icms(icmsPai);
            } else {
                // o original só grava a linha de imposto quando há grupo ICMS e o script
                // cruza por id_linha: item sem ICMS (ex.: serviço ISSQN) não chega ao SAFX08
                avisos.add(arquivo + ": item " + it.nItem + " sem grupo ICMS não vai para o SAFX08.");
            }
            Element ipiTrib = Dom.no(imposto, "IPI", "IPITrib");
            if (ipiTrib != null) {
                it.pIpi = Valores.decOuZero(Dom.txt(ipiTrib, "pIPI"));
                it.vIpi = Valores.decOuZero(Dom.txt(ipiTrib, "vIPI"));
            }
            it.pis = pisCofins(Dom.no(imposto, "PIS"), "PIS");
            it.cofins = pisCofins(Dom.no(imposto, "COFINS"), "COFINS");
            itens.add(it);
        }
        itens.sort((a, b) -> Integer.compare(a.nItem, b.nItem));
        return itens;
    }

    private static Icms icms(Element pai) {
        Icms x = new Icms();
        Element g = Dom.primeiroFilhoElemento(pai);
        if (g == null) return x;
        String grupo = Dom.nomeLocal(g);
        boolean cst = GRUPOS_CST.contains(grupo);
        boolean sn = GRUPOS_SN.contains(grupo);
        if (!cst && !sn) return x;

        // ICMSSN101/201/202/500/900 leem "Orig" (maiúsculo), que o XML não tem: o original
        // grava 0. Só o ICMSSN102 lê "orig". Reproduzido de propósito.
        String nomeOrig = sn && !grupo.equals("ICMSSN102") ? "Orig" : "orig";
        x.orig = Valores.inteiro(Dom.txt(g, nomeOrig));
        if (cst) x.cst = Valores.inteiro(Dom.txt(g, "CST"));
        if (GRUPOS_BASE.contains(grupo)) {
            x.vbc = Valores.decOuZero(Dom.txt(g, "vBC"));
            x.picms = Valores.decOuZero(Dom.txt(g, "pICMS"));
        }
        if (GRUPOS_BASE.contains(grupo) || grupo.equals("ICMS40")) {
            x.vicms = Valores.decOuZero(Dom.txt(g, "vICMS"));
        }
        if (GRUPOS_ST.contains(grupo)) {
            x.picmsst = Valores.decOuZero(Dom.txt(g, "pICMSST"));
            x.vicmsst = Valores.decOuZero(Dom.txt(g, "vICMSST"));
        }
        return x;
    }

    /** PIS ou COFINS: lê só as variantes que o script SQL usa (Aliq, NT, Outr). */
    private static Pis pisCofins(Element pai, String tributo) {
        if (pai == null) return null;
        Pis p = new Pis();
        Element aliq = Dom.no(pai, tributo + "Aliq");
        if (aliq != null) {
            p.cstAliq = Valores.inteiro(Dom.txt(aliq, "CST"));
            p.vbc = Valores.decOuZero(Dom.txt(aliq, "vBC"));
            p.ppis = Valores.decOuZero(Dom.txt(aliq, "p" + tributo));
            p.vpis = Valores.decOuZero(Dom.txt(aliq, "v" + tributo));
        }
        Element nt = Dom.no(pai, tributo + "NT");
        if (nt != null) p.cstNt = Valores.inteiro(Dom.txt(nt, "CST"));
        Element outr = Dom.no(pai, tributo + "Outr");
        if (outr != null) p.cstOutr = Valores.inteiro(Dom.txt(outr, "CST"));
        return p;
    }

    // ------------------------------------------------------------------ regras

    /** 2207 de entrada vira 6207 (case do SAFX07/08). */
    private static String cfop(int tpNf, int cfop) {
        return String.valueOf(tpNf == 0 && cfop == 2207 ? 6207 : cfop);
    }

    /**
     * IND_TP_FRETE do SAFX07. Para a contraparte da lista de estabelecimentos a
     * regra depende da versão (4.00 mantém 3 e 4); para as demais, só 0 e 1 são
     * traduzidos e os outros valores passam. Sem grupo transp o resultado é nulo
     * (ou 0, no ramo geral, porque NOT IN com NULL é desconhecido e cai no ELSE).
     */
    static String indTpFrete(String cnpjContraparte, int versao, Integer m) {
        if (EmpresasFixas.estabelecimentoConhecido(cnpjContraparte)) {
            if (m == null) return null;
            if (m == 0) return "1";
            if (m == 1) return "2";
            if (m == 2 || m == 9) return "0";
            return String.valueOf(m);
        }
        if (m == null) return "0";
        if (m == 0) return "1";
        if (m == 1) return "2";
        return String.valueOf(m);
    }

    /** CST do ICMS (inteiro: 00 chega como 0) -> TRIB_ICMS do SAFX08. */
    static String tribIcms(Integer cst) {
        if (cst == null) return "3";
        switch (cst) {
            case 0: case 10: case 30: case 70:
                return "1";
            case 40: case 41: case 50: case 51: case 60:
                return "2";
            default:
                return "3";
        }
    }

    /** O script reaproveita o CST do ICMS para TRIB_IPI (mapeia 00/01 -> 1 e 02..05 -> 2). */
    static String tribIpi(Integer cst) {
        if (cst == null) return "3";
        switch (cst) {
            case 0: case 1:
                return "1";
            case 2: case 3: case 4: case 5:
                return "2";
            default:
                return "3";
        }
    }

    /** right(replicate('0',7)+cast(REPLACE(pPIS,'.','') as varchar(7)) + '00', 7): 1,65 -> 0016500. */
    private static String aliquota(BigDecimal percentual) {
        return Valores.direita(concat(Valores.digitos(percentual, 2), "00"), 7);
    }

    // --------------------------------------------------------------- utilitários

    private interface Seletor<T> {
        T de(Item i);
    }

    private static String somaZ17(List<Item> itens, Seletor<BigDecimal> campo) {
        BigDecimal soma = null;
        for (Item i : itens) {
            BigDecimal v = campo.de(i);
            if (v != null) soma = soma == null ? v : soma.add(v);
        }
        return Valores.z17(soma);
    }

    private static Item primeiroCom(List<Item> itens, java.util.function.Predicate<Item> filtro) {
        for (Item i : itens) {
            if (filtro.test(i)) return i;
        }
        return null;
    }

    private static Integer item(List<Item> itens, Seletor<Integer> campo) {
        for (Item i : itens) {
            Integer v = campo.de(i);
            if (v != null) return v;
        }
        return null;
    }

    private static Integer coalesce(Integer a, Integer b) {
        return a != null ? a : b;
    }

    private static String doisDigitos(Integer v) {
        return Valores.doisDigitos(v);
    }

    private static String texto(Integer v) {
        return v == null ? null : String.valueOf(v);
    }

    private static String concat(String a, String b) {
        return a == null ? null : a + b;
    }

    private static String limitar(String s, int n) {
        return s == null ? null : s.length() > n ? s.substring(0, n) : s;
    }

    /** SUBSTRING do T-SQL: posição inicial 1-based. */
    private static String substringSql(String s, int inicio, int tamanho) {
        if (s == null || inicio > s.length()) return "";
        return s.substring(inicio - 1, Math.min(s.length(), inicio - 1 + tamanho));
    }
}
