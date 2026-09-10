package com.caldeira.service;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/**
 * Ponto de entrada da conversão XML -> SAFX. Lê uma pasta de XMLs fiscais
 * (NF-e e NFS-e, em qualquer subpasta) — ou um único arquivo XML — e grava em
 * user.dir:
 *
 * <ul>
 *   <li>SAFX3007/3008/3009, as extensões de Reforma Tributária (IBS/CBS)
 *       geradas a partir dos grupos IBSCBS do XML, mais os CSV equivalentes.</li>
 * </ul>
 *
 * O cadastro de Pessoa Física/Jurídica (SAFX04) não é gerado por este fluxo: os
 * participantes já existem na base do MasterSAF, e os registros da Reforma
 * apenas os referenciam pelo IND_FIS_JUR + COD_FIS_JUR.
 */
public final class ConversorXml {

    private ConversorXml() {
    }

    /** @return avisos acumulados na conversão (lista vazia = nada a revisar). */
    public static List<String> converter(String pasta) throws IOException {
        List<String> avisos = new ArrayList<>();
        Participantes.limparAvisos();
        Estabelecimentos.limparAvisos();

        List<XmlNota> notas = XmlLeitor.lerPasta(pasta, avisos);

        for (XmlNota nota : notas) {
            if (nota.cnpjEstab == null || nota.cnpjEstab.isEmpty()) {
                throw new IOException("Não foi possível identificar o estabelecimento no XML "
                        + nota.arquivo + " (sem CNPJ de emitente/destinatário/tomador).");
            }
        }
        // não gera SAFX04, mas o papel consolidado do participante é o que vai
        // no IND_FIS_JUR dos registros da Reforma
        ParticipantesXml.consolidar(notas);

        // Extensões de Reforma Tributária (IBS/CBS): SAFX3007/3008/3009
        XmlToSafxReforma.gerar(notas, avisos);

        for (String cnpj : Estabelecimentos.semCadastro()) {
            avisos.add("Estabelecimento " + cnpj + " sem código no " + Estabelecimentos.NOME_ARQUIVO
                    + ": o COD_EMPRESA/COD_ESTAB foi deduzido da ordem do próprio CNPJ. "
                    + "Confira com o cadastro do MasterSAF — os dois nem sempre coincidem.");
        }
        for (String cnpj : Participantes.semCadastro()) {
            avisos.add("Participante " + cnpj + " sem código no " + Participantes.NOME_ARQUIVO
                    + ": o COD_FIS_JUR foi gerado como \"M" + cnpj + "\". Cadastre o código do ERP "
                    + "se esta base MasterSAF também recebe cargas do SPED, senão o participante "
                    + "entrará duplicado.");
        }

        long nfe = notas.stream().filter(n -> n.origem == XmlNota.Origem.NFE).count();
        System.out.println("Conversão XML concluída: " + notas.size() + " documentos ("
                + nfe + " NF-e, " + (notas.size() - nfe) + " NFS-e).");
        return avisos;
    }
}
