# Pesquisa SAFX e SPED — notas de implementação

## Escopo

Este documento consolida a pesquisa feita para construir um conversor de EFD ICMS/IPI (SPED Fiscal) para os layouts MasterSAF/ONESOURCE **SAFX07**, **SAFX08** e **SAFX49**.

## Distinção essencial

- **SPED** não é um único formato: é o ecossistema de escriturações digitais do Fisco.
- O EFD ICMS/IPI é um arquivo texto hierárquico, composto por registros, para validação e transmissão ao Fisco.
- **SAFX** é uma família de tabelas/layouts internos do MasterSAF DW / ONESOURCE Tax. É uma camada de integração anterior à geração das obrigações, e não um layout oficial da Receita Federal.

## SAFX07 — capa do documento fiscal

O SAFX07 representa um documento fiscal (capa/mestre). O layout fornecido possui **302 posições**, da `COD_EMPRESA` à `IND_VLR_ICMS_COB_ANT_ST`.

Funções principais:

- identifica empresa, estabelecimento, participante, documento, série e datas;
- informa entrada/saída, devolução, modelo, situação e totais do documento;
- carrega referências, transporte, valores de tributos e campos específicos de obrigações estaduais/federais;
- é o registro pai dos itens SAFX08 e, em operações de importação, do SAFX49.

Chave identificada no layout fornecido: `COD_EMPRESA`, `COD_ESTAB`, `MOVTO_E_S`, `NORM_DEV`, `COD_DOCTO`, `IDENT_FIS_JUR`, `COD_FIS_JUR`, `NUM_DOCFIS`, `SERIE_DOCFIS`, `DATA_EMISSAO`, `COD_CLASS_DOC_FIS`, `COD_MODELO`, `SITUACAO`.

## SAFX08 — item de mercadoria/produto

O SAFX08 detalha mercadorias e produtos por item da nota. O layout fornecido possui **262 posições**, da `COD_EMPRESA` à `COD_CSOSN`.

Pontos de conversão:

- cada linha deve pertencer a uma capa SAFX07;
- `NUM_ITEM`, `COD_PRODUTO`, quantidade, unidade, valores, descontos e rateios são informações de item;
- bases, alíquotas e valores de ICMS, ICMS-ST, IPI, PIS e Cofins devem ser preservados no nível de item;
- documentos de serviços usam SAFX09, não SAFX08; documentos mistos podem exigir ambos.

Chave identificada: `COD_EMPRESA`, `COD_ESTAB`, `MOVTO_E_S`, `NORM_DEV`, `COD_DOCTO`, `IND_FIS_JUR`, `COD_FIS_JUR`, `NUM_DOCFIS`, `SERIE_DOCFIS`, `IND_PRODUTO`, `COD_PRODUTO`, `COD_UND_PADRAO`, `NUM_ITEM`, `COD_MEDIDA`.

## SAFX49 — operações de importação

O SAFX49 é a **Tabela das Operações de Importação**. A estimativa obtida na documentação técnica do processo SAP `ZSAFE041` contém **72 posições**, de `COD_EMPRESA` a `TIPO_DI`.

Estrutura funcional:

1. Identificação: empresa, estabelecimento, data/número da DI, nota e participante.
2. Vínculo do item: produto, item, NCM, unidades, peso e valores.
3. Importação: país e moeda de origem, despesas aduaneiras, II, IPI, ICMS, PIS, Cofins, IOF, data de desembaraço e tipo de DI.
4. Valores de comércio exterior: valor em reais, dólar e moeda da operação, frete/seguro em moeda e ajustes.

Chave estimada: `COD_EMPRESA`, `COD_ESTAB`, `DAT_DI`, `NUM_DI`, `NUM_NF`, `SERIE_NF`, `IND_PRODUTO`, `COD_PRODUTO`, `NUM_ITEM`.

### Relações

```text
SAFX07 (capa da NF)
  └─ SAFX08 (item de mercadoria)
       └─ SAFX49 (detalhe de importação por DI/item, quando aplicável)
```

O SAFX49 deve ser vinculado à NF e ao item correspondente. Não deve haver um registro SAFX49 sem documento/capa e item compatíveis.

### Estratégia de origem para um conversor SPED → SAFX49

O SPED não contém necessariamente todos os dados aduaneiros necessários. Usar:

