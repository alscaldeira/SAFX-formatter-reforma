namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx
{
    /// <summary>
    /// Operações de texto com a mesma definição do programa Java original.
    /// </summary>
    internal static class Texto
    {
        /// <summary>
        /// Equivalente ao String.trim() do Java: remove só os caracteres de código
        /// menor ou igual a U+0020 das duas pontas.
        ///
        /// O Trim() do .NET remove espaço Unicode, o que inclui o espaço
        /// inquebrável (U+00A0). Isso importa porque NBSP aparece de verdade em
        /// XML fiscal — ERP que monta xProd/cProd a partir de texto colado de
        /// editor ou de página web manda NBSP no meio e no fim do campo. Se o
        /// C# aparasse e o Java não, o mesmo item sairia com COD_PRODUTO
        /// diferente nos dois, e COD_PRODUTO é campo-chave do SAFX3008.
        /// </summary>
        internal static string Aparar(string valor)
        {
            if (valor == null) return null;
            int inicio = 0;
            int fim = valor.Length;
            while (inicio < fim && valor[inicio] <= ' ') inicio++;
            while (fim > inicio && valor[fim - 1] <= ' ') fim--;
            return inicio == 0 && fim == valor.Length ? valor : valor.Substring(inicio, fim - inicio);
        }
    }
}
