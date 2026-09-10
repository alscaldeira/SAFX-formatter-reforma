using System;
using System.IO;

namespace ABMC.RANFE.LIGHT_SQL.ReformaTributaria
{
    /// <summary>
    /// Substitui o "user.dir" do programa Java original.
    ///
    /// No aplicativo desktop os cadastros (.properties), o descritor de leiaute
    /// e os arquivos gerados ficavam todos na pasta de execução. No servidor
    /// esses dois papéis são distintos: os cadastros são configuração da
    /// aplicação (~/ReformaTributaria/Dados) e a saída acompanha o restante da
    /// exportação (~/Export).
    ///
    /// As regras de negócio não mudam: continua valendo a prioridade do arquivo
    /// externo sobre o embutido, agora expressa como PastaCadastros -> pasta
    /// Dados publicada com o projeto.
    /// </summary>
    public static class ReformaConfig
    {
        [ThreadStatic]
        private static string _pastaCadastros;

        [ThreadStatic]
        private static string _pastaSaida;

        /// <summary>Onde ficam estabelecimentos/participantes/unidades.properties e o safx-reforma-layout.json.</summary>
        public static string PastaCadastros
        {
            get { return _pastaCadastros ?? PadraoCadastros(); }
            set { _pastaCadastros = value; }
        }

        /// <summary>Onde os SAFX3007/3008/3009 (.txt e .csv) são gravados.</summary>
        public static string PastaSaida
        {
            get { return _pastaSaida ?? PadraoCadastros(); }
            set { _pastaSaida = value; }
        }

        /// <summary>Pasta "Dados" publicada junto com o projeto, quando nada foi configurado.</summary>
        private static string PadraoCadastros()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReformaTributaria", "Dados");
        }

        internal static string ArquivoCadastro(string nome)
        {
            return Path.Combine(PastaCadastros, nome);
        }

        internal static string ArquivoSaida(string nome)
        {
            return Path.Combine(PastaSaida, nome);
        }
    }
}
