package com.caldeira.service;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Stream;

/**
 * Lê uma pasta de XMLs fiscais (recursivamente) e devolve o modelo neutro
 * {@link XmlNota}. Reconhece dois schemas:
 *
 * <ul>
 *   <li>NF-e 4.00 — raiz {@code nfeProc} (ou {@code NFe} avulso);</li>
 *   <li>NFS-e Nacional 1.01 — raiz {@code NFSe}.</li>
 * </ul>
 *
 * O parser é propositalmente namespace-unaware: os dois schemas usam namespace
 * default (elementos sem prefixo), então navegar por nome local é suficiente e
 * evita registrar URIs diferentes por versão de layout. O grupo {@code Signature}
 * nunca é acessado.
 */
final class XmlLeitor {

    private XmlLeitor() {
    }

    /** Aceita uma pasta (varrida recursivamente) ou um único arquivo XML. */
    static List<XmlNota> lerPasta(String origem, List<String> avisos) throws IOException {
        Path raiz = Paths.get(origem);
        List<Path> arquivos = new ArrayList<>();

        if (Files.isDirectory(raiz)) {
            try (Stream<Path> stream = Files.walk(raiz)) {
                stream.filter(Files::isRegularFile)
                        .filter(p -> p.getFileName().toString().toLowerCase().endsWith(".xml"))
                        .sorted(Comparator.comparing(Path::toString))
                        .forEach(arquivos::add);
            }
            if (arquivos.isEmpty()) {
                throw new IOException("Nenhum arquivo .xml encontrado em " + origem);
            }
        } else if (Files.isRegularFile(raiz)) {
            arquivos.add(raiz);
        } else {
            throw new IOException("Caminho não encontrado: " + origem);
        }

        List<XmlNota> notas = new ArrayList<>();
        for (Path arquivo : arquivos) {
            XmlNota nota = ler(arquivo.toFile(), avisos);
            if (nota != null) {
                notas.add(nota);
            } else {
                avisos.add("Arquivo ignorado (schema não reconhecido): " + arquivo.getFileName());
            }
        }
        if (notas.isEmpty()) {
            throw new IOException("Nenhum XML de NF-e ou NFS-e reconhecido em " + origem);
        }
        return notas;
    }

    private static XmlNota ler(File arquivo, List<String> avisos) throws IOException {
        Element raiz = raizDe(arquivo);
        String tag = nomeLocal(raiz);

        if ("nfeProc".equals(tag) || "NFe".equals(tag)) {
            return lerNfe(raiz, arquivo, avisos);
        }
        if ("NFSe".equals(tag)) {
            return lerNfse(raiz, arquivo);
        }
        return null;
    }

    private static Element raizDe(File arquivo) throws IOException {
        try {
            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            factory.setNamespaceAware(false);
            // XML fiscal não usa DTD nem entidade externa; desligar evita XXE.
            factory.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
            factory.setExpandEntityReferences(false);
            DocumentBuilder builder = factory.newDocumentBuilder();
            Document doc = builder.parse(arquivo);
            doc.getDocumentElement().normalize();
            return doc.getDocumentElement();
        } catch (Exception e) {
            throw new IOException("Falha ao ler o XML " + arquivo.getName() + ": " + e.getMessage(), e);
        }
    }

    // ------------------------------------------------------------------ NF-e

