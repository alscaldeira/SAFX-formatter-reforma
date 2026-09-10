package com.caldeira.view;

import javax.swing.*;
import java.awt.*;

public class TelaErros extends JFrame {

    private JTextArea txtErros;

    public TelaErros() {

        setTitle("Erros da Conversão");
        setSize(700, 400);
        setLocationRelativeTo(null);

        txtErros = new JTextArea();
        txtErros.setEditable(false);
        txtErros.setFont(new Font(Font.MONOSPACED, Font.PLAIN, 12));

        JScrollPane scroll = new JScrollPane(txtErros);

        add(scroll);

    }

    public void adicionarErro(String erro) {
        txtErros.append(erro + "\n");
    }

}
