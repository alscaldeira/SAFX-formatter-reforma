using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros
{
    /// <summary>
    /// Carregamento dos arquivos de de-para usados pelos conversores
    /// (estabelecimentos, participantes, unidades).
    ///
    /// O conteúdo fica em cache, mas o cache é invalidado quando o arquivo muda
    /// (caminho, data de modificação ou tamanho). Sem isso, quem recebesse
    /// "estabelecimento não cadastrado", corrigisse o .properties e convertesse
    /// de novo continuaria vendo o erro até reciclar a aplicação — o processo do
    /// servidor é longo e a conversão roda muitas vezes nele.
    /// </summary>
    internal static class CadastroProperties
    {
        private sealed class Cache
        {
            internal readonly Dictionary<string, string> Dados;
            internal readonly string Origem;
            internal readonly long ModificadoEm;
            internal readonly long Tamanho;

            internal Cache(Dictionary<string, string> dados, string origem, long modificadoEm, long tamanho)
            {
                Dados = dados;
                Origem = origem;
                ModificadoEm = modificadoEm;
                Tamanho = tamanho;
            }
        }

        private static readonly Dictionary<string, Cache> Caches = new Dictionary<string, Cache>();
        private static readonly object Trava = new object();

        /// <param name="obrigatorio">quando true, a ausência do arquivo é erro;
        /// quando false, devolve um cadastro vazio.</param>
        internal static Dictionary<string, string> Carregar(string nomeArquivo, bool obrigatorio)
        {
            lock (Trava)
            {
                string arquivo = ReformaConfig.ArquivoCadastro(nomeArquivo);
                bool existe = File.Exists(arquivo);
                long modificadoEm = -1;
                long tamanho = -1;
                if (existe)
                {
                    FileInfo info = new FileInfo(arquivo);
                    modificadoEm = info.LastWriteTimeUtc.Ticks;
                    tamanho = info.Length;
                }

                Cache cache;
                if (Caches.TryGetValue(nomeArquivo, out cache) && cache.Origem == arquivo
                    && cache.ModificadoEm == modificadoEm && cache.Tamanho == tamanho)
                {
                    return cache.Dados;
                }

                Dictionary<string, string> carregado = Ler(nomeArquivo, obrigatorio, arquivo, existe);
                Caches[nomeArquivo] = new Cache(carregado, arquivo, modificadoEm, tamanho);
                return carregado;
            }
        }

        private static Dictionary<string, string> Ler(string nomeArquivo, bool obrigatorio,
                                                     string arquivo, bool existe)
        {
            if (!existe)
            {
                if (obrigatorio)
                {
                    throw new FileNotFoundException("Arquivo " + nomeArquivo + " não encontrado.", arquivo);
                }
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            Dictionary<string, string> carregado = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> par in Propriedades(File.ReadAllLines(arquivo, Encoding.UTF8)))
            {
                string chave = Safx.Texto.Aparar(par.Key);
                string valor = Safx.Texto.Aparar(par.Value);
                if (chave.Length > 0 && valor.Length > 0)
                {
                    carregado[chave] = valor;
                }
            }
            return carregado;
        }

        /// <summary>
        /// Equivalente ao java.util.Properties.load para o subconjunto que os
        /// cadastros usam: comentário com # ou !, separador = ou :, continuação
        /// de linha com "\" no fim e escapes \n \t \r \uXXXX.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, string>> Propriedades(string[] linhas)
        {
            List<KeyValuePair<string, string>> resultado = new List<KeyValuePair<string, string>>();
            string acumulado = null;

            foreach (string bruta in linhas)
            {
                string linha = acumulado == null ? bruta.TrimStart() : bruta.TrimStart();
                if (acumulado == null)
                {
                    if (linha.Length == 0) continue;
                    char inicio = linha[0];
                    if (inicio == '#' || inicio == '!') continue;
                }

                if (ContinuaNaProximaLinha(linha))
                {
                    acumulado = (acumulado ?? "") + linha.Substring(0, linha.Length - 1);
                    continue;
                }

                string completa = (acumulado ?? "") + linha;
                acumulado = null;

                resultado.Add(Separar(completa));
            }

            return resultado;
        }

        /// <summary>Fim de linha em "\" só continua quando a barra não está ela mesma escapada.</summary>
        private static bool ContinuaNaProximaLinha(string linha)
        {
            int barras = 0;
            for (int i = linha.Length - 1; i >= 0 && linha[i] == '\\'; i--) barras++;
            return barras % 2 == 1;
        }

        /// <summary>
        /// Separa chave e valor como o java.util.Properties: a chave termina no
        /// primeiro '=', ':' OU BRANCO nao escapado; depois dela pulam-se os
        /// brancos, um '=' ou ':' opcional, e os brancos seguintes.
        ///
        /// O branco como separador nao e detalhe teorico: quem cadastra um
        /// estabelecimento escrevendo "42105890000901 002/0001" tem um cadastro
        /// valido para o programa original. Se aqui isso virasse uma chave unica
        /// "42105890000901 002/0001", o CNPJ nao seria encontrado e o COD_ESTAB
        /// cairia no palpite da ordem do CNPJ — campo-chave errado, com um aviso
        /// no lugar de um erro.
        /// </summary>
        private static KeyValuePair<string, string> Separar(string linha)
        {
            int i = 0;
            while (i < linha.Length)
            {
                char c = linha[i];
                if (c == '\\') { i += 2; continue; }
                if (c == '=' || c == ':' || Branco(c)) break;
                i++;
            }
            string chave = Desescapar(linha.Substring(0, Math.Min(i, linha.Length)));

            while (i < linha.Length && Branco(linha[i])) i++;
            if (i < linha.Length && (linha[i] == '=' || linha[i] == ':')) i++;
            while (i < linha.Length && Branco(linha[i])) i++;

            string valor = i >= linha.Length ? "" : Desescapar(linha.Substring(i));
            return new KeyValuePair<string, string>(chave, valor);
        }

        /// <summary>Branco de separacao do Properties: espaco, tab e form feed.</summary>
        private static bool Branco(char c)
        {
            return c == ' ' || c == '\t' || c == '\f';
        }

        private static string Desescapar(string valor)
        {
            if (valor.IndexOf('\\') < 0) return valor;
            StringBuilder sb = new StringBuilder(valor.Length);
            for (int i = 0; i < valor.Length; i++)
            {
                char c = valor[i];
                if (c != '\\' || i == valor.Length - 1) { sb.Append(c); continue; }
                char escape = valor[++i];
                switch (escape)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 < valor.Length)
                        {
                            sb.Append((char)int.Parse(valor.Substring(i + 1, 4),
                                NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                        }
                        break;
                    default: sb.Append(escape); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Chave de consulta de um CNPJ/CPF nos cadastros: só letras e dígitos, em
        /// maiúsculas, para que "72.977.242/0001-40" no properties case com
        /// "72977242000140" no XML.
        ///
        /// NÃO usar para gravar: o CNPJ vai para o arquivo SAFX exatamente como
        /// veio do XML. Desde 2026 o CNPJ é alfanumérico (as 8 primeiras posições
        /// podem ser letras), então as letras são preservadas aqui — só a
        /// pontuação sai.
        /// </summary>
        internal static string ChaveDocumento(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
