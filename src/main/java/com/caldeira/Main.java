package com.caldeira;

import com.caldeira.service.SpedToSafx07;
import com.caldeira.service.SpedToSafx08;
import com.caldeira.service.SpedToSafx49;

import java.io.IOException;

//TIP To <b>Run</b> code, press <shortcut actionId="Run"/> or
// click the <icon src="AllIcons.Actions.Execute"/> icon in the gutter.
public class Main {
    static void main() throws IOException {
        SpedToSafx07.convertFromSped("/Users/alscaldeira/IdeaProjects/safx-formatter/src/main/resources/SPED.txt");
        SpedToSafx08.convertFromSped("/Users/alscaldeira/IdeaProjects/safx-formatter/src/main/resources/SPED.txt");
        SpedToSafx49.convertFromSped("/Users/alscaldeira/IdeaProjects/safx-formatter/src/main/resources/SPED.txt");
    }
}
