using System;
using System.Collections.Generic;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros
{
    /// <summary>
    /// De-para de unidade de medida do XML para a unidade cadastrada no MasterSAF.
    ///
    /// Existe porque a NF-e pode escrever a mesma unidade de duas formas no mesmo
    /// item (uCom = "TO" e uTrib = "TON"), e o COD_UND_PADRAO do SAFX3008 é campo-
    /// chave: divergência ali vira item duplicado. A conversão usa sempre uCom,
    /// normalizado por esta tabela.
    /// </summary>
    internal static class Unidades
    {
        internal const string NomeArquivo = "unidades.properties";

        [ThreadStatic]
        private static Dictionary<string, string> _bruto;

        [ThreadStatic]
        private static Dictionary<string, string> _mapa;

        internal static string Normalizar(string unidade)
        {
            if (unidade == null) return "";
            string chave = Safx.Texto.Aparar(unidade).ToUpperInvariant();
            if (chave.Length == 0) return "";
            string convertida;
            return Carregar().TryGetValue(chave, out convertida) ? convertida : chave;
        }

        private static Dictionary<string, string> Carregar()
        {
            Dictionary<string, string> atual = CadastroProperties.Carregar(NomeArquivo, false);
            if (ReferenceEquals(atual, _bruto)) return _mapa;

            Dictionary<string, string> normalizado = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> e in atual)
            {
                normalizado[Safx.Texto.Aparar(e.Key).ToUpperInvariant()] = Safx.Texto.Aparar(e.Value).ToUpperInvariant();
            }
            _bruto = atual;
            _mapa = normalizado;
            return _mapa;
        }
    }
}