    private static XmlNota lerNfe(Element raiz, File arquivo, List<String> avisos) {
        Element infNFe = busca(raiz, "infNFe");
        if (infNFe == null) return null;

        XmlNota nota = new XmlNota();
        nota.origem = XmlNota.Origem.NFE;
        nota.arquivo = arquivo.getName();

        Element ide = filho(infNFe, "ide");
        Element emit = filho(infNFe, "emit");
        Element dest = filho(infNFe, "dest");

        nota.chave = soDigitos(atributo(infNFe, "Id"));
        nota.dvChave = txt(ide, "cDV");
        nota.numDoc = txt(ide, "nNF");
        nota.serie = txt(ide, "serie");
        nota.codModelo = txt(ide, "mod");
        nota.dtEmissao = data(txt(ide, "dhEmi"));
        nota.horaEmissao = hora(txt(ide, "dhEmi"));
        nota.dtSaidaEnt = data(txt(ide, "dhSaiEnt"));
        if (nota.dtSaidaEnt.isEmpty()) nota.dtSaidaEnt = nota.dtEmissao;
        nota.natOp = txt(ide, "natOp");
        nota.finalidade = txt(ide, "finNFe");
        nota.indicadorConsumidorFinal = txt(ide, "indFinal");
        nota.indicadorPresenca = txt(ide, "indPres");
        nota.indicadorIntermediador = txt(ide, "indIntermed");
        // NORM_DEV: finNFe 4 (devolução/retorno) -> "2" no domínio do SAFX
        nota.normDev = "4".equals(nota.finalidade) ? "2" : "1";
        nota.codDocto = "NFE";
        nota.codClassDocFis = "1"; // mercadorias/produtos -> SAFX08

        String cnpjEmit = documento(txt(emit, "CNPJ"));
        String cnpjDest = documento(txt(dest, "CNPJ"));
        boolean saida = "1".equals(txt(ide, "tpNF"));

        if (saida) {
            nota.cnpjEstab = cnpjEmit;
            nota.movtoES = "9"; // Documento de Saída
            nota.participante = participanteDest(dest);
            nota.participante.papel = XmlNota.PAPEL_CLIENTE;
        } else {
            nota.cnpjEstab = cnpjDest;
            if (cnpjDest.equals(cnpjEmit)) {
                // entrada emitida pelo próprio estabelecimento (ajuste, retorno, devolução)
                nota.movtoES = "4"; // Entrada própria, outros motivos legais
                nota.participante = participanteDest(dest);
                nota.participante.papel = XmlNota.PAPEL_ESTABELECIMENTO;
            } else {
                nota.movtoES = "1"; // Entrada, documento de terceiros
                nota.participante = participanteEmit(emit);
                nota.participante.papel = XmlNota.PAPEL_FORNECEDOR;
            }
        }

        // Situação: só é cancelada/denegada quando o protocolo diz isso
        Element infProt = busca(raiz, "infProt");
        String cStat = infProt != null ? txt(infProt, "cStat") : "";
        nota.situacao = cStat.isEmpty() || "100".equals(cStat) ? "N" : "S";
        if (infProt != null) {
            nota.dtAutenticacao = data(txt(infProt, "dhRecbto"));
            if (nota.chave.isEmpty()) nota.chave = soDigitos(txt(infProt, "chNFe"));
        }

        // Documentos referenciados: o SAFX07 comporta um só (campos 16/17)
        List<Element> refs = new ArrayList<>();
        for (Element nfRef : filhos(ide, "NFref")) {
            refs.addAll(filhos(nfRef, "refNFe"));
        }
        nota.qtdReferencias = refs.size();

        // nota sem NFref é o caso comum (venda normal): não há chave referenciada
        String chaveRef = refs.isEmpty() ? "" : soDigitos(texto(refs.get(0)));
        nota.chaveReferenciada = chaveRef;
        if (chaveRef.length() == 44) {
            // layout da chave: cUF(2) AAMM(4) CNPJ(14) mod(2) serie(3) nNF(9) ...
            nota.serieDocRef = chaveRef.substring(22, 25);
            nota.numDocRef = chaveRef.substring(25, 34);
        }

        if (refs.size() > 1)
            avisos.add(arquivo.getName() + ": " + refs.size()
                + " documentos referenciados (refNFe) e o SAFX3007 comporta apenas um — "
                + "campos 16/17 deixados em branco; a lista permanece no campo 150 (infCpl).");

        Element total = filho(infNFe, "total");
        Element icmsTot = total != null ? filho(total, "ICMSTot") : null;
        if (icmsTot != null) {
            nota.vlProdutos = num(icmsTot, "vProd");
            nota.vlTotal = num(icmsTot, "vNF");
            nota.vlFrete = num(icmsTot, "vFrete");
            nota.vlSeguro = num(icmsTot, "vSeg");
            nota.vlOutras = num(icmsTot, "vOutro");
            nota.vlDesconto = num(icmsTot, "vDesc");
            nota.vlIcms = num(icmsTot, "vICMS");
            nota.baseIcms = num(icmsTot, "vBC");
            nota.vlIpi = num(icmsTot, "vIPI");
            nota.vlPis = num(icmsTot, "vPIS");
            nota.vlCofins = num(icmsTot, "vCOFINS");
            nota.vlIpiDevolvido = num(icmsTot, "vIPIDevol");
        }

        Element transp = filho(infNFe, "transp");
        if (transp != null) {
            nota.indTpFrete = txt(transp, "modFrete");
            Element transportadora = filho(transp, "transporta");
            if (transportadora != null && !documento(txt(transportadora, "CNPJ")).isEmpty()) {
                nota.transportadora = participanteTransportadora(transportadora);
            }
        }

        nota.indFatura = indFatura(infNFe);

        Element infAdic = filho(infNFe, "infAdic");
        if (infAdic != null) {
            nota.observacao = txt(infAdic, "infCpl");
        }

        for (Element det : filhos(infNFe, "det")) {
            nota.itens.add(lerItem(det, nota, avisos));
        }

        // a NF-e traz total do valor de PIS/COFINS/IPI, mas não da base: somar dos itens
        for (XmlItem item : nota.itens) {
            nota.basePis += item.basePis;
            nota.baseCofins += item.baseCofins;
            nota.baseIpi += item.baseIpi;
        }
        if (!nota.itens.isEmpty()) {
            XmlItem primeiro = nota.itens.get(0);
            nota.aliqPis = primeiro.aliqPis;
            nota.aliqCofins = primeiro.aliqCofins;
            nota.cfop = primeiro.cfop;
        }

        Element ibsCbsTot = total != null ? filho(total, "IBSCBSTot") : null;
        if (ibsCbsTot != null) {
            XmlIbsCbs ibs = new XmlIbsCbs();
            ibs.baseCalculo = num(ibsCbsTot, "vBCIBSCBS");
            Element gIbs = filho(ibsCbsTot, "gIBS");
            if (gIbs != null) {
                ibs.vlIbs = num(gIbs, "vIBS");
                ibs.vlCreditoPresumidoIbs = num(gIbs, "vCredPres");
                ibs.vlCreditoPresumidoCondSuspensaoIbs = num(gIbs, "vCredPresCondSus");
                Element uf = filho(gIbs, "gIBSUF");
                if (uf != null) {
                    ibs.vlIbsUf = num(uf, "vIBSUF");
                    ibs.vlDiferimentoIbsUf = num(uf, "vDif");
                    ibs.vlDevolucaoTributoIbsUf = num(uf, "vDevTrib");
                }
                Element mun = filho(gIbs, "gIBSMun");
                if (mun != null) {
                    ibs.vlIbsMun = num(mun, "vIBSMun");
                    ibs.vlDiferimentoIbsMun = num(mun, "vDif");
                    ibs.vlDevolucaoTributoIbsMun = num(mun, "vDevTrib");
                }
            }
            Element gCbs = filho(ibsCbsTot, "gCBS");
            if (gCbs != null) {
                ibs.vlCbs = num(gCbs, "vCBS");
                ibs.vlDiferimentoCbs = num(gCbs, "vDif");
                ibs.vlDevolucaoTributoCbs = num(gCbs, "vDevTrib");
                ibs.vlCreditoPresumidoCbs = num(gCbs, "vCredPres");
                ibs.vlCreditoPresumidoCondSuspensaoCbs = num(gCbs, "vCredPresCondSus");
            }
            ibs.vlTotalDocumento = num(total, "vNFTot");
            if (ibs.vlTotalDocumento == 0.0) ibs.vlTotalDocumento = nota.vlTotal;
            nota.ibsCbs = ibs;
        }

        return nota;
    }

