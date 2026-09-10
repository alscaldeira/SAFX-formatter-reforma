using System;
using System.Globalization;
using System.Text;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Safx
{
    /// <summary>
    /// Formatação numérica dos campos SAFX: número sem separador decimal,
    /// preenchido com zeros à esquerda até a largura do campo. O que muda de
    /// campo para campo é a escala (quantas casas decimais estão implícitas no
    /// final do número) e a largura declarada no leiaute.
    ///
    /// Ex.: 13,00 com 6 casas -> "0000000013000000"; 18,00 com 4 casas -> "0000000000180000".
    /// </summary>
    internal static class SafxFormat
    {
        /// <summary>Largura padrão da família SAFX quando o leiaute não declara o tamanho.</summary>
        private const int Digitos = 16;

        internal static string Numero(double valor, int decimais)
        {
            return Numero(valor, decimais, Digitos);
        }

        /// <summary>
        /// Variante com largura explícita: o manual da Reforma declara tamanhos
        /// diferentes por campo (015V002 = 17 dígitos, 003V004 = 7, 011V004 = 15)
        /// e não uma largura única.
        /// </summary>
        internal static string Numero(double valor, int decimais, int digitos)
        {
            // largura = digitos + 1 por causa do ponto, que é removido em seguida
            string formatado = Arredondar(valor, decimais);
            string sinal = "";
            if (formatado.Length > 0 && formatado[0] == '-')
            {
                // o "%0" do Java preenche com zeros DEPOIS do sinal
                sinal = "-";
                formatado = formatado.Substring(1);
            }
            int largura = digitos + 1 - sinal.Length;
            if (formatado.Length < largura) formatado = formatado.PadLeft(largura, '0');
            formatado = sinal + formatado;

            formatado = formatado.Replace(".", "");
            if (formatado.Length != digitos)
            {
                // Estourar a largura desalinharia todos os campos seguintes do
                // registro; é melhor falhar dizendo qual valor não coube.
                throw new ArgumentException("Valor " + Texto(valor) + " não cabe em "
                    + digitos + " dígitos com " + decimais + " casas decimais (gerou "
                    + formatado.Length + ").");
            }
            return formatado;
        }

        /// <summary>
        /// Valor com N casas decimais, arredondando como o "%.Nf" do Java: sobre a
        /// representação decimal mais curta que identifica o double, com desempate
        /// meio-para-cima.
        ///
        /// Não dá para usar o ToString("F2") direto: no .NET Core o arredondamento
        /// passou a olhar o valor binário exato, de forma que 2,675 (que em binário
        /// é 2,67499...) sairia "2,67" onde o Java grava "2,68". Como o arquivo
        /// gerado aqui tem de bater com o do programa original, o caminho é
        /// converter para decimal — que preserva a representação curta — e
        /// arredondar meio-para-cima.
        /// </summary>
        private static string Arredondar(double valor, int decimais)
        {
            decimal exato;
            string arredondado;
            if (decimal.TryParse(Texto(valor), NumberStyles.Float, CultureInfo.InvariantCulture, out exato))
            {
                arredondado = decimal.Round(exato, decimais, MidpointRounding.AwayFromZero)
                    .ToString("F" + decimais, CultureInfo.InvariantCulture);
            }
            else
            {
                // fora da faixa do decimal (não acontece com valor fiscal): o valor
                // não caberia na largura do campo de qualquer forma, e o chamador avisa.
                arredondado = valor.ToString("F" + decimais, CultureInfo.InvariantCulture);
            }

            // O "%f" do Java tira o sinal do valor de entrada, não do resultado
            // arredondado: -0,004 com 2 casas sai "-0,00" e -0,0 sai "-0,0000".
            // O decimal do .NET não tem zero negativo, entao o sinal se perderia
            // justamente quando o valor negativo arredonda para zero na escala do
            // campo — um vDif/vDevTrib de centavo em campo de 2 casas. Recolocar o
            // sinal aqui mantém o arquivo igual ao do programa original.
            if (Negativo(valor) && arredondado[0] != '-')
            {
                arredondado = "-" + arredondado;
            }
            return arredondado;
        }

        /// <summary>Sinal pelo bit do double, para que -0,0 conte como negativo (como no Java).</summary>
        private static bool Negativo(double valor)
        {
            return !double.IsNaN(valor) && BitConverter.DoubleToInt64Bits(valor) < 0;
        }

        /// <summary>Representação decimal mais curta que identifica o double, como o Double.toString do Java.</summary>
        private static string Texto(double valor)
        {
            return valor.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
