using System;
using System.Collections.Generic;
using System.IO;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros
{
    /// <summary>
    /// De-para CNPJ/CPF -> COD_PART, usado para montar o COD_FIS_JUR dos
    /// SAFX3007/3008/3009 na conversão a partir de XML.
    ///
    /// Motivo: no fluxo SPED o COD_FIS_JUR sai de "M" + COD_PART do registro 0150,
    /// um código interno do ERP que NÃO é derivável do CNPJ (ex.: 9003065). O XML
    /// fiscal não traz esse código, então sem um de-para os dois fluxos gerariam
    /// chaves diferentes para o mesmo participante e o MasterSAF ficaria com
    /// cadastro duplicado.
    ///
    /// Como em <see cref="Estabelecimentos"/>, a ausência de cadastro NÃO aborta a
    /// conversão: cai-se para o próprio CNPJ como código e o caso entra na lista
    /// de avisos.
    /// </summary>
    internal static class Participantes
    {
        internal const string NomeArquivo = "participantes.properties";

        /// <summary>Tamanho do campo COD_FIS_JUR no leiaute (SAFX3007 campo 7).</summary>
        private const int TamanhoCodFisJur = 14;

        [ThreadStatic]
        private static Dictionary<string, string> _bruto;

        [ThreadStatic]
        private static Dictionary<string, string> _mapa;

        [ThreadStatic]
        private static List<string> _semCadastro;

        private static List<string> SemCadastroLista
        {
            get { return _semCadastro ?? (_semCadastro = new List<string>()); }
        }

        /// <summary>
        /// COD_FIS_JUR pronto para gravação.
        ///
        /// Com de-para, vale a convenção do fluxo SPED: "M" + COD_PART (o código do
        /// ERP é curto e cabe). Sem de-para, o código é o próprio CNPJ/CPF — com o
        /// prefixo "M" ele teria 15 posições e estouraria o campo de 14.
        ///
        /// O documento entra mascarado sempre que a máscara couber no campo: o CPF
        /// (14 posições com máscara) sai como 000.000.000-00; o CNPJ (18) não cabe
        /// nas 14 posições do COD_FIS_JUR e continua sem pontuação.
        /// </summary>
        internal static string CodFisJur(string documento)
        {
            string chave = CadastroProperties.ChaveDocumento(documento);
            string bruto = documento == null ? "" : Safx.Texto.Aparar(documento);
            if (chave.Length == 0 && bruto.Length == 0) return "";

            string procurado = chave.Length == 0 ? bruto : chave;
            string cod;
            if (Carregar().TryGetValue(procurado, out cod))
            {
                string comPrefixo = "M" + cod;
                if (comPrefixo.Length > TamanhoCodFisJur)
                {
                    throw new IOException("COD_PART \"" + cod + "\" cadastrado em " + NomeArquivo
                        + " não cabe no COD_FIS_JUR (máximo " + (TamanhoCodFisJur - 1)
                        + " posições com o prefixo M).");
                }
                return comPrefixo;
            }

            if (!SemCadastroLista.Contains(procurado)) SemCadastroLista.Add(procurado);
            // A máscara só entra quando cabe: o CPF mascarado tem 14 posições e
            // cabe, o CNPJ tem 18 e estouraria o campo.
            string semPrefixo = Documentos.MascararSeCouber(procurado, TamanhoCodFisJur);
            return semPrefixo.Length > TamanhoCodFisJur
                ? semPrefixo.Substring(0, TamanhoCodFisJur)
                : semPrefixo;
        }

        /// <summary>COD_PART do de-para; na falta dele, o próprio documento.</summary>
        internal static string Codigo(string documento)
        {
            string chave = CadastroProperties.ChaveDocumento(documento);
            if (chave.Length == 0) return "";
            string cod;
            if (Carregar().TryGetValue(chave, out cod)) return cod;
            if (!SemCadastroLista.Contains(chave)) SemCadastroLista.Add(chave);
            return chave;
        }

        /// <summary>CNPJs que caíram no fallback nesta conversão.</summary>
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
            Dictionary<string, string> atual = CadastroProperties.Carregar(NomeArquivo, false);
            if (ReferenceEquals(atual, _bruto)) return _mapa;

            Dictionary<string, string> normalizado = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> e in atual)
            {
                normalizado[CadastroProperties.ChaveDocumento(e.Key)] = e.Value;
            }
            _bruto = atual;
            _mapa = normalizado;
            return _mapa;
        }
    }
}
