package com.caldeira.view;

import com.caldeira.service.SpedToSafx07;
import com.caldeira.service.SpedToSafx08;
import com.caldeira.service.SpedToSafx49;

import javax.swing.*;
import java.awt.*;
import java.io.File;

public class TelaPrincipal extends JFrame {

    private JTextField txtArquivo;
    private File arquivoSelecionado;

    public TelaPrincipal() {
        setTitle("Conversor SPED para SAFX");
        setSize(500, 120);
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
        painelArquivo.add(new Label("Arquivo SPED"), BorderLayout.WEST);
        painelArquivo.add(txtArquivo, BorderLayout.CENTER);

        painel.add(painelArquivo, BorderLayout.CENTER);

        JPanel painelBotoes = new JPanel(new FlowLayout(FlowLayout.CENTER));
        painelBotoes.add(btnProcurar);
        painelBotoes.add(btnConverter);

        painel.add(painelBotoes, BorderLayout.SOUTH);

        add(painel);

        btnProcurar.addActionListener(e -> selecionarArquivo());

        btnConverter.addActionListener(e -> {

            if (arquivoSelecionado == null) {
                JOptionPane.showMessageDialog(
                        this,
                        "Selecione um arquivo.",
                        "Aviso",
                        JOptionPane.WARNING_MESSAGE);
                return;
            }

            try {
                converterArquivo(arquivoSelecionado);

                JOptionPane.showMessageDialog(
                        this,
                        "Conversão concluída com sucesso!");

            } catch (Exception ex) {

                TelaErros telaErros = new TelaErros();
                telaErros.adicionarErro(ex.getMessage());
                telaErros.setVisible(true);

            }

        });
    }

    private void selecionarArquivo() {

        JFileChooser chooser = new JFileChooser();

        int retorno = chooser.showOpenDialog(this);

        if (retorno == JFileChooser.APPROVE_OPTION) {
            arquivoSelecionado = chooser.getSelectedFile();
            txtArquivo.setText(arquivoSelecionado.getAbsolutePath());
        }

    }

    /**
     * Chame aqui sua lógica de conversão.
     */
    private void converterArquivo(File arquivo) throws Exception {
        SpedToSafx07.convertFromSped(arquivo.getAbsolutePath());
        SpedToSafx08.convertFromSped(arquivo.getAbsolutePath());
        SpedToSafx49.convertFromSped(arquivo.getAbsolutePath());
    }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            new TelaPrincipal().setVisible(true);
        });
    }

}
