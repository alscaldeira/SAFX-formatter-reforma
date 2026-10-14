package com.caldeira.service;

/**
 * Códigos fixos de empresa/estabelecimento e CNPJ próprio do sistema original.
 *
 * Estão cravados em ScriptSQL/SAFX07.txt e SAFX08.txt (COD_EMPRESA/COD_ESTAB
 * pelo CNPJ da contraparte, com fallback 001/0009) e em HomeController
 * (MEU_CNPJ, que decide se um CT-e é próprio). São regras de negócio do
 * cliente, não do leiaute — por isso ficam num lugar só.
 */
final class EmpresasFixas {

    /** CNPJ do estabelecimento "próprio": CT-e emitido por ele é de saída. */
    static final String MEU_CNPJ = "42105890000901";

    private EmpresasFixas() {
    }

    static String codEmpresa(String cnpjContraparte) {
        if (cnpjContraparte == null) return "001";
        switch (cnpjContraparte) {
            case "03823704000152":
            case "03823704000314":
            case "03823704000233":
                return "001";
            case "03848527000169":
                return "002";
            case "04613348000105":
                return "003";
            case "22725405000120":
            case "22725405000200":
                return "004";
            default:
                return "001";
        }
    }

    static String codEstab(String cnpjContraparte) {
        if (cnpjContraparte == null) return "0009";
        switch (cnpjContraparte) {
            case "03823704000152":
            case "03848527000169":
            case "04613348000105":
            case "22725405000120":
                return "0001";
            case "03823704000314":
            case "22725405000200":
                return "0002";
            case "03823704000233":
                return "0003";
            default:
                return "0009";
        }
    }

    /** Contraparte é um dos estabelecimentos da lista (muda a regra de IND_TP_FRETE do SAFX07). */
    static boolean estabelecimentoConhecido(String cnpjContraparte) {
        if (cnpjContraparte == null) return false;
        switch (cnpjContraparte) {
            case "03823704000152":
            case "03823704000314":
            case "03823704000233":
            case "03848527000169":
            case "04613348000105":
            case "22725405000120":
            case "22725405000200":
                return true;
            default:
                return false;
        }
    }
}