    private static XmlItem lerItem(Element det, XmlNota nota, List<String> avisos) {
        XmlItem item = new XmlItem();
        item.numItem = (int) numero(atributo(det, "nItem"));

        Element prod = filho(det, "prod");
        item.codProduto = txt(prod, "cProd");
        item.descricao = txt(prod, "xProd");
        item.ncm = txt(prod, "NCM");
        item.exTipi = txt(prod, "EXTIPI");
        item.cfop = txt(prod, "CFOP");
        item.unidadeComercial = txt(prod, "uCom");
        item.unidadeTributavel = txt(prod, "uTrib");
        item.qtdComercial = num(prod, "qCom");
        item.qtdTributavel = num(prod, "qTrib");
        item.vlUnitario = num(prod, "vUnCom");
        item.vlProduto = num(prod, "vProd");
        item.vlDesconto = num(prod, "vDesc");
        item.vlFrete = num(prod, "vFrete");
        item.vlSeguro = num(prod, "vSeg");
        item.vlOutras = num(prod, "vOutro");
        item.vlContabil = num(det, "vItem");
        if (item.vlContabil == 0.0) item.vlContabil = item.vlProduto;

        if (item.qtdTributavel > 0 && Math.abs(item.qtdComercial - item.qtdTributavel) > 0.000001) {
            avisos.add(nota.arquivo + " item " + item.numItem + ": qCom (" + item.qtdComercial
                    + ") difere de qTrib (" + item.qtdTributavel
                    + ") — unidades comercial e tributável não são equivalentes, confira a conversão.");
        }

        Element imposto = filho(det, "imposto");
        if (imposto != null) {
            Element icms = unico(filho(imposto, "ICMS"));
            if (icms != null) {
                item.cstIcms = txt(icms, "CST");
                item.origemIcms = txt(icms, "orig");
                item.baseIcms = num(icms, "vBC");
                item.aliqIcms = num(icms, "pICMS");
                item.vlIcms = num(icms, "vICMS");
            }
            Element grupoIpi = filho(imposto, "IPI");
            if (grupoIpi != null) {
                item.codEnquadramentoIpi = txt(grupoIpi, "cEnq");
                Element ipi = unicoIgnorando(grupoIpi, "cEnq");
                if (ipi != null) {
                    item.cstIpi = txt(ipi, "CST");
                    item.baseIpi = num(ipi, "vBC");
                    item.aliqIpi = num(ipi, "pIPI");
                    item.vlIpi = num(ipi, "vIPI");
                }
            }
            Element pis = unico(filho(imposto, "PIS"));
            if (pis != null) {
                item.cstPis = txt(pis, "CST");
                item.basePis = num(pis, "vBC");
                item.aliqPis = num(pis, "pPIS");
                item.vlPis = num(pis, "vPIS");
            }
            Element cofins = unico(filho(imposto, "COFINS"));
            if (cofins != null) {
                item.cstCofins = txt(cofins, "CST");
                item.baseCofins = num(cofins, "vBC");
                item.aliqCofins = num(cofins, "pCOFINS");
                item.vlCofins = num(cofins, "vCOFINS");
            }
            Element ii = filho(imposto, "II");
            if (ii != null) {
                item.baseIi = num(ii, "vBC");
                item.vlIi = num(ii, "vII");
                item.vlDespesasAduaneiras = num(ii, "vDespAdu");
                item.vlIof = num(ii, "vIOF");
            }
            Element ibsCbs = filho(imposto, "IBSCBS");
            if (ibsCbs != null) {
                item.ibsCbs = lerIbsCbsItem(ibsCbs);
            }
        }

        Element di = prod != null ? filho(prod, "DI") : null;
        if (di != null) {
            XmlDi importacao = new XmlDi();
            importacao.numero = txt(di, "nDI");
            importacao.dataRegistro = data(txt(di, "dDI"));
            importacao.dataDesembaraco = data(txt(di, "dDesemb"));
            importacao.ufDesembaraco = txt(di, "UFDesemb");
            importacao.localDesembaraco = txt(di, "xLocDesemb");
            importacao.codExportador = txt(di, "cExportador");
            importacao.tipoViaTransporte = txt(di, "tpViaTransp");
            importacao.tipoIntermedio = txt(di, "tpIntermedio");
            importacao.vlAfrmm = num(di, "vAFRMM");
            Element adi = filho(di, "adi");
            if (adi != null) {
                importacao.numeroAdicao = txt(adi, "nAdicao");
                importacao.numeroDrawback = txt(adi, "nDraw");
                importacao.codFabricante = txt(adi, "cFabricante");
                importacao.vlDescontoAdicao = num(adi, "vDescDI");
            }
            item.di = importacao;
        }

        return item;
    }

