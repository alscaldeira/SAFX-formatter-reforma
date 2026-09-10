package com.caldeira.service;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * Consolida os participantes lidos dos XMLs (destinatário da NF-e de saída,
 * emitente da NF-e de entrada de terceiro, prestador da NFS-e e transportadora)
 * numa instância única por documento (CNPJ/CPF).
 *
 * O cadastro de Pessoa Física/Jurídica (SAFX04) não é mais gerado por este
 * fluxo, mas a consolidação continua sendo necessária: um participante que
 * aparece em papéis diferentes no mesmo lote tem de entrar nos SAFX3007/3008/
 * 3009 com um IND_FIS_JUR só — "5" (Cliente/Fornecedor/Transportadora) —, senão
 * o mesmo CNPJ sairia ora como cliente ora como fornecedor e o vínculo com o
 * cadastro do MasterSAF se perderia.
 */
final class ParticipantesXml {

    private ParticipantesXml() {
    }

    /** Aponta cada nota para a instância consolidada do seu participante. */
    static void consolidar(List<XmlNota> notas) {
        Map<String, XmlParticipante> participantes = new LinkedHashMap<>();

        for (XmlNota nota : notas) {
            acumular(participantes, nota.participante);
            acumular(participantes, nota.transportadora);
        }

        for (XmlNota nota : notas) {
            nota.participante = consolidado(participantes, nota.participante);
            nota.transportadora = consolidado(participantes, nota.transportadora);
        }
    }

    private static void acumular(Map<String, XmlParticipante> destino, XmlParticipante p) {
        if (p == null || p.documento().isEmpty()) return;
        XmlParticipante existente = destino.get(p.documento());
        if (existente == null) {
            destino.put(p.documento(), p);
            return;
        }
        if (!Objects.equals(existente.papel, p.papel)) {
            existente.papel = XmlNota.PAPEL_MISTO;
        }
        // completa o que faltava no primeiro registro (ex.: IE só vem no dest)
        if (vazio(existente.ie)) existente.ie = p.ie;
        if (vazio(existente.im)) existente.im = p.im;
        if (vazio(existente.logradouro)) existente.logradouro = p.logradouro;
        if (vazio(existente.numero)) existente.numero = p.numero;
        if (vazio(existente.complemento)) existente.complemento = p.complemento;
        if (vazio(existente.bairro)) existente.bairro = p.bairro;
        if (vazio(existente.municipio)) existente.municipio = p.municipio;
        if (vazio(existente.codMunicipio)) existente.codMunicipio = p.codMunicipio;
        if (vazio(existente.uf)) existente.uf = p.uf;
        if (vazio(existente.cep)) existente.cep = p.cep;
        if (vazio(existente.codPais)) existente.codPais = p.codPais;
        if (vazio(existente.email)) existente.email = p.email;
    }

    private static XmlParticipante consolidado(Map<String, XmlParticipante> mapa, XmlParticipante p) {
        if (p == null || p.documento().isEmpty()) return p;
        XmlParticipante consolidado = mapa.get(p.documento());
        return consolidado != null ? consolidado : p;
    }

    private static boolean vazio(String s) {
        return s == null || s.trim().isEmpty();
    }
}
