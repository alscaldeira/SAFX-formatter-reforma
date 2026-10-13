using System.Collections.Generic;
using System.IO;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Cadastros;
using ABMC.RANFE.LIGHT_SQL.ReformaTributaria.Modelo;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Ponto de entrada da conversão XML -> SAFX da Reforma Tributária.
    ///
    /// Lê uma pasta de XMLs fiscais (NF-e e NFS-e, em qualquer subpasta) — ou um
    /// único arquivo XML — e gera SAFX3007/3008/3009 (.txt posicional e .csv
    /// nomeado) a partir dos grupos IBSCBS do XML.
    ///
    /// Também gera SAFX04 (cadastro de Pessoa Física/Jurídica), mas só para quem
    /// NÃO tem de-para em participantes.properties: participante já cadastrado no
    /// MasterSAF continua sendo só referenciado pelo IND_FIS_JUR + COD_FIS_JUR,
    /// sem duplicar cadastro. Ver <see cref="XmlToSafxCadastro"/>.
    ///
    /// Não gera SAFX07/08: para NF-e eles já vêm do banco (mesmo upload, via SPED)
    /// e duplicariam; para NFS-e não fazem sentido (são orientados a CFOP/ICMS de
    /// mercadoria).
    /// </summary>
    public static class ConversorXmlReforma
    {
        /// <summary>
        /// Uma conversão por vez.
        ///
        /// Os cadastros e a lista de "sem cadastro" são estado de processo, herdado
        /// do programa desktop, onde só havia um usuário. No servidor duas
        /// requisições simultâneas embaralhariam os avisos de uma com os da outra,
        /// então a conversão inteira é serializada — ela dura frações de segundo
        /// para um lote de XMLs e roda depois do upload, não em laço de requisição.
        /// </summary>
        private static readonly object Trava = new object();

        /// <param name="pastaXml">pasta (varrida recursivamente) ou arquivo XML.</param>
        /// <param name="pastaSaida">onde gravar os SAFX3007/3008/3009; null usa a pasta de cadastros.</param>
        /// <param name="pastaCadastros">onde estão os .properties e o safx-reforma-layout.json;
        /// null usa a pasta Dados publicada com o projeto.</param>
        public static ResultadoReforma Converter(string pastaXml, string pastaSaida, string pastaCadastros)
        {
            lock (Trava)
            {
                string cadastrosAnterior = ReformaConfig.PastaCadastros;
                string saidaAnterior = ReformaConfig.PastaSaida;
                try
                {
                    if (pastaCadastros != null) ReformaConfig.PastaCadastros = pastaCadastros;
                    ReformaConfig.PastaSaida = pastaSaida ?? ReformaConfig.PastaCadastros;
                    if (!Directory.Exists(ReformaConfig.PastaSaida))
                    {
                        Directory.CreateDirectory(ReformaConfig.PastaSaida);
                    }
                    return Executar(pastaXml);
                }
                finally
                {
                    ReformaConfig.PastaCadastros = cadastrosAnterior;
                    ReformaConfig.PastaSaida = saidaAnterior;
                }
            }
        }

        private static ResultadoReforma Executar(string pastaXml)
        {
            ResultadoReforma resultado = new ResultadoReforma();
            Participantes.LimparAvisos();
            Estabelecimentos.LimparAvisos();

            List<XmlNota> notas = XmlLeitor.LerPasta(pastaXml, resultado.Avisos);

            foreach (XmlNota nota in notas)
            {
                if (string.IsNullOrEmpty(nota.CnpjEstab))
                {
                    throw new IOException("Não foi possível identificar o estabelecimento no XML "
                        + nota.Arquivo + " (sem CNPJ de emitente/destinatário/tomador).");
                }
            }
            // o papel consolidado do participante é o que vai no IND_FIS_JUR dos
            // registros da Reforma
            ParticipantesXml.Consolidar(notas);

            XmlToSafxReforma.Gerar(notas, resultado);

            // roda depois do XmlToSafxReforma: é ele quem, ao montar a chave de
            // cada registro, popula Participantes.SemCadastro() — a lista de quem
            // não tem de-para e por isso pode ganhar um SAFX04 novo.
            XmlToSafxCadastro.Gerar(notas, resultado);

            foreach (string cnpj in Estabelecimentos.SemCadastro())
            {
                resultado.Avisos.Add("Estabelecimento " + cnpj + " sem código no " + Estabelecimentos.NomeArquivo
                    + ": o COD_EMPRESA/COD_ESTAB foi deduzido da ordem do próprio CNPJ. "
                    + "Confira com o cadastro do MasterSAF — os dois nem sempre coincidem.");
            }
            foreach (string cnpj in Participantes.SemCadastro())
            {
                resultado.Avisos.Add("Participante " + cnpj + " sem código no " + Participantes.NomeArquivo
                    + ": o COD_FIS_JUR foi gerado como \"M" + cnpj + "\". Cadastre o código do ERP "
                    + "se esta base MasterSAF também recebe cargas do SPED, senão o participante "
                    + "entrará duplicado.");
            }

            int nfe = 0;
            foreach (XmlNota n in notas)
            {
                if (n.Origem == XmlNota.TipoOrigem.Nfe) nfe++;
            }
            resultado.Mensagens.Add("Conversão XML concluída: " + notas.Count + " documentos ("
                + nfe + " NF-e, " + (notas.Count - nfe) + " NFS-e).");
            return resultado;
        }
    }
}
