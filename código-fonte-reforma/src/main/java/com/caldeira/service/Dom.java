package com.caldeira.service;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

import javax.xml.parsers.DocumentBuilderFactory;
import java.io.File;
import java.io.IOException;

/**
 * Acesso ao DOM dos XMLs fiscais por nome local, ignorando namespace — o mesmo
 * documento chega com o namespace do portal fiscal, com o de um ERP, ou sem
 * nenhum. Todo acesso é seguro contra nó ausente: devolve null / "" em vez de
 * estourar (equivalente ao No()/Txt() do HomeController do sistema em C#).
 */
final class Dom {

    private Dom() {
    }

    static Document ler(File arquivo) throws IOException {
        try {
            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            factory.setNamespaceAware(false);
            // XML fiscal não usa DTD nem entidade externa; desligar evita XXE.
            factory.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
            factory.setExpandEntityReferences(false);
            Document doc = factory.newDocumentBuilder().parse(arquivo);
            doc.getDocumentElement().normalize();
            return doc;
        } catch (Exception e) {
            throw new IOException("Falha ao ler o XML " + arquivo.getName() + ": " + e.getMessage(), e);
        }
    }

    static String nomeLocal(Node node) {
        String nome = node.getNodeName();
        int dp = nome.indexOf(':');
        return dp >= 0 ? nome.substring(dp + 1) : nome;
    }

    /** Desce o caminho de filhos diretos; null assim que um deles faltar. */
    static Element no(Node pai, String... caminho) {
        Node atual = pai;
        for (String nome : caminho) {
            if (atual == null) return null;
            atual = filho(atual, nome);
        }
        return atual instanceof Element ? (Element) atual : null;
    }

    static Element filho(Node pai, String nome) {
        if (pai == null) return null;
        NodeList filhos = pai.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE && nomeLocal(n).equals(nome)) {
                return (Element) n;
            }
        }
        return null;
    }

    /** Busca em profundidade pelo primeiro descendente (ou o próprio nó) com o nome. */
    static Element buscar(Node raiz, String nome) {
        if (raiz == null) return null;
        if (raiz.getNodeType() == Node.ELEMENT_NODE && nomeLocal(raiz).equals(nome)) {
            return (Element) raiz;
        }
        NodeList filhos = raiz.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Element achado = buscar(filhos.item(i), nome);
            if (achado != null) return achado;
        }
        return null;
    }

    static Element primeiroFilhoElemento(Node pai) {
        if (pai == null) return null;
        NodeList filhos = pai.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            if (filhos.item(i).getNodeType() == Node.ELEMENT_NODE) return (Element) filhos.item(i);
        }
        return null;
    }

    /**
     * Texto aparado do nó no caminho; "" se o caminho não existir. O aparo
     * reproduz o LimpaTextoXml do original, que tirava os espaços e quebras de
     * linha colados nos delimitadores de tag antes de ler o XML.
     */
    static String txt(Node pai, String... caminho) {
        Element e = no(pai, caminho);
        return e == null || e.getTextContent() == null ? "" : e.getTextContent().trim();
    }

    /** Como {@link #txt}, mas null quando o nó não existe (distingue ausente de vazio). */
    static String txtOuNull(Node pai, String... caminho) {
        Element e = no(pai, caminho);
        return e == null || e.getTextContent() == null ? null : e.getTextContent().trim();
    }
}
