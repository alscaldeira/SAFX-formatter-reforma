# Regras de conversão: sistema em C# → aplicativo Java

Mapeamento das regras de conversão XML → SAFX que existem no sistema em C#
(`ABMC.RANFE.LIGHT_SQL`) e onde cada uma foi replicada no Java
(`código-fonte-reforma`). Cada regra aponta a origem no C# e o destino no Java.

## 1. Visão geral

O sistema em C# converte em três frentes independentes, todas sobre o mesmo lote de XMLs:

| Frente | Origem no C# | Saída | Java (antes) | Java (agora) |
|---|---|---|---|---|
| NF-e → SAFX07/08/04/2013/2043 | `Funcoes.cs` (carga em `INT_NFE_*`) + `ScriptSQL/*.txt` | abas do XLSX | **não existia** | `NfeParaSafx` |
| CT-e → SAFX07/08/04 | `HomeController.LerCTes` | mesmas abas | **não existia** | `CteParaSafx` |
| Classificação do lote | `ClassificadorXml.cs` | — | **não existia** | `ClassificadorXml` |
| Reforma (SAFX3007/3008/3009) | `ReformaTributaria/*` | abas + arquivos | já existia | inalterada (saída idêntica, conferida byte a byte) |
| SAFX04 de participantes sem de-para | `XmlToSafxCadastro.cs` | aba SAFX04 | **não existia** | `XmlToSafxCadastro` |

