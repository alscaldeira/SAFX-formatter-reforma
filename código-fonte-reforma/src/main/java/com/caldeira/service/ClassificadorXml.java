package com.caldeira.service;

import org.w3c.dom.Document;
import org.w3c.dom.Element;

import java.io.File;
import java.nio.file.Path;

/**
 * Separa o lote de XMLs por tipo de documento, pelo conteúdo de cada arquivo.
 *
 * O critério é o modelo declarado em ide/mod — 55 e 65 são NF-e/NFC-e, 57 e 67
 * são CT-e/CT-e OS. Procurar a string "infCte" no texto bruto acerta na maioria
 * dos casos mas casa também com o XML de um evento de CT-e e com qualquer CDATA
 * que cite o nome do nó; por isso a decisão é pela estrutura do DOM.
 */
final class ClassificadorXml {

    private ClassificadorXml() {
    }

    static TipoXmlFiscal classificar(Path caminho) {
        if (caminho == null || !caminho.toFile().isFile()) return TipoXmlFiscal.DESCONHECIDO;
        File arquivo = caminho.toFile();
        try {
            return classificar(Dom.ler(arquivo));
        } catch (Exception e) {
            // XML quebrado não para o lote: o arquivo fica de fora da classificação
            // e quem decide o que fazer com ele é o leitor de cada fluxo.
            return TipoXmlFiscal.DESCONHECIDO;
        }
    }

    static TipoXmlFiscal classificar(Document documento) {
        if (documento == null || documento.getDocumentElement() == null) return TipoXmlFiscal.DESCONHECIDO;
        Element raiz = documento.getDocumentElement();

        // NFS-e antes dos demais: não tem grupo ide/mod; é identificada pela raiz
        // NFSe com o grupo infNFSe, como faz o XmlLeitor da Reforma.
        if (Dom.buscar(raiz, "infNFSe") != null) return TipoXmlFiscal.NFSE;

        // infCte antes de infNFe: o CT-e carrega infDoc/infNFe (as chaves das
        // NF-e transportadas), então procurar infNFe primeiro classificaria todo
        // CT-e com documento vinculado como NF-e. O inverso não ocorre.
        Element inf = Dom.buscar(raiz, "infCte");
        if (inf == null) inf = Dom.buscar(raiz, "infNFe");
        if (inf == null) return TipoXmlFiscal.DESCONHECIDO;

        // O modelo manda. O nome do nó só entra como desempate porque há XML
        // antigo, ou remontado por ERP, que chega sem o grupo ide completo.
        Element mod = Dom.buscar(inf, "mod");
        String modelo = mod == null ? null : mod.getTextContent().trim();
        if ("55".equals(modelo) || "65".equals(modelo)) return TipoXmlFiscal.NFE;
        if ("57".equals(modelo) || "67".equals(modelo)) return TipoXmlFiscal.CTE;

        return "infCte".equals(Dom.nomeLocal(inf)) ? TipoXmlFiscal.CTE : TipoXmlFiscal.NFE;
    }
}