    private static XmlIbsCbs lerIbsCbsItem(Element ibsCbs) {
        XmlIbsCbs ibs = new XmlIbsCbs();
        ibs.cst = txt(ibsCbs, "CST");
        ibs.classificacaoTributaria = txt(ibsCbs, "cClassTrib");
        Element grupo = filho(ibsCbs, "gIBSCBS");
        if (grupo != null) {
            ibs.baseCalculo = num(grupo, "vBC");
            ibs.vlIbs = num(grupo, "vIBS");
            Element uf = filho(grupo, "gIBSUF");
            if (uf != null) {
                ibs.aliqIbsUf = num(uf, "pIBSUF");
                ibs.vlIbsUf = num(uf, "vIBSUF");
            }
            Element mun = filho(grupo, "gIBSMun");
            if (mun != null) {
                ibs.aliqIbsMun = num(mun, "pIBSMun");
                ibs.vlIbsMun = num(mun, "vIBSMun");
            }
            Element cbs = filho(grupo, "gCBS");
            if (cbs != null) {
                ibs.aliqCbs = num(cbs, "pCBS");
                ibs.vlCbs = num(cbs, "vCBS");
            }
        }
        return ibs;
    }

    private static String indFatura(Element infNFe) {
        Element pag = filho(infNFe, "pag");
        Element detPag = pag != null ? filho(pag, "detPag") : null;
        String tPag = detPag != null ? txt(detPag, "tPag") : "";
        if ("90".equals(tPag)) return "3"; // sem pagamento
        Element cobr = filho(infNFe, "cobr");
        if (cobr != null && filho(cobr, "dup") != null) return "2"; // a prazo
        if (!tPag.isEmpty()) return "1"; // a vista
        return null;
    }

