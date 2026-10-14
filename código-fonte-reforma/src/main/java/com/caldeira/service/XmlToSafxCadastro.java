package com.caldeira.service;

import java.io.IOException;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.ArrayList;

/**
 * SAFX04 (cadastro de Pessoa Física/Jurídica) dos participantes lidos pela
 * Reforma Tributária.
 *
 * Diferente do SAFX3007/3008/3009, este NÃO é gerado para todo participante: só
 * entra quem NÃO tem de-para em participantes.properties. Quem já tem de-para já
 * está cadastrado no MasterSAF via COD_PART do SPED — gerar um segundo SAFX04
 * para ele, com o CNPJ cru como chave, duplicaria o cadastro (ver
 * {@link Participantes}). A lista de quem não tem de-para é a que
 * {@link XmlToSafxReforma} já populou ao montar os SAFX3007/3008/3009 — por isso
 * este gerador roda depois dele.
 *
 * Os registros voltam ao chamador, que os junta aos do SAFX04 de NF-e e CT-e no
 * arquivo único (a aba SAFX04 do sistema original recebe as três origens).
 */
final class XmlToSafxCadastro {

    private XmlToSafxCadastro() {
    }

    static List<Map<String, String>> registros(List<XmlNota> notas) throws IOException {
        Set<String> semCadastro = new HashSet<>(Participantes.semCadastro());
        Map<String, XmlParticipante> unicos = new LinkedHashMap<>();
        Map<String, String> dataCompetencia = new LinkedHashMap<>();

        for (XmlNota nota : notas) {
            // NF-e de saída: nota.participante é o destinatário, e o SAFX04 de NF-e
            // (NfeParaSafx) já gera uma linha para ele incondicionalmente. Considerá-lo
            // aqui de novo duplicaria a linha sempre que o participante não tem de-para.
            // Na NF-e de entrada (participante = emitente terceiro) e na NFS-e
            // (prestador) não há equivalente, e a consideração continua necessária.
            boolean jaCobertoPelaNfe = nota.origem == XmlNota.Origem.NFE && "9".equals(nota.movtoES);
            if (!jaCobertoPelaNfe) {
                considerar(unicos, dataCompetencia, semCadastro, nota.participante, nota.dtEmissao);
            }
            considerar(unicos, dataCompetencia, semCadastro, nota.transportadora, nota.dtEmissao);
        }

        List<Map<String, String>> registros = new ArrayList<>(unicos.size());
        for (Map.Entry<String, XmlParticipante> e : unicos.entrySet()) {
            registros.add(registro(e.getValue(), dataCompetencia.get(e.getKey())));
        }
        return registros;
    }

    private static void considerar(Map<String, XmlParticipante> destino, Map<String, String> dataCompetencia,
                                   Set<String> semCadastro, XmlParticipante p, String dtEmissao) {
        if (p == null) return;
        String documento = p.documento();
        if (documento.isEmpty()) return;
        String chave = CadastroProperties.chaveDocumento(documento);
        if (chave.isEmpty()) chave = documento;
        // só entra quem o de-para de participantes.properties não conhece
        if (!semCadastro.contains(chave)) return;
        if (!destino.containsKey(chave)) {
            destino.put(chave, p);
            dataCompetencia.put(chave, competencia(dtEmissao));
        }
    }

    /** DATA_X04 é o mês/ano de competência, dia sempre 01 — mesma convenção do SAFX04 de NF-e. */
    private static String competencia(String dtEmissaoYyyyMmDd) {
        if (dtEmissaoYyyyMmDd == null || dtEmissaoYyyyMmDd.length() < 6) return null;
        return dtEmissaoYyyyMmDd.substring(0, 6) + "01";
    }

    private static Map<String, String> registro(XmlParticipante p, String competencia) throws IOException {
        boolean estrangeiro = isVazio(p.cnpj) && isVazio(p.cpf);

        // CPF_CGC vai SEM máscara: a coluna equivalente do SAFX04 de NF-e grava o
        // CNPJ/CPF cru, e a máscara de 18 posições estouraria o campo.
        String documento = isVazio(p.cnpj) ? p.cpf : p.cnpj;

        Map<String, String> r = new LinkedHashMap<>();
        r.put("IND_FIS_JUR", p.papel);
        r.put("COD_FIS_JUR", Participantes.codFisJur(p.documento()));
        r.put("DATA_X04", competencia);
        r.put("IND_CONTEM_COD", estrangeiro ? "4" : "1");
        r.put("RAZAO_SOCIAL", truncar(p.nome, 50));
        r.put("CPF_CGC", estrangeiro ? null : Documentos.semMascara(documento));
        r.put("INSC_ESTADUAL", truncar(estrangeiro ? "ISENTO" : p.ie, 20));
        r.put("NOME_FANTASIA", truncar(isVazio(p.nomeFantasia) ? p.nome : p.nomeFantasia, 50));
        r.put("ENDERECO", truncar(p.logradouro, 60));
        r.put("NUM_ENDERECO", truncar(p.numero, 10));
        r.put("COMPL_ENDERECO", truncar(p.complemento, 30));
        r.put("BAIRRO", truncar(p.bairro, 20));
        r.put("CIDADE", truncar(p.municipio, 40));
        r.put("UF", truncar(p.uf, 2));
        r.put("CEP", truncar(p.cep, 8));
        r.put("COD_PAIS", codPais(p.codPais));
        r.put("TELEFONE", truncar(p.fone, 10));
        r.put("COD_MUNICIPIO", codMunicipio(p.codMunicipio));
        r.put("IND_SIMPLES_NAC", p.simplesNacional ? "S" : "N");
        r.put("E_MAIL", truncar(p.email, 60));
        return r;
    }

    private static String codPais(String cPais) {
        if (isVazio(cPais)) return "105";
        return cPais.length() > 3 ? cPais.substring(0, 3) : cPais;
    }

    private static String codMunicipio(String cMun) {
        if (isVazio(cMun)) return null;
        return cMun.length() > 5 ? cMun.substring(cMun.length() - 5) : cMun;
    }

    private static String truncar(String valor, int tamanho) {
        if (isVazio(valor)) return valor;
        return valor.length() > tamanho ? valor.substring(0, tamanho) : valor;
    }

    private static boolean isVazio(String s) {
        return s == null || s.isEmpty();
    }
}