- `C100`: identificação e valores globais da NF;
- `C170`: item, quantidade, unidade, valores, ICMS e IPI;
- `C120`: tipo/número do documento de importação e PIS/Cofins de importação;
- `0200`: cadastro do item e NCM, quando disponível;
- XML da NF-e, sobretudo os grupos `DI`/`detDI`: necessário para data de desembaraço, país, moeda, adições, II, IOF e valores aduaneiros ausentes no SPED.

Não inferir `DAT_DI` ou `DAT_DESEMBARACO` a partir de `DAT_NF`/`DAT_ENTRADA`; nem inventar II, IOF, país, moeda ou despesas aduaneiras quando não estiverem na origem.

### Formatação estimada do SAFX49

- Uma linha por item de importação; ordenar por empresa, estabelecimento, DI, NF e item.
- Datas: `YYYYMMDD`.
- Nulo: `@` (conforme a convenção dos layouts SAFX07/08 fornecidos).
- Números: sem separador de milhar. A escala decimal não é confirmada por fonte oficial (ver seção de correções do JSON abaixo).
- A documentação técnica encontrada confirma colunas e tamanhos, mas não basta para afirmar o delimitador/encoding de um arquivo físico de carga — **resolvido**: ver "Delimitador confirmado" abaixo.

### Regra técnica encontrada para `VLR_NF`

No processo SAP ZSAFE041, a documentação descreve uma regra interna baseada em:

`VLR_PRODUTO + VLR_FRETE + VLR_SEGURO + VLR_DESP_ADUAN + ICMS + desconto`

com acréscimo de IPI quando aplicável/não escriturado. Tratar como regra de referência SAP, não como regra universal, e validar contra o ambiente MasterSAF de destino.

## Estado dos artefatos criados

- `safx-layout.json`: arquivo único e centralizado, contendo os três layouts unificados — SAFX07 (302 campos), SAFX08 (262 campos) e SAFX49 (72 campos, estimado). O arquivo `safx-layout.with-safx49.json` foi descontinuado; toda a manutenção passa a ser feita apenas em `safx-layout.json`.

## Fontes consultadas

- Manual EFD ICMS/IPI (CONFAZ): https://www.confaz.fazenda.gov.br/legislacao/arquivo-manuais/nota_tecnica_efd_icms_ipi_2023-001_v1-2.pdf
- Portal SPED: https://www.gov.br/sped/pt-br
- Layout MasterSAF DW: https://pdfcoffee.com/manual-layout-mastersafdw-pdf-free.html
- Mapeamento técnico MasterSAF Interface Namespace, processo ZSAFE041: https://pt.scribd.com/document/373036787/Mastersaf-Interface-Namespace-Manual6-Mapeamento-Tecnico-v1
- Histórico de interface SAP/MasterSAF: https://www.docsity.com/pt/docs/mastersaf-interface-namespace-sap-dw-manual-1-leia-me/7171541/
- Job Servidor / FAQ MasterSAF (confirma separador TAB e numeração de campos por tabela): http://www.mastersaf.com.br/HELPDW/V2R010/basicos/job_servidor/oper_safil.htm e faq_safil.htm
- Documentação técnica do registro C100 (EFD ICMS/IPI): https://atendimento.tecnospeed.com.br/hc/pt-br/articles/11938385543191-Registro-C100
- Documentação técnica do registro C170 (EFD ICMS/IPI): https://atendimento.tecnospeed.com.br/hc/pt-br/articles/11953387830551-Registro-C170
- Documentação técnica do registro C120 (EFD ICMS/IPI): https://atendimento.tecnospeed.com.br/hc/pt-br/articles/11952094410007-Registro-C120

---

## Atualização — validação de formatação do `safx-layout.json`

### Delimitador confirmado: TAB

A documentação técnica MasterSAF DW confirma explicitamente: *"Em todos os arquivos SAFX's, entre cada campo, deve ser colocado o separador TAB"*. Isso resolve a lacuna anterior (delimitador "não inferido") e também esclarece um ponto conceitual importante:

> **`position` no layout é a ordem/coluna do campo dentro de um registro delimitado por TAB — não é um offset de largura fixa.** O arquivo físico não é fixed-width; é um TXT com campos separados por TAB, um registro por linha.

### Correções aplicadas ao `safx-layout.json`

O arquivo já unificava SAFX07 + SAFX08 + SAFX49, mas havia inconsistências de formatação entre o SAFX49 (estimado) e os outros dois (fornecidos). Correções feitas:

