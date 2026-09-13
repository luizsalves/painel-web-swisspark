# Processo de Conciliação Contábil — TOTVS RM / PostgreSQL / Pentaho / Excel

**Documento técnico e funcional**  
**Data da documentação:** 10/09/2026

---

## 1. Objetivo do processo

O processo tem como objetivo **montar a composição do saldo contábil e disponibilizar o detalhamento das contas contábeis por empresa e por conta**, combinando dados contábeis e financeiros originados do TOTVS RM com estruturas auxiliares de controle da conciliação.

Ao final da execução, o processo produz uma base detalhada que permite responder, para cada empresa, filial, período e conta contábil:

- qual era o saldo anterior;
- quais débitos e créditos formaram o movimento;
- qual é o saldo atual;
- quais lançamentos compõem esse saldo;
- quais documentos, fornecedores, notas fiscais e pagamentos estão relacionados, quando aplicável;
- se a conta foi preparada, conferida ou possui pendência;
- qual é a prioridade da conciliação;
- se a conciliação está finalizada.

A **tabela de composição consolidada é `FT_COMPOSICAO`**. A camada final de consumo é a **view `VW_FT_COMPOSICAO`**, que combina a composição com `FVALIDACAO` e `DPRIORIDADE`. O Excel consome essa view.

Em termos de negócio, o resultado final pode ser entendido como:

> **Uma composição analítica do saldo das contas contábeis, detalhada por empresa e conta, com rastreabilidade dos lançamentos e informações de apoio à conciliação.**

---

## 2. Visão geral da arquitetura

```text
TOTVS RM / Bases auxiliares
        |
        |-- CLANCA -------------------- lançamentos contábeis
        |-- GFILIAL ------------------- empresa / filial
        |-- CCONTA -------------------- plano de contas
        |-- FLAN ---------------------- lançamentos financeiros
        |-- FRELLAN ------------------- relacionamentos financeiros
        |-- FLANBAIXA ---------------- baixas financeiras
        |-- FTDO ---------------------- tipos de documento
        |-- FCFO ---------------------- clientes / fornecedores
        |-- TMOV ---------------------- movimentos / notas
        |-- CPARTIDA ------------------ partidas / balancete
        |
        |-- FT_FECH_CONTAS_2023 ------- base de fechamento inicial
        |-- _REDUZIDO ---------------- ajuste auxiliar de contas
        |-- FVALIDACAO ---------------- preparado / conferido / pendência
        |-- DPRIORIDADE --------------- prioridade da conciliação
        v
Pentaho
        |
        v
EXECUTA_CONCILIACAO_PARAM
        |
        |  SELECT public.fc_fech_conciliacao();
        v
fc_fech_conciliacao()
        |
        |-- monta débitos e créditos
        |-- monta fechamento mensal
        |-- monta razão
        |-- monta FT_RAZAO_DASH
        |-- carrega FT_RAZAO_DASH_01
        |
        |-- fc_fech_composicao_v12()
        |       |
        |       +--> monta composição financeira / NF / pagamentos
        |       +--> TRUNCATE + INSERT em FT_COMPOSICAO
        |
        |-- fc_nf_emitidas()
        |       +--> monta visão auxiliar de NF e parcelas
        |
        +-- fc_validacao()
                +--> compara snapshots de CLANCA e FLAN
                +--> gera tabelas de divergências

FT_COMPOSICAO
      +
FVALIDACAO
      +
DPRIORIDADE
      |
      v
VW_FT_COMPOSICAO
      |
      v
EXCEL
```

---

## 3. Disparo do processo pelo Pentaho

A transformação Pentaho identificada é `EXECUTA_CONCILIACAO_PARAM`. Ela utiliza uma conexão PostgreSQL e executa o comando:

```sql
SELECT public.fc_fech_conciliacao();
```

Essa chamada inicia o fluxo principal da conciliação.

O Job superior referencia a transformação `CONCILIACAO.ktr` por meio da entrada `EXECUTA_CONCILIACAO`. O trecho de estrutura identificado no histórico é:

```xml
<entry>
  <name>EXECUTA_CONCILIACAO</name>
  <type>TRANS</type>
  <filename>C:\Contabil\Pentaho\Transformaçoes\CONCILIACAO.ktr</filename>
  ...
</entry>
```

> **Observação:** o XML completo do Job `.kjb` não está entre os quatro arquivos atualmente anexados a esta conversa. O documento registra o trecho e o fluxo que foram identificados anteriormente.

---

## 4. Função principal — `fc_fech_conciliacao()`

### 4.1 Finalidade

É a função orquestradora principal do processamento. Ela lê os lançamentos contábeis, transforma débito e crédito em uma estrutura única, associa empresa e plano de contas, recompõe os fechamentos mensais, gera o razão analítico e prepara a base utilizada nas etapas seguintes.

### 4.2 Principais etapas

1. Remove índices e tabelas intermediárias de uma execução anterior.
2. Usa `FT_FECH_CONTAS_2023` como ponto de partida para o fechamento inicial.
3. Lê `CLANCA` e separa movimentos em `FT_CREDITO` e `FT_DEBITO`.
4. Une débito e crédito em `FT_DEBITO_CREDITO`.
5. Usa `GFILIAL`, `CCONTA`, `FT_FECH_CONTAS_2023`, `CCONTA_GFILIAL` e `_REDUZIDO` para enriquecer empresa, conta, reduzido e descrição.
6. Constrói a base de saldo inicial e garante a existência das contas utilizadas nos lançamentos.
7. Percorre os meses para calcular fechamento, saldo anterior, débitos, créditos e saldo atual.
8. Cria `FT_RAZAO`, combinando lançamentos e registros de fechamento.
9. Enriquce o razão com `FVALIDACAO` e `DPRIORIDADE`.
10. Cria `FT_RAZAO_DASH` e recalcula os saldos de fechamento.
11. Carrega `FT_RAZAO_DASH_01`, incluindo data e hora de execução.
12. Executa, nesta ordem:
    - `fc_fech_composicao_v12()`;
    - `fc_nf_emitidas()`;
    - `fc_validacao()`.

### 4.3 Tabelas externas utilizadas diretamente

| Tabela | Papel |
|---|---|
| `FT_FECH_CONTAS_2023` | Fechamento/saldo inicial de referência |
| `CLANCA` | Lançamentos contábeis |
| `GFILIAL` | Identificação de empresa/filial |
| `CCONTA` | Plano de contas, reduzido e descrição |
| `_REDUZIDO` | Complemento auxiliar para contas não identificadas |
| `FVALIDACAO` | Informações de preparação, conferência e pendência |
| `DPRIORIDADE` | Prioridade da conciliação |
| `FT_RAZAO_DASH_01` | Tabela persistente recarregada ao final da etapa do razão |

### 4.4 Principais tabelas criadas/recriadas dentro da função

`FECHAMENTO_00`, `FT_CREDITO`, `FT_DEBITO`, `FT_DEBITO_CREDITO`, `CCONTA_GFILIAL`, `DIMSALDO_INICIAL`, `DIMLANCAMENTOS`, `FECHAMENTO_01`, `FECHAMENTO_02`, `FT_RAZAO`, `FT_RAZAO_DASH_0`, `FT_RAZAO_DASH` e `FT_RAZAO_DASH_RESUMO`.

---

## 5. Função de composição — `fc_fech_composicao_v12()`

### 5.1 Finalidade

Essa função transforma o razão contábil em uma **composição operacional do saldo**. Ela complementa o saldo contábil com informações financeiras do RM, principalmente adiantamentos, notas fiscais, pagamentos, vínculos e fornecedores.

A função utiliza dados de `FLAN`, `FRELLAN`, `FLANBAIXA`, `FTDO`, `FCFO`, `GFILIAL`, `CLANCA`, `CCONTA` e estruturas produzidas pela função principal.

### 5.2 O que ela entrega

O ponto central dessa função é a carga da tabela `FT_COMPOSICAO`.

A `FT_COMPOSICAO` passa a conter, em uma mesma estrutura:

- saldo e movimento contábil;
- empresa e filial;
- conta contábil e reduzido;
- histórico e partida;
- fornecedor;
- documento;
- informações de baixa;
- valor líquido;
- valor vinculado;
- valor de NF emitida;
- valor de NF em aberto;
- datas de contabilização e entrega da NF.

Isso permite sair de uma visão meramente de saldo e chegar à **explicação do que compõe o saldo de cada conta por empresa**.

### 5.3 Principais regras identificadas

A função contém regras específicas para:

- adiantamentos de fornecedores;
- relacionamento entre lançamentos financeiros e parcelas;
- baixas financeiras;
- identificação de NF emitidas;
- identificação de pagamentos;
- cálculo de valor vinculado acumulado;
- cálculo do valor da NF ainda em aberto;
- associação da conta contábil ao documento;
- preenchimento do nome do fornecedor.

### 5.4 Tabelas externas relevantes

`FT_RAZAO_DASH_01`, `FLAN`, `FRELLAN`, `FTDO`, `FCFO`, `GFILIAL`, `FT_RAZAO_DASH`, `CCONTA`, `FLANBAIXA`, `CLANCA`, `TMOV`, `FT_COMPOSICAO` e `FT_RAZAO`.

> `FT_RAZAO` e `FT_RAZAO_DASH` são externas em relação a essa função, mas são criadas anteriormente pela `fc_fech_conciliacao()` dentro da execução principal.

---

## 6. Função de notas fiscais — `fc_nf_emitidas()`

### 6.1 Finalidade

Monta uma estrutura auxiliar para conciliar **notas fiscais e suas parcelas/pagamentos**.

A função cria três estruturas principais:

- `FT_PARCELAS_NF` — parcelas financeiras ligadas aos movimentos;
- `FT_NOTAS_FISCAIS` — notas/movimentos identificados no RM;
- `FT_NF_EMITIDAS_VS_PARC` — união das notas fiscais e parcelas para comparação e análise.

### 6.2 Fontes

Utiliza `TMOV`, `FLAN`, `FTDO`, `FCFO`, `GFILIAL` e `CLANCA`.

A função também procura a conta contábil correspondente aos documentos por meio dos lançamentos presentes em `CLANCA`.

---

## 7. Função de validação — `fc_validacao()`

### 7.1 Finalidade

Tem como objetivo **identificar lançamentos que existiam em uma fotografia anterior da base e deixaram de existir na base atual**.

Ela trabalha com dois históricos persistentes:

- `TB_CLANCA` — snapshot/histórico dos lançamentos contábeis;
- `TB_FLAN` — snapshot/histórico dos lançamentos financeiros.

A cada execução, os registros novos encontrados em `CLANCA` e `FLAN` são acrescentados aos respectivos históricos.

Depois são criadas:

- `TB_CLANCA_VALIDACAO` — registros históricos que não estão mais em `CLANCA`;
- `TB_FLAN_VALIDACAO` — registros históricos que não estão mais em `FLAN`.

Essas tabelas ajudam a detectar exclusões, alterações ou inconsistências entre execuções.

### 7.2 Observação importante

O script possui comandos de criação inicial de `TB_CLANCA` e `TB_FLAN`, porém esses blocos estão comentados. Na execução atual, portanto, essas duas tabelas **precisam existir previamente**.

---

## 8. Funções auxiliares de balancete

As funções `fc_balancete()` e `fc_fech_balancete()` foram fornecidas como parte do ambiente, porém **não foi identificada chamada delas dentro da `fc_fech_conciliacao()`**. Por isso, neste documento elas são tratadas como rotinas auxiliares, e não como etapas comprovadas do fluxo principal disparado pelo Pentaho.

### 8.1 `fc_balancete()`

Recebe período, faixa de conta, coligada e banco de dados. Utiliza `CCONTA` e `CPARTIDA` para calcular:

- anterior;
- débitos;
- créditos;
- saldo atual.

O cálculo considera a hierarquia das contas, agregando valores da conta pai e de suas contas filhas.

### 8.2 `fc_fech_balancete()`

Recebe `ano_mes`, calcula o primeiro e o último dia do mês e executa lógica semelhante ao balancete, gravando o resultado na tabela `tb_fech_balancete`.

Fontes principais:

- `CCONTA`;
- `CPARTIDA`.

Destino:

- `tb_fech_balancete`.

---

## 9. Camada final — `VW_FT_COMPOSICAO`

### 9.1 Papel da view

`VW_FT_COMPOSICAO` é a **camada final de apresentação da conciliação para o Excel**.

Ela parte de `FT_COMPOSICAO` e adiciona informações de controle:

- `FVALIDACAO` → preparado, conferido, pendência e responsáveis;
- `DPRIORIDADE` → classificação de prioridade da empresa/período.

### 9.2 Regras de apresentação

A view também:

- substitui valores nulos por valores padrão;
- marca registros de fechamento pelo histórico;
- converte a prioridade em `1-Baixa`, `2-Regular`, `3-Média` ou `4-Alta`;
- gera contadores de preparado, conferido, pendência e finalizado;
- gera os status `SIM/NÃO` correspondentes;
- expõe dados de fornecedor, número contábil, segundo número, NF em aberto e data de execução.

### 9.3 Resultado funcional final

O conjunto `FT_COMPOSICAO + FVALIDACAO + DPRIORIDADE`, exposto pela `VW_FT_COMPOSICAO`, é o que permite ao usuário do Excel analisar:

**Empresa → Conta Contábil → Período → Saldo → Composição/Detalhamento → Situação da Conciliação.**

---

## 10. Inventário de tabelas do processo

### 10.1 Fontes nativas do TOTVS RM presentes nos scripts

- `CLANCA`
- `GFILIAL`
- `CCONTA`
- `FLAN`
- `FRELLAN`
- `FTDO`
- `FCFO`
- `FLANBAIXA`
- `TMOV`
- `CPARTIDA`

### 10.2 Tabelas auxiliares/persistentes do processo

- `FT_FECH_CONTAS_2023`
- `_REDUZIDO`
- `FVALIDACAO`
- `DPRIORIDADE`
- `FT_COMPOSICAO`
- `FT_RAZAO_DASH_01`
- `TB_CLANCA`
- `TB_FLAN`
- `tb_fech_balancete`

### 10.3 Tabelas criadas ou recriadas durante as rotinas

- `FECHAMENTO_00`
- `FT_CREDITO`
- `FT_DEBITO`
- `FT_DEBITO_CREDITO`
- `CCONTA_GFILIAL`
- `DIMSALDO_INICIAL`
- `DIMLANCAMENTOS`
- `FECHAMENTO_01`
- `FECHAMENTO_02`
- `FT_RAZAO`
- `FT_RAZAO_DASH_0`
- `FT_RAZAO_DASH`
- `FT_RAZAO_DASH_RESUMO`
- `FT_LANCAMENTOS`
- `FT_LANCAMENTOS_PARCELAS`
- `FT_NF_EMITIDAS`
- `FT_NF_PAGAMENTOS`
- `FT_PARCELAS_NF`
- `FT_NOTAS_FISCAIS`
- `FT_NF_EMITIDAS_VS_PARC`
- `TB_CLANCA_VALIDACAO`
- `TB_FLAN_VALIDACAO`

---

## 11. Dependências ainda não documentadas completamente

Os scripts enviados permitem documentar o fluxo principal, mas a origem/manutenção dos objetos abaixo não foi fornecida:

- processo que alimenta `FT_FECH_CONTAS_2023`;
- processo que alimenta ou mantém `_REDUZIDO`;
- processo que grava/manutenção de `FVALIDACAO`;
- processo que grava/manutenção de `DPRIORIDADE`;
- DDL original (`CREATE TABLE`) de `FT_COMPOSICAO`;
- DDL ativo/original de `TB_CLANCA` e `TB_FLAN`;
- configuração exata da conexão do Excel/Power Query com `VW_FT_COMPOSICAO`;
- XML completo do Job `.kjb` que chama `CONCILIACAO.ktr`.

Esses itens não impedem o entendimento do processamento principal, mas são necessários para uma documentação de implantação/recriação 100% completa.

---

# ANEXO A — Scripts e códigos utilizados

> Os códigos abaixo foram preservados conforme fornecidos. Na transformação Pentaho, o campo de senha foi **redigido por segurança**.