    // ----------------------------------------------------------------- NFS-e

    private static XmlNota lerNfse(Element raiz, File arquivo) {
        Element infNFSe = filho(raiz, "infNFSe");
        if (infNFSe == null) return null;

        XmlNota nota = new XmlNota();
        nota.origem = XmlNota.Origem.NFSE;
        nota.arquivo = arquivo.getName();

        Element dps = filho(infNFSe, "DPS");
        Element infDPS = dps != null ? filho(dps, "infDPS") : null;
        Element prestador = filho(infNFSe, "emit");
        Element tomador = infDPS != null ? filho(infDPS, "toma") : null;

        // A empresa é a tomadora do serviço: entrada de documento de terceiro.
        nota.cnpjEstab = tomador != null ? documento(txt(tomador, "CNPJ")) : "";
        nota.movtoES = "1";
        nota.normDev = "1";
        nota.codDocto = "NFS";
        nota.codClassDocFis = "2"; // serviços -> SAFX09
        nota.situacao = "100".equals(txt(infNFSe, "cStat")) ? "N" : "S";

        nota.participante = participanteNfse(prestador, infDPS);
        nota.participante.papel = XmlNota.PAPEL_FORNECEDOR;

        nota.numDoc = txt(infNFSe, "nNFSe");
        nota.chave = soDigitos(atributo(infNFSe, "Id"));
        nota.codVerificacao = atributo(infNFSe, "Id");
        nota.dtAutenticacao = data(txt(infNFSe, "dhProc"));

        if (infDPS != null) {
            nota.serie = txt(infDPS, "serie");
            nota.dtEmissao = data(txt(infDPS, "dhEmi"));
            nota.horaEmissao = hora(txt(infDPS, "dhEmi"));
            nota.dtSaidaEnt = data(txt(infDPS, "dCompet"));
            if (nota.dtSaidaEnt.isEmpty()) nota.dtSaidaEnt = nota.dtEmissao;
            nota.numRps = txt(infDPS, "nDPS");
            nota.serieRps = txt(infDPS, "serie");
            nota.dtRps = nota.dtEmissao;

            Element serv = filho(infDPS, "serv");
            if (serv != null) {
                Element cServ = filho(serv, "cServ");
                if (cServ != null) {
                    nota.descricaoServico = txt(cServ, "xDescServ");
                    nota.codTributacaoNacional = txt(cServ, "cTribNac");
                }
            }

            Element valores = filho(infDPS, "valores");
            if (valores != null) {
                Element vServPrest = filho(valores, "vServPrest");
                if (vServPrest != null) nota.vlServico = num(vServPrest, "vServ");
                Element trib = filho(valores, "trib");
                Element tribFed = trib != null ? filho(trib, "tribFed") : null;
                if (tribFed != null) {
                    Element pisCofins = filho(tribFed, "piscofins");
                    if (pisCofins != null) {
                        nota.basePis = num(pisCofins, "vBCPisCofins");
                        nota.baseCofins = nota.basePis;
                        nota.aliqPis = num(pisCofins, "pAliqPis");
                        nota.aliqCofins = num(pisCofins, "pAliqCofins");
                        nota.vlPis = num(pisCofins, "vPis");
                        nota.vlCofins = num(pisCofins, "vCofins");
                    }
                    nota.vlIr = num(tribFed, "vRetIRRF");
                    nota.vlCsll = num(tribFed, "vRetCSLL");
                    nota.vlInss = num(tribFed, "vRetCP");
                }
            }
        }

        Element valoresNfse = filho(infNFSe, "valores");
        if (valoresNfse != null) {
            nota.baseIss = num(valoresNfse, "vBC");
            nota.aliqIss = num(valoresNfse, "pAliqAplic");
            nota.vlIss = num(valoresNfse, "vISSQN");
            nota.vlIssRetido = num(valoresNfse, "vTotalRet");
            nota.vlTotal = num(valoresNfse, "vLiq");
        }
        if (nota.vlServico == 0.0) nota.vlServico = nota.baseIss;
        if (nota.vlTotal == 0.0) nota.vlTotal = nota.vlServico;
        nota.codMunicipioIss = txt(infNFSe, "cLocIncid");

        Element ibsCbs = filho(infNFSe, "IBSCBS");
        if (ibsCbs != null) {
            XmlIbsCbs ibs = new XmlIbsCbs();
            ibs.codLocalidadeIncidencia = txt(ibsCbs, "cLocalidadeIncid");
            ibs.nomeLocalidadeIncidencia = txt(ibsCbs, "xLocalidadeIncid");
            Element valores = filho(ibsCbs, "valores");
            if (valores != null) {
                ibs.baseCalculo = num(valores, "vBC");
                Element uf = filho(valores, "uf");
                if (uf != null) {
                    ibs.aliqIbsUf = num(uf, "pIBSUF");
                    ibs.aliqReducaoIbsUf = num(uf, "pRedAliqUF");
                    ibs.aliqEfetivaIbsUf = num(uf, "pAliqEfetUF");
                }
                Element mun = filho(valores, "mun");
                if (mun != null) {
                    ibs.aliqIbsMun = num(mun, "pIBSMun");
                    ibs.aliqReducaoIbsMun = num(mun, "pRedAliqMun");
                    ibs.aliqEfetivaIbsMun = num(mun, "pAliqEfetMun");
                }
                Element fed = filho(valores, "fed");
                if (fed != null) {
                    ibs.aliqCbs = num(fed, "pCBS");
                    ibs.aliqReducaoCbs = num(fed, "pRedAliqCBS");
                    ibs.aliqEfetivaCbs = num(fed, "pAliqEfetCBS");
                }
            }
            Element tot = filho(ibsCbs, "totCIBS");
            if (tot != null) {
                ibs.vlTotalDocumento = num(tot, "vTotNF");
                Element gIbs = filho(tot, "gIBS");
                if (gIbs != null) {
                    ibs.vlIbs = num(gIbs, "vIBSTot");
                    Element ufTot = filho(gIbs, "gIBSUFTot");
                    if (ufTot != null) ibs.vlIbsUf = num(ufTot, "vIBSUF");
                    Element munTot = filho(gIbs, "gIBSMunTot");
                    if (munTot != null) ibs.vlIbsMun = num(munTot, "vIBSMun");
                }
                Element gCbs = filho(tot, "gCBS");
                if (gCbs != null) ibs.vlCbs = num(gCbs, "vCBS");
            }
            // grupo IBSCBS do DPS: finalidade, indicador de operação e CST
            Element ibsDps = infDPS != null ? filho(infDPS, "IBSCBS") : null;
            if (ibsDps != null) {
                ibs.finalidadeNfse = txt(ibsDps, "finNFSe");
                ibs.codIndicadorOperacao = txt(ibsDps, "cIndOp");
                ibs.indicadorDestinatario = txt(ibsDps, "indDest");
                Element valoresDps = filho(ibsDps, "valores");
                Element trib = valoresDps != null ? filho(valoresDps, "trib") : null;
                Element grupo = trib != null ? filho(trib, "gIBSCBS") : null;
                if (grupo != null) {
                    ibs.cst = txt(grupo, "CST");
                    ibs.classificacaoTributaria = txt(grupo, "cClassTrib");
                }
            }
            nota.ibsCbs = ibs;
        }

        return nota;
    }

