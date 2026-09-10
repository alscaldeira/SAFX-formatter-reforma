using System.Collections.Generic;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo
{
    /// <summary>
    /// Modelo neutro de um documento fiscal lido de XML — serve tanto para NF-e
    /// (modelo 55) quanto para NFS-e nacional. É deliberadamente independente do
    /// layout SAFX: o XmlLeitor preenche, o XmlToSafxReforma lê.
    ///
    /// As datas já chegam aqui no formato SAFX (YYYYMMDD) e as horas em HHMMSS.
    /// </summary>
    internal sealed class XmlNota
    {
        internal enum TipoOrigem { Nfe, Nfse }

        /// <summary>Papéis do domínio de IND_FIS_JUR (SAFX3007 campo 7).</summary>
        internal const string PapelFornecedor = "1";
        internal const string PapelCliente = "2";
        internal const string PapelEstabelecimento = "3";
        internal const string PapelTransportadora = "4";
        internal const string PapelMisto = "5";

        internal TipoOrigem Origem;
        internal string Arquivo;

        /// <summary>CNPJ do estabelecimento da empresa (de-para em Estabelecimentos).</summary>
        internal string CnpjEstab;

        /// <summary>Contraparte do documento — cliente, fornecedor ou o próprio estabelecimento.</summary>
        internal XmlParticipante Participante;

        /// <summary>Transportadora, quando o XML traz o grupo transp/transporta.</summary>
        internal XmlParticipante Transportadora;

        // --- identificação da capa ---
        internal string MovtoEs;
        internal string NormDev;
        internal string CodDocto;
        internal string CodClassDocFis;
        internal string CodModelo;
        internal string Situacao;
        internal string NumDoc;
        internal string Serie;
        internal string Chave;
        internal string DvChave;
        internal string DtEmissao;
        internal string HoraEmissao;
        internal string DtSaidaEnt;
        internal string DtAutenticacao;
        internal string NatOp;
        internal string Cfop;
        internal string IndTpFrete;
        internal string IndFatura;
        internal string Finalidade;
        internal string Observacao;

        /// <summary>ide/indFinal, indPres e indIntermed — usados pelos layouts da Reforma.</summary>
        internal string IndicadorConsumidorFinal;
        internal string IndicadorPresenca;
        internal string IndicadorIntermediador;

        /// <summary>Chave do DF-e referenciado, quando existe exatamente uma.</summary>
        internal string ChaveReferenciada;

        /// <summary>Documento referenciado: só preenchido quando existe exatamente um.</summary>
        internal string NumDocRef;
        internal string SerieDocRef;

        /// <summary>Quantidade de refNFe encontradas (para o aviso quando for maior que 1).</summary>
        internal int QtdReferencias;

        // --- valores da capa ---
        internal double VlProdutos;
        internal double VlTotal;
        internal double VlFrete;
        internal double VlSeguro;
        internal double VlOutras;
        internal double VlDesconto;
        internal double VlIcms;
        internal double BaseIcms;
        internal double VlIpi;
        internal double BaseIpi;
        internal double VlPis;
        internal double BasePis;
        internal double VlCofins;
        internal double BaseCofins;
        internal double AliqPis;
        internal double AliqCofins;
        internal double VlIpiDevolvido;

        // --- valores de serviço (NFS-e) ---
        internal double VlServico;
        internal double BaseIss;
        internal double AliqIss;
        internal double VlIss;
        internal double VlIssRetido;
        internal double VlIr;
        internal double VlCsll;
        internal double VlInss;
        internal string CodMunicipioIss;
        internal string NumRps;
        internal string SerieRps;
        internal string DtRps;
        internal string DescricaoServico;
        internal string CodVerificacao;
        internal string CodTributacaoNacional;

        /// <summary>Grupo IBS/CBS da Reforma Tributária (capa).</summary>
        internal XmlIbsCbs IbsCbs;

        internal readonly List<XmlItem> Itens = new List<XmlItem>();

        internal bool TemImportacao()
        {
            foreach (XmlItem item in Itens)
            {
                if (item.Di != null) return true;
            }
            return false;
        }
    }
}