## A.1 Pentaho — transformação `EXECUTA_CONCILIACAO_PARAM`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<transformation>
  <info>
    <name>EXECUTA_CONCILIACAO_PARAM</name>
    <description/>
    <extended_description/>
    <trans_version/>
    <trans_type>Normal</trans_type>
    <trans_status>0</trans_status>
    <directory>/</directory>
    <parameters>
    </parameters>
    <log>
      <trans-log-table>
        <connection/>
        <schema/>
        <table/>
        <size_limit_lines/>
        <interval/>
        <timeout_days/>
        <field>
          <id>ID_BATCH</id>
          <enabled>Y</enabled>
          <name>ID_BATCH</name>
        </field>
        <field>
          <id>CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>CHANNEL_ID</name>
        </field>
        <field>
          <id>TRANSNAME</id>
          <enabled>Y</enabled>
          <name>TRANSNAME</name>
        </field>
        <field>
          <id>STATUS</id>
          <enabled>Y</enabled>
          <name>STATUS</name>
        </field>
        <field>
          <id>LINES_READ</id>
          <enabled>Y</enabled>
          <name>LINES_READ</name>
          <subject/>
        </field>
        <field>
          <id>LINES_WRITTEN</id>
          <enabled>Y</enabled>
          <name>LINES_WRITTEN</name>
          <subject/>
        </field>
        <field>
          <id>LINES_UPDATED</id>
          <enabled>Y</enabled>
          <name>LINES_UPDATED</name>
          <subject/>
        </field>
        <field>
          <id>LINES_INPUT</id>
          <enabled>Y</enabled>
          <name>LINES_INPUT</name>
          <subject/>
        </field>
        <field>
          <id>LINES_OUTPUT</id>
          <enabled>Y</enabled>
          <name>LINES_OUTPUT</name>
          <subject/>
        </field>
        <field>
          <id>LINES_REJECTED</id>
          <enabled>Y</enabled>
          <name>LINES_REJECTED</name>
          <subject/>
        </field>
        <field>
          <id>ERRORS</id>
          <enabled>Y</enabled>
          <name>ERRORS</name>
        </field>
        <field>
          <id>STARTDATE</id>
          <enabled>Y</enabled>
          <name>STARTDATE</name>
        </field>
        <field>
          <id>ENDDATE</id>
          <enabled>Y</enabled>
          <name>ENDDATE</name>
        </field>
        <field>
          <id>LOGDATE</id>
          <enabled>Y</enabled>
          <name>LOGDATE</name>
        </field>
        <field>
          <id>DEPDATE</id>
          <enabled>Y</enabled>
          <name>DEPDATE</name>
        </field>
        <field>
          <id>REPLAYDATE</id>
          <enabled>Y</enabled>
          <name>REPLAYDATE</name>
        </field>
        <field>
          <id>LOG_FIELD</id>
          <enabled>Y</enabled>
          <name>LOG_FIELD</name>
        </field>
        <field>
          <id>EXECUTING_SERVER</id>
          <enabled>N</enabled>
          <name>EXECUTING_SERVER</name>
        </field>
        <field>
          <id>EXECUTING_USER</id>
          <enabled>N</enabled>
          <name>EXECUTING_USER</name>
        </field>
        <field>
          <id>CLIENT</id>
          <enabled>N</enabled>
          <name>CLIENT</name>
        </field>
      </trans-log-table>
      <perf-log-table>
        <connection/>
        <schema/>
        <table/>
        <interval/>
        <timeout_days/>
        <field>
          <id>ID_BATCH</id>
          <enabled>Y</enabled>
          <name>ID_BATCH</name>
        </field>
        <field>
          <id>SEQ_NR</id>
          <enabled>Y</enabled>
          <name>SEQ_NR</name>
        </field>
        <field>
          <id>LOGDATE</id>
          <enabled>Y</enabled>
          <name>LOGDATE</name>
        </field>
        <field>
          <id>TRANSNAME</id>
          <enabled>Y</enabled>
          <name>TRANSNAME</name>
        </field>
        <field>
          <id>STEPNAME</id>
          <enabled>Y</enabled>
          <name>STEPNAME</name>
        </field>
        <field>
          <id>STEP_COPY</id>
          <enabled>Y</enabled>
          <name>STEP_COPY</name>
        </field>
        <field>
          <id>LINES_READ</id>
          <enabled>Y</enabled>
          <name>LINES_READ</name>
        </field>
        <field>
          <id>LINES_WRITTEN</id>
          <enabled>Y</enabled>
          <name>LINES_WRITTEN</name>
        </field>
        <field>
          <id>LINES_UPDATED</id>
          <enabled>Y</enabled>
          <name>LINES_UPDATED</name>
        </field>
        <field>
          <id>LINES_INPUT</id>
          <enabled>Y</enabled>
          <name>LINES_INPUT</name>
        </field>
        <field>
          <id>LINES_OUTPUT</id>
          <enabled>Y</enabled>
          <name>LINES_OUTPUT</name>
        </field>
        <field>
          <id>LINES_REJECTED</id>
          <enabled>Y</enabled>
          <name>LINES_REJECTED</name>
        </field>
        <field>
          <id>ERRORS</id>
          <enabled>Y</enabled>
          <name>ERRORS</name>
        </field>
        <field>
          <id>INPUT_BUFFER_ROWS</id>
          <enabled>Y</enabled>
          <name>INPUT_BUFFER_ROWS</name>
        </field>
        <field>
          <id>OUTPUT_BUFFER_ROWS</id>
          <enabled>Y</enabled>
          <name>OUTPUT_BUFFER_ROWS</name>
        </field>
      </perf-log-table>
      <channel-log-table>
        <connection/>
        <schema/>
        <table/>
        <timeout_days/>
        <field>
          <id>ID_BATCH</id>
          <enabled>Y</enabled>
          <name>ID_BATCH</name>
        </field>
        <field>
          <id>CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>CHANNEL_ID</name>
        </field>
        <field>
          <id>LOG_DATE</id>
          <enabled>Y</enabled>
          <name>LOG_DATE</name>
        </field>
        <field>
          <id>LOGGING_OBJECT_TYPE</id>
          <enabled>Y</enabled>
          <name>LOGGING_OBJECT_TYPE</name>
        </field>
        <field>
          <id>OBJECT_NAME</id>
          <enabled>Y</enabled>
          <name>OBJECT_NAME</name>
        </field>
        <field>
          <id>OBJECT_COPY</id>
          <enabled>Y</enabled>
          <name>OBJECT_COPY</name>
        </field>
        <field>
          <id>REPOSITORY_DIRECTORY</id>
          <enabled>Y</enabled>
          <name>REPOSITORY_DIRECTORY</name>
        </field>
        <field>
          <id>FILENAME</id>
          <enabled>Y</enabled>
          <name>FILENAME</name>
        </field>
        <field>
          <id>OBJECT_ID</id>
          <enabled>Y</enabled>
          <name>OBJECT_ID</name>
        </field>
        <field>
          <id>OBJECT_REVISION</id>
          <enabled>Y</enabled>
          <name>OBJECT_REVISION</name>
        </field>
        <field>
          <id>PARENT_CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>PARENT_CHANNEL_ID</name>
        </field>
        <field>
          <id>ROOT_CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>ROOT_CHANNEL_ID</name>
        </field>
      </channel-log-table>
      <step-log-table>
        <connection/>
        <schema/>
        <table/>
        <timeout_days/>
        <field>
          <id>ID_BATCH</id>
          <enabled>Y</enabled>
          <name>ID_BATCH</name>
        </field>
        <field>
          <id>CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>CHANNEL_ID</name>
        </field>
        <field>
          <id>LOG_DATE</id>
          <enabled>Y</enabled>
          <name>LOG_DATE</name>
        </field>
        <field>
          <id>TRANSNAME</id>
          <enabled>Y</enabled>
          <name>TRANSNAME</name>
        </field>
        <field>
          <id>STEPNAME</id>
          <enabled>Y</enabled>
          <name>STEPNAME</name>
        </field>
        <field>
          <id>STEP_COPY</id>
          <enabled>Y</enabled>
          <name>STEP_COPY</name>
        </field>
        <field>
          <id>LINES_READ</id>
          <enabled>Y</enabled>
          <name>LINES_READ</name>
        </field>
        <field>
          <id>LINES_WRITTEN</id>
          <enabled>Y</enabled>
          <name>LINES_WRITTEN</name>
        </field>
        <field>
          <id>LINES_UPDATED</id>
          <enabled>Y</enabled>
          <name>LINES_UPDATED</name>
        </field>
        <field>
          <id>LINES_INPUT</id>
          <enabled>Y</enabled>
          <name>LINES_INPUT</name>
        </field>
        <field>
          <id>LINES_OUTPUT</id>
          <enabled>Y</enabled>
          <name>LINES_OUTPUT</name>
        </field>
        <field>
          <id>LINES_REJECTED</id>
          <enabled>Y</enabled>
          <name>LINES_REJECTED</name>
        </field>
        <field>
          <id>ERRORS</id>
          <enabled>Y</enabled>
          <name>ERRORS</name>
        </field>
        <field>
          <id>LOG_FIELD</id>
          <enabled>N</enabled>
          <name>LOG_FIELD</name>
        </field>
      </step-log-table>
      <metrics-log-table>
        <connection/>
        <schema/>
        <table/>
        <timeout_days/>
        <field>
          <id>ID_BATCH</id>
          <enabled>Y</enabled>
          <name>ID_BATCH</name>
        </field>
        <field>
          <id>CHANNEL_ID</id>
          <enabled>Y</enabled>
          <name>CHANNEL_ID</name>
        </field>
        <field>
          <id>LOG_DATE</id>
          <enabled>Y</enabled>
          <name>LOG_DATE</name>
        </field>
        <field>
          <id>METRICS_DATE</id>
          <enabled>Y</enabled>
          <name>METRICS_DATE</name>
        </field>
        <field>
          <id>METRICS_CODE</id>
          <enabled>Y</enabled>
          <name>METRICS_CODE</name>
        </field>
        <field>
          <id>METRICS_DESCRIPTION</id>
          <enabled>Y</enabled>
          <name>METRICS_DESCRIPTION</name>
        </field>
        <field>
          <id>METRICS_SUBJECT</id>
          <enabled>Y</enabled>
          <name>METRICS_SUBJECT</name>
        </field>
        <field>
          <id>METRICS_TYPE</id>
          <enabled>Y</enabled>
          <name>METRICS_TYPE</name>
        </field>
        <field>
          <id>METRICS_VALUE</id>
          <enabled>Y</enabled>
          <name>METRICS_VALUE</name>
        </field>
      </metrics-log-table>
    </log>
    <maxdate>
      <connection/>
      <table/>
      <field/>
      <offset>0.0</offset>
      <maxdiff>0.0</maxdiff>
    </maxdate>
    <size_rowset>10000</size_rowset>
    <sleep_time_empty>50</sleep_time_empty>
    <sleep_time_full>50</sleep_time_full>
    <unique_connections>N</unique_connections>
    <feedback_shown>Y</feedback_shown>
    <feedback_size>50000</feedback_size>
    <using_thread_priorities>Y</using_thread_priorities>
    <shared_objects_file/>
    <capture_step_performance>N</capture_step_performance>
    <step_performance_capturing_delay>1000</step_performance_capturing_delay>
    <step_performance_capturing_size_limit>100</step_performance_capturing_size_limit>
    <dependencies>
    </dependencies>
    <partitionschemas>
    </partitionschemas>
    <slaveservers>
    </slaveservers>
    <clusterschemas>
    </clusterschemas>
    <created_user>-</created_user>
    <created_date>2024/11/18 10:34:27.811</created_date>
    <modified_user>-</modified_user>
    <modified_date>2024/11/18 10:34:27.811</modified_date>
    <key_for_session_key>H4sIAAAAAAAAAAMAAAAAAAAAAAA=</key_for_session_key>
    <is_key_private>N</is_key_private>
  </info>
  <notepads>
  </notepads>
  <connection>
    <name>PostgreMySwiss</name>
    <server>10.60.0.14</server>
    <type>POSTGRESQL</type>
    <access>Native</access>
    <database>contabil</database>
    <port>5433</port>
    <username>postgres</username>
    <password>[REDACTED]</password>
    <servername/>
    <data_tablespace/>
    <index_tablespace/>
    <attributes>
      <attribute>
        <code>FORCE_IDENTIFIERS_TO_LOWERCASE</code>
        <attribute>N</attribute>
      </attribute>
      <attribute>
        <code>FORCE_IDENTIFIERS_TO_UPPERCASE</code>
        <attribute>N</attribute>
      </attribute>
      <attribute>
        <code>IS_CLUSTERED</code>
        <attribute>N</attribute>
      </attribute>
      <attribute>
        <code>MSSQL_DOUBLE_DECIMAL_SEPARATOR</code>
        <attribute>N</attribute>
      </attribute>
      <attribute>
        <code>PORT_NUMBER</code>
        <attribute>5433</attribute>
      </attribute>
      <attribute>
        <code>PRESERVE_RESERVED_WORD_CASE</code>
        <attribute>Y</attribute>
      </attribute>
      <attribute>
        <code>QUOTE_ALL_FIELDS</code>
        <attribute>N</attribute>
      </attribute>
      <attribute>
        <code>SUPPORTS_BOOLEAN_DATA_TYPE</code>
        <attribute>Y</attribute>
      </attribute>
      <attribute>
        <code>SUPPORTS_TIMESTAMP_DATA_TYPE</code>
        <attribute>Y</attribute>
      </attribute>
      <attribute>
        <code>USE_POOLING</code>
        <attribute>N</attribute>
      </attribute>
    </attributes>
  </connection>
  <order>
  </order>
  <step>
    <name>Execute SQL script</name>
    <type>ExecSQL</type>
    <description/>
    <distribute>Y</distribute>
    <custom_distribution/>
    <copies>1</copies>
    <partitioning>
      <method>none</method>
      <schema_name/>
    </partitioning>
    <connection>PostgreMySwiss</connection>
    <execute_each_row>N</execute_each_row>
    <single_statement>N</single_statement>
    <replace_variables>N</replace_variables>
    <quoteString>N</quoteString>
    <sql>select public.fc_fech_conciliacao();</sql>
    <set_params>N</set_params>
    <insert_field/>
    <update_field/>
    <delete_field/>
    <read_field/>
    <arguments>
    </arguments>
    <attributes/>
    <cluster_schema/>
    <remotesteps>
      <input>
      </input>
      <output>
      </output>
    </remotesteps>
    <GUI>
      <xloc>128</xloc>
      <yloc>96</yloc>
      <draw>Y</draw>
    </GUI>
  </step>
  <step_error_handling>
  </step_error_handling>
  <slave-step-copy-partition-distribution>
  </slave-step-copy-partition-distribution>
  <slave_transformation>N</slave_transformation>
  <attributes/>
</transformation>

```

## A.2 PostgreSQL — `fc_fech_conciliacao()`

```sql
CREATE OR REPLACE FUNCTION public.fc_fech_conciliacao()
 RETURNS void
 LANGUAGE plpgsql
AS $function$


DECLARE
    mes_array TEXT[] := ARRAY[]::TEXT[];  -- Variável para armazenar os meses
    mes_atual TEXT;
BEGIN
	
	    -- Removendo índices, se existirem
    DROP INDEX IF EXISTS idx_id_banco1;
    DROP INDEX IF EXISTS idx_id_colig1;
    DROP INDEX IF EXISTS idx_id_codconta1;
    DROP INDEX IF EXISTS idx_id_banco2;
    DROP INDEX IF EXISTS idx_id_colig2;
    DROP INDEX IF EXISTS idx_id_codconta2;
    DROP INDEX IF EXISTS idx_id_EMPRESA;
    DROP INDEX IF EXISTS idx_id_EMPRESA2;
    DROP INDEX IF EXISTS idx_id_EMPRESA3;
    DROP INDEX IF EXISTS idx_id_EMPRESA4;
    DROP INDEX IF EXISTS idx_id_EMPRESA5;
    DROP INDEX IF EXISTS idx_id_CODCONTA4;
    DROP INDEX IF EXISTS idx_id_CODCONTA5;
    DROP INDEX IF EXISTS idx_id_EMPRESA8;
    DROP INDEX IF EXISTS idx_id_CONTA8;
    DROP INDEX IF EXISTS idx_id_LCTO;
    DROP INDEX IF EXISTS idx_id_VALOR_CREDITO;
    DROP INDEX IF EXISTS idx_id_VALOR_DEBITO;
    DROP INDEX IF EXISTS IDX_DATA_FECH;
    DROP INDEX IF exists idx_id_RAZAO_EMPRESA;
    DROP INDEX IF exists idx_id_RAZAO_CONTA;
    DROP INDEX IF exists idx_ft_razao_ano_mes;
   	DROP INDEX IF exists idx_ft_razao_documento;
   
   
   
    -- Remover tabelas existentes
    DROP TABLE IF EXISTS public."FT_CREDITO";
    DROP TABLE IF EXISTS public."FT_DEBITO";
    DROP TABLE IF EXISTS public."FT_DEBITO_CREDITO";
    DROP TABLE IF EXISTS public."CCONTA_GFILIAL";
    DROP TABLE IF EXISTS public."FT_RAZAO";
   
   
 DROP TABLE IF EXISTS  public."FECHAMENTO_00";
 CREATE TABLE public."FECHAMENTO_00" AS
 SELECT 
    'FECHAMENTO' AS "TIPO_ITEM",
    TO_CHAR(CAST(A.data AS DATE), 'YYYY-MM') AS "ANO_MES", 
    date_trunc('month', CAST(A.data AS DATE)) + INTERVAL '1 month' - INTERVAL '1 second' AS "DATA",
    A.empresa AS "EMPRESA", 
    A.codconta AS "CODCONTA",
    A.codfilial AS "CODFILIAL",
    A.reduzido AS "REDUZIDO", 
    A.descricao AS "DESCRICAO", 
    A.anterior AS "ANTERIOR", 
    A.debitos AS "DEBITOS", 
    A.creditos AS "CREDITOS", 
    A.saldo_atual AS "SALDO_ATUAL",
    A.idbancodados  as "IDBANCODADOS",
    A.codcoligada  as "CODCOLIGADA"