    // --------------------------------------------------------- participantes

    private static XmlParticipante participanteDest(Element dest) {
        XmlParticipante p = new XmlParticipante();
        if (dest == null) return p;
        p.cnpj = documento(txt(dest, "CNPJ"));
        p.cpf = documento(txt(dest, "CPF"));
        p.idEstrangeiro = txt(dest, "idEstrangeiro");
        p.nome = txt(dest, "xNome");
        p.nomeFantasia = p.nome;
        p.ie = txt(dest, "IE");
        p.indContribuinteIcms = txt(dest, "indIEDest");
        endereco(p, filho(dest, "enderDest"));
        return p;
    }

    private static XmlParticipante participanteEmit(Element emit) {
        XmlParticipante p = new XmlParticipante();
        if (emit == null) return p;
        p.cnpj = documento(txt(emit, "CNPJ"));
        p.cpf = documento(txt(emit, "CPF"));
        p.nome = txt(emit, "xNome");
        p.nomeFantasia = txt(emit, "xFant");
        if (p.nomeFantasia.isEmpty()) p.nomeFantasia = p.nome;
        p.ie = txt(emit, "IE");
        endereco(p, filho(emit, "enderEmit"));
        return p;
    }

    private static XmlParticipante participanteTransportadora(Element transporta) {
        XmlParticipante p = new XmlParticipante();
        p.cnpj = documento(txt(transporta, "CNPJ"));
        p.cpf = documento(txt(transporta, "CPF"));
        p.nome = txt(transporta, "xNome");
        p.nomeFantasia = p.nome;
        p.ie = txt(transporta, "IE");
        p.logradouro = txt(transporta, "xEnder");
        p.municipio = txt(transporta, "xMun");
        p.uf = txt(transporta, "UF");
        p.papel = XmlNota.PAPEL_TRANSPORTADORA;
        return p;
    }