Orquestração: `HomeController.Index` (C#) → `ConversorXml.converter` (Java).

### Fluxo no C# e o equivalente no Java

| Passo no C# (`HomeController.Index`) | Java (`ConversorXml.converter`) |
|---|---|
| `ClassificarPasta`: NF-e / CT-e / NFS-e / desconhecido pelo conteúdo | `ClassificadorXml.classificar` por arquivo |
| CT-e movido para pasta própria → `LerCTes` (dentro de try/catch; falha vira aviso) | `CteParaSafx.converter` (mesmo isolamento) |
| `CarregarArquivo1` + `NormalizarXMLNFe` + 5 scripts SQL | `NfeParaSafx.converter` (direto do XML, sem banco) |
| `RemoverParticipantesRepetidos` + `Mesclar` (CT-e e Reforma entram na aba SAFX04) | mescla em `ConversorXml`; um único SAFX04 |
| `ConversorXmlReforma.Converter` (try/catch; falha vira aviso) | `reformaTributaria(...)`; falha vira aviso se há NF-e/CT-e, senão é propagada |
| Avisos de lote (só NFS-e, nada reconhecido, arquivo ignorado) | idem, em `avisos` |
| Checkboxes "Calcular PIS/COFINS do CT-e" e "Calcular ICMS do CT-e" | `TelaPrincipal` (duas `JCheckBox`) → `Conversao.executar(caminho, calcPisCofins, calcIcms)` |

Saída no Java: `SAFXnn.txt` (TAB, `@` nos nulos) e `SAFXnn.csv` (`;`, BOM) em `user.dir`, na **ordem
de colunas dos scripts SQL** (extraída para `src/main/resources/safx-nfe-colunas.txt`; é a ordem das abas
que o C# exporta).

## 2. Classificação do lote (`ClassificadorXml.cs` → `ClassificadorXml.java`)

1. Raiz com `infNFSe` → **NFS-e** (testado primeiro: não tem `ide/mod`).
2. `infCte` é procurado **antes** de `infNFe`: o CT-e carrega `infDoc/infNFe`, e a ordem inversa
   classificaria todo CT-e com documento vinculado como NF-e (bug corrigido no commit `29e4a94`).
3. `ide/mod`: 55 e 65 → NF-e; 57 e 67 → CT-e. Sem modelo, o nome do nó desempata.
4. XML ilegível ou de outro tipo (evento, inutilização) → desconhecido. Na Java ele segue para o leitor da
   Reforma, que mantém o comportamento anterior (ignora com aviso, ou aponta o erro de leitura).

## 3. NF-e → SAFX (`Funcoes.cs` + `ScriptSQL`)

### 3.1 Regras da carga (`GravarXml310` / `NormalizarXMLNFe`)

| Regra no C# | Java (`NfeParaSafx`) |
|---|---|
| Versão: `Convert.ToInt32(versao)` (2.00→2, 3.10→3, 4.00→4) | `Valores.inteiro` |
| NF-e 2.00 lê `dEmi`/`hSaiEnt`; demais `dhEmi`/`dhSaiEnt` | `versao == 2` |
| **tpNF=1**: a "contraparte" (`INT_NFE_NOTA_DEST`) é o `dest`; o `emit` é o outro lado | `saida == true` |
| **tpNF≠1**: a contraparte é o `emit` e o `dest` ocupa o lugar do emitente | `saida == false` |
| CNPJ do `dest`: `CNPJ`, senão `idEstrangeiro`, senão `CPF` | `contraparte()` |
| IE do `dest`: vazia→0; `isento`→99; senão `Convert.ToInt64` (não numérica **descarta a nota inteira**) | idem, com aviso |
| IE do `emit` (tpNF≠1): vazia→0; senão `ToInt64` (sem tratar `isento`) | idem |
| `fone` é `Int32.TryParse`: 11 dígitos ou símbolo viram **0** | idem |
| `CEP` e `cMun` convertidos para inteiro | idem |
| Item só vai para `INT_NFE_LINHA_IMP_ICMS` quando há grupo `ICMS`; o SAFX08 cruza por `id_linha`, então **item sem ICMS não sai no SAFX08** | idem, com aviso |
| Totais ausentes valem 0 (`Convert.ToDecimal(null)`) | `Valores.decOuZero` |
| Chave = `Id[3..47]`; NF-e repetida no lote não é duplicada | descarta a repetida, com aviso |

Grupos de ICMS lidos (campo → grupos que o preenchem), exatamente os do `Funcoes.cs`:

| Campo | Grupos |
|---|---|
| `orig`, `CST` | ICMS00/10/20/30/40/51/60/70/90/Part/ST (`CST`); os `ICMSSN*` não têm `CST` (ficam nulos) |
| `vBC`, `pICMS`, `vICMS` | 00, 10, 20, 51, 70, 90, Part, SN900 |
| `vICMS` (só) | 40 |
| `pICMSST`, `vICMSST` | 10, 30, 70, 90, Part, SN201, SN202, SN900 |

### 3.2 SAFX07 — capa (`ScriptSQL/SAFX07.txt`)

| Coluna | Regra |
|---|---|
| `COD_EMPRESA` / `COD_ESTAB` | pelo CNPJ da **contraparte**: `03823704000152/…314/…233`→001; `03848527000169`→002; `04613348000105`→003; `22725405000120/…200`→004; fallback 001. Estab: `…152,…169,…105,…120`→0001; `…314,…200`→0002; `…233`→0003; fallback **0009** (`EmpresasFixas`) |
| `MOVTO_E_S` | `tpNF=0`→`4`; senão `9` |
| `NORM_DEV` | `finNFe=4`→`2`; senão `1` |
| `COD_DOCTO` / `COD_MODELO` / `COD_MODELO_COTEPE` | `NFE` / `55` / `55` fixos |
| `IDENT_FIS_JUR`, `SERIE_DOCFIS` | `2` e `1` fixos (a série do XML **não** é usada) |
| `NUM_DOCFIS` | `nNF` em 12 posições |
| `DATA_EMISSAO`, `DATA_SAIDA_REC` | data de emissão (as duas) |
| `COD_CFO` | CFOP do item; entrada (`tpNF=0`) com 2207 vira **6207** |
| `VLR_PRODUTO`, `VLR_TOT_NOTA` | `vProd`, `vNF` do `ICMSTot`: centavos, 17 posições |
| `VLR_FRETE`, `VLR_SEGURO`, `VLR_OUTRAS`, `VLR_SUBST_ICMS`, `VLR_ALIQ_IPI` | totais do `ICMSTot` **sem formatar** (`vFrete`, `vSeg`, `vOutro`, `vST`, `vIPI`). Sim: `VLR_ALIQ_IPI` recebe o `vIPI` total |
| `BASE_TRIB_ICMS`, `BASE_ICMS_ORIGDEST` | `vBC` total (17); `VLR_ICMS_ORIGDEST` = `vICMS` total |
| `BASE_OUTR_ICMS`, `BASE_OUTR_IPI` | `vProd` total (17) |
| `CONTRIB_FINAL` | `indIEDest=1`→`S`; senão `N` (sempre `N` em tpNF≠1) |
| `SITUACAO`, `VLR_ALIQ_ICMS`, `VLR_ICMS`, `DIF_ALIQ_ICMS`, `VLR_IPI` | `N`, `0`, `0`, `0`, `0` |
| `NUM_CONTROLE_DOCTO` | `R` + `nNF` (sem zeros) |
| `IND_TP_FRETE` | contraparte da lista de estabelecimentos: `modFrete` 0→1, 1→2, 2 e 9→0, demais passam (4.00 mantém 3 e 4; 3.10 também passa os demais). Fora da lista: 0→1, 1→2, demais passam; sem grupo `transp`→0 |
| `IND_FATURA` | `indPag` 0→`1`, 1→`2`, outro→nulo (`indPag` ausente na 4.00 vale 0 → `1`) |
| `VLR_BASE_PIS/COFINS`, `VLR_PIS/COFINS` | do `PISAliq`/`COFINSAliq` (17 posições) |
| `VLR_ALIQ_PIS/COFINS` | `right('0000000' + pPIS×100 + '00', 7)`: 1,65→`0016500`, 7,6→`0076000` |
| `COD_SITUACAO_A` / `_B` | `orig` / `CST` do ICMS **como inteiro** (CST 00 sai `0`) |
| `COD_SIT_PIS/COFINS` | UF da contraparte `EX`→CST do `NT`; senão CST do `Aliq`; 2 dígitos |
| `UF_ORIG_DEST`, `UF_ARMAZ_ORIG` | UF do outro lado (emitente em saída) |
| `UF_DESTINO`, `UF_ARMAZ_DEST` | UF da contraparte |
| `IND_SITUACAO_ESP` | `finNFe=2`→`5` |
| `IND_COMPRA_VENDA`, `IND_NAT_FRETE` | `CP`, `9` |
| `NUM_AUTENTIC_NFE`, `NUM_DV_NFE` | chave de 44 dígitos, `cDV` |

### 3.3 SAFX08 — itens (`ScriptSQL/SAFX08.txt`)

Uma linha por item **com ICMS**, ordenada por `NUM_DOCFIS, NUM_ITEM`. Campos comuns à capa (empresa,
estab, movimento, `NORM_DEV`, série, CFOP com a regra 2207→6207, `IND_FIS_JUR=2`) seguem 3.2.

| Coluna | Regra |
|---|---|
| `DATA_FISCAL` | data de emissão |
| `COD_PRODUTO`, `COD_MEDIDA`, `COD_UND_PADRAO`, `COD_NBM` | `cProd`, `uCom`, `uTrib`, `NCM` |
| `DESCRICAO_COMPL` | `xProd` cortado em 50 e **sem acento** (mapa do `TiraAcento`) |
| `QUANTIDADE` | `qCom` com 4 casas + `00`, 17 posições |
| `VLR_UNIT` | `vUnCom` com 4 casas, 19 posições |
| `VLR_ITEM`, `VLR_CONTAB_ITEM`, `BASE_IPI` | `vProd` (17) |
| `VLR_DESCONTO`, `VLR_SEGURO` | `vDesc` e `vSeg` **do total da nota**, repetidos em cada item |
| `COD_FEDERAL` | `orig` 0→`1`, 1→`2`, 2→`3`; outro→nulo |
| `PESO_LIQUIDO` | `pesoL` do 1º `vol`, 3 casas, 14 posições |
| `VLR_FRETE`, `VLR_OUTRAS`, `VLR_OUTROS1` | `vFrete`, `vOutro` do item, sem formatar |
| `VLR_ALIQ_ICMS` / `VLR_ICMS` | `pICMS` (2 casas, **7 posições**: 18,00→`0001800`) / `vICMS` (17) |
| `VLR_ALIQ_IPI` / `VLR_IPI` | `pIPI` (7) / `vIPI` (17), só com `IPITrib` |
| `VLR_ALIQ_SUB_ICMS`, `VLR_SUBST_ICMS` | `pICMSST`, `vICMSST`, sem formatar |
| `TRIB_ICMS` | CST 0,10,30,70→`1`; 40,41,50,51,60→`2`; senão (e SN)→`3` |
| `TRIB_IPI` | **usa o CST do ICMS**: 0 e 1→`1`; 2 a 5→`2`; senão `3` |
| `BASE_ICMS` | `vBC` do ICMS; sem `vBC`→`vProd` |
| `COD_TRIB_IPI` | sempre `53` (o `!= null` do SQL nunca é verdadeiro) |
| `COD_SITUACAO_PIS/COFINS` | `EX`→`NT` (ou `Outr`); senão `Aliq` (ou `Outr`); 2 dígitos |
| `DAT_LANC_PIS_COFINS` | `dhSaiEnt`, ou a emissão se ausente |
| `IND_BEM_PATR`, `IND_PRODUTO`, `IND_IPI_INCLUSO`, `IND_VLR_PIS_COFINS`, `IND_NATUREZA_FRETE` | `N`, `1`, `N`, `S`, `9` |

### 3.4 SAFX04 (`SAFX04.txt`), SAFX2013 (`SAFX2013.txt`), SAFX2043 (`SAFX2043.txt`)

- **SAFX04** (um por nota, sem dedupe, como no SQL): `IND_FIS_JUR=2`; `COD_FIS_JUR`=CNPJ da contraparte;
  `DATA_X04`=AAAAMM01; `UF=EX`→`IND_CONTEM_COD=4`, `CPF_CGC` vazio, `INSC_ESTADUAL=ISENTO`; razão social e
  fantasia = `xNome` (50); bairro 20; `CEP` 8 posições; `COD_PAIS` = 3 primeiros dígitos de `cPais`
  (1058→105); `COD_MUNICIPIO` = posições 3 a 7 do IBGE.
- **SAFX2013** (um por item): `IND_PRODUTO=1`, `DATA_PRODUTO`=AAAAMM01, descrição 50, `CLAS_ITEM=99`.
- **SAFX2043** (um por item): `DATA_NBM=0`, `SECAO_IPI=0`, `NOTA_SECAO_IPI=0`, descrição inteira.

## 4. CT-e → SAFX07/08/04 (`HomeController.LerCTes` → `CteParaSafx`)

| Regra | Detalhe |
|---|---|
| Raízes aceitas | `cteProc`, `cteOSProc`, `CTe`, `CTeOS`; sem `infCte`, sem `infProt` ou sem `enderEmit` → documento ignorado **com aviso nomeando o grupo** |
| Texto | acentos removidos (`TiraAcento`) |
| CT-e **próprio** | `emit/CNPJ = 42105890000901` (`EmpresasFixas.MEU_CNPJ`) |
| CFOP de terceiro | 1000–3999 → **+4000**; 5000–7999 → **−4000** (próprio fica como está) |
| Grupo ICMS | primeiro filho de `imp/ICMS` (não fixo em `ICMS00`); diferente de `ICMS00` gera aviso; sem grupo → ignorado |
| `MOVTO_E_S` | próprio `9`; terceiro `1` |
| `COD_DOCTO` | modelo 57 → `CT-E`; senão `CTE-OS` |
| `NUM_DOCFIS` | `nCT` em 12 posições |
| Datas | `dhEmi` (emissão e fato gerador) e `dhRecbto` (saída/recebimento e lançamento), **na hora de parede do documento** (23:30-03:00 continua no mesmo dia) |
| `COD_EMPRESA`/`COD_ESTAB` | pelo CNPJ do `dest`, mesma tabela da NF-e |
| `COD_MUNICIPIO_ORIG/DEST` | `exped/enderExped/cMun`, `dest/enderDest/cMun` sem os 2 primeiros dígitos (ausente → um espaço) |
| `VLR_PRODUTO` | `vTPrest`; emitente **"FRETE VALOR"** (maiúsculas) fatura `vComp` da 1ª `Comp` |
| `IND_TP_FRETE` | `toma3/toma` (senão `toma4/toma`): 0→`1`, 3→`2`, outro valor passa |
| `COD_SITUACAO_B` (07) | CST do XML; `BASE_OUTR_ICMS`=`vBC` **só quando não calcula ICMS** |
| Calcular PIS/COFINS | alíquotas `0016500`/`0076000`, `COD_SIT` `56`; base = `vTPrest` (próprio) ou `vTPrest − vICMS` (terceiro); PIS = base×1,65%, COFINS = base×7,6%, arredondados a 2 casas (arredondamento bancário, como `decimal.Round`). Desmarcado: tudo zero e `COD_SIT` `70` |
| SAFX08 | 1 item (`NUM_ITEM=1`), `COD_PRODUTO=FRETES`, `IND_PRODUTO=4`, `COD_TRIB_IPI` `53` (próprio) / `03` (terceiro), `VLR_UNIT` = `vTPrest`×10000 (19), `DESCRICAO_COMPL` = `compl/xObs` |
| Calcular ICMS | `COD_SITUACAO_B`=CST, `VLR_ALIQ_ICMS`=`pICMS`×10000 (7: 18,00→`0180000`), `VLR_ICMS`, `TRIB_ICMS=1`. Desmarcado: CST `90`, alíquota/valor zerados, `TRIB_ICMS=3` |
| SAFX04 do CT-e | `COD_FIS_JUR`/`CPF_CGC`=CNPJ cru do emitente; telefone = 10 últimas posições; `COD_PAIS` 105 como fallback; `COD_MUNICIPIO` = 5 últimas; `IND_CONTEM_COD` = `4` se **`rem`** (não o emitente) não tem CNPJ; `IND_FIS_JUR=2`; **uma linha por CT-e**, sem dedupe (como a planilha de referência; o C# ainda deduplica por CNPJ) |
| Campos fixos | `FIXOS_SAFX07/08/04` copiados 1:1 (valores iguais aos dos scripts de NF-e) |
| `COD_CONTA` (07 e 08) | constante `191000`, sem origem no XML; vem da planilha de referência (98/98) e do mapeamento do layout. O `LerCTes` do C# ainda deixa em branco |
| `SUB_SERIE_DOCFIS` (07 e 08) | `@` nos CT-e (planilha de referência) |

## 5. Reforma Tributária e SAFX04 de participantes sem de-para

- SAFX3007/3008/3009: já existiam no Java; a saída foi comparada com a do código anterior sobre os XMLs de
  exemplo e é **idêntica** (`.txt` e `.csv`).
- `XmlToSafxCadastro` (novo): SAFX04 só para quem **não** tem de-para em `participantes.properties`; NF-e
  de saída fica de fora (o SAFX04 de NF-e já traz o destinatário); roda depois do `XmlToSafxReforma`, que
  é quem popula a lista de "sem cadastro". CPF/CNPJ vão sem máscara; campos cortados no tamanho do layout.
- Os três SAFX04 (NF-e, CT-e, Reforma) saem num arquivo só, nessa ordem, com as **80 colunas** do leiaute (as 19 finais saem `@`).

## 6. Divergências deliberadas em relação ao C#

| Tema | C# | Java | Por quê |
|---|---|---|---|
| Fuso da NF-e | `Convert.ToDateTime(dhEmi)` converte para a hora do servidor (em IIS/UTC, NF-e emitida após 21h sai com a data do dia seguinte) | usa a data escrita no documento | mesma correção que o commit `29e4a94` fez no CT-e |
| Join de PIS/COFINS/IPI no SAFX08 | cruza por `id_controle`, sem `id_linha`: nota de N itens gera até N×N linhas (o `distinct` só junta as idênticas) | cada item usa o **seu** PIS/COFINS/IPI | o produto cartesiano é efeito do script, não regra |
| SAFX07 de nota com vários itens | mesma causa: várias linhas para a mesma nota | **uma linha por nota**; bases e valores de PIS/COFINS **somados** nos itens; CFOP, CST e alíquotas do 1º item | chave do SAFX07 é a nota |
| `SAFX2043` | o laço da aba usa `dataTable4.Rows.Count` (de outro script) | uma linha por item | erro de digitação do original |
| Banco | precisa de SQL Server (`INT_NFE_*`, `DAO_SAFX.ClearAllRecords`) | direto do XML; nada a limpar | — |
| Arquivo | XLSX com uma aba por layout | `.txt` TAB + `.csv`, na mesma ordem de colunas | formato já usado pelo app Java |
| Nulo | célula vazia (SAFX08 vira `@`) | `@` em todos os layouts, exceto `SUB_SERIE_DOCFIS` (vazio) | convenção do SAFX e do Java |

## 7. Comportamentos suspeitos do C# que foram **replicados** (para decisão de vocês)

Reproduzidos porque são o que o sistema em produção faz hoje. Se algum for bug a corrigir, é mudança de
regra e deve ser feita nos dois sistemas.

1. `VLR_ALIQ_ICMS` da NF-e = `pICMS` com 2 casas em 7 posições (18,00 → `0001800`), enquanto o CT-e usa
   `pICMS`×10000 (`0180000`). Os dois não podem estar certos para o mesmo campo.
2. `ICMSSN101/201/202/500/900` leem `Orig` (maiúsculo), que o XML não tem: `COD_SITUACAO_A` sai **0**
   e `COD_FEDERAL` `1`. Só o `ICMSSN102` lê `orig`.
3. `COD_SITUACAO_A/B` saem como inteiro (`0` em vez de `00`) e vazios no Simples Nacional (sem `CST`).
4. `TRIB_IPI` é calculado sobre o CST do **ICMS**; `COD_TRIB_IPI` é sempre `53` na NF-e.
5. `VLR_DESCONTO` e `VLR_SEGURO` do item são os do **total** da nota.
6. `VLR_ALIQ_IPI` da capa (SAFX07) recebe o valor total de IPI, não uma alíquota.
7. `COD_EMPRESA`/`COD_ESTAB` usam o CNPJ da **contraparte**; em NF-e de entrada própria (contraparte =
   emitente) cai sempre em 001/0009.
8. `SERIE_DOCFIS` = `1` e `COD_MODELO` = `55` fixos na NF-e.
9. Telefone com 11 dígitos vira `0` (`Int32.TryParse`).
10. IE não numérica (que não seja `isento` no destinatário) descarta a nota inteira, sem erro visível.
11. `IND_CONTEM_COD` do SAFX04 do CT-e olha o remetente, não o emitente.
12. SAFX04 de NF-e sai uma vez por **nota**, sem dedupe por participante.

## 8. Fora do escopo do Java (infraestrutura, não regra)

Upload web e extração de `.zip/.eml/.msg`, persistência em SQL Server, validação de schema (XSD) das NF-e
1.10/2.00/3.10, `INT_NFE_ORIGEM_XML` (origens de e-mail/diretório/Synchro), gravação em Excel (EPPlus) e a
tela `CargaExcel`. O Java lê uma pasta ou arquivo e grava `.txt/.csv`.

## 9. Verificação

- `src/test/java/com/caldeira/TestesRegrasCsharp.java`: 75 verificações (classificação do lote, 40 regras de
  NF-e/SAFX04/2013/2043 e CT-e). Autônoma, porque as demais suítes do projeto ainda referenciam classes
  `SpedToSafxNN` que não existem mais e não compilam (já era assim antes desta mudança).
- Conversão de `temporario/entrada` (98 CT-e, com PIS/COFINS e ICMS calculados) comparada com `temporario/_20260909162928_cte.xlsx`: **0 diferenças** em SAFX07, SAFX08 e SAFX04 (`.txt`).
- Saída da Reforma comparada com a do código anterior: idêntica.
- Rodar:

```bash
cd código-fonte-reforma
javac -encoding UTF-8 -d /tmp/safx-classes $(find src/main/java -name '*.java') \
  src/test/java/com/caldeira/Assercoes.java src/test/java/com/caldeira/TestesRegrasCsharp.java
cp -r src/main/resources/* /tmp/safx-classes/
java -cp /tmp/safx-classes com.caldeira.TestesRegrasCsharp
```