| Item | Antes | Depois | Motivo |
|---|---|---|---|
| `serialization` (raiz) | sem `delimiter` | `delimiter: "TAB"` + `delimiterSource` + `positionMeaning` | Delimitador confirmado por fonte técnica; esclarece o significado de `position` |
| SAFX49 → `valueType` das datas (`DAT_DI`, `DAT_NF`, `DAT_ENTRADA`, `DAT_DESEMBARACO`) | `"D"` (tipo inventado, não usado em nenhum outro layout) | `"N"` (igual ao padrão SAFX07/08: numérico, size `"008"`, `YYYYMMDD`) | SAFX07/08 nunca usam `"D"`; datas são sempre `N` + 8 dígitos |
| SAFX49 → campo `size` | inteiro (`17`, `7`, etc.) | string zero-padded de 3 dígitos (`"017"`, `"007"`) | SAFX07/08 usam `size` como string; tipo misto (`int`/`str`) no mesmo schema quebrava consistência |
| SAFX49 → `formattingEstimate.delimiter` | "não inferido" | "TAB (confirmado para a família SAFX; não testado isoladamente para o SAFX49)" | Atualiza estimativa com o achado acima, sem elevar a confiança além do que a fonte sustenta |
| SAFX49 → `formattingEstimate.numericEncoding` | genérico | alerta explícito: a notação `NNNVNNNN` (casas decimais) usada em SAFX07/08 (ex. `VLR_ALIQ_ICMS = "003V004"`) **não é reproduzida** no SAFX49 porque a fonte técnica (ZSAFE041) não confirma a escala decimal de cada campo — **não foi inventada** para não violar a regra de não presumir dados sem fonte | Mantém o princípio de "não inventar" já presente neste documento, agora aplicado ao próprio JSON |

Essas mudanças foram feitas apenas em `safx-layout.json` (o arquivo `safx-layout.with-safx49.json` foi removido do projeto e não deve mais ser usado como fonte).

---

## Atualização — validação do código de conversão SPED → SAFX (`SpedToSafx07`, `SpedToSafx08`, `SpedToSafx49`)

Contexto: o projeto já possui um conversor Java (`com.caldeira.service.SpedToSafx07/08/49`) que lê um `SPED.txt` e grava `SAFX07.txt`, `SAFX08.txt` e `SAFX49.txt`. Foi feita uma auditoria campo a campo comparando (a) o parsing do SPED de entrada com o **layout oficial dos registros `C100`, `C170` e `C120`** da EFD ICMS/IPI (fonte: documentação técnica citada acima) e (b) a montagem da saída SAFX com o `safx-layout.json` corrigido.

### Layout oficial confirmado dos registros SPED usados

**C100** (capa do documento fiscal — 28 campos após o `REG`):
`IND_OPER(1) IND_EMIT(2) COD_PART(3) COD_MOD(4) COD_SIT(5) SER(6) NUM_DOC(7) CHV_NFE(8) DT_DOC(9) DT_E_S(10) VL_DOC(11) IND_PGTO(12) VL_DESC(13) VL_ABAT_NT(14) VL_MERC(15) IND_FRT(16) VL_FRT(17) VL_SEG(18) VL_OUT_DA(19) VL_BC_ICMS(20) VL_ICMS(21) VL_BC_ICMS_ST(22) VL_ICMS_ST(23) VL_IPI(24) VL_PIS(25) VL_COFINS(26) VL_PIS_ST(27) VL_COFINS_ST(28)`

Importante: **C100 não possui `VL_BC_IPI`, `VL_BC_PIS` nem `VL_BC_COFINS`** — essas bases só existem no nível de item (`C170`) ou no resumo analítico (`C190`).

**C170** (item do documento — 37 campos após o `REG`):
`NUM_ITEM(1) COD_ITEM(2) DESCR_COMPL(3) QTD(4) UNID(5) VL_ITEM(6) VL_DESC(7) IND_MOV(8) CST_ICMS(9) CFOP(10) COD_NAT(11) VL_BC_ICMS(12) ALIQ_ICMS(13) VL_ICMS(14) VL_BC_ICMS_ST(15) ALIQ_ST(16) VL_ICMS_ST(17) IND_APUR(18) CST_IPI(19) COD_ENQ(20) VL_BC_IPI(21) ALIQ_IPI(22) VL_IPI(23) CST_PIS(24) VL_BC_PIS(25) ALIQ_PIS_PERC(26) QUANT_BC_PIS(27) ALIQ_PIS_REAIS(28) VL_PIS(29) CST_COFINS(30) VL_BC_COFINS(31) ALIQ_COFINS_PERC(32) QUANT_BC_COFINS(33) ALIQ_COFINS_REAIS(34) VL_COFINS(35) COD_CTA(36) VL_ABAT_NT(37)`

