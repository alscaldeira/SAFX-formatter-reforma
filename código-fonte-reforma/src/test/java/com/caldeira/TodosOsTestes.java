package com.caldeira;

import com.caldeira.service.TestesCadastros;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Executa todas as suítes. Rodar a partir da raiz do projeto:
 *
 * <pre>
 * javac -encoding UTF-8 -d /tmp/safx-classes $(find src/main/java src/test/java -name '*.java')
 * java -cp /tmp/safx-classes:src/main/resources com.caldeira.TodosOsTestes
 * </pre>
 *
 * Sai com código 1 se alguma verificação falhar. O diretório de trabalho de
 * cada cenário é isolado (a conversão grava em user.dir), por isso as suítes
 * trocam essa propriedade em vez de escrever na pasta do projeto.
 */
public final class TodosOsTestes {

    public static void main(String[] args) throws Exception {
        String raizProjeto = System.getProperty("user.dir");
        Path base = args.length > 0
                ? Paths.get(args[0])
                : Files.createTempDirectory("safx-testes-");
        Files.createDirectories(base);
        System.out.println("Área de trabalho dos testes: " + base);

        try {
            TestesConversaoXml.executar(base.resolve("xml"));
            restaurar(raizProjeto);
            TestesConformidadeLeiaute.executar(base.resolve("conformidade"));
            restaurar(raizProjeto);
            TestesIntegridadeReferencial.executar(base.resolve("integridade"));
            restaurar(raizProjeto);
            TestesReforma.executar(base.resolve("reforma"));
            restaurar(raizProjeto);
            TestesGolden.executar(base.resolve("golden"));
            restaurar(raizProjeto);
            TestesFluxoSped.executar(base.resolve("sped"));
            restaurar(raizProjeto);
            TestesCadastros.executar(base.resolve("cadastros"));
            restaurar(raizProjeto);
            TestesCsv.executar(base.resolve("csv"));
            restaurar(raizProjeto);
            TestesIdempotencia.executar(base.resolve("idempotencia"));
            restaurar(raizProjeto);
            TestesEncodingLocale.executar(base.resolve("encoding"));
            restaurar(raizProjeto);
            TestesInterface.executar(base.resolve("interface"));
            restaurar(raizProjeto);
            TestesFuzz.executar(base.resolve("fuzz"));
            restaurar(raizProjeto);
            TestesVolume.executar(base.resolve("volume"));
        } finally {
            restaurar(raizProjeto);
        }

        System.out.println("\n========================================");
        System.out.println("PASSOU: " + Assercoes.passou() + "   FALHOU: " + Assercoes.falhou());
        if (Assercoes.falhou() > 0) System.exit(1);
    }

    /** As suítes trocam user.dir; o leiaute e os goldens são lidos por caminho relativo. */
    private static void restaurar(String raizProjeto) {
        System.setProperty("user.dir", raizProjeto);
    }
}
