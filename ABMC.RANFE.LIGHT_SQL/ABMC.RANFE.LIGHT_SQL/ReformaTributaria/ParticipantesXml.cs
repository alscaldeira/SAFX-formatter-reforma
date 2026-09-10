using System.Collections.Generic;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Consolida os participantes lidos dos XMLs (destinatário da NF-e de saída,
    /// emitente da NF-e de entrada de terceiro, prestador da NFS-e e transportadora)
    /// numa instância única por documento (CNPJ/CPF).
    ///
    /// Um participante que aparece em papéis diferentes no mesmo lote tem de entrar
    /// nos SAFX3007/3008/3009 com um IND_FIS_JUR só — "5" (Cliente/Fornecedor/
    /// Transportadora) —, senão o mesmo CNPJ sairia ora como cliente ora como
    /// fornecedor e o vínculo com o cadastro do MasterSAF se perderia.
    /// </summary>
    internal static class ParticipantesXml
    {
        /// <summary>Aponta cada nota para a instância consolidada do seu participante.</summary>
        internal static void Consolidar(List<XmlNota> notas)
        {
            Dictionary<string, XmlParticipante> participantes =
                new Dictionary<string, XmlParticipante>(System.StringComparer.Ordinal);

            foreach (XmlNota nota in notas)
            {
                Acumular(participantes, nota.Participante);
                Acumular(participantes, nota.Transportadora);
            }

            foreach (XmlNota nota in notas)
            {
                nota.Participante = Consolidado(participantes, nota.Participante);
                nota.Transportadora = Consolidado(participantes, nota.Transportadora);
            }
        }

        private static void Acumular(Dictionary<string, XmlParticipante> destino, XmlParticipante p)
        {
            if (p == null || p.Documento().Length == 0) return;
            XmlParticipante existente;
            if (!destino.TryGetValue(p.Documento(), out existente))
            {
                destino[p.Documento()] = p;
                return;
            }
            if (existente.Papel != p.Papel)
            {
                existente.Papel = XmlNota.PapelMisto;
            }
            // completa o que faltava no primeiro registro (ex.: IE só vem no dest)
            if (Vazio(existente.Ie)) existente.Ie = p.Ie;
            if (Vazio(existente.Im)) existente.Im = p.Im;
            if (Vazio(existente.Logradouro)) existente.Logradouro = p.Logradouro;
            if (Vazio(existente.Numero)) existente.Numero = p.Numero;
            if (Vazio(existente.Complemento)) existente.Complemento = p.Complemento;
            if (Vazio(existente.Bairro)) existente.Bairro = p.Bairro;
            if (Vazio(existente.Municipio)) existente.Municipio = p.Municipio;
            if (Vazio(existente.CodMunicipio)) existente.CodMunicipio = p.CodMunicipio;
            if (Vazio(existente.Uf)) existente.Uf = p.Uf;
            if (Vazio(existente.Cep)) existente.Cep = p.Cep;
            if (Vazio(existente.CodPais)) existente.CodPais = p.CodPais;
            if (Vazio(existente.Email)) existente.Email = p.Email;
        }

        private static XmlParticipante Consolidado(Dictionary<string, XmlParticipante> mapa, XmlParticipante p)
        {
            if (p == null || p.Documento().Length == 0) return p;
            XmlParticipante consolidado;
            return mapa.TryGetValue(p.Documento(), out consolidado) ? consolidado : p;
        }

        private static bool Vazio(string s)
        {
            return s == null || Safx.Texto.Aparar(s).Length == 0;
        }
    }
}
