package com.caldeira.view;

import com.caldeira.service.Conversao;

import javax.swing.*;
import java.awt.*;
import java.io.File;
import java.util.List;

public class TelaPrincipal extends JFrame {

    private JTextField txtArquivo;
    private JLabel lblTipoDetectado;
    private File origemSelecionada;

    public TelaPrincipal() {
        setTitle("Conversor SPED/XML para SAFX");
        setSize(620, 170);
        setResizable(false);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);

        initComponents();
    }

    private void initComponents() {

        JPanel painel = new JPanel(new BorderLayout(10, 10));
        painel.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));

        txtArquivo = new JTextField();
        txtArquivo.setEditable(false);
        txtArquivo.setPreferredSize(new Dimension(200, 25));

        JButton btnProcurar = new JButton("Procurar...");
        JButton btnConverter = new JButton("Converter");

        JPanel painelArquivo = new JPanel(new BorderLayout(5, 5));
        painelArquivo.add(new JLabel("Arquivo ou pasta"), BorderLayout.WEST);
        painelArquivo.add(txtArquivo, BorderLayout.CENTER);

        // O tipo é descoberto pelo conteúdo, não escolhido pelo usuário.
        lblTipoDetectado = new JLabel(" ");
        lblTipoDetectado.setForeground(new Color(0, 100, 0));

        JPanel painelCentro = new JPanel(new BorderLayout(5, 5));
        painelCentro.add(painelArquivo, BorderLayout.NORTH);
        painelCentro.add(lblTipoDetectado, BorderLayout.CENTER);

        painel.add(painelCentro, BorderLayout.CENTER);

        JPanel painelBotoes = new JPanel(new FlowLayout(FlowLayout.CENTER));
        painelBotoes.add(btnProcurar);
        painelBotoes.add(btnConverter);

        painel.add(painelBotoes, BorderLayout.SOUTH);

        add(painel);

        btnProcurar.addActionListener(e -> selecionarOrigem());

        btnConverter.addActionListener(e -> {

            if (origemSelecionada == null) {
                JOptionPane.showMessageDialog(
                        this,
                        "Selecione um arquivo SPED, um XML ou a pasta com os XMLs.",
                        "Aviso",
                        JOptionPane.WARNING_MESSAGE);
                return;
            }

            try {
                List<String> avisos = Conversao.executar(origemSelecionada.getAbsolutePath());

                if (avisos.isEmpty()) {
                    JOptionPane.showMessageDialog(
                            this,
                            "Conversão concluída com sucesso!");
                } else {
                    JOptionPane.showMessageDialog(
                            this,
                            "Conversão concluída com " + avisos.size() + " aviso(s) — veja a lista.");
                    TelaErros telaAvisos = new TelaErros();
                    telaAvisos.setTitle("Avisos da Conversão");
                    for (String aviso : avisos) {
                        telaAvisos.adicionarErro(aviso);
                    }
                    telaAvisos.setVisible(true);
                }

            } catch (Exception ex) {

                TelaErros telaErros = new TelaErros();
                telaErros.adicionarErro(ex.getMessage());
                telaErros.setVisible(true);

            }

        });
    }

    private void selecionarOrigem() {

        JFileChooser chooser = new JFileChooser();
        chooser.setFileSelectionMode(JFileChooser.FILES_AND_DIRECTORIES);
        chooser.setDialogTitle("Selecione o arquivo SPED, o XML ou a pasta com os XMLs");

        int retorno = chooser.showOpenDialog(this);

        if (retorno != JFileChooser.APPROVE_OPTION) {
            return;
        }

        origemSelecionada = chooser.getSelectedFile();
        txtArquivo.setText(origemSelecionada.getAbsolutePath());
        mostrarTipoDetectado();
    }

    /**
     * Antecipa na tela o que a conversão vai fazer, para o usuário perceber uma
     * seleção errada antes de converter.
     */
    private void mostrarTipoDetectado() {
        try {
            Conversao.Origem origem = Conversao.detectar(origemSelecionada.getAbsolutePath());
            String descricao = origem == Conversao.Origem.SPED
                    ? "Arquivo SPED"
                    : (origemSelecionada.isDirectory() ? "Pasta de XMLs" : "XML avulso");
            lblTipoDetectado.setForeground(new Color(0, 100, 0));
            lblTipoDetectado.setText("Identificado: " + descricao
                    + (origem == Conversao.Origem.SPED
                            ? " — a conversão gera o IMPORTACAO.csv."
                            : " — a conversão gera os arquivos da Reforma Tributária"
                                    + " (SAFX3007/3008/3009)."));
        } catch (Exception ex) {
            lblTipoDetectado.setForeground(Color.RED);
            lblTipoDetectado.setText("Não reconhecido — veja o detalhe ao converter.");
        }
    }

    public static void iniciar() {
        SwingUtilities.invokeLater(() -> {
            new TelaPrincipal().setVisible(true);
        });
    }

}