Importante: **não existe campo "valor unitário" no C170.** `VL_ITEM` já é o valor total do item, fornecido diretamente — não deve (nem precisa) ser calculado por `quantidade × preço unitário`.

**C120** (complemento de importação — 5 campos após o `REG`):
`COD_DOC_IMP(1) NUM_DOC_IMP(2) PIS_IMP(3) COFINS_IMP(4) NUM_ACDRAW(5)`

Importante: **C120 não tem `DT_DOC` (data da DI) nem valores de ICMS-ST.** `COD_DOC_IMP` é apenas um código de tipo de documento (1-DI, 2-DUIMP, 3-DSI); o número da DI está em `NUM_DOC_IMP`; `PIS_IMP`/`COFINS_IMP` são os valores pagos de PIS/Cofins-Importação, não valores de ICMS. A data da DI **não está disponível no SPED** — confirma o que já constava neste documento: precisa vir do XML da NF-e (`detDI`).

### Inconsistências encontradas no código atual

Todas as três classes (`SpedToSafx07`, `SpedToSafx08`, `SpedToSafx49`) compartilham o mesmo parsing de `C100`/`C170`, então os bugs abaixo se repetem nas três:

1. **Parsing do `C100` desalinhado (bug crítico, maior impacto).** O código lê `codPart = campos[3]`, mas o campo correto é `campos[4]` (o código não contou `IND_OPER` e `IND_EMIT` como dois campos antes de `COD_PART`). A partir daí todos os campos seguintes ficam deslocados e, pior, o código pula direto de `VL_DOC` para valores que ele chama de "ICMS/IPI/PIS/COFINS", mas o `C100` real não tem bases de IPI/PIS/COFINS — o código está lendo, sem saber, os valores de `VL_BC_ICMS_ST`, `VL_ICMS_ST`, `IND_FRT` etc. como se fossem outra coisa. Efeito prático comprovado com o `SPED.txt` de exemplo:
    - `nota.dtDoc` (usado como `DATA_EMISSAO` na saída) recebe o conteúdo de `CHV_NFE` (a chave de acesso, 44 caracteres) em vez da data. Como `formatDate` exige exatamente 8 caracteres, o resultado é uma `DATA_EMISSAO` **vazia** no SAFX07/08 gerado.
    - `nota.vlDoc` (usado como `VLR_TOT_NOTA`) recebe o conteúdo de `DT_E_S` (uma data, ex. `24072026`) interpretado como número, virando `24072026,00` em vez de `3124,16`.
    - `nota.codPart` (usado para montar `COD_FIS_JUR`) recebe `IND_EMIT` (`"0"` ou `"1"`) em vez do código do participante (`"CLI001"`).
    - `nota.numDoc`/`nota.serie` (usados em `SpedToSafx08`/`SpedToSafx49` para `NUM_DOCFIS`/`SERIE_DOCFIS`) recebem `SER`/`COD_SIT` em vez de `NUM_DOC`/`SER`, respectivamente.

2. **Parsing do `C170` desalinhado por 1 posição, em todos os três conversores.** O código começa a ler em `campos[1]` (que é literalmente a string `"C170"`, o identificador do registro) como se fosse `NUM_ITEM`. Isso gera `numItem = 0` sempre (falha silenciosa do `parseDouble("C170")`, capturada e convertida em `0.0`). Do mesmo jeito, `codItem` acaba recebendo o valor de `NUM_ITEM`, `qtd` recebe `DESCR_COMPL` (geralmente vazio → `qtd = 0`), `unid` recebe o valor de `QTD`, e o campo inventado `vlUnit` recebe a `UNID` (texto `"UN"`), cujo parse falha e retorna `0.0`. Como `vlItem = qtd * vlUnit`, o valor do item acaba **sempre zerado**, mesmo a nota tendo itens com valor real.

3. **Conceito de "valor unitário" não existe no `C170`.** O layout já traz `VL_ITEM` (valor total do item) diretamente — não há necessidade (nem informação disponível) para calcular `quantidade × preço unitário`. O código deveria ler `VL_ITEM` diretamente do campo correto e, se precisar de um valor unitário para preencher `VLR_UNIT` no SAFX08, derivá-lo por `VL_ITEM / QTD` (com proteção contra divisão por zero) — não o contrário.

