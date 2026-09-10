using System.Collections.Generic;
using System.Data;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Saída da conversão da Reforma Tributária.
    ///
    /// Além dos arquivos gravados em disco (SAFX3007/3008/3009 .txt e .csv, como
    /// no programa original), cada layout volta como DataTable para que o
    /// HomeController acrescente as abas na mesma planilha dos demais SAFX — as
    /// colunas e os valores são exatamente os do arquivo posicional, inclusive o
    /// "@" de campo nulo.
    /// </summary>
    public sealed class ResultadoReforma
    {
        internal ResultadoReforma()
        {
            Avisos = new List<string>();
            Mensagens = new List<string>();
        }

        /// <summary>Capa do documento (vinculada ao SAFX07).</summary>
        public DataTable Safx3007 { get; internal set; }

        /// <summary>Item de mercadoria (vinculado ao SAFX08).</summary>
        public DataTable Safx3008 { get; internal set; }

        /// <summary>Item de serviço.</summary>
        public DataTable Safx3009 { get; internal set; }

        /// <summary>O que precisa de conferência humana (cadastro faltando, dado ausente no XML).</summary>
        public List<string> Avisos { get; private set; }

        /// <summary>Registro do que foi gerado, para log.</summary>
        public List<string> Mensagens { get; private set; }

        public DataTable Tabela(string layout)
        {
            if (layout == "SAFX3007") return Safx3007;
            if (layout == "SAFX3008") return Safx3008;
            if (layout == "SAFX3009") return Safx3009;
            return null;
        }
    }
}
