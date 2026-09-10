using System.Text;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo
{
    /// <summary>
    /// Pessoa física/jurídica extraída de um XML (destinatário, emitente/prestador
    /// ou transportadora). É referenciada pelo COD_FIS_JUR nos SAFX3007/3008/3009.
    /// </summary>
    internal sealed class XmlParticipante
    {
        internal string Cnpj;
        internal string Cpf;

        /// <summary>idEstrangeiro da NF-e: identificação do destinatário do exterior.</summary>
        internal string IdEstrangeiro;
        internal string Nome;
        internal string NomeFantasia;
        internal string Ie;
        internal string Im;
        internal string Logradouro;
        internal string Numero;
        internal string Complemento;
        internal string Bairro;
        internal string Municipio;
        internal string CodMunicipio;
        internal string Uf;
        internal string Cep;
        internal string CodPais;
        internal string Email;
        internal string Fone;

        /// <summary>Domínio de IND_FIS_JUR — ver constantes Papel* em <see cref="XmlNota"/>.</summary>
        internal string Papel;

        /// <summary>indIEDest da NF-e: 1 contribuinte, 2 isento de inscrição, 9 não contribuinte.</summary>
        internal string IndContribuinteIcms;

        /// <summary>opSimpNac da NFS-e.</summary>
        internal bool SimplesNacional;

        /// <summary>
        /// Chave do participante nos arquivos SAFX (COD_FIS_JUR).
        ///
        /// Exportação não tem CNPJ nem CPF — a NF-e traz idEstrangeiro, que pode vir
        /// vazio. Sem uma chave a capa ficaria apontando para nada, então cai-se
        /// para um código derivado do nome.
        /// </summary>
        internal string Documento()
        {
            if (!string.IsNullOrEmpty(Cnpj)) return Cnpj;
            if (!string.IsNullOrEmpty(Cpf)) return Cpf;
            if (IdEstrangeiro != null && Safx.Texto.Aparar(IdEstrangeiro).Length > 0)
            {
                return "EX" + ApenasAlfanumerico(IdEstrangeiro, 12);
            }
            if (Nome != null && Safx.Texto.Aparar(Nome).Length > 0)
            {
                return "EX" + ApenasAlfanumerico(Nome, 12);
            }
            return "";
        }

        private static string ApenasAlfanumerico(string valor, int tamanho)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in valor.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
                if (sb.Length == tamanho) break;
            }
            return sb.ToString();
        }
    }
}
