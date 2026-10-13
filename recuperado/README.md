# Reconstrucao da versao do servidor (CargaSouth32), unificada

## De onde isso veio

O fonte desta versao nao estava disponivel: o PDB aponta para
`J:\Pendrive\...\T2\projeto conversor\`, um pendrive da maquina que compilou.
O que existe e o binario publicado em `CargaSouth32/bin/ABMC.RANFE.LIGHT_SQL.dll`.

Estes arquivos foram reconstruidos a partir dele (ILSpy) e entao modificados.
**Nao sao o fonte original**: nomes de variaveis locais dentro dos metodos que
nao foram reescritos vem do descompilador.

Se o pendrive reaparecer, o fonte de la e a referencia — compare antes de
sobrescrever.

## O que mudou

### 1. Upload unificado

Antes: dois campos (`nfes` e `ctes`) e **duas planilhas** — `empresa_data.xlsx`
e `empresa_data_cte.xlsx` — que o usuario juntava a mao.

Agora: um campo (`iFile`). O tipo de cada documento sai do proprio XML
(`ClassificadorXml`, por `ide/mod`), e NF-e, NFS-e e CT-e alimentam as mesmas
abas SAFX07/08/04 num arquivo so.

`CTeToXls` virou `LerCTes`: le o XML e devolve as tabelas, sem gravar planilha.
Quem escreve e o `Index`, uma vez.

### 2. NullReferenceException corrigido

O erro relatado:

    NullReferenceException em HomeController.cs:683 (CTeToXls)

Causa: `xmlElement3["enderEmit"]` devolvia null e a linha seguinte indexava esse
null. Nao era `ctes` nulo nem `ccByCfop` — ambos ja tinham guarda.

O metodo inteiro encadeava indexadores de `XmlElement` sem verificacao. Todo
acesso agora passa por `No()`/`Txt()`, e documento incompleto e pulado com aviso
nomeando arquivo e campo. Outros pontos com o mesmo defeito, tambem corrigidos:

- `imp/ICMS/ICMS00` fixo — quebrava em ICMS45/60/90, comuns em frete;
  agora le o grupo que o XML traz e avisa quando nao e ICMS00.
- `ide/toma3` fixo — quebrava quando o tomador e `toma4`.
- raiz `cteProc` fixa — quebrava em CT-e OS (`cteOSProc`) e em CT-e sem protocolo.

### 3. Reforma Tributaria integrada

SAFX3007/3008/3009 entram como abas da mesma planilha. Exige o pacote
`ReformaTributaria/` (ja no repositorio) adicionado ao projeto.

### 4. Correcoes menores

- `<form>` aninhado dentro do `Html.BeginForm` trocado por `<div>`.
- Lote vazio devolve mensagem em vez de estourar depois de `ClearAllRecords()`.
- `accept` inclui `.zip/.eml/.msg`, que o `Funcoes.ExtractFiles` ja suporta.

## Como aplicar

1. Recupere o fonte do `projeto conversor` (pendrive, backup ou o desenvolvedor).
2. Substitua `Controllers/HomeController.cs` e `Views/Home/Index.cshtml`.
3. Acrescente ao projeto: `ClassificadorXml.cs` e a pasta `ReformaTributaria/`
   (com `ReformaTributaria/Dados/` marcado como conteudo a publicar).
4. Compile no Visual Studio e publique.

## O que foi verificado, e o que nao foi

Verificado nesta maquina (macOS, mono):

- `HomeController.cs` **compila limpo** contra System.Web.Mvc, EPPlus,
  EntityFramework e o pacote ReformaTributaria real.
- As **76 colunas** que `LerCTes` grava existem nas classes
  `DataTableSAFX04/07/08` reais, extraidas da propria assembly.
- `ClassificadorXml` classifica corretamente 10 casos, incluindo evento de
  cancelamento com "infCte" no texto, NFS-e e CT-e OS.

NAO verificado:

- Execucao real. Depende de IIS, SQL Server e do banco `abmc5`.
- Os valores gerados para CT-e nao foram conferidos contra um CT-e real —
  nao havia nenhum no material disponivel. **Rode um lote conhecido e compare
  a planilha com a da versao anterior antes de usar em producao.**
- O fluxo NF-e nao foi executado; as regras de transformacao foram preservadas
  da assembly, mas so um lote real confirma.