4. **Parsing do `C120` conceitualmente errado (`SpedToSafx49`).** O código assume `campos[2]` = número da DI, `campos[3]` = data da DI, `campos[4]`/`campos[5]` = base/valor de ICMS-ST. Nenhum desses campos existe nessa posição/semântica no `C120` real: `campos[2]` é `COD_DOC_IMP` (tipo do documento, não o número), `campos[3]` é de fato `NUM_DOC_IMP` (o número da DI/DUIMP — aqui por coincidência de índice o valor certo seria pego, mas rotulado errado como "data"), e `campos[4]`/`campos[5]` são `PIS_IMP`/`COFINS_IMP`, não ICMS. **Não existe campo de data da DI no C120** — confirma a orientação já registrada neste documento de não inferir `DAT_DI`/`DAT_DESEMBARACO` a partir de outros campos; esse dado só pode vir do XML da NF-e.

5. **Prefixo de tipo de registro adicionado indevidamente na saída.** As três classes escrevem a linha como `"07"/"08"/"49" + TAB + campo1 + TAB + campo2...`. Isso não corresponde ao layout MasterSAF: cada arquivo `SAFXnn.TXT` já contém apenas registros daquele tipo (não há um "registro de abertura" nem uma coluna de tipo por linha, ao contrário do SPED). Esse prefixo desloca **todos os campos em uma posição** — o campo 1 do arquivo gerado passa a ser `"07"` em vez de `COD_EMPRESA`, o que invalida o arquivo inteiro para importação no MasterSAF.

6. **Uso inconsistente de `String.split`.** `SpedToSafx07` usa `line.split(Pattern.quote("|"), -1)` (preserva campos finais vazios), enquanto `SpedToSafx08` e `SpedToSafx49` usam `line.split("\\|")` sem o limite `-1` — o comportamento padrão do Java remove tokens vazios no final da string. Isso pode derrubar o último campo de uma linha (ex. quando o SPED termina com `|` seguido de nada) e causar `ArrayIndexOutOfBoundsException` em linhas mais "no limite". Recomenda-se padronizar todas as classes para usar `-1`.

7. **Array de saída do SAFX07 menor que o layout real.** `gerarLinha07` aloca `String[300]` e escreve `campos.length` (300) posições, mas o `safx-layout.json` confirma que o SAFX07 tem **302 posições**. As duas últimas colunas obrigatórias do layout (`NUM_AUTENTIC_NFE_SUBST`, posição 301, e `IND_VLR_ICMS_COB_ANT_ST`, posição 302) nunca são escritas — o arquivo gerado sempre tem 2 colunas a menos que o esperado pelo importador.

8. **Layout do SAFX49 gerado não corresponde ao layout real (72 campos).** `gerarLinha49` monta um array de apenas ~30 campos, com nomes e posições "inventados" (o próprio comentário no código admite: *"Como não temos o leiaute, vou criar uma linha com cerca de 30 campos"*). Isso já foi corrigido a nível de layout (`safx-layout.json` tem os 72 campos reais, estimados a partir do processo `ZSAFE041`), mas o gerador de linha ainda não usa esse layout. Recomenda-se reescrever `gerarLinha49` para montar o array com 72 posições, nos nomes/ordem do `safx-layout.json`, preenchendo com `@` os campos que o SPED genuinamente não fornece (em especial `DAT_DI` e `DAT_DESEMBARACO`, que exigem XML da NF-e) em vez de tentar aproximar com dados de outros campos.

9. **Ponto positivo confirmado por amostragem:** o posicionamento de saída do `SpedToSafx07` (isto é, em qual posição do array cada campo do layout SAFX07 é escrito) foi conferido contra o `safx-layout.json` em ~25 posições espalhadas pelo arquivo (`VLR_PRODUTO`, `VLR_TOT_NOTA`, `VLR_ICMS`, `VLR_IPI`, `BASE_TRIB_ICMS`, `VLR_BASE_PIS`, `VLR_COFINS`, `NUM_AUTENTIC_NFE`, `COD_MODELO_COTEPE`, `DAT_LANC_PIS_COFINS`, `IND_NAT_FRETE`, entre outras) e **bateu em todos os casos verificados**. Ou seja: o problema não está em "para onde" os dados vão dentro do SAFX07, e sim em "de onde" eles vêm dentro do SPED (itens 1–4 acima) e no prefixo indevido (item 5) e no tamanho do array (item 7).

### Status

**Correção aplicada e validada** nas três classes (`SpedToSafx07`, `SpedToSafx08`, `SpedToSafx49`), compiladas com `javac` e reexecutadas contra o `SPED.txt` de exemplo. Mudanças feitas:

