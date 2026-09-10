using System.Collections.Generic;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros
{
    /// <summary>
    /// De-para CNPJ -> COD_EMPRESA/COD_ESTAB usado pelos SAFX3007/3008/3009.
    ///
    /// O código do estabelecimento é um cadastro do MasterSAF e NÃO é derivável do
    /// CNPJ (o CNPJ 72977242000493 tem ordem 0004, mas o estabelecimento é 0005),
    /// por isso a tabela é externa.
    ///
    /// A ausência de cadastro não aborta a conversão: cai-se para a ordem do
    /// estabelecimento no próprio CNPJ (posições 9 a 12) com COD_EMPRESA "001", e
    /// o caso entra na lista de avisos — esse código é um chute razoável, não o
    /// cadastro do MasterSAF, e precisa ser conferido.
    ///
    /// O valor aceita duas formas:
    /// <code>
    ///   72977242000140=0001        -> COD_EMPRESA "001" (padrão), COD_ESTAB "0001"
    ///   42105890000901=002/0001    -> COD_EMPRESA "002",          COD_ESTAB "0001"
    /// </code>
    /// Isso é necessário porque um mesmo lote pode conter estabelecimentos de
    /// empresas diferentes.
    /// </summary>
    internal static class Estabelecimentos
    {
        internal const string NomeArquivo = "estabelecimentos.properties";

        private const string EmpresaPadrao = "001";

        [System.ThreadStatic]
        private static Dictionary<string, string> _bruto;

        [System.ThreadStatic]
        private static Dictionary<string, string> _mapa;

        [System.ThreadStatic]
        private static List<string> _semCadastro;

        private static List<string> SemCadastroLista
        {
            get { return _semCadastro ?? (_semCadastro = new List<string>()); }
        }

        /// <summary>COD_ESTAB (campo 2 do SAFX3007/3008/3009).</summary>
        internal static string Codigo(string cnpj)
        {
            return Partes(cnpj)[1];
        }

        /// <summary>COD_EMPRESA (campo 1); "001" quando o cadastro não informa.</summary>
        internal static string Empresa(string cnpj)
        {
            return Partes(cnpj)[0];
        }

        private static string[] Partes(string cnpj)
        {
            string chave = CadastroProperties.ChaveDocumento(cnpj);
            string cod = ExtrairCodigoEstabelecimento(chave);
            if (cod == null)
            {
                if (!SemCadastroLista.Contains(chave)) SemCadastroLista.Add(chave);
                return new[] { EmpresaPadrao, OrdemNoCnpj(chave) };
            }
            int barra = cod.IndexOf('/');
            if (barra < 0)
            {
                return new[] { EmpresaPadrao, cod };
            }
            return new[] { Safx.Texto.Aparar(cod.Substring(0, barra)), Safx.Texto.Aparar(cod.Substring(barra + 1)) };
        }

        /// <summary>
        /// Devolve o valor cadastrado para o CNPJ ("0001" ou "002/0001"), ou null
        /// quando o CNPJ não está no cadastro. O código NÃO é derivado do CNPJ.
        /// </summary>
        private static string ExtrairCodigoEstabelecimento(string cnpj)
        {
            if (string.IsNullOrEmpty(cnpj)) return null;
            string cod;
            return Carregar().TryGetValue(cnpj, out cod) ? cod : null;
        }

        /// <summary>
        /// COD_ESTAB deduzido da ordem do estabelecimento no CNPJ (posições 9 a 12:
        /// 42105890000901 -> "0009"). Usado só quando o CNPJ não está cadastrado.
        /// </summary>
        private static string OrdemNoCnpj(string cnpj)
        {
            return cnpj != null && cnpj.Length == 14 ? cnpj.Substring(8, 4) : "0001";
        }

        /// <summary>CNPJs de estabelecimento que caíram no fallback nesta conversão.</summary>
        internal static IEnumerable<string> SemCadastro()
        {
            return SemCadastroLista;
        }

        internal static void LimparAvisos()
        {
            SemCadastroLista.Clear();
        }

        private static Dictionary<string, string> Carregar()
        {
            // Comparação por identidade: CadastroProperties devolve a mesma instância
            // enquanto o arquivo não muda, e outra quando ele é editado.
            Dictionary<string, string> atual = CadastroProperties.Carregar(NomeArquivo, true);
            if (ReferenceEquals(atual, _bruto)) return _mapa;

            Dictionary<string, string> carregado = new Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> e in atual)
            {
                string cnpj = CadastroProperties.ChaveDocumento(e.Key);
                if (cnpj.Length > 0) carregado[cnpj] = e.Value;
            }
            _bruto = atual;
            _mapa = carregado;
            return _mapa;
        }
    }
}
