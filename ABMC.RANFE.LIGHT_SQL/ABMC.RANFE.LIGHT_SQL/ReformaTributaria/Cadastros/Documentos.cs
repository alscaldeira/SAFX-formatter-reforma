namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros
{
    /// <summary>
    /// Máscara de CPF/CNPJ dos campos de documento gravados nos arquivos gerados.
    ///
    /// O XML fiscal traz CNPJ e CPF sempre sem pontuação (é o que o schema exige),
    /// mas os arquivos gerados aqui saem com a máscara usual — 000.000.000-00 para
    /// CPF e 00.000.000/0000-00 para CNPJ — independentemente de como a origem
    /// informou.
    /// </summary>
    internal static class Documentos
    {
        /// <summary>Dígitos de um CPF; com máscara ele ocupa 14 posições.</summary>
        private const int DigitosCpf = 11;

        /// <summary>Posições de um CNPJ; com máscara ele ocupa 18.</summary>
        private const int PosicoesCnpj = 14;

        /// <summary>
        /// Documento com máscara. Valor que não tem cara de CPF nem de CNPJ volta
        /// como veio — é o caso dos códigos sintéticos "EXnnnnnnnnnnnn" que a
        /// exportação usa quando o destinatário do exterior não tem documento.
        /// </summary>
        internal static string Mascarar(string documento)
        {
            string limpo = SemMascara(documento);
            if (limpo.Length == DigitosCpf && SoDigitos(limpo))
            {
                return limpo.Substring(0, 3) + "." + limpo.Substring(3, 3) + "."
                       + limpo.Substring(6, 3) + "-" + limpo.Substring(9);
            }
            if (limpo.Length == PosicoesCnpj && Cnpj(limpo))
            {
                return limpo.Substring(0, 2) + "." + limpo.Substring(2, 3) + "."
                       + limpo.Substring(5, 3) + "/" + limpo.Substring(8, 4) + "-"
                       + limpo.Substring(12);
            }
            return documento == null ? "" : Safx.Texto.Aparar(documento);
        }

        /// <summary>
        /// Máscara aplicada só quando o resultado cabe no campo; senão, o documento
        /// sem pontuação. Existe por causa do COD_FIS_JUR, que tem 14 posições: o
        /// CPF mascarado cabe (14), o CNPJ mascarado não (18).
        /// </summary>
        internal static string MascararSeCouber(string documento, int tamanho)
        {
            string mascarado = Mascarar(documento);
            return mascarado.Length <= tamanho ? mascarado : SemMascara(documento);
        }

        /// <summary>Documento sem pontuação, em maiúsculas (o CNPJ é alfanumérico desde 2026).</summary>
        internal static string SemMascara(string documento)
        {
            return CadastroProperties.ChaveDocumento(documento);
        }

        /// <summary>
        /// Formato do CNPJ alfanumérico (IN RFB 2.229/2024): as 12 primeiras
        /// posições são letras ou dígitos e as 2 últimas, os dígitos verificadores,
        /// são sempre numéricas. A regra também separa um CNPJ de um código
        /// sintético de 14 posições, que dificilmente termina em dois dígitos.
        /// </summary>
        private static bool Cnpj(string limpo)
        {
            return char.IsDigit(limpo[12]) && char.IsDigit(limpo[13]);
        }

        private static bool SoDigitos(string valor)
        {
            for (int i = 0; i < valor.Length; i++)
            {
                if (!char.IsDigit(valor[i])) return false;
            }
            return true;
        }
    }
}