    private static XmlParticipante participanteNfse(Element prestador, Element infDPS) {
        XmlParticipante p = new XmlParticipante();
        if (prestador != null) {
            p.cnpj = documento(txt(prestador, "CNPJ"));
            p.cpf = documento(txt(prestador, "CPF"));
            p.nome = txt(prestador, "xNome");
            p.nomeFantasia = p.nome;
            p.im = txt(prestador, "IM");
            p.email = txt(prestador, "email");
            p.fone = txt(prestador, "fone");
            Element ender = filho(prestador, "enderNac");
            if (ender != null) {
                p.logradouro = txt(ender, "xLgr");
                p.numero = txt(ender, "nro");
                p.complemento = txt(ender, "xCpl");
                p.bairro = txt(ender, "xBairro");
                p.codMunicipio = txt(ender, "cMun");
                p.uf = txt(ender, "UF");
                p.cep = txt(ender, "CEP");
                // enderNac é, por definição, endereço nacional: a NFS-e não traz
                // cPais, mas o COD_PAIS do SAFX04 é Brasil (BACEN 1058)
                p.codPais = "1058";
            }
        }
        if (infDPS != null) {
            Element prest = filho(infDPS, "prest");
            Element regTrib = prest != null ? filho(prest, "regTrib") : null;
            if (regTrib != null) {
                p.simplesNacional = "1".equals(txt(regTrib, "opSimpNac"));
            }
        }
        return p;
    }

    private static void endereco(XmlParticipante p, Element ender) {
        if (ender == null) return;
        p.logradouro = txt(ender, "xLgr");
        p.numero = txt(ender, "nro");
        p.complemento = txt(ender, "xCpl");
        p.bairro = txt(ender, "xBairro");
        p.municipio = txt(ender, "xMun");
        p.codMunicipio = txt(ender, "cMun");
        p.uf = txt(ender, "UF");
        p.cep = txt(ender, "CEP");
        p.codPais = txt(ender, "cPais");
        p.fone = txt(ender, "fone");
    }

