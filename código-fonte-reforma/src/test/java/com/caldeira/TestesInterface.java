package com.caldeira;

import com.caldeira.service.Conversao;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

/**
 * Fachada usada pela tela. A decisão de fluxo foi extraída da classe Swing
 * justamente para poder ser testada sem abrir janela.
 */
public final class TestesInterface {

    private TestesInterface() {
    }

    public static void executar(Path base) throws IOException {
        Assercoes.secao("Fachada de conversão (usada pela tela)");

        Path casoXml = Fixtures.caso(base, "fachada-xml");
        Fixtures.escrever(casoXml, "nota.xml",
                Fixtures.nfe("2001", true, "42105890000901", "46754545000194"));
        System.setProperty("user.dir", casoXml.resolve("out").toString());
        List<String> avisosXml = Conversao.executar(Conversao.Origem.XML,
                casoXml.resolve("xmls").toString());
        Assercoes.check("fachada: origem XML gera os arquivos",
                Fixtures.linhas(casoXml, "SAFX07").size() == 1,
                "" + Fixtures.linhas(casoXml, "SAFX07").size());
        Assercoes.check("fachada: origem XML devolve avisos", avisosXml != null && !avisosXml.isEmpty(),
                String.valueOf(avisosXml));

        Path casoSped = Fixtures.caso(base, "fachada-sped");
        Path sped = Fixtures.escreverSped(casoSped, "sped.txt", Fixtures.sped(1, 1),
                StandardCharsets.UTF_8);
        System.setProperty("user.dir", casoSped.resolve("out").toString());
        List<String> avisosSped = Conversao.executar(Conversao.Origem.SPED, sped.toString());
        Assercoes.check("fachada: origem SPED gera os arquivos",
                Fixtures.linhas(casoSped, "SAFX07").size() == 1,
                "" + Fixtures.linhas(casoSped, "SAFX07").size());
        Assercoes.check("fachada: origem SPED não tem avisos", avisosSped.isEmpty(),
                String.valueOf(avisosSped));

        // --- detecção automática: a tela não pergunta mais se é arquivo ou pasta
        Assercoes.check("detecção: pasta é lote de XML",
                Conversao.detectar(casoXml.resolve("xmls").toString()) == Conversao.Origem.XML,
                String.valueOf(Conversao.detectar(casoXml.resolve("xmls").toString())));
        Assercoes.check("detecção: arquivo SPED reconhecido pelo conteúdo",
                Conversao.detectar(sped.toString()) == Conversao.Origem.SPED,
                String.valueOf(Conversao.detectar(sped.toString())));
        Assercoes.check("detecção: XML avulso reconhecido pelo conteúdo",
                Conversao.detectar(casoXml.resolve("xmls/nota.xml").toString()) == Conversao.Origem.XML,
                String.valueOf(Conversao.detectar(casoXml.resolve("xmls/nota.xml").toString())));

        // extensão não decide nada: SPED chamado .xml continua sendo SPED
        Path disfarce = Fixtures.caso(base, "fachada-disfarce");
        Path spedComNomeXml = Fixtures.escreverSped(disfarce, "parece-xml.xml",
                Fixtures.sped(1, 1), StandardCharsets.UTF_8);
        Assercoes.check("detecção: SPED com extensão .xml ainda é SPED",
                Conversao.detectar(spedComNomeXml.toString()) == Conversao.Origem.SPED,
                String.valueOf(Conversao.detectar(spedComNomeXml.toString())));

        // XML avulso convertido sem precisar de pasta
        Path avulso = Fixtures.caso(base, "fachada-xml-avulso");
        Fixtures.escrever(avulso, "nota.xml",
                Fixtures.nfe("2002", true, "42105890000901", "46754545000194"));
        System.setProperty("user.dir", avulso.resolve("out").toString());
        Conversao.executar(avulso.resolve("xmls/nota.xml").toString());
        Assercoes.check("detecção: XML avulso é convertido sem selecionar pasta",
                Fixtures.linhas(avulso, "SAFX07").size() == 1,
                "" + Fixtures.linhas(avulso, "SAFX07").size());

        // arquivo que não é nem um nem outro: erro explicativo
        Path lixo = Fixtures.caso(base, "fachada-desconhecido");
        Files.write(lixo.resolve("xmls/planilha.txt"),
                "col1,col2\n1,2\n".getBytes(StandardCharsets.UTF_8));
        String erro = null;
        try {
            Conversao.executar(lixo.resolve("xmls/planilha.txt").toString());
        } catch (IOException e) {
            erro = e.getMessage();
        }
        Assercoes.check("detecção: arquivo irreconhecível dá erro explicativo",
                erro != null && erro.contains("Não foi possível identificar"), String.valueOf(erro));

        String erroVazio = null;
        try {
            Files.write(lixo.resolve("xmls/vazio.txt"), new byte[0]);
            Conversao.executar(lixo.resolve("xmls/vazio.txt").toString());
        } catch (IOException e) {
            erroVazio = e.getMessage();
        }
        Assercoes.check("detecção: arquivo vazio avisa que está vazio",
                erroVazio != null && erroVazio.contains("vazio"), String.valueOf(erroVazio));
    }
}
