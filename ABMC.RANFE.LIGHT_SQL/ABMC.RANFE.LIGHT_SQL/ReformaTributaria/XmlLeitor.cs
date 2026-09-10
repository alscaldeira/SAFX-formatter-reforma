using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Lê uma pasta de XMLs fiscais (recursivamente) e devolve o modelo neutro
    /// <see cref="XmlNota"/>. Reconhece dois schemas:
    ///
    /// <list type="bullet">
    ///   <item>NF-e 4.00 — raiz nfeProc (ou NFe avulso);</item>
    ///   <item>NFS-e Nacional 1.01 — raiz NFSe.</item>
    /// </list>
    ///
    /// A navegação é por nome local, ignorando namespace: os dois schemas usam
    /// namespace default (elementos sem prefixo), então isso é suficiente e evita
    /// registrar URIs diferentes por versão de layout. O grupo Signature nunca é
    /// acessado.
    /// </summary>
    internal static class XmlLeitor
    {
        /// <summary>Aceita uma pasta (varrida recursivamente) ou um único arquivo XML.</summary>
        internal static List<XmlNota> LerPasta(string origem, List<string> avisos)
        {
            List<string> arquivos = new List<string>();

            if (Directory.Exists(origem))
            {
                foreach (string caminho in Directory.GetFiles(origem, "*", SearchOption.AllDirectories))
                {
                    if (caminho.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        arquivos.Add(caminho);
                    }
                }
                arquivos.Sort(StringComparer.Ordinal);
                if (arquivos.Count == 0)
                {
                    throw new IOException("Nenhum arquivo .xml encontrado em " + origem);
                }
            }
            else if (File.Exists(origem))
            {
                arquivos.Add(origem);
            }
            else
            {
                throw new IOException("Caminho não encontrado: " + origem);
            }

            List<XmlNota> notas = new List<XmlNota>();
            foreach (string arquivo in arquivos)
            {
                XmlNota nota = Ler(arquivo, avisos);
                if (nota != null)
                {
                    notas.Add(nota);
                }
                else
                {
                    avisos.Add("Arquivo ignorado (schema não reconhecido): " + Path.GetFileName(arquivo));
                }
            }
            if (notas.Count == 0)
            {
                throw new IOException("Nenhum XML de NF-e ou NFS-e reconhecido em " + origem);
            }
            return notas;
        }

        private static XmlNota Ler(string arquivo, List<string> avisos)
        {
            XmlElement raiz = RaizDe(arquivo);
            string tag = raiz.LocalName;

            if (tag == "nfeProc" || tag == "NFe")
            {
                return LerNfe(raiz, arquivo, avisos);
            }
            if (tag == "NFSe")
            {
                return LerNfse(raiz, arquivo);
            }
            return null;
        }

        private static XmlElement RaizDe(string arquivo)
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                // XML fiscal não usa DTD nem entidade externa; desligar evita XXE.
                settings.DtdProcessing = DtdProcessing.Prohibit;
                settings.XmlResolver = null;

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;
                using (XmlReader reader = XmlReader.Create(arquivo, settings))
                {
                    doc.Load(reader);
                }
                doc.Normalize();
                return doc.DocumentElement;
            }
            catch (Exception e)
            {
                throw new IOException("Falha ao ler o XML " + Path.GetFileName(arquivo)
                    + ": " + e.Message, e);
            }
        }

        // ------------------------------------------------------------------ NF-e

        private static XmlNota LerNfe(XmlElement raiz, string arquivo, List<string> avisos)
        {
            XmlElement infNFe = Busca(raiz, "infNFe");
            if (infNFe == null) return null;

            XmlNota nota = new XmlNota();
            nota.Origem = XmlNota.TipoOrigem.Nfe;
            nota.Arquivo = Path.GetFileName(arquivo);

            XmlElement ide = Filho(infNFe, "ide");
            XmlElement emit = Filho(infNFe, "emit");
            XmlElement dest = Filho(infNFe, "dest");

            nota.Chave = SoDigitos(Atributo(infNFe, "Id"));
            nota.DvChave = Txt(ide, "cDV");
            nota.NumDoc = Txt(ide, "nNF");
            nota.Serie = Txt(ide, "serie");
            nota.CodModelo = Txt(ide, "mod");
            nota.DtEmissao = Data(Txt(ide, "dhEmi"));
            nota.HoraEmissao = Hora(Txt(ide, "dhEmi"));
            nota.DtSaidaEnt = Data(Txt(ide, "dhSaiEnt"));
            if (nota.DtSaidaEnt.Length == 0) nota.DtSaidaEnt = nota.DtEmissao;
            nota.NatOp = Txt(ide, "natOp");
            nota.Finalidade = Txt(ide, "finNFe");
            nota.IndicadorConsumidorFinal = Txt(ide, "indFinal");
            nota.IndicadorPresenca = Txt(ide, "indPres");
            nota.IndicadorIntermediador = Txt(ide, "indIntermed");
            // NORM_DEV: finNFe 4 (devolução/retorno) -> "2" no domínio do SAFX
            nota.NormDev = nota.Finalidade == "4" ? "2" : "1";
            nota.CodDocto = "NFI";
            nota.CodClassDocFis = "1"; // mercadorias/produtos -> SAFX3008

            string cnpjEmit = Documento(Txt(emit, "CNPJ"));
            string cnpjDest = Documento(Txt(dest, "CNPJ"));
            bool saida = Txt(ide, "tpNF") == "1";

            if (saida)
            {
                nota.CnpjEstab = cnpjEmit;
                nota.MovtoEs = "9"; // Documento de Saída
                nota.Participante = ParticipanteDest(dest);
                nota.Participante.Papel = XmlNota.PapelCliente;
            }
            else
            {
                nota.CnpjEstab = cnpjDest;
                if (cnpjDest == cnpjEmit)
                {
                    // entrada emitida pelo próprio estabelecimento (ajuste, retorno, devolução)
                    nota.MovtoEs = "4"; // Entrada própria, outros motivos legais
                    nota.Participante = ParticipanteDest(dest);
                    nota.Participante.Papel = XmlNota.PapelEstabelecimento;
                }
                else
                {
                    nota.MovtoEs = "1"; // Entrada, documento de terceiros
                    nota.Participante = ParticipanteEmit(emit);
                    nota.Participante.Papel = XmlNota.PapelFornecedor;
                }
            }

            // Situação: só é cancelada/denegada quando o protocolo diz isso
            XmlElement infProt = Busca(raiz, "infProt");
            string cStat = infProt != null ? Txt(infProt, "cStat") : "";
            nota.Situacao = cStat.Length == 0 || cStat == "100" ? "N" : "S";
            if (infProt != null)
            {
                nota.DtAutenticacao = Data(Txt(infProt, "dhRecbto"));
                if (nota.Chave.Length == 0) nota.Chave = SoDigitos(Txt(infProt, "chNFe"));
            }

            // Documentos referenciados: o SAFX3007 comporta um só (campos 16/17)
            List<XmlElement> refs = new List<XmlElement>();
            foreach (XmlElement nfRef in Filhos(ide, "NFref"))
            {
                refs.AddRange(Filhos(nfRef, "refNFe"));
            }
            nota.QtdReferencias = refs.Count;

            // nota sem NFref é o caso comum (venda normal): não há chave referenciada
            string chaveRef = refs.Count == 0 ? "" : SoDigitos(TextoDe(refs[0]));
            nota.ChaveReferenciada = chaveRef;
            if (chaveRef.Length == 44)
            {
                // layout da chave: cUF(2) AAMM(4) CNPJ(14) mod(2) serie(3) nNF(9) ...
                nota.SerieDocRef = chaveRef.Substring(22, 3);
                nota.NumDocRef = chaveRef.Substring(25, 9);
            }

            if (refs.Count > 1)
            {
                avisos.Add(Path.GetFileName(arquivo) + ": " + refs.Count
                    + " documentos referenciados (refNFe) e o SAFX3007 comporta apenas um — "
                    + "campos 16/17 deixados em branco; a lista permanece no campo 150 (infCpl).");
            }

            XmlElement total = Filho(infNFe, "total");
            XmlElement icmsTot = total != null ? Filho(total, "ICMSTot") : null;
            if (icmsTot != null)
            {
                nota.VlProdutos = Num(icmsTot, "vProd");
                nota.VlTotal = Num(icmsTot, "vNF");
                nota.VlFrete = Num(icmsTot, "vFrete");
                nota.VlSeguro = Num(icmsTot, "vSeg");
                nota.VlOutras = Num(icmsTot, "vOutro");
                nota.VlDesconto = Num(icmsTot, "vDesc");
                nota.VlIcms = Num(icmsTot, "vICMS");
                nota.BaseIcms = Num(icmsTot, "vBC");
                nota.VlIpi = Num(icmsTot, "vIPI");
                nota.VlPis = Num(icmsTot, "vPIS");
                nota.VlCofins = Num(icmsTot, "vCOFINS");
                nota.VlIpiDevolvido = Num(icmsTot, "vIPIDevol");
            }

            XmlElement transp = Filho(infNFe, "transp");
            if (transp != null)
            {
                nota.IndTpFrete = Txt(transp, "modFrete");
                XmlElement transportadora = Filho(transp, "transporta");
                if (transportadora != null && Documento(Txt(transportadora, "CNPJ")).Length > 0)
                {
                    nota.Transportadora = ParticipanteTransportadora(transportadora);
                }
            }

            nota.IndFatura = IndFatura(infNFe);

            XmlElement infAdic = Filho(infNFe, "infAdic");
            if (infAdic != null)
            {
                nota.Observacao = Txt(infAdic, "infCpl");
            }

            foreach (XmlElement det in Filhos(infNFe, "det"))
            {
                nota.Itens.Add(LerItem(det, nota, avisos));
            }

            // a NF-e traz total do valor de PIS/COFINS/IPI, mas não da base: somar dos itens
            foreach (XmlItem item in nota.Itens)
            {
                nota.BasePis += item.BasePis;
                nota.BaseCofins += item.BaseCofins;
                nota.BaseIpi += item.BaseIpi;
            }
            if (nota.Itens.Count > 0)
            {
                XmlItem primeiro = nota.Itens[0];
                nota.AliqPis = primeiro.AliqPis;
                nota.AliqCofins = primeiro.AliqCofins;
                nota.Cfop = primeiro.Cfop;
            }

            XmlElement ibsCbsTot = total != null ? Filho(total, "IBSCBSTot") : null;
            if (ibsCbsTot != null)
            {
                XmlIbsCbs ibs = new XmlIbsCbs();
                ibs.BaseCalculo = Num(ibsCbsTot, "vBCIBSCBS");
                XmlElement gIbs = Filho(ibsCbsTot, "gIBS");
                if (gIbs != null)
                {
                    ibs.VlIbs = Num(gIbs, "vIBS");
                    ibs.VlCreditoPresumidoIbs = Num(gIbs, "vCredPres");
                    ibs.VlCreditoPresumidoCondSuspensaoIbs = Num(gIbs, "vCredPresCondSus");
                    XmlElement uf = Filho(gIbs, "gIBSUF");
                    if (uf != null)
                    {
                        ibs.VlIbsUf = Num(uf, "vIBSUF");
                        ibs.VlDiferimentoIbsUf = Num(uf, "vDif");
                        ibs.VlDevolucaoTributoIbsUf = Num(uf, "vDevTrib");
                    }
                    XmlElement mun = Filho(gIbs, "gIBSMun");
                    if (mun != null)
                    {
                        ibs.VlIbsMun = Num(mun, "vIBSMun");
                        ibs.VlDiferimentoIbsMun = Num(mun, "vDif");
                        ibs.VlDevolucaoTributoIbsMun = Num(mun, "vDevTrib");
                    }
                }
                XmlElement gCbs = Filho(ibsCbsTot, "gCBS");
                if (gCbs != null)
                {
                    ibs.VlCbs = Num(gCbs, "vCBS");
                    ibs.VlDiferimentoCbs = Num(gCbs, "vDif");
                    ibs.VlDevolucaoTributoCbs = Num(gCbs, "vDevTrib");
                    ibs.VlCreditoPresumidoCbs = Num(gCbs, "vCredPres");
                    ibs.VlCreditoPresumidoCondSuspensaoCbs = Num(gCbs, "vCredPresCondSus");
                }
                ibs.VlTotalDocumento = Num(total, "vNFTot");
                if (ibs.VlTotalDocumento == 0.0) ibs.VlTotalDocumento = nota.VlTotal;
                nota.IbsCbs = ibs;
            }

            return nota;
        }

        private static XmlItem LerItem(XmlElement det, XmlNota nota, List<string> avisos)
        {
            XmlItem item = new XmlItem();
            item.NumItem = (int)Numero(Atributo(det, "nItem"));

            XmlElement prod = Filho(det, "prod");
            item.CodProduto = Txt(prod, "cProd");
            item.Descricao = Txt(prod, "xProd");
            item.Ncm = Txt(prod, "NCM");
            item.ExTipi = Txt(prod, "EXTIPI");
            item.Cfop = Txt(prod, "CFOP");
            item.UnidadeComercial = Txt(prod, "uCom");
            item.UnidadeTributavel = Txt(prod, "uTrib");
            item.QtdComercial = Num(prod, "qCom");
            item.QtdTributavel = Num(prod, "qTrib");
            item.VlUnitario = Num(prod, "vUnCom");
            item.VlProduto = Num(prod, "vProd");
            item.VlDesconto = Num(prod, "vDesc");
            item.VlFrete = Num(prod, "vFrete");
            item.VlSeguro = Num(prod, "vSeg");
            item.VlOutras = Num(prod, "vOutro");
            item.VlContabil = Num(det, "vItem");
            if (item.VlContabil == 0.0) item.VlContabil = item.VlProduto;

            if (item.QtdTributavel > 0 && Math.Abs(item.QtdComercial - item.QtdTributavel) > 0.000001)
            {
                avisos.Add(nota.Arquivo + " item " + item.NumItem + ": qCom (" + Decimal(item.QtdComercial)
                    + ") difere de qTrib (" + Decimal(item.QtdTributavel)
                    + ") — unidades comercial e tributável não são equivalentes, confira a conversão.");
            }

            XmlElement imposto = Filho(det, "imposto");
            if (imposto != null)
            {
                XmlElement icms = Unico(Filho(imposto, "ICMS"));
                if (icms != null)
                {
                    item.CstIcms = Txt(icms, "CST");
                    item.OrigemIcms = Txt(icms, "orig");
                    item.BaseIcms = Num(icms, "vBC");
                    item.AliqIcms = Num(icms, "pICMS");
                    item.VlIcms = Num(icms, "vICMS");
                }
                XmlElement grupoIpi = Filho(imposto, "IPI");
                if (grupoIpi != null)
                {
                    item.CodEnquadramentoIpi = Txt(grupoIpi, "cEnq");
                    XmlElement ipi = UnicoIgnorando(grupoIpi, "cEnq");
                    if (ipi != null)
                    {
                        item.CstIpi = Txt(ipi, "CST");
                        item.BaseIpi = Num(ipi, "vBC");
                        item.AliqIpi = Num(ipi, "pIPI");
                        item.VlIpi = Num(ipi, "vIPI");
                    }
                }
                XmlElement pis = Unico(Filho(imposto, "PIS"));
                if (pis != null)
                {
                    item.CstPis = Txt(pis, "CST");
                    item.BasePis = Num(pis, "vBC");
                    item.AliqPis = Num(pis, "pPIS");
                    item.VlPis = Num(pis, "vPIS");
                }
                XmlElement cofins = Unico(Filho(imposto, "COFINS"));
                if (cofins != null)
                {
                    item.CstCofins = Txt(cofins, "CST");
                    item.BaseCofins = Num(cofins, "vBC");
                    item.AliqCofins = Num(cofins, "pCOFINS");
                    item.VlCofins = Num(cofins, "vCOFINS");
                }
                XmlElement ii = Filho(imposto, "II");
                if (ii != null)
                {
                    item.BaseIi = Num(ii, "vBC");
                    item.VlIi = Num(ii, "vII");
                    item.VlDespesasAduaneiras = Num(ii, "vDespAdu");
                    item.VlIof = Num(ii, "vIOF");
                }
                XmlElement ibsCbs = Filho(imposto, "IBSCBS");
                if (ibsCbs != null)
                {
                    item.IbsCbs = LerIbsCbsItem(ibsCbs);
                }
            }

            XmlElement di = prod != null ? Filho(prod, "DI") : null;
            if (di != null)
            {
                XmlDi importacao = new XmlDi();
                importacao.Numero = Txt(di, "nDI");
                importacao.DataRegistro = Data(Txt(di, "dDI"));
                importacao.DataDesembaraco = Data(Txt(di, "dDesemb"));
                importacao.UfDesembaraco = Txt(di, "UFDesemb");
                importacao.LocalDesembaraco = Txt(di, "xLocDesemb");
                importacao.CodExportador = Txt(di, "cExportador");
                importacao.TipoViaTransporte = Txt(di, "tpViaTransp");
                importacao.TipoIntermedio = Txt(di, "tpIntermedio");
                importacao.VlAfrmm = Num(di, "vAFRMM");
                XmlElement adi = Filho(di, "adi");
                if (adi != null)
                {
                    importacao.NumeroAdicao = Txt(adi, "nAdicao");
                    importacao.NumeroDrawback = Txt(adi, "nDraw");
                    importacao.CodFabricante = Txt(adi, "cFabricante");
                    importacao.VlDescontoAdicao = Num(adi, "vDescDI");
                }
                item.Di = importacao;
            }

            return item;
        }

        private static XmlIbsCbs LerIbsCbsItem(XmlElement ibsCbs)
        {
            XmlIbsCbs ibs = new XmlIbsCbs();
            ibs.Cst = Txt(ibsCbs, "CST");
            ibs.ClassificacaoTributaria = Txt(ibsCbs, "cClassTrib");
            XmlElement grupo = Filho(ibsCbs, "gIBSCBS");
            if (grupo != null)
            {
                ibs.BaseCalculo = Num(grupo, "vBC");
                ibs.VlIbs = Num(grupo, "vIBS");
                XmlElement uf = Filho(grupo, "gIBSUF");
                if (uf != null)
                {
                    ibs.AliqIbsUf = Num(uf, "pIBSUF");
                    ibs.VlIbsUf = Num(uf, "vIBSUF");
                }
                XmlElement mun = Filho(grupo, "gIBSMun");
                if (mun != null)
                {
                    ibs.AliqIbsMun = Num(mun, "pIBSMun");
                    ibs.VlIbsMun = Num(mun, "vIBSMun");
                }
                XmlElement cbs = Filho(grupo, "gCBS");
                if (cbs != null)
                {
                    ibs.AliqCbs = Num(cbs, "pCBS");
                    ibs.VlCbs = Num(cbs, "vCBS");
                }
            }
            return ibs;
        }

        private static string IndFatura(XmlElement infNFe)
        {
            XmlElement pag = Filho(infNFe, "pag");
            XmlElement detPag = pag != null ? Filho(pag, "detPag") : null;
            string tPag = detPag != null ? Txt(detPag, "tPag") : "";
            if (tPag == "90") return "3"; // sem pagamento
            XmlElement cobr = Filho(infNFe, "cobr");
            if (cobr != null && Filho(cobr, "dup") != null) return "2"; // a prazo
            if (tPag.Length > 0) return "1"; // a vista
            return null;
        }

        // ----------------------------------------------------------------- NFS-e

        private static XmlNota LerNfse(XmlElement raiz, string arquivo)
        {
            XmlElement infNFSe = Filho(raiz, "infNFSe");
            if (infNFSe == null) return null;

            XmlNota nota = new XmlNota();
            nota.Origem = XmlNota.TipoOrigem.Nfse;
            nota.Arquivo = Path.GetFileName(arquivo);

            XmlElement dps = Filho(infNFSe, "DPS");
            XmlElement infDPS = dps != null ? Filho(dps, "infDPS") : null;
            XmlElement prestador = Filho(infNFSe, "emit");
            XmlElement tomador = infDPS != null ? Filho(infDPS, "toma") : null;

            // A empresa é a tomadora do serviço: entrada de documento de terceiro.
            nota.CnpjEstab = tomador != null ? Documento(Txt(tomador, "CNPJ")) : "";
            nota.MovtoEs = "1";
            nota.NormDev = "1";
            nota.CodDocto = "NFS";
            nota.CodClassDocFis = "2"; // serviços -> SAFX3009
            nota.Situacao = Txt(infNFSe, "cStat") == "100" ? "N" : "S";

            nota.Participante = ParticipanteNfse(prestador, infDPS);
            nota.Participante.Papel = XmlNota.PapelFornecedor;

            nota.NumDoc = Txt(infNFSe, "nNFSe");
            nota.Chave = SoDigitos(Atributo(infNFSe, "Id"));
            nota.CodVerificacao = Atributo(infNFSe, "Id");
            nota.DtAutenticacao = Data(Txt(infNFSe, "dhProc"));

            if (infDPS != null)
            {
                nota.Serie = Txt(infDPS, "serie");
                nota.DtEmissao = Data(Txt(infDPS, "dhEmi"));
                nota.HoraEmissao = Hora(Txt(infDPS, "dhEmi"));
                nota.DtSaidaEnt = Data(Txt(infDPS, "dCompet"));
                if (nota.DtSaidaEnt.Length == 0) nota.DtSaidaEnt = nota.DtEmissao;
                nota.NumRps = Txt(infDPS, "nDPS");
                nota.SerieRps = Txt(infDPS, "serie");
                nota.DtRps = nota.DtEmissao;

                XmlElement serv = Filho(infDPS, "serv");
                if (serv != null)
                {
                    XmlElement cServ = Filho(serv, "cServ");
                    if (cServ != null)
                    {
                        nota.DescricaoServico = Txt(cServ, "xDescServ");
                        nota.CodTributacaoNacional = Txt(cServ, "cTribNac");
                    }
                }

                XmlElement valores = Filho(infDPS, "valores");
                if (valores != null)
                {
                    XmlElement vServPrest = Filho(valores, "vServPrest");
                    if (vServPrest != null) nota.VlServico = Num(vServPrest, "vServ");
                    XmlElement trib = Filho(valores, "trib");
                    XmlElement tribFed = trib != null ? Filho(trib, "tribFed") : null;
                    if (tribFed != null)
                    {
                        XmlElement pisCofins = Filho(tribFed, "piscofins");
                        if (pisCofins != null)
                        {
                            nota.BasePis = Num(pisCofins, "vBCPisCofins");
                            nota.BaseCofins = nota.BasePis;
                            nota.AliqPis = Num(pisCofins, "pAliqPis");
                            nota.AliqCofins = Num(pisCofins, "pAliqCofins");
                            nota.VlPis = Num(pisCofins, "vPis");
                            nota.VlCofins = Num(pisCofins, "vCofins");
                        }
                        nota.VlIr = Num(tribFed, "vRetIRRF");
                        nota.VlCsll = Num(tribFed, "vRetCSLL");
                        nota.VlInss = Num(tribFed, "vRetCP");
                    }
                }
            }

            XmlElement valoresNfse = Filho(infNFSe, "valores");
            if (valoresNfse != null)
            {
                nota.BaseIss = Num(valoresNfse, "vBC");
                nota.AliqIss = Num(valoresNfse, "pAliqAplic");
                nota.VlIss = Num(valoresNfse, "vISSQN");
                nota.VlIssRetido = Num(valoresNfse, "vTotalRet");
                nota.VlTotal = Num(valoresNfse, "vLiq");
            }
            if (nota.VlServico == 0.0) nota.VlServico = nota.BaseIss;
            if (nota.VlTotal == 0.0) nota.VlTotal = nota.VlServico;
            nota.CodMunicipioIss = Txt(infNFSe, "cLocIncid");

            XmlElement ibsCbs = Filho(infNFSe, "IBSCBS");
            if (ibsCbs != null)
            {
                XmlIbsCbs ibs = new XmlIbsCbs();
                ibs.CodLocalidadeIncidencia = Txt(ibsCbs, "cLocalidadeIncid");
                ibs.NomeLocalidadeIncidencia = Txt(ibsCbs, "xLocalidadeIncid");
                XmlElement valores = Filho(ibsCbs, "valores");
                if (valores != null)
                {
                    ibs.BaseCalculo = Num(valores, "vBC");
                    XmlElement uf = Filho(valores, "uf");
                    if (uf != null)
                    {
                        ibs.AliqIbsUf = Num(uf, "pIBSUF");
                        ibs.AliqReducaoIbsUf = Num(uf, "pRedAliqUF");
                        ibs.AliqEfetivaIbsUf = Num(uf, "pAliqEfetUF");
                    }
                    XmlElement mun = Filho(valores, "mun");
                    if (mun != null)
                    {
                        ibs.AliqIbsMun = Num(mun, "pIBSMun");
                        ibs.AliqReducaoIbsMun = Num(mun, "pRedAliqMun");
                        ibs.AliqEfetivaIbsMun = Num(mun, "pAliqEfetMun");
                    }
                    XmlElement fed = Filho(valores, "fed");
                    if (fed != null)
                    {
                        ibs.AliqCbs = Num(fed, "pCBS");
                        ibs.AliqReducaoCbs = Num(fed, "pRedAliqCBS");
                        ibs.AliqEfetivaCbs = Num(fed, "pAliqEfetCBS");
                    }
                }
                XmlElement tot = Filho(ibsCbs, "totCIBS");
                if (tot != null)
                {
                    ibs.VlTotalDocumento = Num(tot, "vTotNF");
                    XmlElement gIbs = Filho(tot, "gIBS");
                    if (gIbs != null)
                    {
                        ibs.VlIbs = Num(gIbs, "vIBSTot");
                        XmlElement ufTot = Filho(gIbs, "gIBSUFTot");
                        if (ufTot != null) ibs.VlIbsUf = Num(ufTot, "vIBSUF");
                        XmlElement munTot = Filho(gIbs, "gIBSMunTot");
                        if (munTot != null) ibs.VlIbsMun = Num(munTot, "vIBSMun");
                    }
                    XmlElement gCbs = Filho(tot, "gCBS");
                    if (gCbs != null) ibs.VlCbs = Num(gCbs, "vCBS");
                }
                // grupo IBSCBS do DPS: finalidade, indicador de operação e CST
                XmlElement ibsDps = infDPS != null ? Filho(infDPS, "IBSCBS") : null;
                if (ibsDps != null)
                {
                    ibs.FinalidadeNfse = Txt(ibsDps, "finNFSe");
                    ibs.CodIndicadorOperacao = Txt(ibsDps, "cIndOp");
                    ibs.IndicadorDestinatario = Txt(ibsDps, "indDest");
                    XmlElement valoresDps = Filho(ibsDps, "valores");
                    XmlElement trib = valoresDps != null ? Filho(valoresDps, "trib") : null;
                    XmlElement grupo = trib != null ? Filho(trib, "gIBSCBS") : null;
                    if (grupo != null)
                    {
                        ibs.Cst = Txt(grupo, "CST");
                        ibs.ClassificacaoTributaria = Txt(grupo, "cClassTrib");
                    }
                }
                nota.IbsCbs = ibs;
            }

            return nota;
        }

        // --------------------------------------------------------- participantes

        private static XmlParticipante ParticipanteDest(XmlElement dest)
        {
            XmlParticipante p = new XmlParticipante();
            if (dest == null) return p;
            p.Cnpj = Documento(Txt(dest, "CNPJ"));
            p.Cpf = Documento(Txt(dest, "CPF"));
            p.IdEstrangeiro = Txt(dest, "idEstrangeiro");
            p.Nome = Txt(dest, "xNome");
            p.NomeFantasia = p.Nome;
            p.Ie = Txt(dest, "IE");
            p.IndContribuinteIcms = Txt(dest, "indIEDest");
            Endereco(p, Filho(dest, "enderDest"));
            return p;
        }

        private static XmlParticipante ParticipanteEmit(XmlElement emit)
        {
            XmlParticipante p = new XmlParticipante();
            if (emit == null) return p;
            p.Cnpj = Documento(Txt(emit, "CNPJ"));
            p.Cpf = Documento(Txt(emit, "CPF"));
            p.Nome = Txt(emit, "xNome");
            p.NomeFantasia = Txt(emit, "xFant");
            if (p.NomeFantasia.Length == 0) p.NomeFantasia = p.Nome;
            p.Ie = Txt(emit, "IE");
            Endereco(p, Filho(emit, "enderEmit"));
            return p;
        }

        private static XmlParticipante ParticipanteTransportadora(XmlElement transporta)
        {
            XmlParticipante p = new XmlParticipante();
            p.Cnpj = Documento(Txt(transporta, "CNPJ"));
            p.Cpf = Documento(Txt(transporta, "CPF"));
            p.Nome = Txt(transporta, "xNome");
            p.NomeFantasia = p.Nome;
            p.Ie = Txt(transporta, "IE");
            p.Logradouro = Txt(transporta, "xEnder");
            p.Municipio = Txt(transporta, "xMun");
            p.Uf = Txt(transporta, "UF");
            p.Papel = XmlNota.PapelTransportadora;
            return p;
        }

        private static XmlParticipante ParticipanteNfse(XmlElement prestador, XmlElement infDPS)
        {
            XmlParticipante p = new XmlParticipante();
            if (prestador != null)
            {
                p.Cnpj = Documento(Txt(prestador, "CNPJ"));
                p.Cpf = Documento(Txt(prestador, "CPF"));
                p.Nome = Txt(prestador, "xNome");
                p.NomeFantasia = p.Nome;
                p.Im = Txt(prestador, "IM");
                p.Email = Txt(prestador, "email");
                p.Fone = Txt(prestador, "fone");
                XmlElement ender = Filho(prestador, "enderNac");
                if (ender != null)
                {
                    p.Logradouro = Txt(ender, "xLgr");
                    p.Numero = Txt(ender, "nro");
                    p.Complemento = Txt(ender, "xCpl");
                    p.Bairro = Txt(ender, "xBairro");
                    p.CodMunicipio = Txt(ender, "cMun");
                    p.Uf = Txt(ender, "UF");
                    p.Cep = Txt(ender, "CEP");
                    // enderNac é, por definição, endereço nacional: a NFS-e não traz
                    // cPais, mas o COD_PAIS é Brasil (BACEN 1058)
                    p.CodPais = "1058";
                }
            }
            if (infDPS != null)
            {
                XmlElement prest = Filho(infDPS, "prest");
                XmlElement regTrib = prest != null ? Filho(prest, "regTrib") : null;
                if (regTrib != null)
                {
                    p.SimplesNacional = Txt(regTrib, "opSimpNac") == "1";
                }
            }
            return p;
        }

        private static void Endereco(XmlParticipante p, XmlElement ender)
        {
            if (ender == null) return;
            p.Logradouro = Txt(ender, "xLgr");
            p.Numero = Txt(ender, "nro");
            p.Complemento = Txt(ender, "xCpl");
            p.Bairro = Txt(ender, "xBairro");
            p.Municipio = Txt(ender, "xMun");
            p.CodMunicipio = Txt(ender, "cMun");
            p.Uf = Txt(ender, "UF");
            p.Cep = Txt(ender, "CEP");
            p.CodPais = Txt(ender, "cPais");
            p.Fone = Txt(ender, "fone");
        }

        // ------------------------------------------------------------ utilidades

        /// <summary>Primeiro filho direto com o nome informado.</summary>
        internal static XmlElement Filho(XmlElement pai, string nome)
        {
            if (pai == null) return null;
            foreach (XmlNode n in pai.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element && n.LocalName == nome)
                {
                    return (XmlElement)n;
                }
            }
            return null;
        }

        internal static List<XmlElement> Filhos(XmlElement pai, string nome)
        {
            List<XmlElement> lista = new List<XmlElement>();
            if (pai == null) return lista;
            foreach (XmlNode n in pai.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element && n.LocalName == nome)
                {
                    lista.Add((XmlElement)n);
                }
            }
            return lista;
        }

        /// <summary>Busca em profundidade — usada só para elementos únicos (infNFe, infProt).</summary>
        private static XmlElement Busca(XmlElement raiz, string nome)
        {
            if (raiz.LocalName == nome) return raiz;
            foreach (XmlNode n in raiz.ChildNodes)
            {
                if (n.NodeType != XmlNodeType.Element) continue;
                XmlElement achado = Busca((XmlElement)n, nome);
                if (achado != null) return achado;
            }
            return null;
        }

        /// <summary>
        /// Filho único de um grupo de imposto. Necessário porque o nome muda com a
        /// tributação (ICMS00 x ICMS40, PISAliq x PISOutr, ...).
        /// </summary>
        private static XmlElement Unico(XmlElement grupo)
        {
            if (grupo == null) return null;
            foreach (XmlNode n in grupo.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element) return (XmlElement)n;
            }
            return null;
        }

        /// <summary>Igual a <see cref="Unico"/>, mas pulando um filho conhecido (IPI/cEnq).</summary>
        private static XmlElement UnicoIgnorando(XmlElement grupo, string ignorar)
        {
            if (grupo == null) return null;
            foreach (XmlNode n in grupo.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element && n.LocalName != ignorar)
                {
                    return (XmlElement)n;
                }
            }
            return null;
        }

        private static string TextoDe(XmlElement e)
        {
            return e == null || e.InnerText == null ? "" : Texto.Aparar(e.InnerText);
        }

        internal static string Txt(XmlElement pai, string nome)
        {
            return TextoDe(Filho(pai, nome));
        }

        internal static double Num(XmlElement pai, string nome)
        {
            return Numero(Txt(pai, nome));
        }

        private static double Numero(string s)
        {
            if (s == null || Texto.Aparar(s).Length == 0) return 0.0;
            double v;
            return double.TryParse(Texto.Aparar(s).Replace(",", "."), NumberStyles.Float,
                CultureInfo.InvariantCulture, out v) ? v : 0.0;
        }

        /// <summary>Número como o Double.toString do Java o escreve, para as mensagens de aviso.</summary>
        private static string Decimal(double v)
        {
            string texto = v.ToString("R", CultureInfo.InvariantCulture);
            return texto.IndexOf('.') < 0 && texto.IndexOf('E') < 0 ? texto + ".0" : texto;
        }

        private static string Atributo(XmlElement e, string nome)
        {
            return e == null ? "" : e.GetAttribute(nome);
        }

        /// <summary>"2026-03-13T09:31:45-03:00" ou "2026-03-13" -> "20260313".</summary>
        private static string Data(string iso)
        {
            if (iso == null || iso.Length < 10) return "";
            return iso.Substring(0, 4) + iso.Substring(5, 2) + iso.Substring(8, 2);
        }

        /// <summary>"2026-03-13T09:31:45-03:00" -> "093145".</summary>
        private static string Hora(string iso)
        {
            if (iso == null || iso.Length < 19 || iso[10] != 'T') return "";
            return iso.Substring(11, 2) + iso.Substring(14, 2) + iso.Substring(17, 2);
        }

        /// <summary>
        /// CNPJ/CPF com máscara, venha o XML com pontuação ou sem (o schema fiscal
        /// manda sem): a formatação de saída é do conversor, não do emitente.
        /// </summary>
        private static string Documento(string s)
        {
            string limpo = SafxLinha.Limpar(s);
            return limpo == null ? "" : Documentos.Mascarar(limpo);
        }

        private static string SoDigitos(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (char.IsDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