    // ------------------------------------------------------------ utilidades

    private static String nomeLocal(Node node) {
        String nome = node.getNodeName();
        int dp = nome.indexOf(':');
        return dp >= 0 ? nome.substring(dp + 1) : nome;
    }

    /** Primeiro filho direto com o nome informado. */
    static Element filho(Element pai, String nome) {
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

    static List<Element> filhos(Element pai, String nome) {
        List<Element> lista = new ArrayList<>();
        if (pai == null) return lista;
        NodeList filhos = pai.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE && nomeLocal(n).equals(nome)) {
                lista.add((Element) n);
            }
        }
        return lista;
    }

    /** Busca em profundidade — usada só para elementos únicos (infNFe, infProt). */
    private static Element busca(Element raiz, String nome) {
        if (nomeLocal(raiz).equals(nome)) return raiz;
        NodeList filhos = raiz.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() != Node.ELEMENT_NODE) continue;
            Element achado = busca((Element) n, nome);
            if (achado != null) return achado;
        }
        return null;
    }

    /**
     * Filho único de um grupo de imposto. Necessário porque o nome muda com a
     * tributação (ICMS00 x ICMS40, PISAliq x PISOutr, ...).
     */
    private static Element unico(Element grupo) {
        if (grupo == null) return null;
        NodeList filhos = grupo.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE) return (Element) n;
        }
        return null;
    }

    /** Igual a {@link #unico}, mas pulando um filho conhecido (IPI/cEnq). */
    private static Element unicoIgnorando(Element grupo, String ignorar) {
        if (grupo == null) return null;
        NodeList filhos = grupo.getChildNodes();
        for (int i = 0; i < filhos.getLength(); i++) {
            Node n = filhos.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE && !nomeLocal(n).equals(ignorar)) {
                return (Element) n;
            }
        }
        return null;
    }

    private static String texto(Element e) {
        return e == null || e.getTextContent() == null ? "" : e.getTextContent().trim();
    }

    static String txt(Element pai, String nome) {
        return texto(filho(pai, nome));
    }

    static double num(Element pai, String nome) {
        return numero(txt(pai, nome));
    }

    private static double numero(String s) {
        if (s == null || s.trim().isEmpty()) return 0.0;
        try {
            return Double.parseDouble(s.trim().replace(",", "."));
        } catch (NumberFormatException e) {
            return 0.0;
        }
    }

    private static String atributo(Element e, String nome) {
        return e == null ? "" : e.getAttribute(nome);
    }

    /** "2026-03-13T09:31:45-03:00" ou "2026-03-13" -> "20260313". */
    private static String data(String iso) {
        if (iso == null || iso.length() < 10) return "";
        return iso.substring(0, 4) + iso.substring(5, 7) + iso.substring(8, 10);
    }

    /** "2026-03-13T09:31:45-03:00" -> "093145". */
    private static String hora(String iso) {
        if (iso == null || iso.length() < 19 || iso.charAt(10) != 'T') return "";
        return iso.substring(11, 13) + iso.substring(14, 16) + iso.substring(17, 19);
    }

    /**
     * CNPJ/CPF com máscara, venha o XML com pontuação ou sem (o schema fiscal
     * manda sem): a formatação de saída é do conversor, não do emitente — ver
     * {@link Documentos}. As letras do CNPJ alfanumérico são preservadas; só a
     * pontuação de origem é refeita.
     *
     * A comparação com os cadastros continua tolerante a formato, via
     * {@link CadastroProperties#chaveDocumento}, e o campo que não comporta a
     * máscara volta ao documento sem pontuação na hora de gravar
     * ({@link Documentos#mascararSeCouber}).
     */
    private static String documento(String s) {
        String limpo = SafxLinha.limpar(s);
        return limpo == null ? "" : Documentos.mascarar(limpo);
    }

    private static String soDigitos(String s) {
        if (s == null) return "";
        StringBuilder sb = new StringBuilder();
        for (char c : s.toCharArray()) {
            if (Character.isDigit(c)) sb.append(c);
        }
        return sb.toString();
    }
}