1. Parsing do `C100` corrigido em todas as três classes, usando os índices reais do array (`campos[0]` vazio, `campos[1]="C100"`, `COD_PART` em `campos[4]`, etc., conforme o layout oficial de 28 campos documentado acima).
2. Parsing do `C170` corrigido (índices reais a partir de `campos[2]=NUM_ITEM`), e `VL_ITEM` passou a ser lido diretamente do campo 6 (`campos[7]`) em vez de `quantidade × preço unitário`; `VLR_UNIT` agora é derivado por `VL_ITEM / QTD` (com proteção contra divisão por zero).
3. Parsing do `C120` corrigido em `SpedToSafx49` (`COD_DOC_IMP`, `NUM_DOC_IMP`, `PIS_IMP`, `COFINS_IMP` nos índices corretos); os campos inventados `dtDi`/`vlBcIcmsSt`/`vlIcmsSt` (que não existem no C120) foram removidos da classe `Importacao`.
4. Removido o prefixo `"07"`/`"08"`/`"49"` da primeira coluna das três linhas geradas — cada arquivo `SAFXnn.TXT` agora começa diretamente pelo campo 1 (`COD_EMPRESA`).
5. Padronizado `line.split(Pattern.quote("|"), -1)` nas três classes (antes `SpedToSafx08`/`SpedToSafx49` usavam `split("\\|")` sem o limite `-1`).
6. `gerarLinha07` agora aloca `String[302]` (antes 300), cobrindo as 302 posições do layout.
7. **Bug adicional encontrado durante a validação (não estava no diagnóstico original):** `SpedToSafx07.convertFromSped` escrevia, para cada nota, uma linha de capa (`gerarLinha07`) seguida de uma linha de item usando um método `gerarLinha08` local — duplicado, incompleto (30 campos) e diferente do `SpedToSafx08.java` real — misturando registros de item dentro do arquivo `SAFX07.txt`. Esse método duplicado e sua chamada foram removidos; `SAFX07.txt` agora contém **apenas** linhas de capa, e os itens são gerados exclusivamente por `SpedToSafx08.java`.
8. `gerarLinha49` foi reescrito do zero para as 72 posições reais do layout (antes ~30 campos "inventados"). Os campos que o SPED genuinamente não fornece (`DAT_DI`, `DAT_DESEMBARACO`, país/moeda de origem, II, IOF, despesas aduaneiras, frete/seguro em moeda, valores em dólar) permanecem `@`, conforme o princípio de não inventar dados sem fonte. `COD_NBM` agora é preenchido a partir do registro `0200` do produto, quando disponível (dado real que já era parseado mas não usado).

### Validação com o `SPED.txt` de exemplo (após a correção)

Reexecutando as três classes contra o `SPED.txt` de exemplo (1 registro `C100`, 2 registros `C170`, nenhum `C120`):

- `DATA_EMISSAO` (SAFX07, posição 11) passou de vazio para `20260724` (correto, batendo com `DT_DOC`).
- `VLR_TOT_NOTA` (SAFX07, posição 23) passou de `24072026,00` (a data mal interpretada) para `0000000000312416` = `3124,16`, batendo exatamente com `VL_DOC` do `C100`.
- `COD_FIS_JUR` (SAFX07/08, posição 7/8) passou de `M0`/`M1` (o `IND_EMIT`) para `MCLI001` (o `COD_PART` real).
- `VLR_ICMS`, `VLR_IPI`, `BASE_TRIB_ICMS`, `BASE_TRIB_IPI` (SAFX07) batem exatamente com a soma dos itens: ICMS `562,36` (`281,18 × 2`), IPI `133,82` (`66,91 × 2`), base ICMS `3124,14`, base IPI `2058,92` — os mesmos valores documentados no `C170` de exemplo.
- `VLR_UNIT`/`VLR_ITEM` (SAFX08) passaram de `0,00`/`0,00` (sempre zerados) para `12,49`/`1562,07`, batendo com `VL_ITEM ÷ QTD` e `VL_ITEM` do `C170`.
- `SAFX07.txt` e `SAFX08.txt` não têm mais o prefixo `"07"`/`"08"` como primeira coluna, e `SAFX07.txt` não contém mais linhas de item.
- `SAFX49.txt` ficou vazio (0 linhas), como esperado — a amostra não tem nenhum registro `C120`, então nenhuma nota é tratada como operação de importação.

