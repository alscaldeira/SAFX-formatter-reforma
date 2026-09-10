using System.Text.RegularExpressions;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx
{
    /// <summary>
    /// Montagem da linha SAFX: campos separados por TAB. Cada arquivo SAFXnn.TXT
    /// contém apenas registros do seu próprio tipo — não há coluna de tipo de
    /// registro nem prefixo por linha.
    /// </summary>
    internal static class SafxLinha
    {
        private static readonly Regex EspacosRepetidos = new Regex(" {2,}", RegexOptions.Compiled);

        /// <summary>
        /// Neutraliza os caracteres que quebrariam o arquivo: o TAB é o delimitador
        /// de campo e o LF/CR encerra o registro. Texto vindo de XML (infCpl, xProd,
        /// xDescServ) chega com os dois.
        /// </summary>
        internal static string Limpar(string valor)
        {
            if (valor == null) return null;
            string limpo = valor.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            return Texto.Aparar(EspacosRepetidos.Replace(limpo, " "));
        }
    }
}