FROM public."FT_FECH_CONTAS_2023" A;


    -- Esse ajuste foi necessario pois o campo vinha com espaço no final
   UPDATE public."FECHAMENTO_00"
    SET "EMPRESA"  = TRIM(UPPER("EMPRESA"))
    WHERE "EMPRESA" like 'SWISS PARK NEGÓCIOS IMOBILIÁRIOS LTDA S%';
   
   -- Ajusta 'AEPL AGROPECUARIA LTDA I'   
  --  UPDATE public."FECHAMENTO_00"
  --  SET "EMPRESA"  = 'AEPL AGROPECUARIA LTDA I'
  --  WHERE "EMPRESA" = 'AEPL AGROPECUARIA LTDA';

    -- Criar FT_CREDITO
    CREATE TABLE public."FT_CREDITO" AS
    SELECT
        TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
        CAST('DESCONHECIDO' AS VARCHAR(100)) AS "EMPRESA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_OSTENSIVA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP2",
        A."CREDITO" AS "CODCONTA",
        CAST(0.0000 AS numeric(15, 4)) AS "VALOR_DEBITO",
        --CAST(A."VALOR" AS numeric(15, 4)) AS "VALOR_CREDITO",
        CASE WHEN SUBSTRING(A."CREDITO",1,1) = '2' THEN CAST(A."VALOR" AS numeric(15, 4)) 
			ELSE CAST(A."VALOR" AS numeric(15, 4)) * -1 
		END AS "VALOR_CREDITO",
        A.*,
        CAST('DESCONHECIDO' AS VARCHAR(20)) AS "REDUZIDO",
        CAST('DESCONHECIDO' AS VARCHAR(250)) AS "DESCRICAO"
    FROM public."CLANCA" A
    WHERE A."CREDITO" IS NOT NULL;


    -- Criar FT_DEBITO
    CREATE TABLE public."FT_DEBITO" AS
    SELECT
        TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
        CAST('DESCONHECIDO' AS VARCHAR(100)) AS "EMPRESA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_OSTENSIVA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP2",
        A."DEBITO" AS "CODCONTA",
        --CAST(A."VALOR" AS numeric(15, 4)) AS "VALOR_DEBITO",
        CASE WHEN SUBSTRING(A."DEBITO",1,1) = '2' THEN CAST(A."VALOR" AS numeric(15, 4)) * -1
			ELSE CAST(A."VALOR" AS numeric(15, 4)) 
		END AS "VALOR_DEBITO",
        CAST(0.0 AS numeric(15, 4)) AS "VALOR_CREDITO",
        A.*,
        CAST('DESCONHECIDO' AS VARCHAR(20)) AS "REDUZIDO",
        CAST('DESCONHECIDO' AS VARCHAR(250)) AS "DESCRICAO"
    FROM public."CLANCA" A
    WHERE A."DEBITO" IS NOT NULL;

    -- Criar índices
    CREATE INDEX idx_id_banco1 ON public."FT_CREDITO" ("IDBANCODADOS");
    CREATE INDEX idx_id_colig1 ON public."FT_CREDITO" ("CODCOLIGADA");
    CREATE INDEX idx_id_codconta1 ON public."FT_CREDITO" ("CODCONTA");
    CREATE INDEX idx_id_banco2 ON public."FT_DEBITO" ("IDBANCODADOS");
    CREATE INDEX idx_id_colig2 ON public."FT_DEBITO" ("CODCOLIGADA");
    CREATE INDEX idx_id_codconta2 ON public."FT_DEBITO" ("CODCONTA");
    CREATE INDEX idx_id_EMPRESA ON public."FT_DEBITO" ("EMPRESA");
    CREATE INDEX idx_id_EMPRESA2 ON public."FT_CREDITO" ("EMPRESA");
    
   /*
   -- AJUSTA AEPL AGROPECUARIA
   -- ELA POSSUI 3 FILIAIS E CADA UMA DEVE TER UM NOME DIFERENTE
   UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA I'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 1
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
   UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA II'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 2
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
  UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA III'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 3
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
      */
       

    -- Atualizar EMPRESA em FT_DEBITO
    UPDATE public."FT_DEBITO"
    SET "EMPRESA" = TRIM(UPPER(subquery."NOME"))
    FROM public."GFILIAL" AS subquery
    WHERE public."FT_DEBITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_DEBITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_DEBITO"."CODFILIAL" = subquery."CODFILIAL";

    -- Atualizar EMPRESA em FT_CREDITO
    UPDATE public."FT_CREDITO"
    SET "EMPRESA" = TRIM(UPPER(subquery."NOME"))
    FROM public."GFILIAL" AS subquery
    WHERE public."FT_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_CREDITO"."CODFILIAL" = subquery."CODFILIAL";
       
    -- Criar tabela FT_DEBITO_CREDITO
    CREATE TABLE public."FT_DEBITO_CREDITO" AS
    SELECT
        *,
        replace(replace(replace(replace(replace(replace(replace(replace(replace("DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_DEBITO"
    UNION ALL
    SELECT
        *,
        replace(replace(replace(replace(replace(replace(replace(replace(replace("DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_CREDITO";

   
    DROP TABLE IF EXISTS public."FT_CREDITO";
    DROP TABLE IF EXISTS public."FT_DEBITO";
   
   
  
    -- Criar tabela CCONTA_GFILIAL
    CREATE TABLE public."CCONTA_GFILIAL" AS
    SELECT DISTINCT
			        'NAO' AS "ENCONTRADO_GFILIAL",
			        TRIM(UPPER(A.empresa)) as "EMPRESA",
			        A.idbancodados  as "IDBANCODADOS",
			        A.codcoligada   as "CODCOLIGADA",
			        A.codfilial   as "CODFILIAL",
			        A.codconta as "CODCONTA",
			        A.reduzido as "REDUZIDO",
			        A.descricao as "DESCRICAO",
			        replace(replace(replace(replace(replace(replace(replace(replace(replace(A.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_FECH_CONTAS_2023" A;

   
   
    -- Atualizar ENCONTRADO_GFILIAL
    UPDATE public."CCONTA_GFILIAL"
    SET "ENCONTRADO_GFILIAL" = 'SIM'
    FROM public."GFILIAL" AS subquery
    WHERE TRIM(UPPER(public."CCONTA_GFILIAL"."EMPRESA")) = TRIM(UPPER(subquery."NOME"));

    -- Criar índices adicionais
    CREATE INDEX idx_id_EMPRESA4 ON public."CCONTA_GFILIAL" ("EMPRESA");
    CREATE INDEX idx_id_EMPRESA5 ON public."FT_DEBITO_CREDITO" ("EMPRESA");
    CREATE INDEX idx_id_CODCONTA4 ON public."CCONTA_GFILIAL" ("CODCONTA");
    CREATE INDEX idx_id_CODCONTA5 ON public."FT_DEBITO_CREDITO" ("CODCONTA");
   
   
   	-- CARIMBA REDUZIDOS VAZIOS PROCURAR NA CCONTA
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery."REDUZIDO",
    	"DESCRICAO" = subquery."DESCRICAO",
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery."DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."CCONTA" 
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
           AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery."CODCONTA"; 	
         
         
    -- INCLUI REDUZIDO EM QUEM FALTOU PELO .PDF ENVIADO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery.reduzido,
    	"DESCRICAO" = subquery.descricao,
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."FT_FECH_CONTAS_2023"
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
          AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery.idbancodados
          AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery.codcoligada
          AND public."FT_DEBITO_CREDITO"."CODFILIAL" = subquery.codfilial
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery.codconta;


       
     -- Atualizar FT_DEBITO_CREDITO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery."REDUZIDO",
        "DESCRICAO" = subquery."DESCRICAO",
        "DESCRICAO_TRATADA" = subquery."DESCRICAO_TRATADA"
    FROM public."CCONTA_GFILIAL" AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
    	AND  public."FT_DEBITO_CREDITO"."CODCONTA" = subquery."CODCONTA"
    	AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_DEBITO_CREDITO"."CODFILIAL" = subquery."CODFILIAL";
       -- AND TRIM(UPPER(public."FT_DEBITO_CREDITO"."EMPRESA")) = TRIM(UPPER(subquery."EMPRESA"));
	
	  
       
    -- INCLUI REDUZIDO EM QUEM FALTOU PELO .PDF ENVIADO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery.reduzido,
    	"DESCRICAO" = subquery.descricao,
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."_REDUZIDO"
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
          AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery.idbancodados
           AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery.coligada
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery.conta;
         
         
-- Tabela com os arquivos PDF de fechamento de Dezembro/2024
DROP TABLE IF EXISTS public."DIMSALDO_INICIAL";
CREATE TABLE public."DIMSALDO_INICIAL" AS
SELECT DISTINCT   *  , CONCAT("EMPRESA", '_', "CODCONTA") as "CHAVE_2"
FROM public."FECHAMENTO_00";

DROP INDEX IF EXISTS idx_id_DIMLANCAMENTOS_EMPRESA;
DROP INDEX IF EXISTS idx_idDIMLANCAMENTOS_CONTA;




-- Essa tabela é criada para Iniciar as contas com valores zerados '0', caso elas não estejam no PDF
DROP TABLE IF EXISTS public."DIMLANCAMENTOS";
CREATE TABLE public."DIMLANCAMENTOS" AS
SELECT DISTINCT
    "EMPRESA",
    "CODCONTA",
    "IDBANCODADOS" ,
    "CODCOLIGADA" ,
    "CODFILIAL" ,
    SUM(CAST(0 AS numeric(15,4))) AS "ANTERIOR",
    SUM(CAST(0 AS numeric(15,4))) AS "DEBITOS",
    SUM(CAST(0 AS numeric(15,4))) AS "CREDITOS",
    SUM(CAST(0 AS numeric(15,4))) AS "SALDO_ATUAL"
FROM public."FT_DEBITO_CREDITO"
GROUP BY "EMPRESA","CODCONTA","IDBANCODADOS" ,"CODCOLIGADA" ,"CODFILIAL";

CREATE INDEX idx_id_DIMLANCAMENTOS_EMPRESA ON public."DIMLANCAMENTOS" ("EMPRESA");
CREATE INDEX idx_idDIMLANCAMENTOS_CONTA ON public."DIMLANCAMENTOS" ("CODCONTA");

DELETE FROM public."DIMLANCAMENTOS"
WHERE ctid NOT IN (
    SELECT MIN(ctid)
    FROM public."DIMLANCAMENTOS"
    GROUP BY "EMPRESA", "IDBANCODADOS","CODCOLIGADA", "CODFILIAL" , "CODCONTA"
);


-------------- AQUI 

INSERT INTO public."DIMSALDO_INICIAL" ("TIPO_ITEM","ANO_MES", "DATA", "EMPRESA","CODCONTA","CODFILIAL", "REDUZIDO", "DESCRICAO", "ANTERIOR", "DEBITOS", "CREDITOS", "SALDO_ATUAL", "IDBANCODADOS", "CODCOLIGADA","CHAVE_2")
SELECT DISTINCT 
	'FECHAMENTO' as "TIPO_ITEM",
    CAST('2024-01' AS text) AS "ANO_MES",
    CAST('2024-01-31 23:59:59.000 - 0300' AS DATE) AS "DATA",
    "EMPRESA",
    "CODCONTA",
    "CODFILIAL",
    CAST('DESCONHECIDO' as VARCHAR(255)) as "REDUZIDO",
    CAST('DESCONHECIDO' as VARCHAR(255)) as "DESCRICAO",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "IDBANCODADOS",
    "CODCOLIGADA",
    CAST(CONCAT("EMPRESA", cast("IDBANCODADOS" as varchar(10)), cast("CODCOLIGADA" as varchar(10)),cast("CODFILIAL" as varchar(10)), '_', "CODCONTA") AS VARCHAR(160)) as "CHAVE_2"
FROM public."DIMLANCAMENTOS" D
WHERE CONCAT(D."EMPRESA",  '_', cast(D."CODCONTA" as varchar(10)))
	NOT IN (select  CONCAT(S."EMPRESA",  '_', cast(S."CODCONTA" as varchar(10))) FROM public."DIMSALDO_INICIAL" S);
--CAST(CONCAT("EMPRESA", cast("IDBANCODADOS" as varchar(10)), cast("CODCOLIGADA" as varchar(10)),cast("CODFILIAL" as varchar(10)), '_', "CODCONTA") AS VARCHAR(160))
    

   
DELETE FROM public."DIMSALDO_INICIAL"
WHERE ctid NOT IN (
    SELECT MIN(ctid)
    FROM public."DIMSALDO_INICIAL"
    GROUP BY "CHAVE_2"
);


-- Carimba reduzidos vazios, procurar na CCONTA
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."REDUZIDO",
    "DESCRICAO" = subquery."DESCRICAO"
FROM (
    SELECT * FROM public."CCONTA"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."IDBANCODADOS"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."CODCOLIGADA"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."CODCONTA";

-- Inclui reduzido em quem faltou pelo PDF enviado
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."reduzido",
    "DESCRICAO" = subquery."descricao"
FROM (
    SELECT * FROM public."FT_FECH_CONTAS_2023"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."idbancodados"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."codcoligada"
    AND public."DIMSALDO_INICIAL"."CODFILIAL" = subquery."codfilial"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."codconta";

-- Atualizar FT_DEBITO_CREDITO com dados do CCONTA_GFILIAL
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."REDUZIDO",
    "DESCRICAO" = subquery."DESCRICAO"
FROM public."CCONTA_GFILIAL" AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."CODCONTA"
    AND TRIM(UPPER(public."DIMSALDO_INICIAL"."EMPRESA")) = TRIM(UPPER(subquery."EMPRESA"));

-- Inclui reduzido em quem faltou pelo PDF enviado (dados do _REDUZIDO)
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."reduzido",
    "DESCRICAO" = subquery."descricao"
FROM (
    SELECT * FROM public."_REDUZIDO"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."idbancodados"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."coligada"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."conta";

   
-- INICIO  
    DROP TABLE IF EXISTS public."FECHAMENTO_01";
    CREATE TABLE public."FECHAMENTO_01" AS
    SELECT CONCAT(CAST("IDBANCODADOS" AS VARCHAR(10)), CAST("CODCOLIGADA" AS VARCHAR(10)),CAST("CODFILIAL" AS VARCHAR(10)),"CODCONTA") AS "CHAVE",
           *
    FROM public."DIMSALDO_INICIAL";
   
   
------------------------------------ LOOP -------------------------------------------------------------------------

    -- Obter os valores únicos de ANO_MES (exceto '2024-01')
    SELECT ARRAY_AGG("ANO_MES" ORDER BY "ANO_MES") 
    INTO mes_array
    FROM (
        SELECT X."ANO_MES"
        FROM (
            SELECT DISTINCT "ANO_MES", CAST(REPLACE("ANO_MES", '-', '') AS INTEGER) AS "ORDEM"
            FROM public."FT_DEBITO_CREDITO"
            WHERE "ANO_MES" <> '2024-01'
        ) X
        ORDER BY X."ORDEM" ASC
    ) AS subquery;

    -- Iterar sobre o array de meses
    FOREACH mes_atual IN ARRAY mes_array LOOP
        -- Inserir dados no fechamento
        INSERT INTO public."FECHAMENTO_01" (
            "CHAVE",
            "TIPO_ITEM",
            "ANO_MES",
            "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            "ANTERIOR",
            "DEBITOS",
            "CREDITOS",
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        )
        SELECT 
            CONCAT(CAST("IDBANCODADOS" AS VARCHAR(10)), CAST("CODCOLIGADA" AS VARCHAR(10)),CAST("CODFILIAL" AS VARCHAR(10)),"CODCONTA") AS "CHAVE",
            'FECHAMENTO' AS "TIPO_ITEM",
            TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
            (DATE_TRUNC('month', A."DATA") + interval '1 month' - interval '1 day')::DATE AS "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            CAST(0 AS NUMERIC(15, 2)) AS "ANTERIOR",
            SUM("VALOR_DEBITO") AS "DEBITOS",
            SUM("VALOR_CREDITO") AS "CREDITOS",
            CAST(0 AS NUMERIC(15, 2)) AS "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            CONCAT("EMPRESA", '_', "CODCONTA") AS "CHAVE_2"
        FROM public."FT_DEBITO_CREDITO" A
        WHERE mes_atual = TO_CHAR(A."DATA", 'YYYY-MM')
        GROUP BY 
            TO_CHAR(A."DATA", 'YYYY-MM'),
            DATE_TRUNC('month', A."DATA") + interval '1 month' - interval '1 day',
            "EMPRESA", "CODCONTA", "CODFILIAL", "REDUZIDO", "DESCRICAO", "IDBANCODADOS", "CODCOLIGADA";

        -- Inserir fechamento sem lançamentos
        INSERT INTO public."FECHAMENTO_01" (
            "CHAVE",
            "TIPO_ITEM",
            "ANO_MES",
            "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            "ANTERIOR",
            "DEBITOS",
            "CREDITOS",
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        )
        SELECT 
            "CHAVE",
            "TIPO_ITEM",
            TO_CHAR("DATA" + interval '1 month', 'YYYY-MM') AS "ANO_MES",
            ("DATA" + interval '1 month') AS "DATA",
            "EMPRESA", 
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO", 
            "DESCRICAO", 
            "ANTERIOR", 
            0,  
            0, 
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        FROM public."FECHAMENTO_01" A	
        WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
          AND A."CHAVE" NOT IN (
              SELECT "CHAVE" 
              FROM public."FECHAMENTO_01" B 
              WHERE TO_CHAR(B."DATA", 'YYYY-MM') = mes_atual
          );

        -- Atualizar valores ANTERIOR e SALDO_ATUAL
        UPDATE public."FECHAMENTO_01"
        SET "ANTERIOR" = subquery."SALDO_ATUAL"
        FROM (
            SELECT *
            FROM public."FECHAMENTO_01"
            WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
        ) AS subquery
        WHERE public."FECHAMENTO_01"."ANO_MES" = mes_atual
          AND public."FECHAMENTO_01"."EMPRESA" = subquery."EMPRESA"
         -- AND public."FECHAMENTO_01"."IDBANCODADOS" = subquery."IDBANCODADOS"
         -- AND public."FECHAMENTO_01"."CODCOLIGADA" = subquery."CODCOLIGADA"
         -- AND public."FECHAMENTO_01"."CODFILIAL" = subquery."CODFILIAL"
          AND public."FECHAMENTO_01"."CODCONTA" = subquery."CODCONTA";

        UPDATE public."FECHAMENTO_01"
        SET "SALDO_ATUAL" = subquery."SALDO"
        FROM (
            SELECT *, ("ANTERIOR" + "DEBITOS" + "CREDITOS") AS "SALDO"
            FROM public."FECHAMENTO_01"
            WHERE "ANO_MES" = mes_atual
        ) AS subquery
        WHERE public."FECHAMENTO_01"."ANO_MES" = mes_atual
          AND public."FECHAMENTO_01"."EMPRESA" = subquery."EMPRESA"
         -- AND public."FECHAMENTO_01"."IDBANCODADOS" = subquery."IDBANCODADOS"
         -- AND public."FECHAMENTO_01"."CODCOLIGADA" = subquery."CODCOLIGADA"
         -- AND public."FECHAMENTO_01"."CODFILIAL" = subquery."CODFILIAL"
          AND public."FECHAMENTO_01"."CODCONTA" = subquery."CODCONTA";
    END LOOP; 	
           
drop table if exists public."FECHAMENTO_02";

create table public."FECHAMENTO_02" as
  select	* ,
	replace(    replace(
			            replace(
			                replace(
			                    replace(
			                        replace(
			                            replace(
			                                replace(
			                                    replace(A."DESCRICAO", '\\', '.'), -- Escapando a barra invertida
			                                    ':', '.'
			                                ),
			                                '*', '.'
			                            ),
			                            '>', '.'
			                        ),
			                        '<', '.'
			                    ),
			                    '|', '.'
			                ),
			                '?', '.'
			            ), 
			            '/', '.'
			        ),
			        '.', '.' -- Isso foi adicionado para evitar erro de função com um argumento
			    ) as "DESCRICAO_TRATADA"
	from public."FECHAMENTO_01" A;        
 
    -- Dropar e criar a tabela FT_RAZAO
    DROP TABLE IF EXISTS public."FT_RAZAO";
    CREATE TABLE public."FT_RAZAO" AS  
    SELECT
        CAST(1 AS INT) AS "POSICAO",
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        CAST("IDPARTIDA" AS VARCHAR(50)), 
        "DESCRICAO",
        "COMPLEMENTO",
        CAST(0 AS numeric(15, 4)) AS "ANTERIOR",
        "VALOR_DEBITO" AS "DEBITOS", 
        "VALOR_CREDITO"  AS "CREDITOS",
        CAST(0 AS numeric(15, 4)) AS "SALDO_ATUAL",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        	replace(    replace(
			            replace(
			                replace(
			                    replace(
			                        replace(
			                            replace(
			                                replace(
			                                    replace(A."DESCRICAO", '\\', '.'), -- Escapando a barra invertida
			                                    ':', '.'
			                                ),
			                                '*', '.'
			                            ),
			                            '>', '.'
			                        ),
			                        '<', '.'
			                    ),
			                    '|', '.'
			                ),
			                '?', '.'
			            ), 
			            '/', '.'
			        ),
			        '.', '.' -- Isso foi adicionado para evitar erro de função com um argumento
			    ) as "DESCRICAO_TRATADA",
			    "DOCUMENTO"
    FROM public."FT_DEBITO_CREDITO" A;

   
    -- Inserir dados na tabela FT_RAZAO a partir de FT_FECHAMENTO_02
    INSERT INTO public."FT_RAZAO" (
        "POSICAO",
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        "IDPARTIDA", 
        "DESCRICAO",
        "COMPLEMENTO",
        "ANTERIOR",
        "DEBITOS", 
        "CREDITOS",
        "SALDO_ATUAL",
        "DESCRICAO_TRATADA",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        "DOCUMENTO"
    )
    SELECT
        CAST(9999 AS INT) AS "POSICAO", 
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        "ANO_MES" AS "IDPARTIDA", 
        "DESCRICAO",
        NULL,
        "ANTERIOR",
        "DEBITOS", 
        "CREDITOS",
        "SALDO_ATUAL",
        "DESCRICAO_TRATADA",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        NULL
    FROM public."FECHAMENTO_02" A;
   
   CREATE INDEX idx_id_RAZAO_EMPRESA ON public."FT_RAZAO" ("EMPRESA");
   CREATE INDEX idx_id_RAZAO_CONTA ON public."FT_RAZAO" ("CODCONTA");
   CREATE INDEX idx_ft_razao_ano_mes ON public."FT_RAZAO"("ANO_MES");
   CREATE INDEX idx_ft_razao_documento ON public."FT_RAZAO"("DOCUMENTO");


       
-- CRIA A TABELA RAZAO PARA USAR NO EXCEL  
DROP TABLE IF EXISTS public."FT_RAZAO_DASH_0";
CREATE TABLE public."FT_RAZAO_DASH_0" AS 
SELECT     A."POSICAO",
		    A."ANO_MES",
		    A."DATA",
		    A."EMPRESA",
		    A."REDUZIDO",
		    A."CODCONTA",
		    A."IDPARTIDA",
		    A."ANTERIOR",
		    A."DEBITOS",
		    A."CREDITOS",
		    A."SALDO_ATUAL",
		    A."DESCRICAO_TRATADA",
		    A."CODFILIAL",
		    CASE 
		        WHEN A."COMPLEMENTO" IS NULL AND A."IDPARTIDA" LIKE '202%' THEN 'Fechamento'
		        ELSE A."COMPLEMENTO" 
		    END AS "HISTORICO",
		    A."DEBITOS" AS "VALOR",
		    A."CODCOLIGADA",
		    CONCAT(A."CODFILIAL", A."EMPRESA", '-', A."CODCONTA", '-', A."ANO_MES") AS "CHAVE_EMPRESA_CONTA_MES",
		    B.preparado  AS "PREPARADO",
		    B.data_preparado  AS "DATA_PREPARADO",
		    B.conferido  AS "CONFERIDO",
		    B.data_conferido  AS "DATA_CONFERIDO",
		    B.pendencia  AS "PENDENCIA",
		    B.resp_pendencia  AS "RESP_PENDENCIA",
		    B.data_pendencia  AS "DATA_PENDENCIA",
			CASE 
		        WHEN TRIM(C.prioridade)  = '1' THEN '1-Baixa'
		        WHEN TRIM(C.prioridade)  = '2' THEN '2-Regular'
		        WHEN TRIM(C.prioridade)  = '3' THEN '3-Média'
		        WHEN TRIM(C.prioridade)  = '4' THEN '4-Alta'
		        ELSE 'Não definido'
		    END AS prioridade,
		    CASE 
		        WHEN TRIM(B.preparado) IS NULL OR TRIM(B.preparado) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_PREPARADO",
		    CASE 
		        WHEN TRIM(B.conferido) IS NULL OR TRIM(B.conferido) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_CONFERIDO",
		    CASE 
		        WHEN TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_PENDENCIA",
		    CASE 
		        WHEN 
		            (TRIM(B.preparado) IS NOT NULL OR TRIM(B.preparado) <> '') AND 
		            (TRIM(B.conferido) IS NOT NULL OR TRIM(B.conferido) <> '') AND 
		            (TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '') 
		        THEN 1
		        ELSE 0 
		    END AS "QTD_FINALIZADO",
		    CASE 
		        WHEN TRIM(B.preparado) IS NULL OR TRIM(B.preparado) = '' THEN 'NÃO'
		        ELSE 'SIM' 
		    END AS "STATUS_PREPARADO",
		    CASE 
		        WHEN TRIM(B.conferido) IS NULL OR TRIM(B.conferido) = '' THEN 'NÃO'
		        ELSE 'SIM'
		    END AS "STATUS_CONFERIDO",
		    CASE 
		        WHEN TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '' THEN 'NÃO'
		        ELSE 'SIM'
		    END AS "STATUS_PENDENCIA",
		    CASE 
		        WHEN 
		            (TRIM(B.preparado) IS NOT NULL OR TRIM(B.preparado) <> '') AND 
		            (TRIM(B.conferido) IS NOT NULL OR TRIM(B.conferido) <> '') AND 
		            (TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '') 
		        THEN 'SIM'
		        ELSE 'NÃO' 
		    END AS "STATUS_FINALIZADO" ,
		  ROW_NUMBER() OVER () AS "NUMERO_LINHA", -- Número da linha
		  A."DOCUMENTO",
		  A."IDBANCODADOS" 
FROM 
    public."FT_RAZAO" A
LEFT JOIN 
    public."FVALIDACAO" B 
    ON B.chave_validacao  = CONCAT(A."CODFILIAL", A."EMPRESA", A."CODCONTA", A."ANO_MES")
LEFT JOIN 
    public."DPRIORIDADE" C 
    ON C.chave  = CONCAT(A."EMPRESA", A."ANO_MES")
ORDER BY 
    A."DATA",  
    A."EMPRESA",  
    A."POSICAO" ASC;
    
DROP INDEX IF EXISTS IDX__RAZAO_DASH_ANO_MES;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_EMPRESA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_CODCONTA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_DEBITOS;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_CREDITOS;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_NUMERO_LINHA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_HISTORICO;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_DATA;

CREATE INDEX IDX__RAZAO_DASH_DATA ON public."FT_RAZAO_DASH_0" ("DATA");
CREATE INDEX IDX__RAZAO_DASH_HISTORICO ON public."FT_RAZAO_DASH_0" ("HISTORICO");
CREATE INDEX IDX__RAZAO_DASH_ANO_MES ON public."FT_RAZAO_DASH_0" ("ANO_MES");
CREATE INDEX IDX__RAZAO_DASH_EMPRESA ON public."FT_RAZAO_DASH_0" ("EMPRESA");
CREATE INDEX IDX__RAZAO_DASH_CODCONTA ON public."FT_RAZAO_DASH_0" ("CODCONTA");
CREATE INDEX IDX__RAZAO_DASH_DEBITOS ON public."FT_RAZAO_DASH_0" ("DEBITOS");
CREATE INDEX IDX__RAZAO_DASH_CREDITOS ON public."FT_RAZAO_DASH_0" ("CREDITOS");   
CREATE INDEX IDX__RAZAO_DASH_NUMERO_LINHA ON public."FT_RAZAO_DASH_0" ("NUMERO_LINHA");   
   
/*
WITH registros_duplicados AS (
    SELECT "ANO_MES", "EMPRESA", "CODCONTA", COUNT(DISTINCT "CODFILIAL") AS qtd_filiais
    FROM public."FT_RAZAO_DASH_0"
    WHERE "EMPRESA" IN ('AEPL AGROPECUARIA LTDA', 'AGV PARTICIPAÇÕES LTDA')  -- Empresas desejadas
      AND "HISTORICO" = 'Fechamento'
    GROUP BY "ANO_MES", "EMPRESA", "CODCONTA"
    HAVING COUNT(DISTINCT "CODFILIAL") = 2  -- Quando existem dois registros de filiais diferentes
)
DELETE FROM public."FT_RAZAO_DASH_0" A
WHERE A."CODFILIAL" in (2,3)  and A."EMPRESA" IN ('AEPL AGROPECUARIA LTDA', 'AGV PARTICIPAÇÕES LTDA') -- Eliminar somente os registros com CODFILIAL = 2
  AND EXISTS (
    SELECT 1
    FROM registros_duplicados R
    WHERE R."ANO_MES" = A."ANO_MES"
      AND R."EMPRESA" = A."EMPRESA"
      AND R."CODCONTA" = A."CODCONTA"
  );
*/

DROP TABLE IF EXISTS public."FT_RAZAO_DASH";
CREATE TABLE public."FT_RAZAO_DASH" AS
SELECT DISTINCT A.*
FROM public."FT_RAZAO_DASH_0" A
ORDER BY 
    A."DATA",  
    A."EMPRESA",  
    A."POSICAO" ASC;
   


   
-- ajustes no SALDO_ATUAL E ANTERIOR
UPDATE public."FT_RAZAO_DASH"
        SET "SALDO_ATUAL" 	= null,
        	 "ANTERIOR"		= NULL
	WHERE "HISTORICO" = 'Fechamento' 
			AND "ANO_MES" <> '2024-01'
            AND "IDBANCODADOS" not in (2,1)
      		AND "CODCOLIGADA" not in (26,3);
	

DROP INDEX IF EXISTS IDX_FT_RAZAO_DASH_CODCONTA; 
DROP INDEX IF EXISTS IDX_FT_RAZAO_DASH_HISTORICO; 
DROP INDEX IF EXISTS IDX_RAZAO_DASH2_NUMERO_LINHA;

CREATE INDEX IDX_FT_RAZAO_DASH_CODCONTA ON public."FT_RAZAO_DASH" ("CODCONTA");
CREATE INDEX IDX_FT_RAZAO_DASH_HISTORICO ON public."FT_RAZAO_DASH" ("HISTORICO");
CREATE INDEX IDX_RAZAO_DASH2_NUMERO_LINHA ON public."FT_RAZAO_DASH" ("NUMERO_LINHA");  

    -- Obter os meses ordenados
    SELECT ARRAY_AGG("ANO_MES" ORDER BY "ANO_MES") 
        INTO mes_array
        FROM (
            SELECT DISTINCT "ANO_MES" 
            FROM public."FT_DEBITO_CREDITO"
            where "IDBANCODADOS" not in (2,1)
      			 AND "CODCOLIGADA" not in (26,3)
				 AND "ANO_MES" <> '2024-01' 
        ) AS subquery;

    -- Iterar sobre o array de meses
    FOREACH mes_atual IN ARRAY mes_array LOOP      
	    
	 UPDATE public."FT_RAZAO_DASH"
        SET "ANTERIOR" = null
	  WHERE "HISTORICO" = 'Fechamento' and "ANO_MES" = mes_atual
	  AND "IDBANCODADOS" not in (2,1)
      AND "CODCOLIGADA" not in (26,3);
	 
	 -- SALDO ANTERIOR
	 UPDATE public."FT_RAZAO_DASH"
        SET "ANTERIOR" = subquery."SALDO_ATUAL"
        FROM (
            SELECT distinct *
            FROM public."FT_RAZAO_DASH"
            WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
              AND "HISTORICO" = 'Fechamento'
              AND "IDBANCODADOS" not in (2,1)
      		  AND "CODCOLIGADA" not in (26,3)
        ) AS subquery
        WHERE public."FT_RAZAO_DASH"."HISTORICO" = 'Fechamento'
          	AND public."FT_RAZAO_DASH"."ANO_MES"  = mes_atual
          --AND public."FT_RAZAO_DASH"."IDBANCODADOS" = subquery."IDBANCODADOS"
          --AND public."FT_RAZAO_DASH"."CODCOLIGADA" = subquery."CODCOLIGADA"
          --AND public."FT_RAZAO_DASH"."CODFILIAL" = subquery."CODFILIAL"
          	AND public."FT_RAZAO_DASH"."EMPRESA" = subquery."EMPRESA"
          	AND public."FT_RAZAO_DASH"."CODCONTA" = subquery."CODCONTA"
          	AND public."FT_RAZAO_DASH"."IDBANCODADOS" not in (2,1)
      		AND public."FT_RAZAO_DASH"."CODCOLIGADA" not in (26,3);
	 
        -- Atualizar valores ANTERIOR e SALDO_ATUAL
     UPDATE public."FT_RAZAO_DASH"
        SET "SALDO_ATUAL" = subquery."SALDO"
        FROM (
            SELECT "NUMERO_LINHA","ANO_MES","ANTERIOR" + "DEBITOS" + "CREDITOS" AS "SALDO"
            FROM public."FT_RAZAO_DASH"
            WHERE "HISTORICO" = 'Fechamento' 
             AND "ANO_MES" = mes_atual
             AND "IDBANCODADOS" not in (2,1)
      		 AND "CODCOLIGADA" not in (26,3)
 
        ) AS subquery
        WHERE public."FT_RAZAO_DASH"."HISTORICO" = 'Fechamento'
          AND public."FT_RAZAO_DASH"."NUMERO_LINHA" = subquery."NUMERO_LINHA"
          AND public."FT_RAZAO_DASH"."ANO_MES"  = subquery."ANO_MES"
		  AND public."FT_RAZAO_DASH"."ANO_MES"  = mes_atual
		  AND public."FT_RAZAO_DASH"."IDBANCODADOS" not in (2,1)
          AND public."FT_RAZAO_DASH"."CODCOLIGADA" not in (26,3);
		 
    END LOOP; 	
----------------------------------------------------------------------------------------------   
   
    
   
   
DROP TABLE IF EXISTS public."FT_RAZAO_DASH_RESUMO";
CREATE TABLE public."FT_RAZAO_DASH_RESUMO" AS
SELECT 
	TO_DATE(X."ANO_MES", 'YYYY-MM') AS "DATA",
    X."ANO_MES",
    X."EMPRESA",
    X."CODCONTA",
    X."CHAVE_EMPRESA_CONTA_MES",
    CONCAT(X."EMPRESA",X."ANO_MES") as "CHAVE",
    1 AS "QTD_TOTAL",
    CASE WHEN X."QTD_PREPARADO" = 0 THEN 0 ELSE 1 END AS "QTD_PREPARADO",
    CASE WHEN X."QTD_CONFERIDO" = 0 THEN 0 ELSE 1 END AS "QTD_CONFERIDO",
    CASE WHEN X."QTD_PENDENCIA" = 0 THEN 0 ELSE 1 END AS "QTD_PENDENCIA",
    CASE WHEN X."QTD_FINALIZADO" = 0 THEN 0 ELSE 1 END AS "QTD_FINALIZADO"
FROM (
    SELECT 
        "ANO_MES",
        "EMPRESA",
        "CODCONTA",
        "CHAVE_EMPRESA_CONTA_MES",        
        1 AS "QTD_TOTAL",
        SUM("QTD_PREPARADO") AS "QTD_PREPARADO",
        SUM("QTD_CONFERIDO") AS "QTD_CONFERIDO",
        SUM("QTD_PENDENCIA") AS "QTD_PENDENCIA",
        SUM("QTD_FINALIZADO") AS "QTD_FINALIZADO"
    FROM public."FT_RAZAO_DASH" 
    GROUP BY 
        "ANO_MES",
        "EMPRESA",
        "CODCONTA",
        "CHAVE_EMPRESA_CONTA_MES"
) X;

-- indice para que essa tabela possa montar uma view com a fvalicao e dprioridade
-- DROP INDEX IF exists idx_CHAVE_EMPRESA_CONTA_MES;
-- CREATE INDEX idx_CHAVE_EMPRESA_CONTA_MES ON public."FT_RAZAO_DASH" ("CHAVE_EMPRESA_CONTA_MES");   


	TRUNCATE TABLE public."FT_RAZAO_DASH_01";
	
	INSERT INTO public."FT_RAZAO_DASH_01"
	SELECT A."POSICAO", 
			A."ANO_MES", 
			A."DATA",
			A."EMPRESA", 
			A."REDUZIDO", 
			A."CODCONTA", 
			A."IDPARTIDA", 
			A."ANTERIOR", 
			A."DEBITOS", 
			A."CREDITOS", 
			A."SALDO_ATUAL", 
			A."DESCRICAO_TRATADA",
			A."CODFILIAL",
			A."HISTORICO", 
			A."VALOR", 
			A."CODCOLIGADA", 
			A."CHAVE_EMPRESA_CONTA_MES", 
			A."PREPARADO",
			A."DATA_PREPARADO", 
			A."CONFERIDO", 
			A."DATA_CONFERIDO",
			A."PENDENCIA", 
			A."RESP_PENDENCIA",
			A."DATA_PENDENCIA",
			A.prioridade,
			A."QTD_PREPARADO",
			A."QTD_CONFERIDO",
			A."QTD_PENDENCIA", 
			A."QTD_FINALIZADO",
			A."STATUS_PREPARADO",
			A."STATUS_CONFERIDO",
			A."STATUS_PENDENCIA",
			A."STATUS_FINALIZADO",
			A."NUMERO_LINHA",			
			TO_CHAR(CURRENT_DATE, 'YYYY-MM-DD') AS "DATAEXECUCAO",
     		TO_CHAR(CURRENT_TIMESTAMP, 'HH24:MI') as "HORAEXECUCAO",
     		A."DOCUMENTO",
     		null as "NOME_FORNECEDOR",
			null as "CODCFO" ,
			null as "CODTDO" ,
			null as "SEGUNDONUMERO" ,
			null as "NUMERODOCUMENTO" ,
			null as "VALORVINCULO" ,
			null as "VALOR_NF_EM_ABERTO",
			null as "TIPOBAIXA"
	FROM public."FT_RAZAO_DASH" A;

    DROP INDEX IF EXISTS IDX_01DASH_EMPRESA;
   	DROP INDEX IF EXISTS IDX_01DASH_CODCONTA;
	DROP INDEX IF EXISTS IDX_01DASH_POSICAO;
   	DROP INDEX IF EXISTS IDX_01DASH_DATA;   
   
    CREATE INDEX IDX_01DASH_EMPRESA ON public."FT_RAZAO_DASH_01" ("EMPRESA");
    CREATE INDEX IDX_01DASH_CODCONTA ON public."FT_RAZAO_DASH_01" ("CODCONTA");
    CREATE INDEX IDX_01DASH_POSICAO ON public."FT_RAZAO_DASH_01" ("POSICAO");
    CREATE INDEX IDX_01DASH_DATA ON public."FT_RAZAO_DASH_01" ("DATA");
   	
   
   
   
   	--select * from public.fc_fech_composicao();
  /* 
    DROP TABLE IF EXISTS public."FT_DEBITO_CREDITO";
    DROP TABLE IF EXISTS public."FT_VALIDACAO_1";
    DROP TABLE IF EXISTS public."FT_RAZAO_0";
    DROP TABLE IF EXISTS public."FT_RAZAO_00";
    DROP TABLE IF EXISTS public."FT_RAZAO_01";
    --DROP TABLE IF EXISTS public."FT_COMPOSICAO";
    DROP TABLE IF EXISTS public."FT_VALIDACAO_1";
    DROP TABLE IF EXISTS public."FECHAMENTO_01";
    DROP TABLE IF EXISTS public."FECHAMENTO_02";
    DROP TABLE IF EXISTS public."FT_RAZAO_DASH_0";
    DROP TABLE IF EXISTS public."FVALIDACAO_1";
   */
   	PERFORM fc_fech_composicao_v12();
   
   	PERFORM public.fc_nf_emitidas();
  
   	PERFORM public.fc_validacao();
   
END;
$function$
;

```

## A.3 PostgreSQL — `fc_fech_composicao_v12()`

```sql
CREATE OR REPLACE FUNCTION public.fc_fech_composicao_v12()
 RETURNS void
 LANGUAGE plpgsql
AS $function$


/* v12 Ajustado o Fornecedor deSErviços foi comentado essa parte 
 * pois todos os valores devem entrar inlcusive os Zerados
 * 
 * 1) Fornecedores de Serviços - comentado o valor > 0 
FROM public."FT_NF_EMITIDAS"  B
where  
--B."VALOR_NF_EM_ABERTO" > 0
 "DESCRICAO" in ('FORNECEDORES DE MATERIAIS','FORNECEDORES DE SERVIÇOS') ;
 
 2) Adiantamento de Fornecedores - comentado o valor > 0 
 FROM public.FT_LANCAMENTOS  B
where  
--B."VALOR_NF_EM_ABERTO" > 0 
TRIM(UPPER("DESCRICAO")) in 
('ADIANTAMENTO A FORNECEDORES','ADIANTAMENTO DE FORNECEDORES','ADIANTAMENTO DIVERSOS','ADIANTAMENTOS DIVERSOS') ;
*/

DECLARE
    data_mes_anterior DATE;
    ano_mes_anterior TEXT;
    primeiro_dia_mes_anterior DATE; 
    data_execucao  TEXT;
BEGIN
    -- Atribui o ultimo dia do mês anterior
	data_mes_anterior := DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 day';

	-- Calcula o primeiro dia do mês anterior
    primeiro_dia_mes_anterior := DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month');
    
    -- Atribui o ano e o mês anterior no formato 'YYYY-MM'
    ano_mes_anterior := TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM');
   
    SELECT 
        "DATAEXECUCAO" || ' ' || "HORAEXECUCAO"
    INTO 
        data_execucao
    FROM 
        public."FT_RAZAO_DASH_01" A
    WHERE 
        A."DATAEXECUCAO" IS NOT NULL
    LIMIT 1;  
   
   
drop index if exists idx_id_LANCA_CODCFO;
drop index if exists idx_id_LANCA_NUMERODOCUMENTO;   
drop index if exists idx_id_LANCA_DATABAIXA;
drop index if exists idx_id_LANCA_DATACANCELAMENTO;
drop index if exists idx_id_FT_COMPO_HISTORICO;

CREATE INDEX idx_id_LANCA_CODCFO ON public."FLAN"  ("CODCFO");		
CREATE INDEX idx_id_LANCA_NUMERODOCUMENTO ON public."FLAN"  ("NUMERODOCUMENTO");
CREATE INDEX idx_id_LANCA_DATABAIXA ON public."FLAN"  ("DATABAIXA");
CREATE INDEX idx_id_LANCA_DATACANCELAMENTO ON public."FLAN"  ("DATACANCELAMENTO");
CREATE INDEX idx_id_FT_COMPO_HISTORICO ON public."FT_COMPOSICAO"  ("HISTORICO");

------------------------------------------------------- REGRA 1 ADIANTAMENTO DE FORNECEDORES ------------------------------------------------------------------------ 
-- OS Adiantamentos aos Fornecedores nascem no Lançamento de DÉBITO com valor Positivo
-- E quando as NFs são emitidas as baixas nos lançamentos vem pelo CRÉDITO com valor Negativo
  
DROP TABLE IF EXISTS public.FT_LANCAMENTOS;
CREATE TABLE public.FT_LANCAMENTOS AS   
SELECT distinct
	row_number() OVER() as "NUMERO_LINHA",
    UPPER(TRIM(E."NOME")) AS "EMPRESA",
    TO_CHAR(CURRENT_DATE, 'YYYY-MM') AS "ANOMES_CONTABILIZBX",
	TO_DATE('2000-01-01', 'YYYY-MM-DD') AS "DATACONTABILIZBX",	
    D."NOME",
    F."IDBANCODADOS", 
    F."CODCOLIGADA",
    cast(null as VARCHAR(40))  as "CODCONTA",
    cast(null as VARCHAR(250))  as "DESCRICAO",
    F."IDLAN", 
    F."CODFILIAL",
    F."CODCFO",
    F."CODTDO",
    T."DESCRICAO" AS "CODTDO_DESC",
    F."NUMERODOCUMENTO", 
    F."SEGUNDONUMERO",
    C."IDLANREL", 
    C."TIPOREL",
    C."IDBAIXA",
    C."IDBAIXAREL",
    C."VALORVINCULO",
    F."VALORORIGINAL",
    F."VALORBAIXADO",
    F."VALORLIQUIDO",
    cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
    cast(0 as numeric(15,4)) as "VALOR_NF_EM_ABERTO",
    cast(0 as numeric(15,4)) as "VALOR_NF_EMITIDAS",
    F."STATUSLAN",
    F."CODCCUSTO",    
    F."DATAVENCIMENTO",
    F."DATAEMISSAO",
    F."DATABAIXA",
    F."DATAPAG",
    F."DATACANCELAMENTO"
FROM public."FLAN" F
LEFT JOIN public."FRELLAN" C 
    ON F."IDBANCODADOS" = C."IDBANCODADOS" 
    AND F."CODCOLIGADA" = C."CODCOLIGADA"
    AND F."IDLAN" = C."IDLAN"
LEFT JOIN public."FTDO" T
    ON T."IDBANCODADOS" = F."IDBANCODADOS"
    AND T."CODCOLIGADA" = F."CODCOLIGADA"
    AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D
    ON D."CODCFO" = F."CODCFO"
    AND D."IDBANCODADOS" = F."IDBANCODADOS"
    AND D."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E
    ON E."IDBANCODADOS" = F."IDBANCODADOS"
    AND E."CODCOLIGADA" = F."CODCOLIGADA"
    AND E."CODFILIAL" = F."CODFILIAL"
WHERE F."CODTDO" in ('ADF', 'ADC')
	--and F."DATABAIXA" <= DATE '2024-12-31' 
	and F."DATABAIXA"  <= data_mes_anterior
	and F."DATACANCELAMENTO" is NULL;


drop index if exists idx_id_FRELLAN_DT_CONTABILIZBX;
drop index if exists idx_id_FRELLAN_CODCFO;
drop index if exists idx_id_FRELLAN_NOME;
drop index if exists idx_id_FRELLAN_NUMERO_LINHA;
drop index if exists idx_id_FRELLAN_IDLANREL;
drop index if exists idx_id_FT_LAN_NUMERODOCUMENTO;
drop index if exists idx_id_FRELLAN_SEGUNDONUMERO;
drop index if exists idx_id_FT_LAN_EMPRESA;

CREATE INDEX idx_id_FRELLAN_DT_CONTABILIZBX ON public.FT_LANCAMENTOS ("ANOMES_CONTABILIZBX");
CREATE INDEX idx_id_FRELLAN_CODCFO ON public.FT_LANCAMENTOS ("CODCFO");
CREATE INDEX idx_id_FRELLAN_NOME ON public.FT_LANCAMENTOS ("NOME");
CREATE INDEX idx_id_FT_LAN_EMPRESA ON public.FT_LANCAMENTOS ("EMPRESA");
CREATE INDEX idx_id_FT_LAN_NUMERODOCUMENTO ON public.FT_LANCAMENTOS ("NUMERODOCUMENTO");
CREATE INDEX idx_id_FRELLAN_NUMERO_LINHA ON public.FT_LANCAMENTOS ("NUMERO_LINHA"); 
CREATE INDEX idx_id_FRELLAN_IDLANREL ON public.FT_LANCAMENTOS ("IDLANREL"); 
CREATE INDEX idx_id_FRELLAN_SEGUNDONUMERO ON public.FT_LANCAMENTOS ("SEGUNDONUMERO"); 


UPDATE public.FT_LANCAMENTOS
  SET "NOME" = subquery."NOME"
   FROM ( 
		SELECT *  	FROM public."FCFO" 
        ) 
       AS subquery
WHERE public.FT_LANCAMENTOS."CODCFO" = subquery."CODCFO" AND
	  public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" AND
	  public.FT_LANCAMENTOS."NOME" is null;
	
	 
-- CARIMBA CONTA	 
UPDATE public.FT_LANCAMENTOS
  SET 	"CODCONTA" 	= subquery."CODCONTA"
FROM (  
		SELECT ltrim(split_part("DOCUMENTO", '/', 1), '0') as "DOCUMENTO",
				"IDBANCODADOS" ,
				"CODCOLIGADA" ,
				"CODCONTA" 
		FROM public."FT_RAZAO_DASH"
	  ) 
	    AS subquery
WHERE 	public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public.FT_LANCAMENTOS."CODCOLIGADA" =  subquery."CODCOLIGADA"		and
		ltrim(public.FT_LANCAMENTOS."NUMERODOCUMENTO",'0') =  subquery."DOCUMENTO";
		

-- CARIMBA DESCRICAO		
UPDATE public.FT_LANCAMENTOS
  SET 	"DESCRICAO" 	= subquery."DESCRICAO"
FROM (  
		select "IDBANCODADOS", "CODCOLIGADA", "CODCONTA", "REDUZIDO", TRIM(UPPER("DESCRICAO")) as "DESCRICAO"
		FROM public."CCONTA"
	  ) 
	    AS subquery
WHERE 	public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public.FT_LANCAMENTOS."CODCOLIGADA" =  subquery."CODCOLIGADA"		and
		public.FT_LANCAMENTOS."CODCONTA" =  subquery."CODCONTA";
		

		
		
	
	
-- Nessa tabela eu capturo os vinculos que são feitos conforme as NFs que vão sendo emitidas
-- FRELLAN = parcelas dos lançamentos acima 
-- FLANBAIXA = Baixa das parcelas

DROP TABLE IF EXISTS public.FT_LANCAMENTOS_PARCELAS;
CREATE TABLE public.FT_LANCAMENTOS_PARCELAS AS 
SELECT distinct	
			B."IDBANCODADOS" ,
			B."CODCOLIGADA" ,
			B."CODFILIAL",
		    F."CODCFO",
		    F."SEGUNDONUMERO",
		    F."NUMERODOCUMENTO" ,
		    C."IDLAN" ,
		    C."IDLANREL" ,
		    C."TIPOREL" ,
		    C."IDBAIXA" ,
		    C."IDBAIXAREL" ,
		    C."VALORVINCULO" ,
		    B."VALORBAIXA" ,
		    B."VALORORIGINAL" ,
		    L."DATAEMISSAO",
		    B."DATABAIXA" ,
		    B."DATACONTABILIZBX" ,
		    B."DATACANCELBAIXA" ,
		   	B."NUMCONTABILBX" ,
		   	B."TIPOBAIXA" ,
		   	B."STATUS" 
FROM public.FT_LANCAMENTOS F
LEFT JOIN public."FRELLAN" C 
    ON F."IDBANCODADOS" = C."IDBANCODADOS"
    AND F."CODCOLIGADA" = C."CODCOLIGADA"
    AND F."IDLANREL" = C."IDLANREL"
LEFT JOIN public."FLANBAIXA" B
    ON B."IDBANCODADOS" = C."IDBANCODADOS"
    AND B."CODCOLIGADA" = C."CODCOLIGADA"
    AND B."IDBAIXA" = C."IDBAIXA"
LEFT JOIN public."FLAN" L
    ON B."IDBANCODADOS" = L."IDBANCODADOS"
    AND B."CODCOLIGADA" = L."CODCOLIGADA"
    AND B."IDLAN" = L."IDLAN"  
where 	 C."TIPOREL"  = 28
	 and B."DATABAIXA" <= data_mes_anterior   
	 and L."DATAEMISSAO" <= data_mes_anterior;
	--AND B."DATABAIXA"   <= DATE '2024-12-31'
	--and L."DATAEMISSAO" <= DATE '2024-12-31';
	



--------------------------------------------- 401---------------------------------------------------		 
-- Atualiza o valor das NFs entregues
--  Valor dos Vinculos FREELAN lançados pelo contas a pagar
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL",
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NOT NULL AND "DATACANCELBAIXA" IS NULL
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA", 
        "CODFILIAL",
        "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 401,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."SEGUNDONUMERO" = cte."SEGUNDONUMERO"
    AND t."IDLANREL" = cte."IDLANREL";
   

   
--------------------------------------------- 501---------------------------------------------------
--------------------------------------- Ajuste para os Lançamentos pagos sem IDBAIXAREL ---------------------------------------------------------   
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL",
        SUM("VALORVINCULO"),
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NULL AND "DATACANCELBAIXA" IS null and "DATABAIXA" is not null 
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL", 
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 501,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" <> 401
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."NUMERODOCUMENTO" = cte."NUMERODOCUMENTO"
    AND t."IDLANREL" = cte."IDLANREL"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);
   
  
   
 
   
--------------------------------------------- 601---------------------------------------------------   
--------------------------------------- Ajuste S/NFs ---------------------------------------------------------
   
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL",
        SUM("VALORVINCULO"),
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    where "IDBAIXAREL" is NOT NULL AND "DATACANCELBAIXA" IS null and "DATABAIXA" is not null 
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL", 
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 601,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."NUMERODOCUMENTO" = cte."NUMERODOCUMENTO"
    AND t."IDLANREL" = cte."IDLANREL"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);
   
--------------------------------------------------------------------------------------------------------


   
   
   
   
--------------------------------------------- 701---------------------------------------------------
------------------------------------------ CAIXINHA ----------------------------------------------------
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CREDITO"  as "CODCONTA",
        "DOCUMENTO",
        MAX("DATA") as "DATACONTABILIZBX",
        SUM("VALOR") AS "VALORVINCULO_ACUM"
    FROM public."CLANCA" C 
     GROUP BY 
         "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CREDITO" ,
        "DOCUMENTO"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 701,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501, 601)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."NUMERODOCUMENTO" = cte."DOCUMENTO"
    AND t."CODCONTA" = cte."CODCONTA"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);  
-------------------------------------------------------------------------------------------------------- 
  
   
   
   
   
--------------------------------------------- 801---------------------------------------------------  
-- Ajuste para os casos sem CODCFO
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        --"CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL",
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NOT NULL AND "DATACANCELBAIXA" IS null
    --	and "IDLANREL" = 353431
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA", 
        "CODFILIAL",
     --   "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 801,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501, 601, 701)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
  --  AND t."CODCFO" = cte."CODCFO"
    AND t."SEGUNDONUMERO" = cte."SEGUNDONUMERO"
    AND t."IDLANREL" = cte."IDLANREL";  
   
   
  
   
   
   
-- Calcula NF em Aberto e emitidas
UPDATE public.FT_LANCAMENTOS
SET 
    "VALOR_NF_EM_ABERTO" = subquery."VALOR_NF_EM_ABERTO",
    "VALOR_NF_EMITIDAS" = subquery."VALOR_NF_EMITIDAS"
FROM (
    SELECT 
        "NUMERO_LINHA", 
        ("VALORLIQUIDO" - "VALORVINCULO_ACUM") AS "VALOR_NF_EM_ABERTO",
        "VALORVINCULO_ACUM" AS "VALOR_NF_EMITIDAS"
    FROM public.FT_LANCAMENTOS
) AS subquery
WHERE public.FT_LANCAMENTOS."NUMERO_LINHA" = subquery."NUMERO_LINHA";



TRUNCATE TABLE public."FT_COMPOSICAO";

INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO",
    "ANO_MES",
    "DATA",
    "DATACONTABILIZBX",
    "DATA_ENTREGA_NF",
    "EMPRESA",
    "REDUZIDO",
    "CODCONTA",
    "IDPARTIDA",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "DESCRICAO_TRATADA",
    "CODFILIAL",
    "HISTORICO",
    "VALOR",
    "CODCOLIGADA",
    "CHAVE_EMPRESA_CONTA_MES",
    "PREPARADO",
    "DATA_PREPARADO",
    "CONFERIDO",
    "DATA_CONFERIDO",
    "PENDENCIA",
    "RESP_PENDENCIA",
    "DATA_PENDENCIA",
    "prioridade",
    "QTD_PREPARADO",
    "QTD_CONFERIDO",
    "QTD_PENDENCIA",
    "QTD_FINALIZADO",
    "STATUS_PREPARADO",
    "STATUS_CONFERIDO",
    "STATUS_PENDENCIA",
    "STATUS_FINALIZADO",
    "NUMERO_LINHA",
    "NOME_FORNECEDOR",
    "IDBAIXAREL",
    "TIPOREL",
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO",
    "VALORVINCULO",
    "VALORVINCULO_ACUM",
    "VALOR_NF_EM_ABERTO",
    "VALOR_NF_EMITIDAS",
    "DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
) 
SELECT
    "POSICAO",
    "ANO_MES",
    "DATA",
    CAST('2000-01-01' AS DATE) as "DATACONTABILIZBX",
    CAST('2000-01-01' AS DATE) as "DATA_ENTREGA_NF",
    "EMPRESA",
    "REDUZIDO",
    "CODCONTA",
    "IDPARTIDA",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "DESCRICAO_TRATADA",
    "CODFILIAL",
    "HISTORICO",
    "VALOR",
    "CODCOLIGADA",
    "CHAVE_EMPRESA_CONTA_MES",
    "PREPARADO",
    "DATA_PREPARADO",
    "CONFERIDO",
    "DATA_CONFERIDO",
    "PENDENCIA",
    "RESP_PENDENCIA",
    "DATA_PENDENCIA",
    prioridade,
    "QTD_PREPARADO",
    "QTD_CONFERIDO",
    "QTD_PENDENCIA",
    "QTD_FINALIZADO",
    "STATUS_PREPARADO",
    "STATUS_CONFERIDO",
    "STATUS_PENDENCIA",
    "STATUS_FINALIZADO",
    "NUMERO_LINHA",
    cast('-' as VARCHAR(250)) as "NOME_FORNECEDOR",
    cast(0 as INT4) as "IDBAIXAREL",
    cast(0 as INT2) as "TIPOREL",
    cast(0 as INT4) as "TIPOBAIXA",
    cast('-' as VARCHAR(20)) as "NUMCONTABILBX",
    cast('-' as VARCHAR(20)) as "SEGUNDONUMERO",
    cast(0.000 as numeric(15,4)) as "VALORLIQUIDO",
    cast(0.000 as numeric(15,4)) as "VALORVINCULO",
    cast(0.000 as numeric(15,4)) as "VALORVINCULO_ACUM",
    cast(0.000 as numeric(15,4)) as "VALOR_NF_EM_ABERTO",
    cast(0.000 as numeric(15,4)) as "VALOR_NF_EMITIDAS",
    data_execucao as "DATAEXECUCAO",
    "IDBANCODADOS",
    substring("HISTORICO" FROM '^\d+(?=\s*-)') as "CODCFO",
    ltrim(substring("HISTORICO" FROM 'NF\.\s*(\d+)'), '0') as "NUMERODOCUMENTO"
FROM public."FT_RAZAO_DASH";




-- SOMENTE OS ADIANTAMENTOS DE FORNECEDORES SÃO INSERIDOS 
INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO", 
    "ANO_MES", 
   	"DATA",
	"DATACONTABILIZBX",
	"DATA_ENTREGA_NF",
    "EMPRESA", 
    "REDUZIDO", 
    "CODCONTA", 
    "IDPARTIDA", 
    "ANTERIOR", 
    "DEBITOS", 
    "CREDITOS", 
    "SALDO_ATUAL", 
    "DESCRICAO_TRATADA", 
    "CODFILIAL", 
    "HISTORICO", 
    "VALOR", 
    "CODCOLIGADA", 
    "CHAVE_EMPRESA_CONTA_MES", 
    "PREPARADO", 
    "DATA_PREPARADO", 
    "CONFERIDO", 
    "DATA_CONFERIDO", 
    "PENDENCIA", 
    "RESP_PENDENCIA", 
    "DATA_PENDENCIA", 
    "prioridade", 
    "QTD_PREPARADO", 
    "QTD_CONFERIDO", 
    "QTD_PENDENCIA", 
    "QTD_FINALIZADO", 
    "STATUS_PREPARADO", 
    "STATUS_CONFERIDO", 
    "STATUS_PENDENCIA", 
    "STATUS_FINALIZADO", 
    "NUMERO_LINHA", 
    "NOME_FORNECEDOR", 
    "IDBAIXAREL", 
    "TIPOREL", 
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO", 
    "VALORVINCULO", 
    "VALORVINCULO_ACUM", 
    "VALOR_NF_EM_ABERTO", 
    "VALOR_NF_EMITIDAS",
	"DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
)
SELECT 
    101 as "POSICAO",  -- Adiantamento de fornecedor
    ano_mes_anterior  as  "ANO_MES",
    primeiro_dia_mes_anterior as "DATA",
    B."DATABAIXA",
    B."DATACONTABILIZBX" as "DATA_ENTREGA_NF",
    B."EMPRESA", 
    '-' as "REDUZIDO", 
    B."CODCONTA", 
    '-' as "IDPARTIDA", 
    0.0000 as "ANTERIOR", 
    0.0000 as "DEBITOS", 
    0.0000 as "CREDITOS", 
    0.0000 as "SALDO_ATUAL", 
    B."DESCRICAO", 
    B."CODFILIAL", 
    '-' as "HISTORICO", 
    0 as "VALOR", 
    B."CODCOLIGADA", 
    '-' as "CHAVE_EMPRESA_CONTA_MES", 
    '-' as "PREPARADO", 
    '-' as "DATA_PREPARADO", 
    '-' as "CONFERIDO", 
    '-' as "DATA_CONFERIDO", 
    '-' as "PENDENCIA", 
    '-' as "RESP_PENDENCIA", 
    '-' as "DATA_PENDENCIA", 
    '-' as "prioridade", 
    0  as "QTD_PREPARADO", 
    0 as "QTD_CONFERIDO", 
    0 as "QTD_PENDENCIA", 
    0 as "QTD_FINALIZADO", 
    'NÃO' as "STATUS_PREPARADO", 
    'NÃO' as "STATUS_CONFERIDO", 
    'NÃO' as "STATUS_PENDENCIA", 
    'NÃO' as "STATUS_FINALIZADO", 
    99999999 as "NUMERO_LINHA", 
    B."NOME" as "NOME_FORNECEDOR", 
    B."IDBAIXAREL", 
    B."TIPOREL", 
    null as "TIPOBAIXA",
    B."NUMERODOCUMENTO" as  "NUMCONTABILBX",
    B."SEGUNDONUMERO",
    B."VALORLIQUIDO", 
    B."VALORVINCULO", 
    B."VALORVINCULO_ACUM", 
    B."VALOR_NF_EM_ABERTO", 
    B."VALOR_NF_EMITIDAS",
	data_execucao as "DATAEXECUCAO",
    B."IDBANCODADOS",
    B."CODCFO",
    B."NUMERODOCUMENTO"
FROM public.FT_LANCAMENTOS  B
where  
--B."VALOR_NF_EM_ABERTO" > 0 
TRIM(UPPER("DESCRICAO")) in 
('ADIANTAMENTO A FORNECEDORES',
	'ADIANTAMENTO DE FORNECEDORES',
	'ADIANTAMENTO DIVERSOS',
	'ADIANTAMENTOS DIVERSOS',
	'ADIANTAMENTO DE CLIENTES',
	'ADIANTAMENTO DE CLIENTES VILLA SUIÇA');
----------------------------------------------------- FIM ADIANTAMENTO DE FORNECEDORES ------------------------------
------------------------------------------------------- REGRA 1 ADIANTAMENTO DE FORNECEDORES ------------------------------------------------------------------------ 



-------------------------------X------------X-------------------X----------------X-------------X-XXX---------------------------------------------------------------





------------------------------------------------------- REGRA 2 EMISSAO DE NF À FORNECEDORES E PAGAMENTOS ------------------------------------------------------------------------ 
-- A NF NASCE EM UM LANÇAMENTO DE CREDITO
-- E a partir da FLAN REGRAS: 
--					1) que tenha "DATAEMISSAO" <= MÊS ANTERIOR 
--					2) DATABAIXA NULA
--					3) DATACANCELAMENTO NULA

-- Exemplo
-- 11393 - NF.0001551 HIDROTAM COMERCIO DE TUBOS E CONEXÕES LTDA
-- Estou tentando pegar as NF abertas pela CLANCA
-- SWISS PARK NEGÓCIOS IMOBILIÁRIOS LTDA
-- 2.1.1.01.01.0001
-- 4
-- 12
DROP TABLE IF EXISTS public."FT_NF_EMITIDAS";
CREATE TABLE public."FT_NF_EMITIDAS" AS 
		select   row_number() OVER() as "NUMERO_LINHA",
				 UPPER(TRIM(E."NOME")) AS "EMPRESA",
				 D."NOME"  as "NOME_FORNECEDOR",
				 F."DOCUMENTO",
				 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO",
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 F."DATA" as "DATAEMISSAO_NF",
	     		 F."IDBANCODADOS",
				 F."CODCOLIGADA",
				 F."CODFILIAL",
				 F."CREDITO" as "CODCONTA",
				 CAST(null as VARCHAR(250)) as "DESCRICAO",
				 F."IDPARTIDA",
				 F."LCTREF" ,
				 F."VALOR" as "VALOR_NF_EMITIDAS",
				 "VALOR" as "VALOR_NF_EM_ABERTO",
				 cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
				 F."COMPLEMENTO" ,
				 F."CODCCUSTO" ,
				 F."CODLOTE" ,
				 F."IDLANCAMENTO" 
		from "CLANCA" F
		LEFT JOIN public."GFILIAL" E
		    ON E."IDBANCODADOS" = F."IDBANCODADOS"
		    AND E."CODCOLIGADA" = F."CODCOLIGADA"
		    AND E."CODFILIAL" = F."CODFILIAL"
		LEFT JOIN public."FCFO" D
		    ON D."CODCFO" = substring(F."COMPLEMENTO" FROM '^\d+(?=\s*-)')
		    AND D."IDBANCODADOS" = F."IDBANCODADOS"
		    AND D."CODCOLIGADA" = F."CODCOLIGADA"
		where  F."DATA" <= data_mes_anterior
			-- F."DATA" <= 	DATE '2024-12-31'
			and "CREDITO" is not null
			and upper("COMPLEMENTO") not like '%PAGAM%'  
			and upper("COMPLEMENTO") not like '%S/NF%' 
			and upper("COMPLEMENTO") not like '%S/ NF%'
			and upper("COMPLEMENTO")  like '%NF%';
	
	
/*
CREATE TABLE public."FT_NF_EMITIDAS" AS 
		select   row_number() OVER() as "NUMERO_LINHA",
				 UPPER(TRIM(E."NOME")) AS "EMPRESA",
				 D."NOME"  as "NOME_FORNECEDOR",
				 F."NUMEROMOV",
				 regexp_replace(split_part("NUMEROMOV", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO",
				 F."CODCFO",
				 F."DATAEMISSAO" as "DATAEMISSAO_NF",
	     		 F."IDBANCODADOS",
				 F."CODCOLIGADA",
				 F."CODFILIAL",
				 CAST(null as VARCHAR(40)) as "CODCONTA",
				 CAST(null as VARCHAR(250)) as "DESCRICAO",
				 cast(null as INT4) as "IDPARTIDA",
				 cast(null as INT4) as  "LCTREF" ,
				 F."VALORLIQUIDO" as "VALOR_NF_EMITIDAS",
				 F."VALORLIQUIDO" as "VALOR_NF_EM_ABERTO",
				 cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
				 cast(null as INT4) as "COMPLEMENTO" ,
				 F."CODCCUSTO" ,
				 cast(null as INT4) as "CODLOTE" ,
				 cast(null as INT4) as "IDLANCAMENTO" 
		from "TMOV" F
		LEFT JOIN public."GFILIAL" E
		    ON E."IDBANCODADOS" = F."IDBANCODADOS"
		    AND E."CODCOLIGADA" = F."CODCOLIGADA"
		    AND E."CODFILIAL" = F."CODFILIAL"
	    LEFT JOIN public."FCFO" D
		    ON D."CODCFO" = F."CODCFO"
		    AND D."IDBANCODADOS" = F."IDBANCODADOS"
		    AND D."CODCOLIGADA" = F."CODCOLIGADA"
		where  	--F."DATA" <= data_mes_anterior;
		       F."DATA" <= 	DATE '2024-12-31';
	
*/	
	
	
	

DROP TABLE IF EXISTS public."FT_NF_PAGAMENTOS";
CREATE TABLE public."FT_NF_PAGAMENTOS" AS
select 	 	F."IDBANCODADOS",
		 	F."CODCOLIGADA",
			F."CODFILIAL",
			F."CODCFO" ,
			F."CODTDO" ,
			TRIM(UPPER(T."DESCRICAO")) as "CODTDO_DESCRICAO",
			F."IDLAN" ,
			-- F."IDBAIXA" ,
			-- F."DATACANCELBAIXA",
			F."NUMERODOCUMENTO" ,
			ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "DOCUMENTO_TRATADO",
			F."SEGUNDONUMERO" ,
			F."IDMOV",
			F."VALORORIGINAL" ,
			F."VALORBAIXADO" ,
			F."VALORLIQUIDO" ,
			F."VALORDESCONTO" ,
			F."DATAVENCIMENTO" ,
			F."DATAEMISSAO" ,
			F."DATABAIXA" ,
			F."DATACANCELAMENTO" ,
			F."DATAPAG" ,
			F."STATUSLAN" ,
			F."HISTORICO" ,
			F."CODCCUSTO" 			
from "FLAN" F
LEFT JOIN public."TMOV" M
    ON  M."IDMOV" = F."IDMOV" 
    AND M."IDBANCODADOS" = F."IDBANCODADOS"
    AND M."CODCOLIGADA" = F."CODCOLIGADA"
    AND M."CODFILIAL" = F."CODFILIAL"      
LEFT JOIN public."FTDO" T
    ON T."IDBANCODADOS" = F."IDBANCODADOS"
    AND T."CODCOLIGADA" = F."CODCOLIGADA"
    AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D
    ON D."CODCFO" 		= F."CODCFO"
    AND D."IDBANCODADOS" = F."IDBANCODADOS"
    AND D."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E
    ON E."IDBANCODADOS" = F."IDBANCODADOS"
    AND E."CODCOLIGADA" = F."CODCOLIGADA"
    AND E."CODFILIAL" = F."CODFILIAL"
where 
  F."DATACANCELAMENTO"  is null
	and F."DATABAIXA" IS NOT null
	--and F."DATABAIXA" <= DATE '2024-12-31';
	and F."DATABAIXA"  <= data_mes_anterior;





-- Ajuste para o pagamento do financiamento do veiculo fornecedor 11127
drop table 	if exists temp_pagamento_diferenca;
	
CREATE TEMP TABLE temp_pagamento_diferenca AS
SELECT * 
FROM public."FT_NF_PAGAMENTOS"
WHERE "CODCFO" = '11127'
and "NUMERODOCUMENTO" in ('02551/01/3','02551/01/4','02551/01/5')
LIMIT 3;


update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/23'
where "NUMERODOCUMENTO" = '02551/01/3';

update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/24'
where "NUMERODOCUMENTO" = '02551/01/4';

update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/25'
where "NUMERODOCUMENTO" = '02551/01/5';




insert into public."FT_NF_PAGAMENTOS"
select * from  temp_pagamento_diferenca ;




------------------- Usa a Flan para carimbar os valores pagos -----------------------------		
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "DOCUMENTO_TRATADO",
        SUM("VALORORIGINAL") as "VALORVINCULO_ACUM"
    FROM public."FT_NF_PAGAMENTOS" 
    where  "DATABAIXA" is not null 
   		and "DATABAIXA"  <= data_mes_anterior
   	--	and "DATABAIXA"  <=	DATE '2024-12-31'
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "DOCUMENTO_TRATADO"
)
UPDATE public."FT_NF_EMITIDAS" t
SET 	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM"
FROM cte_acumulado cte
WHERE t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."DOCUMENTO_TRATADO" = cte."DOCUMENTO_TRATADO";

-- ajustes no valor da NF
UPDATE public."FT_NF_EMITIDAS" t
SET "VALOR_NF_EM_ABERTO" = "VALOR_NF_EMITIDAS"
WHERE  "VALOR_NF_EM_ABERTO" is null or  "VALOR_NF_EM_ABERTO" = 0;


UPDATE public."FT_NF_EMITIDAS" t
SET "VALORVINCULO_ACUM" = 0
WHERE  "VALORVINCULO_ACUM" is null;
   
   
UPDATE public."FT_NF_EMITIDAS" t
SET "VALOR_NF_EM_ABERTO" = t."VALOR_NF_EMITIDAS" - t."VALORVINCULO_ACUM"
FROM (
     SELECT "NUMERO_LINHA", "VALOR_NF_EMITIDAS", "VALORVINCULO_ACUM"
    FROM public."FT_NF_EMITIDAS"
) AS cte
WHERE t."NUMERO_LINHA" = cte."NUMERO_LINHA"
	and t."VALORVINCULO_ACUM" > 0;



-- CARIMBA DESCRICAO		
UPDATE public."FT_NF_EMITIDAS"
  SET 	"DESCRICAO" 	= subquery."DESCRICAO"
FROM (  
		select "IDBANCODADOS", "CODCOLIGADA", "CODCONTA", "REDUZIDO", TRIM(UPPER("DESCRICAO")) as "DESCRICAO"
		FROM public."CCONTA"
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND
		public."FT_NF_EMITIDAS"."CODCONTA" =  subquery."CODCONTA";
		


/*
UPDATE public."FT_NF_EMITIDAS"
  SET 	"DESCRICAO"	= subquery."DESCRICAO_TRATADA"	
FROM (  
		SELECT *
		FROM public."FT_RAZAO_DASH" 
		where  UPPER(TRIM("DESCRICAO_TRATADA")) like 'ADIANTAMENTO%' or UPPER(TRIM("DESCRICAO_TRATADA")) like 'FORNECEDO%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS"."EMPRESA" =  subquery."EMPRESA" 		and
		public."FT_NF_EMITIDAS"."CODCONTA" =  subquery."CODCONTA" 		and 
		UPPER(TRIM(subquery."DESCRICAO_TRATADA")) like 'ADIANTAMENTO%';
*/




INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO", 
    "ANO_MES", 
   	"DATA",
	"DATACONTABILIZBX",
	"DATA_ENTREGA_NF",
    "EMPRESA", 
    "REDUZIDO", 
    "CODCONTA", 
    "IDPARTIDA", 
    "ANTERIOR", 
    "DEBITOS", 
    "CREDITOS", 
    "SALDO_ATUAL", 
    "DESCRICAO_TRATADA", 
    "CODFILIAL", 
    "HISTORICO", 
    "VALOR", 
    "CODCOLIGADA", 
    "CHAVE_EMPRESA_CONTA_MES", 
    "PREPARADO", 
    "DATA_PREPARADO", 
    "CONFERIDO", 
    "DATA_CONFERIDO", 
    "PENDENCIA", 
    "RESP_PENDENCIA", 
    "DATA_PENDENCIA", 
    "prioridade", 
    "QTD_PREPARADO", 
    "QTD_CONFERIDO", 
    "QTD_PENDENCIA", 
    "QTD_FINALIZADO", 
    "STATUS_PREPARADO", 
    "STATUS_CONFERIDO", 
    "STATUS_PENDENCIA", 
    "STATUS_FINALIZADO", 
    "NUMERO_LINHA", 
    "NOME_FORNECEDOR", 
    "IDBAIXAREL", 
    "TIPOREL", 
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO", 
    "VALORVINCULO", 
    "VALORVINCULO_ACUM", 
    "VALOR_NF_EM_ABERTO", 
    "VALOR_NF_EMITIDAS",
	"DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
)
SELECT 
    101 as "POSICAO",  -- Adiantamento de fornecedor
    ano_mes_anterior  as  "ANO_MES",    
    primeiro_dia_mes_anterior as "DATA",
   -- '2024-11'  as  "ANO_MES",
   -- DATE '2024-12-31' as "DATA",
    B."DATAEMISSAO_NF" as "DATABAIXA",
    B."DATAEMISSAO_NF" as "DATA_ENTREGA_NF",
    B."EMPRESA", 
    '-' as "REDUZIDO", 
    B."CODCONTA", 
    '-' as "IDPARTIDA", 
    0.0000 as "ANTERIOR", 
    0.0000 as "DEBITOS", 
    0.0000 as "CREDITOS", 
    0.0000 as "SALDO_ATUAL", 
    B."DESCRICAO", 
    B."CODFILIAL", 
    '-' as "HISTORICO", 
    0 as "VALOR", 
    B."CODCOLIGADA", 
    '-' as "CHAVE_EMPRESA_CONTA_MES", 
    '-' as "PREPARADO", 
    '-' as "DATA_PREPARADO", 
    '-' as "CONFERIDO", 
    '-' as "DATA_CONFERIDO", 
    '-' as "PENDENCIA", 
    '-' as "RESP_PENDENCIA", 
    '-' as "DATA_PENDENCIA", 
    '-' as "prioridade", 
    0  as "QTD_PREPARADO", 
    0 as "QTD_CONFERIDO", 
    0 as "QTD_PENDENCIA", 
    0 as "QTD_FINALIZADO", 
    'NÃO' as "STATUS_PREPARADO", 
    'NÃO' as "STATUS_CONFERIDO", 
    'NÃO' as "STATUS_PENDENCIA", 
    'NÃO' as "STATUS_FINALIZADO", 
    99999999 as "NUMERO_LINHA", 
    B."NOME_FORNECEDOR", 
    0  as "IDBAIXAREL", 
    0  as "TIPOREL", 
    null as "TIPOBAIXA",
    B."DOCUMENTO" as  "NUMCONTABILBX",
    B."DOCUMENTO_TRATADO" as "SEGUNDONUMERO",
    B."VALOR_NF_EMITIDAS" as "VALORLIQUIDO", 
    B."VALORVINCULO_ACUM" as "VALORVINCULO", 
    B."VALORVINCULO_ACUM", 
    B."VALOR_NF_EM_ABERTO", 
    B."VALOR_NF_EMITIDAS",
	data_execucao as "DATAEXECUCAO",
    B."IDBANCODADOS",
    B."CODCFO",
    B."DOCUMENTO" as "NUMERODOCUMENTO"
FROM public."FT_NF_EMITIDAS"  B
where  
--B."VALOR_NF_EM_ABERTO" > 0
 "DESCRICAO" in ('FORNECEDORES DE MATERIAIS','FORNECEDORES DE SERVIÇOS') ;

-------------------------------------------------------- Fornecedores Diversos ----------------------------------------
------------------------------------------------------- REGRA 2 EMISSAO DE NF À FORNECEDORES E PAGAMENTOS ------------------------------------------------------------------------ 


----------------------------------------------------- NF EMITIDAS A PAGAR --------------------------------------------

	 
-- Carimba Reduzido	 
UPDATE public."FT_COMPOSICAO"
SET "REDUZIDO" = subquery."REDUZIDO"
FROM (
    SELECT DISTINCT "REDUZIDO" , "CODCONTA" , "EMPRESA" , "CODFILIAL" 
    FROM PUBLIC."FT_RAZAO" 
) AS subquery
where public."FT_COMPOSICAO"."EMPRESA" = subquery."EMPRESA"
    and public."FT_COMPOSICAO"."CODCONTA" = subquery."CODCONTA"
    AND public."FT_COMPOSICAO"."CODFILIAL" = subquery."CODFILIAL"
    and ( 
   			public."FT_COMPOSICAO"."REDUZIDO" is null or  public."FT_COMPOSICAO"."REDUZIDO" = '-'
   		);

 UPDATE public."FT_COMPOSICAO"
SET "NOME_FORNECEDOR" = subquery."NOME_FORNECEDOR"
FROM (
    SELECT "CODCFO", trim(upper("NOME")) AS "NOME_FORNECEDOR"
    FROM public."FCFO"
) AS subquery
WHERE public."FT_COMPOSICAO"."CODCFO" = subquery."CODCFO"
  AND (public."FT_COMPOSICAO"."NOME_FORNECEDOR" IS NULL 
       OR public."FT_COMPOSICAO"."NOME_FORNECEDOR" = '-');

   
END;
$function$
;

```

## A.4 PostgreSQL — `fc_nf_emitidas()`

```sql
CREATE OR REPLACE FUNCTION public.fc_nf_emitidas()
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

DROP TABLE IF EXISTS public."FT_PARCELAS_NF";
CREATE TABLE public."FT_PARCELAS_NF" AS 
SELECT  row_number() OVER() as "NUMERO_LINHA",
        'PARCELA' as "TIPO_ITEM",
        cast('EM ABERTO' as VARCHAR(20)) as "STATUS_PAGAMENTO",
        UPPER(E."NOME") as "EMPRESA",
        --cast(null  as text),
        CAST(null as VARCHAR(40)) as "CODCONTA",
        UPPER(D."NOME")  as "NOME_FORNECEDOR",
        --cast(null as varchar(250))  as "NOME_FORNECEDOR",
        M."CODCFO",
        M."IDBANCODADOS",
        M."CODCOLIGADA",
        M."CODFILIAL",
        M."NUMEROMOV" as "NUMERO_NF",
        M."DATAEMISSAO" as "DATAEMISSAO_NF",
        M."DATASAIDA" as "DATASAIDA_NF",
        M."CODCCUSTO",
        TRIM(UPPER(T."DESCRICAO")) as "CODTDO_DESCRICAO",
       -- cast(null as text) as "CODTDO_DESCRICAO",
        ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "NUMERO_NF_TRATADO",
        F."NUMERODOCUMENTO" as "NUMERO_PARC",
        F."IDLAN",
        F."SEGUNDONUMERO",
        F."IDMOV",
        CAST(0 as NUMERIC(15,4)) as "VALORBRUTO_NF",
        CAST(0 as NUMERIC(15,4)) as "VALORLIQUIDO_NF",
        F."VALORORIGINAL" as "VALOR_PARC",
         CAST(0 as NUMERIC(15,4)) as "VALOR_PAGO",
         CAST(0 as NUMERIC(15,4)) as "VALOR_APAGAR",
        F."VALORORIGINAL" as "VALOR",
        F."VALORDESCONTO" as "VALORDESCONTO_PARC",       
        F."DATAVENCIMENTO" as "DATAVENCIMENTO_PARC",
        F."DATABAIXA" as "DATABAIXA_PARC",
        F."DATACANCELAMENTO" as "DATACANCELAMENTO_PARC",
        F."DATAPAG" as "DATAPAG_PARC",
        F."STATUSLAN",
        F."HISTORICO"
FROM public."TMOV" M
LEFT JOIN "FLAN" F ON M."IDMOV" = F."IDMOV" AND M."IDBANCODADOS" = F."IDBANCODADOS" AND M."CODCOLIGADA" = F."CODCOLIGADA" AND M."CODFILIAL" = F."CODFILIAL"
LEFT JOIN public."FTDO" T ON T."IDBANCODADOS" = F."IDBANCODADOS" AND T."CODCOLIGADA" = F."CODCOLIGADA" AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D ON D."CODCFO" = M."CODCFO" AND D."IDBANCODADOS" = M."IDBANCODADOS" and d."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = M."IDBANCODADOS" AND E."CODCOLIGADA" = M."CODCOLIGADA" AND E."CODFILIAL" = M."CODFILIAL"
WHERE F."DATACANCELAMENTO" IS null;


-- As parcelas nascem no DÉBITO
UPDATE public."FT_PARCELAS_NF"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_PARCELAS_NF"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_PARCELAS_NF"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_PARCELAS_NF"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_PARCELAS_NF"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_PARCELAS_NF"."CODCFO" =  subquery."CODCFO";	
		

DROP TABLE IF EXISTS public."FT_NOTAS_FISCAIS";
CREATE TABLE public."FT_NOTAS_FISCAIS" AS 
SELECT  row_number() OVER() as "NUMERO_LINHA",
        'NF' as "TIPO_ITEM",
        cast('EM ABERTO' as VARCHAR(20)) as "STATUS_PAGAMENTO",
        UPPER(E."NOME") as "EMPRESA",
        CAST(null as VARCHAR(40)) as "CODCONTA",
        D."NOME" as "NOME_FORNECEDOR",
        M."CODCFO",
        M."IDBANCODADOS",
        M."CODCOLIGADA",
        M."CODFILIAL",
        M."NUMEROMOV" as "NUMERO_NF",
        M."DATAEMISSAO" as "DATAEMISSAO_NF",
        M."DATASAIDA" as "DATASAIDA_NF",
        M."CODCCUSTO",
        CAST(null as VARCHAR(40)) as "CODTDO_DESCRICAO",
        ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "NUMERO_NF_TRATADO",
        CAST(null as VARCHAR(40)) as "NUMERO_PARC",
        CAST(0 as INT4) as "IDLAN",
        CAST(null as VARCHAR(20)) as "SEGUNDONUMERO",
        M."IDMOV",
        M."VALORBRUTO" as "VALORBRUTO_NF",
        M."VALORLIQUIDO" as "VALORLIQUIDO_NF",
        CAST(0 as NUMERIC(15,4)) as "VALOR_PARC",
        CAST(0 as NUMERIC(15,4)) as "VALOR_PAGO",
        CAST(0 as NUMERIC(15,4)) as "VALOR_APAGAR",
        M."VALORLIQUIDO" as "VALOR",
        CAST(0 as NUMERIC(15,4)) as "VALORDESCONTO_PARC",        
        CAST(null as DATE) as "DATAVENCIMENTO_PARC",
        CAST(null as DATE) as "DATABAIXA_PARC",
        CAST(null as DATE) as "DATACANCELAMENTO_PARC",
        CAST(null as DATE) as "DATAPAG_PARC",
        CAST(null as INT4) as "STATUSLAN",
        CAST(null as VARCHAR(255)) as "HISTORICO"
FROM public."TMOV" M
LEFT JOIN public."FCFO" D ON D."CODCFO" = M."CODCFO" AND D."IDBANCODADOS" = M."IDBANCODADOS" and D."CODCOLIGADA" = M."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = M."IDBANCODADOS" AND E."CODCOLIGADA" = M."CODCOLIGADA" AND E."CODFILIAL" = M."CODFILIAL";


-- As parcelas nascem no DÉBITO
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO";	


UPDATE public."FT_NOTAS_FISCAIS" N
SET "VALOR_PAGO" = subquery."VALOR_PARC"
FROM (
    SELECT 
        SUM("VALOR_PARC") AS "VALOR_PARC",
        "CODCFO",
        "IDMOV",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL"
    FROM public."FT_PARCELAS_NF"
    GROUP BY 
        "CODCFO",
        "IDMOV",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL"
) AS subquery
WHERE 
    N."CODCFO" = subquery."CODCFO" AND
    N."IDMOV" = subquery."IDMOV" AND
    N."IDBANCODADOS" = subquery."IDBANCODADOS" AND
    N."CODCOLIGADA" = subquery."CODCOLIGADA" AND
    N."CODFILIAL" = subquery."CODFILIAL";

   
   
UPDATE public."FT_NOTAS_FISCAIS" N
SET "VALOR_APAGAR" = subquery."VALOR_APAGAR"
FROM (
    SELECT DISTINCT  "VALORLIQUIDO_NF" - "VALOR_PAGO" AS "VALOR_APAGAR",
    				"VALORBRUTO_NF",
    				"VALOR_PAGO",
        	"NUMERO_LINHA"
    FROM public."FT_NOTAS_FISCAIS"

) AS subquery
WHERE 
    N."NUMERO_LINHA" = subquery."NUMERO_LINHA";
	

DROP TABLE IF EXISTS public."FT_NF_EMITIDAS_VS_PARC";
CREATE TABLE public."FT_NF_EMITIDAS_VS_PARC" AS 	
select * from public."FT_NOTAS_FISCAIS"
union 
select * from public."FT_PARCELAS_NF";



UPDATE public."FT_NF_EMITIDAS_VS_PARC"
SET "NOME_FORNECEDOR" = "NOME"
FROM (
    SELECT DISTINCT  "IDBANCODADOS", "CODCFO", "NOME"
    FROM public."FCFO"

) AS subquery
WHERE 
    public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" = subquery."IDBANCODADOS" and
    public."FT_NF_EMITIDAS_VS_PARC"."CODCFO" = subquery."CODCFO" AND
    public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" is NULL;
   
   
   

UPDATE public."FT_NF_EMITIDAS_VS_PARC" N
SET "STATUS_PAGAMENTO" = 'PAGO'
WHERE "VALOR_APAGAR" = 0;
	



-- As parcelas nascem no DÉBITO
UPDATE public."FT_NF_EMITIDAS_VS_PARC"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				-- substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 TRIM(substring("COMPLEMENTO" FROM '^[^-]+')) as "NOME_FORNECEDOR",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" =  subquery."NOME_FORNECEDOR" AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCONTA" is null;

	
	
UPDATE public."FT_NF_EMITIDAS_VS_PARC"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				-- substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 TRIM(substring("COMPLEMENTO" FROM '^[^-]+')) as "NOME_FORNECEDOR",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" =  subquery."NOME_FORNECEDOR" AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCONTA" is null;
	


UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	--regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		--public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;		
	
	
	
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	--regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		--public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;	
	

	
	
	
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT --	regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
			AND trim(substring("COMPLEMENTO" FROM '-(.{4})')) = 'NF.'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
	--	public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;	
	
	
	
END;
$function$
;

```

## A.5 PostgreSQL — `fc_validacao()`

```sql
CREATE OR REPLACE FUNCTION public.fc_validacao() 
 RETURNS void 
 LANGUAGE plpgsql 
AS $function$ 
 
-- PROCEDURE QUE MOSTRA OS LANÇAMENTOS QUE FALTAM NA BASE 
 
 
DECLARE 
    data_mes_anterior DATE; 
    ano_mes_anterior TEXT; 
    primeiro_dia_mes_anterior DATE;  
    data_execucao  TEXT; 
BEGIN 
    -- Atribui o ultimo dia do mês anterior 
\tdata_mes_anterior := DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 day'; 
 
\t-- Calcula o primeiro dia do mês anterior 
    primeiro_dia_mes_anterior := DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month'); 
     
    -- Atribui o ano e o mês anterior no formato 'YYYY-MM' 
    ano_mes_anterior := TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM'); 
   
    SELECT  
        "DATAEXECUCAO" || ' ' || "HORAEXECUCAO" 
    INTO  
        data_execucao 
    FROM  
        public."FT_RAZAO_DASH_01" A 
    WHERE  
        A."DATAEXECUCAO" IS NOT NULL 
    LIMIT 1;   
   
    
    
    
 
/* 
DROP TABLE IF EXISTS public."TB_CLANCA"; 
CREATE TABLE public."TB_CLANCA" AS  
select distinct  '2025-01-13 14:00' as  "DATA_EXECUCAO",  
\t\t"DATA" as "DATA_LANCAMENTO",  
\t\t"IDLANCAMENTO",  
\t\t"IDPARTIDA" , 
\t\t"IDBANCODADOS", 
    \t"CODCOLIGADA", 
    \t"CODFILIAL", 
    \t"DEBITO", 
\t\t"CREDITO", 
    \t"VALOR",   
\t\t'CPARTIDA' as "NOME_TABELA_TOTVS" 
from "CLANCA_BACKUP_13012025"; 
 
CREATE INDEX idx_id_CLANCA_IDLANCAMENTO ON public."TB_CLANCA" ("IDLANCAMENTO"); 
CREATE INDEX idx_id_CLANCA_IDPARTIDA ON public."TB_CLANCA" ("IDPARTIDA"); 
 
*/ 
 
 
REINDEX TABLE public."TB_CLANCA"; 
 
 
INSERT INTO public."TB_CLANCA" ("DATA_EXECUCAO", "DATA_LANCAMENTO", "IDLANCAMENTO", "IDPARTIDA","IDBANCODADOS","CODCOLIGADA","CODFILIAL","DEBITO","CREDITO","VALOR","NOME_TABELA_TOTVS") 
SELECT  
    data_execucao AS "DATA_EXECUCAO",  
    "DATA" as "DATA_LANCAMENTO",  
    "IDLANCAMENTO",  
    "IDPARTIDA",  
    "IDBANCODADOS", 
    "CODCOLIGADA", 
    "CODFILIAL",  
    "DEBITO", 
\t"CREDITO", 
    "VALOR", 
    'CLANCA' AS "NOME_TABELA_TOTVS" 
FROM public."CLANCA" AS c 
WHERE NOT EXISTS ( 
    SELECT 1 
    FROM public."TB_CLANCA" AS t 
    WHERE t."IDLANCAMENTO" = c."IDLANCAMENTO" 
      AND t."IDPARTIDA" = c."IDPARTIDA" 
); 
 
 
 
 
DROP TABLE IF EXISTS public."TB_CLANCA_VALIDACAO"; 
CREATE TABLE public."TB_CLANCA_VALIDACAO" AS  
SELECT  
    c."DATA_EXECUCAO",  
    c."DATA_LANCAMENTO",  
    c."IDLANCAMENTO",  
    c."IDPARTIDA",  
    UPPER(E."NOME") as "EMPRESA", 
    c."IDBANCODADOS", 
    c."CODCOLIGADA", 
    c."CODFILIAL",  
    c."DEBITO", 
\tc."CREDITO", 
    c."VALOR", 
    'CLANCA' AS "NOME_TABELA_TOTVS" 
FROM public."TB_CLANCA" AS c 
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = c."IDBANCODADOS"  
\t\t\t\t\t\t\tAND E."CODCOLIGADA" = c."CODCOLIGADA"  
\t\t\t\t\t\t\tAND E."CODFILIAL" = c."CODFILIAL" 
WHERE NOT EXISTS ( 
    SELECT 1 
    FROM public."CLANCA" AS t 
    WHERE t."IDLANCAMENTO" = c."IDLANCAMENTO" 
      AND t."IDPARTIDA" = c."IDPARTIDA" 
); 
 
 
 
/* 
DROP TABLE IF EXISTS public."TB_FLAN"; 
CREATE TABLE public."TB_FLAN" AS 
select '2025-01-13 14:00' as "DATA_EXECUCAO",  
\t\t"DATAEMISSAO",  
\t\t"IDLAN",  
\t\t"NUMERODOCUMENTO"  , 
\t\t"IDBANCODADOS", 
    \t"CODCOLIGADA", 
    \t"CODFILIAL",   
    \t"VALORLIQUIDO", 
\t\t'FLAN' as "NOME_TABELA_TOTVS" 
from "FLAN"; 
 
CREATE INDEX idx_id_FLAN_IDLAN ON public."TB_FLAN" ("IDLAN"); 
CREATE INDEX idx_id_FLAN_NUMERODOCUMENTO ON public."TB_FLAN" ("NUMERODOCUMENTO"); 
 
*/ 
 
REINDEX TABLE public."TB_FLAN"; 
 
INSERT INTO public."TB_FLAN" ("DATA_EXECUCAO", "DATAEMISSAO", "IDLAN", "NUMERODOCUMENTO","IDBANCODADOS","CODCOLIGADA","CODFILIAL","VALORLIQUIDO", "NOME_TABELA_TOTVS") 
SELECT  
    data_execucao as "DATA_EXECUCAO",  
    "DATAEMISSAO",  
    "IDLAN",  
    "NUMERODOCUMENTO",  
    "IDBANCODADOS", 
    "CODCOLIGADA", 
    "CODFILIAL", 
    "VALORLIQUIDO", 
    'FLAN' AS "NOME_TABELA_TOTVS" 
FROM public."FLAN" AS f 
WHERE NOT EXISTS ( 
    SELECT 1 
    FROM public."TB_FLAN" AS t 
    WHERE t."IDLAN" = f."IDLAN" 
      AND t."NUMERODOCUMENTO" = f."NUMERODOCUMENTO" 
); 
 
 
 
 
DROP TABLE IF EXISTS public."TB_FLAN_VALIDACAO"; 
CREATE TABLE public."TB_FLAN_VALIDACAO" AS 
SELECT  
    f."DATA_EXECUCAO",  
    f."DATAEMISSAO",  
    f."IDLAN",  
    f."NUMERODOCUMENTO", 
    UPPER(E."NOME") as "EMPRESA", 
    f."IDBANCODADOS", 
    f."CODCOLIGADA", 
    f."CODFILIAL", 
    f."VALORLIQUIDO", 
    'FLAN' AS "NOME_TABELA_TOTVS" 
FROM public."TB_FLAN" AS f 
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = f."IDBANCODADOS"  
\t\t\t\t\t\t\tAND E."CODCOLIGADA" = f."CODCOLIGADA"  
\t\t\t\t\t\t\tAND E."CODFILIAL" = f."CODFILIAL" 
WHERE NOT EXISTS ( 
    SELECT 1 
    FROM public."FLAN" AS t 
    WHERE t."IDLAN" = f."IDLAN" 
      AND t."NUMERODOCUMENTO" = f."NUMERODOCUMENTO" 
); 
 
 
 
 
 
END; 
$function$ 
;
```

## A.6 PostgreSQL — `fc_balancete()`

```sql
CREATE OR REPLACE FUNCTION public.fc_balancete(datainicial date, datafinal date, containicial character varying, contafinal character varying, coligada integer, bancodedados integer)
 RETURNS TABLE(codconta character varying, descricao character varying, codcoligada integer, idbancodados integer, anterior numeric, debitos numeric, creditos numeric, saldoatual numeric)
 LANGUAGE plpgsql
AS $function$
BEGIN 
RETURN QUERY
WITH "CONTAHIERARCHY" AS (
    SELECT 
        C1."CODCONTA" AS CONTA_PAI,
        C2."CODCONTA" AS CONTA_FILHA,
        (SELECT SUM(B."VALORDEBITOANTERIOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS VALORDEBITOANTERIOR_PAI,
        (SELECT SUM(B."VALORCREDITOANTERIOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS VALORCREDITOANTERIOR_PAI,
        (SELECT SUM(B."VALORDEBITOANTERIOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS VALORDEBITOANTERIOR_FILHA,
        (SELECT SUM(B."VALORCREDITOANTERIOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS VALORCREDITOANTERIOR_FILHA,
        (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS DEBITOS_PAI,
        (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS CREDITOS_PAI,
        (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS DEBITOS_FILHA,
        (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN DATAINICIAL AND DATAFINAL) AS CREDITOS_FILHA,
        C1."IDBANCODADOS"
    FROM "CCONTA" C1
    LEFT JOIN "CCONTA" C2 ON C2."CODCONTA" LIKE C1."CODCONTA" || '.%' AND C1."CODCOLIGADA" = C2."CODCOLIGADA" AND C1."IDBANCODADOS" = C2."IDBANCODADOS"
    WHERE C1."IDBANCODADOS" = BANCODEDADOS
    AND C1."CODCOLIGADA" = COLIGADA
    AND C1."CODCONTA" BETWEEN CONTAINICIAL AND CONTAFINAL
),
VALORAGREGADO AS (
    SELECT 
        C1."CODCONTA" AS CONTA,
        C1."IDBANCODADOS" AS BANCODADOS,
        SUM(VALORDEBITOANTERIOR_PAI) AS VALORDEBITOANTERIOR_PAI,
        SUM(VALORCREDITOANTERIOR_PAI) AS VALORCREDITOANTERIOR_PAI,
        SUM(VALORDEBITOANTERIOR_FILHA) AS VALORDEBITOANTERIOR_FILHA,
        SUM(VALORCREDITOANTERIOR_FILHA) AS VALORCREDITOANTERIOR_FILHA,
        SUM(DEBITOS_PAI) AS DEBITOS_PAI,
        SUM(CREDITOS_PAI) AS CREDITOS_PAI,
        SUM(DEBITOS_FILHA) AS DEBITOS_FILHA,
        SUM(CREDITOS_FILHA) AS CREDITOS_FILHA
    FROM "CCONTA" C1
    LEFT JOIN "CONTAHIERARCHY" C2 ON C1."CODCONTA" = C2.CONTA_PAI AND C1."IDBANCODADOS" = C2."IDBANCODADOS"
    WHERE C1."IDBANCODADOS" = BANCODEDADOS
    AND C1."CODCOLIGADA" = COLIGADA
    AND C1."CODCONTA" BETWEEN CONTAINICIAL AND CONTAFINAL
    GROUP BY C1."CODCONTA",C1."IDBANCODADOS"
)
SELECT 
    C."CODCONTA",
    C."DESCRICAO",
    CAST(C."CODCOLIGADA" AS INT),
    CAST(C."IDBANCODADOS" AS INT),
    COALESCE(V.VALORDEBITOANTERIOR_PAI, 0) + COALESCE(V.VALORDEBITOANTERIOR_FILHA, 0) +
    COALESCE(V.VALORCREDITOANTERIOR_PAI, 0) + COALESCE(V.VALORCREDITOANTERIOR_FILHA, 0) AS ANTERIOR,
    COALESCE(V.DEBITOS_PAI, 0) + COALESCE(V.DEBITOS_FILHA, 0) AS DEBITOS,
    COALESCE(V.CREDITOS_PAI, 0) + COALESCE(V.CREDITOS_FILHA, 0) AS CREDITOS,
    COALESCE(V.VALORDEBITOANTERIOR_PAI, 0) + COALESCE(V.VALORDEBITOANTERIOR_FILHA, 0) +
    COALESCE(V.VALORCREDITOANTERIOR_PAI, 0) + COALESCE(V.VALORCREDITOANTERIOR_FILHA, 0) +
    COALESCE(V.DEBITOS_PAI, 0) + COALESCE(V.DEBITOS_FILHA, 0) +
    COALESCE(V.CREDITOS_PAI, 0) + COALESCE(V.CREDITOS_FILHA, 0) AS SALDOATUAL
FROM 
    "CCONTA" C
LEFT JOIN VALORAGREGADO V ON C."CODCONTA" = V.CONTA AND V.BANCODADOS = C."IDBANCODADOS"
WHERE C."CODCOLIGADA" = COLIGADA
AND C."IDBANCODADOS" = BANCODEDADOS
AND C."CODCONTA" BETWEEN CONTAINICIAL AND CONTAFINAL
ORDER BY 
    C."CODCONTA";
RETURN;
END; 
$function$
;
```

## A.7 PostgreSQL — `fc_fech_balancete()`

```sql
CREATE OR REPLACE FUNCTION public.fc_fech_balancete(ano_mes character varying)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    datainicial DATE;
    datafinal DATE;
    containicial character varying := '0001'; -- ajuste conforme necessário
    contafinal character varying := '9999'; -- ajuste conforme necessário
BEGIN
    -- Converter o ano_mes para as datas inicial e final
    datainicial := TO_DATE(ano_mes || '-01', 'YYYY-MM-DD'); -- Primeiro dia do mês
    datafinal := (datainicial + INTERVAL '1 month - 1 day'); -- Último dia do mês

    -- Inserir os resultados na tabela tb_fech_balancete
    INSERT INTO tb_fech_balancete (codconta, descricao, codcoligada, idbancodados, anterior, debitos, creditos, saldoatual)
    WITH "CONTAHIERARCHY" AS (
        SELECT 
            C1."CODCONTA" AS CONTA_PAI,
            C2."CODCONTA" AS CONTA_FILHA,
            (SELECT SUM(B."VALORDEBITOANTERIOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS VALORDEBITOANTERIOR_PAI,
            (SELECT SUM(B."VALORCREDITOANTERIOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS VALORCREDITOANTERIOR_PAI,
            (SELECT SUM(B."VALORDEBITOANTERIOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS VALORDEBITOANTERIOR_FILHA,
            (SELECT SUM(B."VALORCREDITOANTERIOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS VALORCREDITOANTERIOR_FILHA,
            (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS DEBITOS_PAI,
            (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C1."CODCONTA" AND B."IDBANCODADOS" = C1."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS CREDITOS_PAI,
            (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."DEBITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS DEBITOS_FILHA,
            (SELECT SUM(B."VALOR") FROM "CPARTIDA" B WHERE B."CREDITO" LIKE C2."CODCONTA" AND B."IDBANCODADOS" = C2."IDBANCODADOS" AND B."DATA" BETWEEN datainicial AND datafinal) AS CREDITOS_FILHA,
            C1."IDBANCODADOS"
        FROM "CCONTA" C1
        LEFT JOIN "CCONTA" C2 ON C2."CODCONTA" LIKE C1."CODCONTA" || '.%' AND C1."CODCOLIGADA" = C2."CODCOLIGADA" AND C1."IDBANCODADOS" = C2."IDBANCODADOS"
        WHERE C1."CODCONTA" BETWEEN containicial AND contafinal
    ),
    VALORAGREGADO AS (
        SELECT 
            C1."CODCONTA" AS CONTA,
            C1."IDBANCODADOS" AS BANCODADOS,
            SUM(VALORDEBITOANTERIOR_PAI) AS VALORDEBITOANTERIOR_PAI,
            SUM(VALORCREDITOANTERIOR_PAI) AS VALORCREDITOANTERIOR_PAI,
            SUM(VALORDEBITOANTERIOR_FILHA) AS VALORDEBITOANTERIOR_FILHA,
            SUM(VALORCREDITOANTERIOR_FILHA) AS VALORCREDITOANTERIOR_FILHA,
            SUM(DEBITOS_PAI) AS DEBITOS_PAI,
            SUM(CREDITOS_PAI) AS CREDITOS_PAI,
            SUM(DEBITOS_FILHA) AS DEBITOS_FILHA,
            SUM(CREDITOS_FILHA) AS CREDITOS_FILHA
        FROM "CCONTA" C1
        LEFT JOIN "CONTAHIERARCHY" C2 ON C1."CODCONTA" = C2.CONTA_PAI AND C1."IDBANCODADOS" = C2."IDBANCODADOS"
        WHERE C1."CODCONTA" BETWEEN containicial AND contafinal
        GROUP BY C1."CODCONTA", C1."IDBANCODADOS"
    )
    SELECT 
        C."CODCONTA",
        C."DESCRICAO",
        CAST(C."CODCOLIGADA" AS INT),
        CAST(C."IDBANCODADOS" AS INT),
        COALESCE(V.VALORDEBITOANTERIOR_PAI, 0) + COALESCE(V.VALORDEBITOANTERIOR_FILHA, 0) +
        COALESCE(V.VALORCREDITOANTERIOR_PAI, 0) + COALESCE(V.VALORCREDITOANTERIOR_FILHA, 0) AS ANTERIOR,
        COALESCE(V.DEBITOS_PAI, 0) + COALESCE(V.DEBITOS_FILHA, 0) AS DEBITOS,
        COALESCE(V.CREDITOS_PAI, 0) + COALESCE(V.CREDITOS_FILHA, 0) AS CREDITOS,
        COALESCE(V.VALORDEBITOANTERIOR_PAI, 0) + COALESCE(V.VALORDEBITOANTERIOR_FILHA, 0) +
        COALESCE(V.VALORCREDITOANTERIOR_PAI, 0) + COALESCE(V.VALORCREDITOANTERIOR_FILHA, 0) +
        COALESCE(V.DEBITOS_PAI, 0) + COALESCE(V.DEBITOS_FILHA, 0) +
        COALESCE(V.CREDITOS_PAI, 0) + COALESCE(V.CREDITOS_FILHA, 0) AS SALDOATUAL
    FROM 
        "CCONTA" C
    LEFT JOIN VALORAGREGADO V ON C."CODCONTA" = V.CONTA AND V.BANCODADOS = C."IDBANCODADOS"
    WHERE C."CODCONTA" BETWEEN containicial AND contafinal;

    RETURN;
END; 
$function$
;
```

## A.8 PostgreSQL — view final `VW_FT_COMPOSICAO`

```sql
-- public."VW_FT_COMPOSICAO" source

CREATE OR REPLACE VIEW public."VW_FT_COMPOSICAO"
AS SELECT a."POSICAO",
    COALESCE(a."ANO_MES", '-'::text) AS "ANO_MES",
    a."DATA",
    a."DATACONTABILIZBX",
    COALESCE(a."EMPRESA", '-'::character varying) AS "EMPRESA",
    COALESCE(a."REDUZIDO", '-'::character varying) AS "REDUZIDO",
    COALESCE(a."CODCONTA", '-'::character varying) AS "CODCONTA",
    COALESCE(a."IDPARTIDA", '-'::character varying) AS "IDPARTIDA",
    a."ANTERIOR",
    a."DEBITOS",
    a."CREDITOS",
        CASE
            WHEN a."SALDO_ATUAL" IS NULL THEN 0::numeric
            ELSE a."SALDO_ATUAL"
        END AS "SALDO_ATUAL",
    COALESCE(a."DESCRICAO_TRATADA", '-'::text) AS "DESCRICAO_TRATADA",
    a."CODFILIAL",
        CASE
            WHEN a."HISTORICO" IS NULL AND a."IDPARTIDA"::text ~~ '202%'::text THEN 'Fechamento'::character varying
            ELSE COALESCE(a."HISTORICO", '-'::character varying)
        END AS "HISTORICO",
    COALESCE(a."CODCOLIGADA"::character varying(10), '-'::character varying) AS "CODCOLIGADA",
    COALESCE(b.preparado, '-'::character varying) AS "PREPARADO",
    COALESCE(b.data_preparado, '-'::character varying) AS "DATA_PREPARADO",
    COALESCE(b.conferido, '-'::character varying) AS "CONFERIDO",
    COALESCE(b.data_conferido, '-'::character varying) AS "DATA_CONFERIDO",
    COALESCE(b.pendencia, '-'::character varying) AS "PENDENCIA",
    COALESCE(b.resp_pendencia, '-'::character varying) AS "RESP_PENDENCIA",
        CASE
            WHEN btrim(c.prioridade::text) = '1'::text THEN '1-Baixa'::text
            WHEN btrim(c.prioridade::text) = '2'::text THEN '2-Regular'::text
            WHEN btrim(c.prioridade::text) = '3'::text THEN '3-Média'::text
            WHEN btrim(c.prioridade::text) = '4'::text THEN '4-Alta'::text
            ELSE 'Não definido'::text
        END AS prioridade,
        CASE
            WHEN btrim(b.preparado::text) IS NULL OR btrim(b.preparado::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_PREPARADO",
        CASE
            WHEN btrim(b.conferido::text) IS NULL OR btrim(b.conferido::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_CONFERIDO",
        CASE
            WHEN btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_PENDENCIA",
        CASE
            WHEN (btrim(b.preparado::text) IS NOT NULL OR btrim(b.preparado::text) <> ''::text) AND (btrim(b.conferido::text) IS NOT NULL OR btrim(b.conferido::text) <> ''::text) AND (btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text) THEN 1
            ELSE 0
        END AS "QTD_FINALIZADO",
        CASE
            WHEN btrim(b.preparado::text) IS NULL OR btrim(b.preparado::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_PREPARADO",
        CASE
            WHEN btrim(b.conferido::text) IS NULL OR btrim(b.conferido::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_CONFERIDO",
        CASE
            WHEN btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_PENDENCIA",
        CASE
            WHEN (btrim(b.preparado::text) IS NOT NULL OR btrim(b.preparado::text) <> ''::text) AND (btrim(b.conferido::text) IS NOT NULL OR btrim(b.conferido::text) <> ''::text) AND (btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text) THEN 'SIM'::text
            ELSE 'NÃO'::text
        END AS "STATUS_FINALIZADO",
    COALESCE(a."NOME_FORNECEDOR", '-'::character varying) AS "NOME_FORNECEDOR",
    COALESCE(a."NUMCONTABILBX", '-'::character varying) AS "NUMCONTABILBX",
    COALESCE(a."SEGUNDONUMERO", '-'::character varying) AS "SEGUNDONUMERO",
    a."VALOR_NF_EM_ABERTO",
    a."DATAEXECUCAO",
    a."IDBANCODADOS"
   FROM "FT_COMPOSICAO" a
     LEFT JOIN "FVALIDACAO" b ON b.chave_validacao::text = concat(a."CODFILIAL", a."EMPRESA", a."CODCONTA", a."ANO_MES")
     LEFT JOIN "DPRIORIDADE" c ON c.chave::text = concat(a."EMPRESA", a."ANO_MES")
  ORDER BY a."DATA", a."EMPRESA", a."POSICAO";
```

---

## 12. Resumo executivo

O processo é uma solução de apoio à **conciliação contábil do TOTVS RM**. O Pentaho dispara a função principal no PostgreSQL. A rotina transforma os lançamentos em razão e fechamento mensal, recompõe os saldos, associa empresa e plano de contas e, em seguida, cruza essas informações com o financeiro para identificar documentos, fornecedores, notas fiscais, parcelas, pagamentos e valores em aberto.

O resultado é concentrado em `FT_COMPOSICAO`: **uma tabela que representa a composição do saldo e o detalhamento das contas contábeis por empresa, conta e período**. A `VW_FT_COMPOSICAO` acrescenta os controles de validação e prioridade e funciona como a camada final consultada pelo Excel.

Assim, o usuário deixa de enxergar apenas um saldo contábil agregado e passa a ter uma visão analítica de **como aquele saldo foi formado, quais itens o compõem e qual é a situação da conciliação**.
