package com.caldeira.service;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Ponto de entrada da conversão XML -> SAFX. Lê uma pasta de XMLs fiscais (em
 * qualquer subpasta) — ou um único arquivo XML — e grava em user.dir:
 *
 * <ul>
 *   <li>SAFX07, SAFX08, SAFX04, SAFX2013 e SAFX2043 a partir de NF-e
 *       ({@link NfeParaSafx}) e de CT-e ({@link CteParaSafx});</li>
 *   <li>SAFX3007/3008/3009, as extensões de Reforma Tributária (IBS/CBS)
 *       geradas a partir dos grupos IBSCBS do XML de NF-e e NFS-e, mais os CSV
 *       equivalentes.</li>
 * </ul>
 *
 * O tipo de cada XML sai do próprio conteúdo ({@link ClassificadorXml}), e não
 * de qual campo ele foi entregue. O SAFX04 é um arquivo só, com o destinatário
 * da NF-e, o emitente do CT-e e os participantes da Reforma que não têm
 * de-para em participantes.properties.
 */
public final class ConversorXml {

    private static final String[] LAYOUTS_NFE_CTE = {"SAFX07", "SAFX08", "SAFX04", "SAFX2013", "SAFX2043"};

    private ConversorXml() {
    }

    /** @return avisos acumulados na conversão (lista vazia = nada a revisar). */
    public static List<String> converter(String pasta) throws IOException {
        return converter(pasta, false, false);
    }

    /**
     * @param calcPisCofins calcula PIS/COFINS (1,65% / 7,6%) do CT-e — a tela do original
     *                      chama "Calcular PIS/COFINS do CT-e"
     * @param calcIcms      usa a alíquota e o valor de ICMS do próprio CT-e — "Calcular ICMS do CT-e"
     */
    public static List<String> converter(String pasta, boolean calcPisCofins, boolean calcIcms)
            throws IOException {
        List<String> avisos = new ArrayList<>();
        Participantes.limparAvisos();
        Estabelecimentos.limparAvisos();

        List<Path> nfe = new ArrayList<>();
        List<Path> cte = new ArrayList<>();
        List<Path> nfse = new ArrayList<>();
        // lote da Reforma: NF-e, NFS-e e o que não foi reconhecido (o leitor da Reforma
        // decide o que fazer com o arquivo — ignora com aviso ou aponta o erro)
        List<Path> reforma = new ArrayList<>();
        for (Path arquivo : XmlLeitor.listarXml(pasta)) {
            switch (ClassificadorXml.classificar(arquivo)) {
                case NFE:
                    nfe.add(arquivo);
                    reforma.add(arquivo);
                    break;
                case CTE:
                    cte.add(arquivo);
                    break;
                case NFSE:
                    nfse.add(arquivo);
                    reforma.add(arquivo);
                    break;
                default:
                    reforma.add(arquivo);
            }
        }

        // ---- Reforma Tributária (SAFX3007/3008/3009) — como antes
        List<XmlNota> notas = new ArrayList<>();
        boolean haOutroResultado = !nfe.isEmpty() || !cte.isEmpty();
        if (!reforma.isEmpty()) {
            try {
                notas = reformaTributaria(reforma, pasta, avisos);
            } catch (IOException e) {
                // A falha da Reforma não derruba a exportação de NF-e/CT-e (o XML anterior ao
                // novo regime nem tem grupo IBS/CBS). Sem NF-e nem CT-e para entregar, o erro
                // é o resultado e segue para quem chamou.
                if (!haOutroResultado) throw e;
                avisos.add("Reforma Tributária (SAFX3007/3008/3009) não gerada: " + e.getMessage());
            }
        }

        // ---- NF-e e CT-e (SAFX07/08/04/2013/2043)
        NfeParaSafx.Resultado resNfe = new NfeParaSafx.Resultado();
        if (!nfe.isEmpty()) resNfe = NfeParaSafx.converter(nfe, avisos);

        CteParaSafx.Resultado resCte = new CteParaSafx.Resultado();
        if (!cte.isEmpty()) {
            try {
                resCte = CteParaSafx.converter(cte, calcPisCofins, calcIcms, avisos);
            } catch (IOException | RuntimeException e) {
                resCte = new CteParaSafx.Resultado();
                avisos.add("CT-e nao convertido: " + e.getMessage() + " Os demais arquivos foram gerados normalmente.");
            }
        }

        // SAFX04: NF-e, depois CT-e (uma linha por CT-e, como na planilha de referência), depois a Reforma
        List<Map<String, String>> safx04 = new ArrayList<>(resNfe.safx04);
        safx04.addAll(resCte.safx04);
        if (!notas.isEmpty()) safx04.addAll(XmlToSafxCadastro.registros(notas));

        Set<String> gravados = new HashSet<>();
        if (!nfe.isEmpty() || !cte.isEmpty()) {
            List<Map<String, String>> safx07 = new ArrayList<>(resNfe.safx07);
            safx07.addAll(resCte.safx07);
            List<Map<String, String>> safx08 = new ArrayList<>(resNfe.safx08);
            safx08.addAll(resCte.safx08);
            SafxSaida.gravar("SAFX07", safx07, avisos);
            SafxSaida.gravar("SAFX08", safx08, avisos);
            SafxSaida.gravar("SAFX2013", resNfe.safx2013, avisos);
            SafxSaida.gravar("SAFX2043", resNfe.safx2043, avisos);
            gravados.addAll(List.of("SAFX07", "SAFX08", "SAFX2013", "SAFX2043"));
        }
        if (!nfe.isEmpty() || !cte.isEmpty() || !safx04.isEmpty()) {
            SafxSaida.gravar("SAFX04", safx04, avisos);
            gravados.add("SAFX04");
        }

        if (nfe.isEmpty() && cte.isEmpty()) {
            avisos.add(nfse.isEmpty()
                    ? "Nenhuma NF-e ou CT-e no lote: SAFX07/08/04/2013/2043 não foram gerados."
                    : "Lote só de NFS-e: SAFX07/08/04/2013/2043 vêm de NF-e e CT-e e não foram gerados. "
                    + "Os serviços estão no SAFX3009.");
        }
        for (String layout : LAYOUTS_NFE_CTE) {
            if (gravados.contains(layout)) continue;
            Path antigo = Paths.get(System.getProperty("user.dir"), layout + ".txt");
            if (Files.exists(antigo)) {
                avisos.add(layout + ".txt de uma conversão anterior continua em " + antigo.getParent()
                        + ": esta conversão não gerou " + layout + ".");
            }
        }

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

        System.out.println("Conversão XML concluída: " + (nfe.size() + nfse.size() + cte.size())
                + " documentos (" + nfe.size() + " NF-e, " + nfse.size() + " NFS-e, " + cte.size() + " CT-e).");
        return avisos;
    }

    private static List<XmlNota> reformaTributaria(List<Path> arquivos, String origem, List<String> avisos)
            throws IOException {
        List<XmlNota> notas = XmlLeitor.lerArquivos(arquivos, origem, avisos);

        for (XmlNota nota : notas) {
            if (nota.cnpjEstab == null || nota.cnpjEstab.isEmpty()) {
                throw new IOException("Não foi possível identificar o estabelecimento no XML "
                        + nota.arquivo + " (sem CNPJ de emitente/destinatário/tomador).");
            }
        }
        // o papel consolidado do participante é o que vai no IND_FIS_JUR dos
        // registros da Reforma
        ParticipantesXml.consolidar(notas);

        // Extensões de Reforma Tributária (IBS/CBS): SAFX3007/3008/3009
        XmlToSafxReforma.gerar(notas, avisos);
        return notas;
    }
}