**Limitação encontrada na amostra `SPED.txt` (não é bug do código):** o registro `C170` de exemplo tem 3 campos a menos do que os 37 campos obrigatórios do layout oficial (a contagem de campos delimitados por `|` pára antes de `VL_ABAT_NT`). Isso faz com que, especificamente para essa amostra, os valores extraídos de `VL_PIS`/`VL_COFINS` fiquem incorretos (o parser, seguindo o índice oficial, lê posições que na amostra contêm outros dados ou ficam fora do array). O parser foi corrigido para seguir fielmente a especificação oficial documentada (fonte: Tecnospeed, citada acima) — o problema está no arquivo de exemplo, que não é 100% aderente ao layout do SPED, e não no código. Recomenda-se regenerar/completar o `SPED.txt` de exemplo com os 37 campos completos do `C170` para validar também a extração de PIS/COFINS de ponta a ponta.

## Próximo passo recomendado

1. ~~Aplicar as correções de parsing/geração descritas acima nas três classes Java do projeto.~~ **Feito.**
2. ~~Reexecutar a conversão sobre o `SPED.txt` de exemplo e conferir manualmente algumas linhas geradas contra o `safx-layout.json`.~~ **Feito — ver seção de validação acima.**
3. Corrigir o `SPED.txt` de exemplo para ter os 37 campos completos do `C170` (ver limitação acima) e revalidar PIS/COFINS.
4. ~~Buscar um `SPED.txt` de exemplo que contenha um registro `C120` (operação de importação) para validar de fato o caminho do `SAFX49`.~~ **Feito — ver seção "Segunda rodada de validação" abaixo.**
5. Quando possível, obter o XML da NF-e correspondente para preencher os campos do SAFX49 que o SPED não fornece (`DAT_DI`, `DAT_DESEMBARACO`, país/moeda, II, IOF).

---

## Segunda rodada de validação — SPED.txt real (12 C100, 83 C170, 12 C120)

O usuário reexecutou a conversão usando um `SPED.txt` real e maior (o arquivo de recursos do projeto em `IdeaProjects/safx-formatter`, referenciado pelo caminho absoluto hardcoded em `Main.java`), com 12 registros `C100`, 83 `C170` e — pela primeira vez — 12 registros `C120` (operação de importação). Isso permitiu validar o caminho do `SAFX49` de ponta a ponta, o que a amostra anterior não cobria.

### Resultado: contagens batem exatamente

- `SAFX07.txt`: 12 linhas = 12 `C100` ✓
- `SAFX08.txt`: 83 linhas = 83 `C170` ✓
- `SAFX49.txt`: 83 linhas = todos os itens de todas as notas, pois as 12 notas têm `C120` associado (12 C100 = 12 C120 nesta amostra) ✓
- Todas as linhas têm exatamente 302 / 262 / 72 campos, sem exceções de parsing.

### Conferência campo a campo (nota 1, item 1)

Valores extraídos diretamente do `SPED.txt` (`C100`: `COD_PART=9820939`, `NUM_DOC=271`, `SER=002`, `DT_DOC=20052026`, `VL_DOC=3124,16`; `C170` item 1: `QTD=2,00`, `VL_ITEM=12,49`) batem exatamente com a saída: `COD_FIS_JUR=M9820939`, `NUM_DOCFIS=000000000271`, `SERIE_DOCFIS=002`, `DATA_EMISSAO=20260520`, `VLR_TOT_NOTA=3124,16`, `QUANTIDADE=2,00`, `VLR_ITEM=12,49`, `VLR_UNIT=6,25` (`=12,49÷2,00`).

### Novo bug encontrado (não fazia parte do diagnóstico anterior): parsing do registro `0200` desalinhado

Ao validar o `SAFX49.txt` gerado, a coluna `COD_NBM` (NCM, posição 15) veio `@` em **todas** as 83 linhas, apesar do `SPED.txt` conter 24 registros `0200` com NCM preenchido. Investigando, o parsing do `0200` em `SpedToSafx08.java` e `SpedToSafx49.java` tinha o mesmo padrão de bug já corrigido em `C100`/`C170`/`C120`: usava `campos[1]` (o literal `"0200"`, identificador do registro) como `COD_ITEM`, em vez de `campos[2]`.

Efeito prático: como toda `Produto` criada era inserida no mapa com a chave fixa `"0200"`, o mapa de produtos só guardava **um único produto** (o último `0200` lido), e a busca posterior por `produtos.get(item.codItem)` (com um código de item real, ex. `"10529ABV0033LBSD"`) nunca encontrava nada — daí o `COD_NBM` sempre `@`. Os campos `unidade`/`ncm` também liam colunas erradas (`COD_ANT_ITEM` e `UNID_INV` em vez de `UNID_INV` e `COD_NCM`).

