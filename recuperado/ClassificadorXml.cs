using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ABMC.RANFE.LIGHT_SQL
{
    /// <summary>
    /// Tipo fiscal de um XML do lote, deduzido do proprio arquivo.
    ///
    /// O upload e um campo so: quem decide se um XML e NF-e ou CT-e e o conteudo
    /// dele, nao o campo do formulario de onde veio. Um campo por tipo dependia de
    /// o usuario acertar a separacao a mao, e quando ele errava (ou deixava um dos
    /// campos vazio) o controller recebia null e quebrava.
    /// </summary>
    public enum TipoXmlFiscal
    {
        /// <summary>XML ilegivel, ou fiscal mas de um tipo que este conversor nao exporta
        /// (evento, cancelamento, inutilizacao, NFS-e).</summary>
        Desconhecido = 0,
        NFe = 1,
        CTe = 2,
        /// <summary>NFS-e Nacional (raiz NFSe). Nao alimenta as abas vindas do banco,
        /// mas e fonte do SAFX3009 na conversao da Reforma.</summary>
        NFSe = 3
    }

    /// <summary>
    /// Separa o lote de XMLs por tipo de documento.
    ///
    /// O criterio e o modelo declarado em ide/mod — 55 e 65 sao NF-e/NFC-e, 57 e
    /// 67 sao CT-e/CT-e OS. Procurar a string "infCte" no texto bruto, como faz a
    /// carga do banco em Funcoes.CarregarArquivo1, acerta na maioria dos casos mas
    /// casa tambem com o XML de um evento de CT-e (que carrega a chave e as vezes o
    /// documento original embutido) e com qualquer CDATA que cite o nome do no.
    /// Esses arquivos entrariam no lote de CT-e e chegariam a exportacao sem os
    /// campos que ela espera.
    /// </summary>
    public static class ClassificadorXml
    {
        public static TipoXmlFiscal Classificar(string caminhoArquivo)
        {
            if (string.IsNullOrEmpty(caminhoArquivo) || !File.Exists(caminhoArquivo))
            {
                return TipoXmlFiscal.Desconhecido;
            }

            XDocument documento;

            try
            {
                documento = XDocument.Load(caminhoArquivo);
            }
            catch (Exception)
            {
                // XML quebrado nao para o lote: o arquivo fica de fora e os demais
                // seguem. A carga do banco ja registra o descarte em ParaAnalise.
                return TipoXmlFiscal.Desconhecido;
            }

            return Classificar(documento);
        }

        public static TipoXmlFiscal Classificar(XDocument documento)
        {
            if (documento == null || documento.Root == null)
            {
                return TipoXmlFiscal.Desconhecido;
            }

            // Comparacao por LocalName, ignorando o namespace: o mesmo documento
            // aparece com o namespace do portal fiscal, com o da Synchro, e as
            // vezes sem namespace nenhum depois de passar por um ERP.
            // NFS-e antes dos demais: nao tem grupo ide/mod, e identificada pela
            // raiz NFSe com o grupo infNFSe, como faz o XmlLeitor da Reforma.
            if (PrimeiroPorNome(documento, "infNFSe") != null)
            {
                return TipoXmlFiscal.NFSe;
            }

            // infCte antes de infNFe: o CT-e carrega infDoc/infNFe (as chaves das
            // NF-e transportadas), entao procurar infNFe primeiro classificava todo
            // CT-e com documento vinculado como NF-e. O inverso nao ocorre — uma
            // NF-e nunca contem infCte.
            XElement inf = PrimeiroPorNome(documento, "infCte") ?? PrimeiroPorNome(documento, "infNFe");

            if (inf == null)
            {
                return TipoXmlFiscal.Desconhecido;
            }

            // O modelo manda. O nome do no so entra como desempate porque ha XML
            // antigo, e XML remontado por ERP, que chega sem o grupo ide completo.
            switch (ModeloDeclarado(inf))
            {
                case "55":
                case "65":
                    return TipoXmlFiscal.NFe;

                case "57":
                case "67":
                    return TipoXmlFiscal.CTe;
            }

            return inf.Name.LocalName == "infCte" ? TipoXmlFiscal.CTe : TipoXmlFiscal.NFe;
        }

        /// <summary>
        /// Agrupa os arquivos de uma pasta pelo tipo. A pasta e varrida no primeiro
        /// nivel, igual a carga do banco, para que os dois enxerguem o mesmo lote.
        /// </summary>
        public static Dictionary<TipoXmlFiscal, List<string>> ClassificarPasta(string pasta)
        {
            Dictionary<TipoXmlFiscal, List<string>> porTipo = new Dictionary<TipoXmlFiscal, List<string>>
            {
                { TipoXmlFiscal.NFe, new List<string>() },
                { TipoXmlFiscal.CTe, new List<string>() },
                { TipoXmlFiscal.NFSe, new List<string>() },
                { TipoXmlFiscal.Desconhecido, new List<string>() }
            };

            if (string.IsNullOrEmpty(pasta) || !Directory.Exists(pasta))
            {
                return porTipo;
            }

            foreach (string arquivo in Directory.GetFiles(pasta, "*.xml", SearchOption.TopDirectoryOnly))
            {
                porTipo[Classificar(arquivo)].Add(arquivo);
            }

            return porTipo;
        }

        private static XElement PrimeiroPorNome(XDocument documento, string nomeLocal)
        {
            return documento.Descendants().FirstOrDefault(e => e.Name.LocalName == nomeLocal);
        }

        private static string ModeloDeclarado(XElement inf)
        {
            XElement mod = inf.Descendants().FirstOrDefault(e => e.Name.LocalName == "mod");

            return mod == null ? null : mod.Value.Trim();
        }
    }
}
