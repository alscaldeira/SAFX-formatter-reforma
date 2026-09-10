package com.caldeira.service;

/**
 * Pessoa física/jurídica extraída de um XML (destinatário, emitente/prestador
 * ou transportadora). Vira uma linha do SAFX04 e é referenciada pelo
 * COD_FIS_JUR nos SAFX07/08/49.
 */
class XmlParticipante {

    String cnpj;
    String cpf;
    /** idEstrangeiro da NF-e: identificação do destinatário do exterior. */
    String idEstrangeiro;
    String nome;
    String nomeFantasia;
    String ie;
    String im;
    String logradouro;
    String numero;
    String complemento;
    String bairro;
    String municipio;
    String codMunicipio;
    String uf;
    String cep;
    String codPais;
    String email;
    String fone;
    /** Domínio de IND_FIS_JUR — ver constantes PAPEL_* em {@link XmlNota}. */
    String papel;
    /** indIEDest da NF-e: 1 contribuinte, 2 isento de inscrição, 9 não contribuinte. */
    String indContribuinteIcms;
    /** opSimpNac da NFS-e. */
    boolean simplesNacional;

    /**
     * Chave do participante nos arquivos SAFX (COD_FIS_JUR e cadastro SAFX04).
     *
     * Exportação não tem CNPJ nem CPF — a NF-e traz idEstrangeiro, que pode vir
     * vazio. Sem uma chave o participante sumiria do SAFX04 e a capa ficaria
     * apontando para nada, então cai-se para um código derivado do nome. O
     * de-para {@link Participantes} continua sendo o lugar de corrigir isso com
     * o código real do ERP, e a conversão avisa enquanto ele não existir.
     */
    String documento() {
        if (cnpj != null && !cnpj.isEmpty()) return cnpj;
        if (cpf != null && !cpf.isEmpty()) return cpf;
        if (idEstrangeiro != null && !idEstrangeiro.trim().isEmpty()) {
            return "EX" + apenasAlfanumerico(idEstrangeiro, 12);
        }
        if (nome != null && !nome.trim().isEmpty()) {
            return "EX" + apenasAlfanumerico(nome, 12);
        }
        return "";
    }

    private static String apenasAlfanumerico(String valor, int tamanho) {
        StringBuilder sb = new StringBuilder();
        for (char c : valor.toUpperCase().toCharArray()) {
            if (Character.isLetterOrDigit(c) && c < 128) sb.append(c);
            if (sb.length() == tamanho) break;
        }
        return sb.toString();
    }
}
