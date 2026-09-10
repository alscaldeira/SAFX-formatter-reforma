using System.Text.RegularExpressions;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx
{
    /// <summary>Suporte de escrita do CSV equivalente ao SAFXnn.txt.</summary>
    internal static class SafxCsvSupport
    {
        private static readonly Regex SoDigitos = new Regex(@"^\d+$", RegexOptions.Compiled);

        /// <summary>
        /// Uma célula do CSV, já protegida contra a conversão automática do Excel.
        ///
        /// Campos como CNPJ, CPF, número de documento e os valores zerados à
        /// esquerda do SAFX são texto no leiaute, mas o Excel os lê como número:
        /// 07060718000112 viraria 7060718000112 (CNPJ inválido) e um valor de 17
        /// dígitos viraria notação científica. A fórmula ="..." força a leitura
        /// como texto sem alterar o conteúdo do campo.
        /// </summary>
        internal static string CelulaCsv(string valor)
        {
            if (valor == null) valor = "";
            if (NumeroQueOExcelDeturpa(valor))
            {
                return "\"=\"\"" + valor + "\"\"\"";
            }
            if (valor.Contains(";") || valor.Contains("\"") || valor.Contains("\n"))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }
            return valor;
        }

        /// <summary>
        /// Só dígitos e: com zero à esquerda, com cara de documento (CPF/CNPJ e
        /// códigos derivados deles, de 11 dígitos para cima) ou grande demais para
        /// o Excel (15 dígitos significativos).
        /// </summary>
        private static bool NumeroQueOExcelDeturpa(string valor)
        {
            if (valor.Length < 2 || !SoDigitos.IsMatch(valor)) return false;
            return valor[0] == '0' || valor.Length >= 11;
        }
    }
}
