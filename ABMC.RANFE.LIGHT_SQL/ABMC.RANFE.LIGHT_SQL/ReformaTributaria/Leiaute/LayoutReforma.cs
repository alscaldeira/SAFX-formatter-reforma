using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Leiaute
{
    /// <summary>
    /// Descritor dos layouts da Reforma Tributária (SAFX3007/3008/3009), lido de
    /// "safx-reforma-layout.json".
    ///
    /// A conversão é guiada por NOME de campo, não por posição fixa no código: a
    /// ordem sai do descritor. Assim, quando o leiaute oficial do cliente mudar,
    /// basta editar o JSON — sem recompilar o servidor.
    /// </summary>
    internal static class LayoutReforma
    {
        internal const string NomeArquivo = "safx-reforma-layout.json";

        internal sealed class Campo
        {
            internal readonly string Nome;
            internal readonly string Origem;

            /// <summary>"N" para numérico, "D" para data, "A" para alfanumérico.</summary>
            internal readonly string Tipo;

            /// <summary>Total de dígitos do campo: "015V002" -> 17; "003V004" -> 7.</summary>
            internal readonly int Digitos;

            /// <summary>Casas decimais implícitas: "015V002" -> 2; -1 quando não é decimal.</summary>
            internal readonly int Decimais;

            internal Campo(string nome, string origem, string tipo, string tamanho)
            {
                Nome = nome;
                Origem = origem;
                Tipo = tipo;
                int v = tamanho == null ? -1 : tamanho.ToUpperInvariant().IndexOf('V');
                if (v > 0)
                {
                    int inteiros = int.Parse(Safx.Texto.Aparar(tamanho.Substring(0, v)), CultureInfo.InvariantCulture);
                    Decimais = int.Parse(Safx.Texto.Aparar(tamanho.Substring(v + 1)), CultureInfo.InvariantCulture);
                    Digitos = inteiros + Decimais;
                }
                else
                {
                    Decimais = -1;
                    Digitos = string.IsNullOrEmpty(tamanho) ? 0
                        : int.Parse(Safx.Texto.Aparar(tamanho), CultureInfo.InvariantCulture);
                }
            }

            internal bool Decimal()
            {
                return Decimais >= 0;
            }
        }

        internal sealed class Layout
        {
            internal readonly string Nome;
            internal readonly List<Campo> Campos;
            internal readonly bool OrdemConfirmada;
            internal readonly int FieldCountOficial;

            internal Layout(string nome, List<Campo> campos, bool ordemConfirmada, int fieldCountOficial)
            {
                Nome = nome;
                Campos = campos;
                OrdemConfirmada = ordemConfirmada;
                FieldCountOficial = fieldCountOficial;
            }
        }

        private static readonly object Trava = new object();
        private static Dictionary<string, Layout> _layouts;
        private static string _origemCache;
        private static long _modificadoEmCache;

        internal static Layout De(string nome)
        {
            lock (Trava)
            {
                // mesma invalidação dos cadastros: editar o descritor e converter de
                // novo tem de valer sem reciclar a aplicação
                string arquivo = ReformaConfig.ArquivoCadastro(NomeArquivo);
                long modificadoEm = File.Exists(arquivo) ? new FileInfo(arquivo).LastWriteTimeUtc.Ticks : -1;
                if (_layouts == null || _origemCache != arquivo || _modificadoEmCache != modificadoEm)
                {
                    _layouts = Carregar(arquivo);
                    _origemCache = arquivo;
                    _modificadoEmCache = modificadoEm;
                }
                Layout layout;
                if (!_layouts.TryGetValue(nome, out layout))
                {
                    throw new IOException("Layout " + nome + " não declarado em " + NomeArquivo);
                }
                return layout;
            }
        }

        private static Dictionary<string, Layout> Carregar(string arquivo)
        {
            if (!File.Exists(arquivo))
            {
                throw new FileNotFoundException("Arquivo " + NomeArquivo + " não encontrado.", arquivo);
            }
            return JsonMinimo.Layouts(File.ReadAllText(arquivo, Encoding.UTF8));
        }
    }
}