Layout oficial do `0200` usado na correção: `COD_ITEM(1) DESCR_ITEM(2) COD_BARRA(3) COD_ANT_ITEM(4) UNID_INV(5) TIPO_ITEM(6) COD_NCM(7) EX_IPI(8) COD_GEN(9) COD_LST(10) ALIQ_ICMS(11)` (com `campos[1]="0200"`, logo `COD_ITEM` em `campos[2]` e `COD_NCM` em `campos[8]`).

**Correção aplicada:**
- `SpedToSafx08.java` e `SpedToSafx49.java`: `p.codItem = campo(campos, 2)`, `p.descricao = campo(campos, 3)`, `p.unidade = campo(campos, 6)`, `p.ncm = campo(campos, 8)`.
- `SpedToSafx08.java` também passou a **usar** o mapa de produtos (antes parseado mas nunca consultado — `COD_NBM` ficava vazio por design, com comentário "não temos NCM diretamente"), preenchendo `COD_NBM` (posição 26) quando há um `0200` correspondente.

**Validação pós-correção:** recompilado e reexecutado contra o mesmo `SPED.txt` real — `COD_NBM` agora aparece preenchido em todas as 83 linhas de `SAFX08.txt` e `SAFX49.txt`, com uma distribuição plausível de NCMs reais (ex. `38249989` em 28 itens, `38249929` em 26, etc.), idêntica nos dois arquivos.

### Status final

Nenhuma outra inconsistência encontrada nesta rodada: contagens de registro corretas, todos os campos numéricos batendo com a soma/origem no SPED, nenhuma exceção de parsing, nenhum campo faltando ou com tamanho de linha incorreto.

---

## Terceira rodada — validação automatizada campo a campo (100% do arquivo) + correção do VLR_DESCONTO

Um "relatório de erros" externo foi recebido e verificado; **todos os itens do relatório eram falsos positivos**, causados por comparar a saída atual (gerada a partir do `SPED.txt` real de 212 linhas) contra valores esperados calculados em cima do `SPED.txt` de amostra pequeno usado na primeira rodada de validação (arquivos de entrada diferentes, não um bug de conversão). Casos verificados e refutados: `COD_FIS_JUR`, `SERIE_DOCFIS`, `DATA_EMISSAO`, `VLR_PRODUTO` (o relatório confundiu com `VL_BC_ICMS`), `VLR_OUTRAS` (bate exatamente com `VL_OUT_DA`), `CHAVE_NFE`, `COD_CFO`, `QUANTIDADE`, `VLR_UNIT`/`VLR_ITEM`, CSTs de ICMS/PIS/COFINS, `DESCRICAO_COMPL`, `COD_NBM`, e a própria existência do `SAFX49.txt` (o SPED real tem 12 registros `C120`, o pequeno não tinha nenhum).

Durante essa verificação, encontrei um bug real (pequeno): `VLR_DESCONTO` (SAFX07, posição 28) estava hardcoded como `0.0` no código, em vez de usar `nota.vlDesc` (que já era corretamente parseado do `VL_DESC` do C100, mas nunca usado). Para a nota de exemplo isso não alterava o resultado (`VL_DESC` real também é `0,00`), mas para uma nota com desconto real o campo sairia sempre zerado. **Corrigido** em `SpedToSafx07.java` (linha ~236-237): `campos[27] = formatNumber(nota.vlDesc)`.

### Validação automatizada de 100% dos registros (não apenas amostras)

Escrevi um script Python que reimplementa o mapeamento oficial C100/C170/C120/0200 → SAFX07/08/49 (o mesmo mapeamento documentado acima) e comparou, campo a campo, **todas as 12 notas, todos os 83 itens e todos os 12 registros de importação** dos arquivos gerados contra os valores calculados diretamente do `SPED.txt` real. Resultado:

- **SAFX07: 0 divergências** em 21 campos verificados × 12 notas.
- **SAFX08: 0 divergências** em 30 campos verificados × 83 itens.
- **SAFX49: 0 divergências** em 22 campos verificados × 83 itens (todas as 12 notas têm `C120` associado).
- Contagem de linhas, contagem de colunas por linha (302/262/72) e ausência de exceções de parsing confirmadas em 100% do arquivo, não apenas na primeira linha.

**Conclusão: nesta data, com este `SPED.txt`, os três arquivos `SAFXnn.TXT` gerados estão corretos e não apresentam nenhuma inconsistência conhecida.**