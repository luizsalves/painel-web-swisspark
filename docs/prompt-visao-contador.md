# Visão do contador: especificação completa para implementação

## 0. Como usar este documento

Este documento descreve **tudo** o que precisa existir na aba **Visão do contador** da tela Indicadores do Painel Web SwissPark: dados, regras de negócio, contratos de API, layout, medidas, cores, textos, estados e testes.

Anexei:
- `visao-contador.png`: imagem do esboço inteiro da aba.
- `tabela-indicadores.png`: recorte da tabela de indicadores.
- `referencia-visao-contador.html`: a aba inteira desenhada em HTML e JavaScript puro. Abra no navegador: é a referência visual executável, com as medidas, cores e interações finais (Apêndice B).

O **Apêndice A**, no fim deste documento, traz o código de referência em React + TypeScript + CSS de cada parte da tela: régua, minigráfico, tabela, cards, painel do lucro, gaveta, matriz e base de cálculo.

**O resultado precisa ficar igual ao esboço**, com as mesmas seções na mesma ordem, os mesmos gráficos, medidas, textos e cores definidos aqui. As únicas diferenças permitidas são:

1. O azul do esboço (#1D4F91) vira a **cor primária verde do sistema**, a mesma do botão "Filtros". Use o token que já existe.
2. A fonte é a do sistema. Não carregue IBM Plex.
3. Os dados são **reais**. **Nunca use dado ilustrativo ou inventado.** Quando faltar dado, mostre "—" e o aviso da seção 6.6.

Este documento substitui qualquer instrução anterior sobre a visão do contador, incluindo o arquivo `prompt-indicadores-duas-visoes.md`. Se ele conflitar com uma instrução antiga, vale este.

---

## 1. Contexto do projeto

- **Backend:** .NET 10 / ASP.NET Core, em `backend/PainelWebSwissPark.Api`. Camadas: Controller → Service → Repository → DbContext. Os endpoints já nascem protegidos por JWT.
- **Dados contábeis:** TOTVS RM (SQL Server, somente leitura), via `AppDbContext`, com quatro bases lidas em sequência (`BasesTotvs.Todas`). Consultas paralelas no mesmo DbContext dão erro.
- **Tabelas próprias do painel:** Postgres, via `PainelDbContext`.
- **Frontend:** React 19 + Vite + TanStack Query, em `frontend/`. Filtros globais no `FiltrosContext`. Lint com oxlint (`.oxlintrc.json`).
- **Tela atual:** já existe a tela **Indicadores**, com:
  - os chips de filtro Empresa, Filial e Período (intervalo de meses, por exemplo "Mar a Set de 2026") e o botão Atualizar;
  - o card "Índices financeiros", com grupos de cards por indicador;
  - base de cálculo e DFC.
- **Cálculo atual:** `Services/BusinessLogic/IndicadoresService.cs`, método `CalcularAsync(DateTime inicio, DateTime fim, short codColigada, short? codFilial, CancellationToken)`, que devolve `IndicadoresViewModel`. Ele usa `IndicadoresTotvsRepository` e `BalanceteTotvsRepository` e calcula a partir dos saldos da CPARTIDA, com um de-para único (coligada 0) que foi conferido ao centavo contra a DFC de 2025 da coligada 1. Ele já trata a apuração do resultado (`Medida.Apuracao`). **Essa regra contábil não pode ser reescrita nem duplicada. Reaproveite.**
- **Exportação para Excel:** NPOI, com exemplos em `DfcExcel.cs`, `BalancoExcel.cs` e `FechamentoExcel.cs`.
- **Lançamentos de uma conta:** `ILancamentosTotvsService.ListarAsync(codColigada, codFilial, anoMes, codConta)`, já usado pela tela de Fechamentos.

---

## 2. Regras de trabalho (obrigatórias)

1. **Antes de editar qualquer arquivo**, investigue e me entregue um plano. Ele deve cobrir:
   - os arquivos da tela Indicadores atual (front e back), o fluxo de dados e o formato do `IndicadoresViewModel`;
   - como o `IndicadoresTotvsRepository.ListarSaldosAsync` monta a consulta, e se dá para agrupar por mês (seção 5.1);
   - como o `IndicadoresService` trata a apuração, o de-para da DRE e a linha "variações não classificadas" da DFC;
   - se existe projeto de testes no backend e executor de testes no front;
   - quais componentes de UI, tokens de cor, Tooltip e Drawer o front já tem;
   - **se existe implementação de uma tentativa anterior** (branch `feat/indicadores-duas-visoes`, componentes como `ReguaFaixas` e `Sparkline`): liste o que vai reaproveitar, corrigir ou descartar;
   - a lista de arquivos que vai criar ou alterar em cada etapa da seção 7.

   **Espere minha aprovação.**
2. Trabalhe na branch `feat/indicadores-visao-contador`. **Não faça commit nem push** sem eu pedir.
3. Não altere o schema de nenhum banco. Não crie tabela.
4. **Não adicione dependência nova**, nem no front nem no back. Gráficos em SVG e CSS. Se não houver projeto de testes, me pergunte antes de criar.
5. **Não mude nenhum valor calculado hoje**, exceto as regras novas descritas aqui (status, casos especiais, EBITDA 12m e a retirada do PCT). A seção 9 tem um teste de regressão.
6. Todas as faixas, textos de fórmula e regras de status ficam **num único lugar no backend** (seção 5.3). O front não decide status nem guarda número de faixa.
7. Ao fim de cada etapa:
   - rode `dotnet build`, os testes do backend, `npm run build`, o oxlint e os testes do front;
   - me mostre o que mudou, os resultados dos testes e o tempo de resposta do endpoint;
   - **pare e espere minha revisão.**
8. Textos da interface em português do Brasil, em frase normal (só a primeira letra maiúscula). Use exatamente os textos deste documento.
9. Se algo daqui for impossível ou contraditório com o código, **pare e me pergunte**. Não improvise.

---

## 3. Resultado esperado (resumo)

A tela Indicadores ganha duas abas: **Visão do contador** e **Visão da diretoria**. Nesta tarefa, a aba da diretoria mostra só um placeholder.

A aba do contador mostra, de cima para baixo:

1. O título "Índices financeiros" e o subtítulo atual (empresa, coligada, data do balanço, período do resultado).
2. A barra com o select **Comparar com** e o botão **Exportar**.
3. Um aviso de dados faltantes (só quando houver).
4. A faixa **Pontos a verificar** (só quando houver).
5. Os **4 cards de resumo**.
6. A **tabela de indicadores**: 13 indicadores em 3 grupos, com régua de faixas, atual, anterior, variação, ano anterior, minigráfico de 12 meses e status.
7. O painel **O que explica a variação do lucro**, com gaveta de contas e lançamentos.
8. A **matriz dos últimos 12 meses**.
9. A **base de cálculo** recolhível.
10. A **DFC** que já existe, sem mudança.

Os itens 2 a 9 vêm de **um endpoint novo**. A DFC continua vindo do endpoint atual.

---

## 4. Conceitos e regras de negócio

### 4.1 Períodos

- O período selecionado vem do filtro global: `inicio` e `fim`, ambos no formato `AAAA-MM`. **N** é o número de meses entre os dois, incluindo as pontas.
- **Período anterior:** os N meses imediatamente antes de `inicio`. Exemplo: mar/26 → fev/26; mar–set/26 → ago/25–fev/26.
- **Mesmo período do ano anterior:** `inicio − 12` a `fim − 12`. Exemplo: mar–set/26 → mar–set/25.
- **Itens do balanço:** em cada período, usam o saldo **no fim do último mês**.
- **Itens do resultado:** em cada período, usam a **soma dos meses** do período.
- **Série de 12 meses:** os 12 meses terminando em `fim`, sempre, qualquer que seja N. Exemplo: fim = set/26 → out/25 a set/26.
- **EBITDA 12m:** soma do EBITDA dos 12 meses terminando no `fim` do período considerado. Cada período tem o seu: atual, anterior e ano anterior, e cada mês da série também.
- **Meses sem dados:** meses anteriores ao primeiro mês com lançamento na coligada e filial. Indicadores desses meses ficam nulos (`temDados = false`).

### 4.2 Itens da base e convenção de sinais

Todos os valores em R$, com 2 casas, calculados pelo de-para existente.

**Balanço** (no fim do mês ou do período). Valores positivos, em módulo:

| Campo | Descrição |
|---|---|
| `ativoTotal` | Ativo total |
| `ativoCirculante` | Ativo circulante |
| `disponivel` | Disponível (caixa e equivalentes) |
| `estoques` | Estoques |
| `realizavelLongoPrazo` | Realizável a longo prazo |
| `passivoCirculante` | Passivo circulante |
| `passivoNaoCirculante` | Passivo não circulante |
| `patrimonioLiquido` | Patrimônio líquido. **Pode ser negativo** |
| `dividaBruta` | Empréstimos e financiamentos (circulante + não circulante) |
| `dividaLiquida` | `dividaBruta − disponivel`. **Pode ser negativa** |

Derivado: `passivoTotal = passivoCirculante + passivoNaoCirculante`, o passivo exigível, sem PL.

**Resultado** (soma do período). **O sinal é a contribuição para o lucro:** receitas positivas, despesas negativas.

| Campo | Descrição |
|---|---|
| `receitaLiquida` | Receita líquida (+) |
| `custos` | Custos (−) |
| `lucroBruto` | `receitaLiquida + custos` |
| `despesasOperacionais` | Despesas e outras receitas operacionais, exceto D&A (normalmente −) |
| `ebitda` | `lucroBruto + despesasOperacionais` |
| `depreciacaoAmortizacao` | D&A (−) |
| `ebit` | `ebitda + depreciacaoAmortizacao` |
| `resultadoFinanceiro` | Receitas − despesas financeiras (±) |
| `resultadoAntesIr` | `ebit + resultadoFinanceiro` |
| `irCs` | IRPJ + CSLL (−) |
| `outrosResultados` | O que sobrar para fechar a conta. Normalmente 0 |
| `lucroLiquido` | `resultadoAntesIr + irCs + outrosResultados` |

Regras:

- Se o de-para atual não separa as despesas operacionais, calcule `despesasOperacionais = ebitda − lucroBruto` e marque `despesasPorDiferenca = true`.
- **A equação do lucro líquido tem que fechar ao centavo.** `outrosResultados` existe só para garantir isso. Se ele der diferente de 0, registre um log de aviso com coligada, filial e período.
- O resultado mensal **exclui os lançamentos de apuração e encerramento**, pela mesma regra que o `IndicadoresService` já usa. Senão dezembro fica distorcido.

### 4.3 Indicadores

São 13 indicadores, nesta ordem e com estes textos. O PCT **sai da tela**, porque é igual ao Endividamento geral.

**Grupo "Liquidez"**

| Código | Nome na tela | Fórmula (tooltip) | Explicação (tooltip) | Unidade | Casas | Direção |
|---|---|---|---|---|---|---|
| `LIQ_CORRENTE` | Liquidez corrente | Ativo circulante ÷ Passivo circulante | Acima de 1: ativos suficientes para cobrir dívidas de até 1 ano. | número | 2 | maior melhor |
| `LIQ_SECA` | Liquidez seca | (Ativo circulante − Estoques) ÷ Passivo circulante | Visão mais conservadora; exclui estoques do cálculo. | número | 2 | maior melhor |
| `LIQ_IMEDIATA` | Liquidez imediata | Disponibilidades ÷ Passivo circulante | Acima de 1: cobre obrigações imediatas só com caixa e equivalentes. | número | 2 | maior melhor |
| `LIQ_GERAL` | Liquidez geral | (Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante) | Visão de longo prazo (mais de 12 meses). | número | 2 | maior melhor |

**Grupo "Endividamento"**

| Código | Nome na tela | Fórmula | Explicação | Unidade | Casas | Direção |
|---|---|---|---|---|---|---|
| `END_GERAL` | Endividamento geral | Passivo total ÷ Ativo total | A proporção dos ativos da empresa que foi financiada por terceiros. | % | 2 | menor melhor |
| `END_CURTO_PRAZO` | Dívida no curto prazo | Passivo circulante ÷ Passivo total | O percentual das dívidas que vence no curto prazo. | % | 2 | menor melhor |
| `DIV_LIQ_EBITDA_12M` | Dív. líquida / EBITDA 12m | (Dívida bruta − Caixa) ÷ EBITDA dos últimos 12 meses | Quantos anos de geração operacional de caixa levariam para quitar a dívida. | x | 2 | menor melhor |
| `DIV_BRUTA_PL` | Dívida bruta / PL | Dívida bruta ÷ Patrimônio líquido | A proporção entre o capital de terceiros e o capital próprio. | x | 2 | menor melhor |

**Grupo "Rentabilidade e resultado · no período"**

| Código | Nome na tela | Fórmula | Explicação | Unidade | Casas | Direção |
|---|---|---|---|---|---|---|
| `ROE` | ROE | Lucro líquido ÷ Patrimônio líquido | Retorno sobre o patrimônio líquido (ganho dos acionistas). | % | 2 | maior melhor |
| `ROA` | ROA | Lucro líquido ÷ Ativo total | Retorno sobre o ativo total (eficiência dos investimentos). | % | 2 | maior melhor |
| `MARGEM_LIQUIDA` | Margem líquida | Lucro líquido ÷ Receita líquida | Lucro líquido gerado a cada R$ 1 de venda. | % | 2 | maior melhor |
| `MARGEM_BRUTA` | Margem bruta | Lucro bruto ÷ Receita líquida | Percentual de lucro após o custo dos produtos e serviços. | % | 2 | maior melhor |
| `EBITDA` | EBITDA | EBIT + Depreciação + Amortização | Desempenho operacional puro (sem juros, impostos, depreciação e amortização). | R$ | 1 (em mil) | maior melhor |

Percentuais são armazenados em pontos percentuais: 71,79% é guardado como `71.79`, não `0.7179`.

**Faixas.** Em `maior melhor`, Risco fica abaixo do limite de risco e Bom a partir do limite bom. Em `menor melhor`, Bom vai até o limite bom e Risco fica acima do limite de risco. O resto é Atenção.

| Código | Limite de risco | Limite bom | Escala da régua (mín – máx) | Status pelo anualizado? |
|---|---|---|---|---|
| `LIQ_CORRENTE` | 1,00 | 1,50 | 0 – 2 | não |
| `LIQ_SECA` | 0,50 | 1,00 | 0 – 1,5 | não |
| `LIQ_IMEDIATA` | 0,10 | 0,30 | 0 – 0,5 | não |
| `LIQ_GERAL` | 1,00 | 1,20 | 0 – 2 | não |
| `END_GERAL` | 80 | 60 | 0 – 100 | não |
| `END_CURTO_PRAZO` | 70 | 40 | 0 – 100 | não |
| `DIV_LIQ_EBITDA_12M` | 4,00 | 2,50 | 0 – 6 | não |
| `DIV_BRUTA_PL` | 2,00 | 1,00 | 0 – 3 | não |
| `ROE` | 0 | 12 | −10 – 30 | **sim** |
| `ROA` | 0 | 4 | −5 – 10 | **sim** |
| `MARGEM_LIQUIDA` | 0 | 10 | −40 – 40 | não |
| `MARGEM_BRUTA` | 20 | 40 | 0 – 100 | não |
| `EBITDA` | 0 | 0 | sem régua | não |

### 4.4 Como calcular o status

O status tem 4 valores: `bom`, `atencao`, `risco` e `verificar`.

```
avaliar(valor, cfg):
  se valor é nulo → verificar
  x = arredondar(valorParaStatus, cfg.casas)       // arredondamento "metade para longe do zero"
  se cfg.direcao = maior_melhor:
      x >= limiteBom   → bom
      x <  limiteRisco → risco
      senão            → atencao
  se cfg.direcao = menor_melhor:
      x <= limiteBom   → bom
      x >  limiteRisco → risco
      senão            → atencao
```

- **Compare o valor já arredondado nas casas de exibição.** Assim o status nunca contradiz o número na tela. Exemplo: liquidez corrente 0,995 vira 1,00, que é Atenção, não Risco.
- **EBITDA:** < 0 é Risco; ≥ 0 é Bom.
- `valorParaStatus` é o valor anualizado quando a coluna "status pelo anualizado" é "sim" (seção 4.6). Nos outros casos, é o próprio valor.
- Os casos especiais da seção 4.5 têm prioridade sobre esta regra.

### 4.5 Casos especiais

Estes casos valem para todo lugar onde o indicador aparece: tabela, cards, matriz e exportação.

- **Texto:** substitui o número na célula.
- **Texto curto:** é o usado na matriz.
- **Nota:** aparece embaixo do nome do indicador na tabela.

| Indicador | Condição | Texto | Texto curto | Status | Nota | Régua |
|---|---|---|---|---|---|---|
| Liquidez corrente, seca e imediata | `passivoCirculante = 0` | Sem passivo CP | Sem PC | bom | Não há passivo circulante no período | marcador no extremo bom |
| Liquidez geral | `passivoTotal = 0` | Sem passivo | Sem passivo | bom | — | marcador no extremo bom |
| Liquidez seca | numérico e `estoques ÷ ativoCirculante ≥ 50%` | (número) | (número) | (normal) | Estoques são {X}% do ativo circulante (X sem casas) | normal |
| Endividamento geral | `ativoTotal = 0` | — | — | verificar | Sem ativo no período | "Sem dados" |
| Dívida no curto prazo | `passivoTotal = 0` | — | — | verificar | Sem passivo no período | "Sem dados" |
| Dív. líquida / EBITDA 12m | `dividaLiquida ≤ 0` | Caixa líquido | Caixa líq. | bom | Caixa supera a dívida em R$ {abs(dividaLiquida) abreviado} | marcador no extremo bom |
| Dív. líquida / EBITDA 12m | `dividaLiquida > 0` e `ebitda12m ≤ 0` | EBITDA negativo | EBITDA neg. | risco | EBITDA dos últimos 12 meses: R$ {ebitda12m abreviado} | marcador no extremo risco |
| Dív. líquida / EBITDA 12m | numérico e `mesesEbitda12m < 12` | (número) | (número) | (normal) | EBITDA anualizado com {m} mês(es) | normal |
| Dívida bruta / PL | `dividaBruta = 0` | Sem dívida | Sem dívida | bom | — | marcador no extremo bom |
| Dívida bruta / PL | `dividaBruta > 0` e `patrimonioLiquido ≤ 0` | PL negativo | PL neg. | risco | Patrimônio líquido negativo | marcador no extremo risco |
| ROE | `patrimonioLiquido ≤ 0` | — | — | verificar | Patrimônio líquido negativo | "Sem dados" |
| ROA | `ativoTotal = 0` | — | — | verificar | Sem ativo no período | "Sem dados" |
| Margem líquida e bruta | `receitaLiquida = 0` | — | — | verificar | Sem receita no período | "Sem receita" |
| Margem bruta | `custos = 0` e `estoques > 0` | (número) | (número) | **verificar** | Custos zerados no período: conferir o de-para | "Sem custos para comparar" |
| Margem líquida | `resultadoAntesIr > 0` e `lucroLiquido < 0` | (número) | (número) | (normal) | IR e CS maiores que o resultado antes dos impostos | normal |
| EBITDA | sempre | (número) | (número) | (regra 4.4) | — | "Sem faixa definida" |
| Qualquer um | período ou mês sem dados | — | — | verificar | — | "Sem dados" |

Detalhes:

- "Extremo bom" é a posição 1% (escala `menor melhor`) ou 99% (`maior melhor`). "Extremo risco" é o contrário.
- Se um indicador tiver mais de uma nota, mostre só a primeira desta ordem: sem dados, PL negativo, sem receita, custos zerados, os demais.
- Para o **minigráfico**, quando o texto for especial, use `valorGrafico`:
  - Caixa líquido → 0;
  - Sem dívida → 0;
  - EBITDA negativo → nulo;
  - PL negativo → nulo;
  - "—" → nulo.

  Nos demais casos, `valorGrafico = valor`.

### 4.6 Anualização (ROE e ROA)

- `valorAnualizado = valor × 12 ÷ N`, calculado a partir do valor **sem arredondar**.
- O status e a posição na régua usam o valor anualizado.
- **Na célula aparece o valor do período.** Quando N < 12, aparece também uma segunda linha: `≈ {valorAnualizado}% a.a.`.
- Na matriz, cada mês tem N = 1, então o status usa `valor × 12`.

### 4.7 Comparações e variação

- O usuário escolhe a base da comparação no select **Comparar com**: `anterior`, que é o padrão, ou `ano-anterior`. A escolha fica na URL (`?comparar=`).
- As colunas Anterior e Ano anterior aparecem **sempre**. A coluna **Variação**, a linha de comparação dos cards e o painel de explicação do lucro usam a base escolhida.
- Cálculo da variação:
  1. `a = arredondar(atual, casas)` e `b = arredondar(base, casas)`. Para ROE e ROA, use o valor do período, não o anualizado. Para moeda (EBITDA e lucro líquido), arredonde em centavos.
  2. `variação = a − b`.
  3. Unidade da variação:
     - número → absoluta ("−0,03");
     - % → pontos percentuais ("+0,39 p.p.");
     - x → múltiplo ("+0,13x");
     - R$ → moeda ("+3,1 mil").
  4. `melhorou`:
     - em maior melhor: `variação > 0`;
     - em menor melhor: `variação < 0`;
     - `variação = 0` → nulo (neutro).
  5. Variação **nula** ("—") quando atual ou base for nulo, tiver texto especial ou estiver sem dados.
  6. `melhorou = nulo` quando o status atual for `verificar`, mesmo que haja variação.
- **Nunca** mostre variação em % para indicadores que já são percentuais, nem para bases zero ou negativas.

### 4.8 Pontos a verificar

Gere a lista nesta ordem e mostre só os itens que se aplicam:

| Código | Condição (no período atual) | Texto |
|---|---|---|
| `CUSTOS_ZERADOS` | `custos = 0` e `estoques > 0` | Custos zerados com R$ {estoques abreviado} em estoque |
| `DFC_NAO_CLASSIFICADO` | linha "variações não classificadas" da DFC ≠ 0 | R$ {abs(valor) abreviado} na DFC sem classificação |
| `CONTAS_SEM_DE_PARA` | existem contas com movimento fora do de-para (só se o serviço já consegue identificar) | {n} contas com movimento sem classificação |

Se o serviço atual não conseguir identificar contas fora do de-para, **não implemente o terceiro item** e me avise no plano.

### 4.9 Explicação da variação do lucro

- Compara o período atual com a base escolhida (4.7).
- Linhas, nesta ordem:

| Código | Rótulo |
|---|---|
| `receitaLiquida` | Receita líquida |
| `custos` | Custos |
| `despesasOperacionais` | Despesas operacionais |
| `depreciacaoAmortizacao` | Depreciação e amortização |
| `resultadoFinanceiro` | Resultado financeiro |
| `irCs` | IR e CS |
| `outrosResultados` | Outros resultados |

- Omita a linha quando o valor for 0 nos dois períodos. Na prática, Custos e Outros costumam sumir.
- Para cada linha: `valorBase`, `valorAtual`, `variacao = valorAtual − valorBase` e `peso = abs(variacao) ÷ maior abs(variacao) entre as linhas × 100`.
- Linha de total: Lucro líquido base, atual e variação.
- **A soma das variações das linhas tem que ser igual à variação do lucro líquido.** Se a diferença passar de R$ 0,01, registre um log de aviso.

---

## 5. Backend

### 5.1 Série mensal no repositório

- Hoje `ListarSaldosAsync` devolve saldos para um intervalo. Crie no mesmo repositório o método `ListarSaldosMensaisAsync(baseDados, inicio, fim, codColigada, codFilial)`. Ele faz **uma consulta por base**, agrupando o movimento por conta e mês (`GROUP BY` conta, ano, mês), e devolve também o saldo anterior a `inicio`, para dar para acumular o balanço.
- No service, monte em memória, para cada mês:
  - o saldo de fim de mês das contas de balanço (saldo anterior + movimentos acumulados);
  - o movimento do mês das contas de resultado, sem apuração (4.2).
- Intervalo de meses a buscar: de `min(fim − 23, inicio − N, inicio − 12)` até `fim`. Isso cobre a série de 12 meses, o EBITDA 12m de cada mês da série e os dois períodos de comparação.
- Os períodos (atual, anterior e ano anterior) são **agregados a partir dos meses**: saldo do último mês, soma dos resultados.
- Se agrupar por mês não for viável, explique no plano e proponha a alternativa antes de implementar.

### 5.2 Calculadora pura

- Extraia o cálculo dos indicadores para `CalculadoraIndicadores`, uma classe sem acesso a banco. Ela recebe uma `BaseIndicadores` (os campos de 4.2 + `ebitda12m` + `mesesEbitda12m`) e devolve os valores numéricos de cada código. Devolve nulo quando o denominador for 0.
- O `IndicadoresService.CalcularAsync` atual passa a usar essa calculadora. **Os valores da tela atual não podem mudar** (teste de regressão na seção 9).
- **Teste de consistência:** o período atual agregado a partir dos meses tem que bater, ao centavo, com o resultado do `CalcularAsync` para o mesmo filtro. Teste três casos: AEPL mar/26, AGV mar–set/26 e um intervalo que cruze dezembro (out/25–mar/26).

### 5.3 Configuração, avaliador e comparador

Ficam em `Services/BusinessLogic/Indicadores/`, ou no padrão de pastas que o projeto usar.

- **`IndicadoresConfiguracao`:** uma lista estática com tudo da seção 4.3 (código, grupo, ordem, nome, fórmula, explicação, unidade, casas, direção, limites, escala e se o status usa o anualizado). **É o único lugar com número de faixa no sistema.**
- **`AvaliadorIndicadores`:** função pura `(codigo, base, valor, mesesNoPeriodo) → Avaliacao`, com as regras de 4.4, 4.5 e 4.6.
- **`ComparadorIndicadores`:** função pura `(avaliacaoAtual, avaliacaoBase, config) → Variacao`, com a regra de 4.7.
- **`PontosVerificacao`:** gera a lista de 4.8.
- **`ExplicacaoLucro`:** gera as linhas de 4.9.
- **Formatação de textos no backend** (notas e pontos a verificar): use `CultureInfo("pt-BR")` e a regra de moeda abreviada da seção 6.2, com os mesmos exemplos. Arredondamento com `MidpointRounding.AwayFromZero`.

### 5.4 Endpoint `GET /api/indicadores/contador`

**Parâmetros:**
- `codColigada` (obrigatório)
- `codFilial` (opcional)
- `inicio` (AAAA-MM)
- `fim` (AAAA-MM)
- `atualizar` (bool, padrão false; ignora o cache)

**Resposta:**

```json
{
  "empresa": { "codColigada": 17, "codFilial": null, "nome": "AEPL PARTICIPACOES LTDA" },
  "periodo":            { "inicio": "2026-03", "fim": "2026-03", "meses": 1, "rotulo": "Mar/26" },
  "periodoAnterior":    { "inicio": "2026-02", "fim": "2026-02", "rotulo": "Fev/26" },
  "periodoAnoAnterior": { "inicio": "2025-03", "fim": "2025-03", "rotulo": "Mar/25" },
  "dataBalanco": "2026-03-31",
  "primeiroMesComDados": "2018-01",
  "mesesSemDados": [],
  "mesesSerie": ["2025-04", "2025-05", "...", "2026-03"],
  "grupos": [
    {
      "codigo": "liquidez",
      "titulo": "Liquidez",
      "indicadores": [ /* IndicadorLinha */ ]
    }
  ],
  "cards": [ /* CardResumo, 4 itens */ ],
  "pontosVerificar": [ { "codigo": "CUSTOS_ZERADOS", "texto": "Custos zerados com R$ 20,39 mi em estoque" } ],
  "explicacaoLucro": {
    "anterior":    { /* ExplicacaoLucro */ },
    "anoAnterior": { /* ExplicacaoLucro */ }
  },
  "baseCalculo": {
    "atual": { /* Base */ }, "anterior": { /* Base */ }, "anoAnterior": { /* Base */ }
  }
}
```

**`IndicadorLinha`**

```json
{
  "codigo": "LIQ_CORRENTE",
  "nome": "Liquidez corrente",
  "formula": "Ativo circulante ÷ Passivo circulante",
  "explicacao": "Acima de 1: ativos suficientes para cobrir dívidas de até 1 ano.",
  "unidade": "numero",
  "casas": 2,
  "direcao": "maior_melhor",
  "limiteRisco": 1.0,
  "limiteBom": 1.5,
  "escala": { "min": 0, "max": 2 },
  "atual":       { /* Avaliacao */ },
  "anterior":    { /* Avaliacao */ },
  "anoAnterior": { /* Avaliacao */ },
  "variacaoAnterior":    { /* Variacao */ },
  "variacaoAnoAnterior": { /* Variacao */ },
  "meses": [ { "anoMes": "2025-04", "avaliacao": { /* Avaliacao */ } } ]
}
```

- `unidade` aceita `numero`, `percentual`, `multiplo` ou `moeda`.
- `escala` é nula quando não há régua.
- `meses` tem exatamente 12 itens, na ordem de `mesesSerie`.

**`Avaliacao`**

```json
{
  "temDados": true,
  "valor": 1.0249,
  "valorAnualizado": null,
  "valorGrafico": 1.0249,
  "texto": null,
  "textoCurto": null,
  "status": "atencao",
  "nota": null,
  "posicaoRegua": 51.2,
  "motivoSemRegua": null
}
```

- `posicaoRegua` vai de 1 a 99. É nula quando não há régua; nesse caso, `motivoSemRegua` traz o texto da seção 4.5.
- Na posição da régua, ROE e ROA usam o valor anualizado.
- Fórmula: `posicao = clamp((v − min) ÷ (max − min) × 100, 1, 99)`.

**`Variacao`**

```json
{ "valor": -0.03, "unidade": "absoluta", "melhorou": false }
```

- `unidade` aceita `absoluta`, `pp`, `multiplo` ou `moeda`.
- `valor` é nulo quando não for comparável.
- `melhorou` é nulo quando for neutro.

**`CardResumo`:** 4 itens, nesta ordem:
1. `LIQ_CORRENTE` (rótulo "Liquidez corrente")
2. `END_GERAL` ("Endividamento geral")
3. `DIV_LIQ_EBITDA_12M` ("Dív. líquida / EBITDA 12m")
4. `LUCRO_LIQUIDO` ("Lucro líquido do período")

Cada card leva `codigo`, `rotulo`, `unidade`, `casas`, `atual`, `anterior`, `anoAnterior`, `variacaoAnterior` e `variacaoAnoAnterior`.

No card de lucro líquido:
- a unidade é `moeda` e a direção é maior melhor;
- o status é Risco quando `lucroLiquido < 0`. Quando `lucroLiquido ≥ 0`, o status é o da margem líquida, ou Bom se a margem for nula.

**`ExplicacaoLucro`**

```json
{
  "linhas": [
    { "codigo": "receitaLiquida", "rotulo": "Receita líquida", "valorBase": 176300.00, "valorAtual": 187530.96, "variacao": 11230.96, "peso": 14.5 }
  ],
  "lucroBase": 1200.00,
  "lucroAtual": -75670.44,
  "variacaoLucro": -76870.44
}
```

**`Base`:** todos os campos da seção 4.2, mais `ebitda12m`, `mesesEbitda12m`, `despesasPorDiferenca`, `dataBalanco` e `temDados`.

### 5.5 Endpoint `GET /api/indicadores/variacao-contas`

**Parâmetros:** `codColigada`, `codFilial`, `linha` (um dos códigos de 4.9), `inicio`, `fim`, `inicioBase` e `fimBase`.

**Resposta:**

```json
{
  "linha": "despesasOperacionais",
  "rotulo": "Despesas operacionais",
  "totalBase": -95300.00,
  "totalAtual": -103395.23,
  "contas": [
    { "codConta": "3.2.1.01.0001", "descricao": "Salários", "valorBase": -40000.00, "valorAtual": -46500.00, "variacao": -6500.00 }
  ]
}
```

- Use **o mesmo de-para** que classifica as contas nas linhas da DRE. O sinal segue a convenção de contribuição para o lucro.
- Ordene as contas por `abs(variacao)` decrescente. Inclua todas as contas com valor diferente de 0 em pelo menos um dos períodos.
- A soma das contas tem que bater com os totais. Se `despesasPorDiferenca = true` e não bater, devolva também `"diferencaNaoExplicada": valor`.

### 5.6 Lançamentos

**Reaproveite** o endpoint que a tela de Fechamentos já usa para listar os lançamentos de uma conta num mês (`ILancamentosTotvsService`). Não crie um novo.

### 5.7 Endpoint `GET /api/indicadores/contador/exportar`

- Mesmos parâmetros do 5.4, mais `comparar`.
- Devolve um .xlsx gerado com NPOI, no padrão do `DfcExcel.cs`. Nome do arquivo: `indicadores-{nome-da-empresa-simplificado}-{inicio}_{fim}.xlsx`.
- Abas da planilha:
  1. **Indicadores:** colunas Grupo, Indicador, {rótulo atual}, {rótulo anterior}, Variação (contra a base escolhida), {rótulo ano anterior}, Status, Nota, Fórmula. Valores numéricos como número, com formato de célula. Textos especiais como texto.
  2. **Matriz 12 meses:** uma linha por indicador, uma coluna por mês, com o valor ou texto curto. Fundo da célula na cor de fundo do status.
  3. **Base de cálculo:** os itens de balanço e de resultado dos três períodos, lado a lado.

### 5.8 Cache e desempenho

- Guarde no `IMemoryCache` o resultado mensal, por base, coligada, filial e mês, com validade de 10 minutos. `atualizar=true` ignora e renova o cache.
- **Meta:** primeira chamada em menos de 4 s para 24 meses; com cache, em menos de 500 ms. Meça e me informe. Se a primeira chamada passar de 4 s, me avise e proponha dividir em dois endpoints (atual e comparativos).

### 5.9 Testes do backend

Use o padrão do projeto (xUnit, se existir; se não existir, pergunte antes). Os testes devem cobrir:

- `CalculadoraIndicadores`, `AvaliadorIndicadores` e `ComparadorIndicadores` com os casos da seção 9.
- Cada linha da tabela de casos especiais (4.5).
- Os limites exatos: liquidez corrente 1,50 → Bom; 1,00 → Atenção; 0,995 → Atenção; 0,994 → Risco. Endividamento geral 60,00 → Bom; 80,00 → Atenção; 80,01 → Risco.
- A equação do lucro líquido fecha; a soma da explicação do lucro fecha.
- Os rótulos de período (6.2).

---

## 6. Frontend

### 6.1 Estrutura sugerida

Siga a organização de pastas que o projeto já usa. Sugestão:

```
indicadores/
  api.ts                    // useIndicadoresContador, useVariacaoContas, exportar
  tipos.ts                  // tipos do contrato 5.4 e 5.5
  formatacao.ts             // + formatacao.test.ts
  AbasIndicadores.tsx
  contador/
    VisaoContador.tsx
    BarraComparacao.tsx
    AvisoDados.tsx
    PontosVerificar.tsx
    CardsResumo.tsx
    TabelaIndicadores.tsx
    ReguaFaixas.tsx
    Sparkline.tsx
    StatusPill.tsx
    Variacao.tsx
    TooltipFormula.tsx
    PainelVariacaoLucro.tsx
    GavetaContas.tsx
    MatrizMensal.tsx
    BaseCalculo.tsx
  diretoria/
    VisaoDiretoria.tsx      // placeholder nesta tarefa
```

**O front não calcula indicador, status nem variação.** Ele só formata e desenha o que a API devolve.

### 6.2 Formatação (`formatacao.ts`)

Todas as funções em pt-BR, com `Intl.NumberFormat('pt-BR')` e casas fixas. Nulo vira "—" (travessão, U+2014).

| Função | Regra | Exemplos |
|---|---|---|
| `formatarValor(v, 'numero', 2)` | 2 casas | 1,02 · 0,19 |
| `formatarValor(v, 'percentual', 2)` | casas + "%" | 71,79% · −40,35% |
| `formatarValor(v, 'multiplo', 2)` | casas + "x" | 3,52x · 0,47x |
| `formatarValor(v, 'moeda')` | moeda abreviada, sem "R$" | 84,1 mil · −75,7 mil · 3,56 mi · 950 |
| `moedaAbreviada(v)` | \|v\| ≥ 1.000.000 → v/1e6 com 2 casas + " mi"; \|v\| ≥ 1.000 → v/1e3 com 1 casa + " mil"; senão, 0 casas | 20.392.081,09 → 20,39 mi · 359.713,43 → 359,7 mil |
| `moedaCompleta(v)` | 2 casas; negativo entre parênteses | 34.087.932,98 · (75.670,44) |
| `formatarVariacao({valor, unidade}, casas)` | sinal "+" se > 0, "-" se < 0, sem sinal se 0. absoluta: casas; pp: casas + " p.p."; multiplo: casas + "x"; moeda: moeda abreviada | −0,03 · +0,39 p.p. · +0,13x · +3,1 mil · 0,00 |
| `rotuloMes('2026-03')` | mês minúsculo/ano com 2 dígitos | mar/26 |
| `rotuloPeriodo(inicio, fim)` | mesmo mês → "Mar/26"; mesmo ano → "Mar–Set/26"; anos diferentes → "Out/25–Mar/26" | |
| `rotuloPeriodoLongo(inicio, fim)` | "mar/2026" ou "mar/2026 a set/2026" | |

Nas notas e nos pontos a verificar, o backend usa a mesma regra de moeda abreviada, com o prefixo "R$ ". Exemplos: "R$ 20,39 mi" e "R$ 359,7 mil". As implementações em C# e em TypeScript têm que dar o mesmo resultado para os mesmos exemplos.

O sinal de menos é o hífen "-", o mesmo que o `Intl` gera. Nos exemplos deste documento, "−" representa esse hífen. Normalize `-0` para `0` antes de formatar, para nunca aparecer "-0,00".

Use `font-variant-numeric: tabular-nums` em **todos** os números.

### 6.3 Tokens visuais

Crie tokens (CSS variables ou o padrão do projeto) com estes valores.

**Cores gerais**

| Token | Valor | Uso |
|---|---|---|
| texto-principal | #17202B | valores, nomes |
| texto-secundario | #4E5968 | rótulos de campo, textos neutros |
| texto-suave | #5B6573 | cabeçalhos de tabela, subtítulos, notas de rodapé |
| texto-comparacao | #3D4754 | valores de Anterior e Ano anterior |
| icone | #6B7684 | ícone de informação |
| borda-card | #E2E5EA | borda de cards, linha do cabeçalho da tabela |
| borda-linha | #EEF0F3 | separador entre linhas |
| borda-forte | #CBD1D8 | inputs, linha de total |
| trilho | #F0F2F5 | trilho de barras |
| celula-vazia | #F7F8FA | célula da matriz sem dado |
| primaria | **verde do sistema** | abas, títulos de grupo, títulos de seção, links |

**Status**

| Status | Texto (pílula, célula) | Fundo (pílula, célula) | Segmento da régua |
|---|---|---|---|
| Bom | #1B6B43 | #E3F2E9 | #B5DCC4 |
| Atenção | #7A4A00 | #FCEFD6 | #F1D08E |
| Risco | #A12A24 | #FBE4E2 | #ECB0AA |
| Verificar | #4E5968 | #EEF0F3 | #DDE1E6 |

**Variação e barras**

| Uso | Cor |
|---|---|
| Variação que melhorou | #1B6B43 |
| Variação que piorou | #A12A24 |
| Variação neutra | #4E5968 |
| Barra de peso que aumenta o lucro | #7CC29A |
| Barra de peso que reduz o lucro | #E08A82 |
| Linha do minigráfico | #4E5968 |
| Marcador da régua | #17202B |

**Faixas de aviso**

| Uso | Fundo | Borda | Título | Texto |
|---|---|---|---|---|
| Pontos a verificar | #FFF8EB | #F0D9A8 | #7A4A00 | #4A3B1C |
| Aviso de dados | #EEF3FA | #C9D8EC | #1D3F6E | #1D3F6E |

**Cantos:** card 12px · faixa de pontos 10px · input, botão e aviso 8px · pílula 6px · célula da matriz 4px · trilho da régua 4px.

**Tipografia:**

| Uso | Tamanho e peso |
|---|---|
| Título de seção | 18px, 600 |
| Corpo da tabela | 14px |
| Cabeçalho de tabela | 12px, 500 |
| Notas | 12px |
| Linha "a.a." | 11px |
| Valor do card | 30px, 600, letter-spacing −0,01em |

Nunca use texto menor que 11px.

**Espaçamento:** 24px entre as seções da aba.

### 6.4 Abas

- Ficam logo abaixo da linha de chips de filtro (Empresa, Filial, Período e Atualizar), que valem para as duas abas.
- Uma barra com `border-bottom: 1px solid borda-card` e dois links-aba, com 4px de espaço entre eles:
  - cada aba tem `min-height: 44px`, `padding: 0 16px`, 15px de fonte e `border-bottom: 2px solid transparent`, com `margin-bottom: -1px`;
  - a aba ativa tem peso 600, cor primária, borda inferior de 2px na cor primária e `aria-current="page"`;
  - a aba inativa tem cor `texto-secundario`.
- Textos: **Visão do contador** | **Visão da diretoria**.
- A aba ativa fica na URL: `?visao=contador` (padrão) ou `?visao=diretoria`.
- Por enquanto, a aba da diretoria mostra um card com o texto "A visão da diretoria será construída na próxima etapa."

### 6.5 Cabeçalho da aba e barra "Comparar com"

- **No topo:** o título "Índices financeiros" e o subtítulo atual, mantidos como estão (exemplo: "AGV CAMPINAS EMPREENDIMENTOS LTDA · Coligada 1 · balanço em 30/09/2026 · resultado de 03/2026 a 09/2026").
- **Abaixo:** uma linha flex com `wrap`, `gap: 16px` e `align-items: flex-end`:
  - **Campo "Comparar com":**
    - rótulo de 12px, peso 500, cor `texto-secundario`, 6px acima do select;
    - select com 44px de altura, `min-width: 250px`, `padding: 0 12px`, borda de 1px `borda-forte`, cantos de 8px, fundo branco e fonte de 14px;
    - opções "Período anterior ({rótulo})" e "Mesmo período do ano anterior ({rótulo})". Exemplo: "Período anterior (Fev/26)".
  - um espaçador flexível;
  - **Botão "Exportar"** (5.7): 44px de altura, `padding: 0 18px`, borda de 1px `borda-forte`, cantos de 8px, fundo branco, 14px, peso 500. Enquanto gera, mostra "Exportando…" e fica desabilitado.
- Trocar a opção do select **não refaz a chamada**. Só muda qual variação é exibida.

### 6.6 Aviso de dados faltantes

- Só aparece quando `mesesSemDados` não está vazio.
- Caixa flex com `gap: 10px`, `padding: 12px 16px`, cores de "Aviso de dados", borda de 1px, cantos de 8px e fonte de 13px com `line-height: 1.5`.
- Conteúdo: "**Dados**" (peso 600) seguido do texto "Não há dados contábeis antes de {rotuloMes(primeiroMesComDados)}. Comparações e meses sem dados aparecem como —."

### 6.7 Pontos a verificar

- Só aparece quando `pontosVerificar` não está vazio. Usa `<section aria-label="Pontos a verificar">`.
- Caixa flex com `wrap`, `gap: 10px 28px`, `align-items: center`, `padding: 14px 18px`, cores de "Pontos a verificar", borda de 1px, cantos de 10px e fonte de 14px.
- Conteúdo, em linha:
  1. o título, em peso 600 e na cor de título: "{n} pontos a verificar antes de fechar o mês". Com um item só: "1 ponto a verificar antes de fechar o mês";
  2. cada `texto`, como um `<span>`;
  3. o link "Revisar em Contas contábeis", com `margin-left: auto`, peso 500 e cor primária, que leva para a tela Contas contábeis existente.

### 6.8 Cards de resumo

- Grade com `grid-template-columns: repeat(auto-fit, minmax(210px, 1fr))` e `gap: 16px`.
- Cada card:
  - fundo branco, borda de 1px `borda-card`, cantos de 12px, `padding: 18px 20px`;
  - flex em coluna com `gap: 8px` e `min-width: 0`.
- Conteúdo, de cima para baixo:
  1. Uma linha com `justify-content: space-between`:
     - à esquerda, o rótulo em 13px, peso 500, `texto-secundario`;
     - à direita, a pílula de status (6.12).
  2. O valor: 30px, peso 600, letter-spacing −0,01em, `texto-principal`. Se houver `texto` especial, ele aparece no lugar do número, no mesmo tamanho.
  3. A linha de comparação, em 13px `texto-suave`: "{rótulo da base}: {valor da base}" seguido de um espaço e da variação, em peso 600 e na cor da variação (6.12). Exemplo: "Fev/26: 1,05 −0,03".

### 6.9 Tabela de indicadores

Esta é a peça principal. Siga exatamente o recorte `tabela-indicadores.png`.

**Contêiner**
- Fundo branco, borda de 1px `borda-card`, cantos de 12px, `padding: 8px 24px 18px`. Usa `<section aria-label="Indicadores por grupo">`.
- Dentro dele, uma caixa com `overflow-x: auto` e, dentro dela, o conteúdo com `min-width: 1040px`.

**Grade (vale para o cabeçalho e para todas as linhas)**
- `display: grid`
- `grid-template-columns: minmax(230px, 2fr) 150px 92px 92px 104px 92px 104px 92px`
- `gap: 12px`
- `align-items: center`

**Cabeçalho**
- `padding: 14px 0 12px`, fonte de 12px, peso 500, cor `texto-suave`, `border-bottom: 1px solid borda-card`.
- Colunas:

| # | Texto | Alinhamento |
|---|---|---|
| 1 | Indicador | esquerda |
| 2 | Faixa | esquerda |
| 3 | {periodo.rotulo} | direita |
| 4 | {periodoAnterior.rotulo} | direita |
| 5 | Variação | direita |
| 6 | {periodoAnoAnterior.rotulo} | direita |
| 7 | 12 meses | direita |
| 8 | Status | esquerda |

- O cabeçalho da coluna que é a base da comparação (4 ou 6) fica em `texto-principal`, peso 600.
- Os cabeçalhos das colunas 3, 4 e 6 têm um tooltip com o período longo.
- Rótulos longos (exemplo: "Out/25–Mar/26") podem quebrar em duas linhas.

**Título de grupo**
- Linha inteira, `padding: 22px 0 6px`, 13px, peso 600, cor primária, letter-spacing 0,02em.
- Textos: "Liquidez", "Endividamento", "Rentabilidade e resultado · no período".

**Linha de indicador**
- `padding: 12px 0`, `border-top: 1px solid borda-linha`, fonte de 14px.
- Colunas:

| # | Conteúdo |
|---|---|
| 1 | Flex em coluna com `gap: 2px` e `min-width: 0`. **Primeira linha:** flex com `gap: 6px`, o nome em peso 500 e o ícone de informação (6.13). **Segunda linha**, só se houver `atual.nota`: a nota em 12px, cor #7A4A00 |
| 2 | Régua de faixas (6.10) ou, se `posicaoRegua` for nulo, o `motivoSemRegua` em 12px `texto-suave` |
| 3 | Valor atual alinhado à direita, peso 600, `texto-principal`. Com texto especial: o texto em 13px, peso 600, podendo quebrar. Para ROE e ROA, quando `valorAnualizado` não for nulo, uma segunda linha em 11px `texto-suave`: "≈ {valorAnualizado formatado} a.a." |
| 4 | Valor anterior, à direita, `texto-comparacao` (texto especial em 12px) |
| 5 | Variação contra a base escolhida (6.12), à direita, peso 500 |
| 6 | Valor do ano anterior, à direita, `texto-comparacao` |
| 7 | Minigráfico (6.11), alinhado à direita |
| 8 | Pílula de status (6.12) do período atual |

**Rodapé da tabela**
- Flex com `wrap`, `gap: 8px 20px`, `padding-top: 16px`, 12px, `texto-suave`.
- Dois textos: "Dív. líquida / EBITDA usa o EBITDA dos últimos 12 meses." e "Passe o mouse no ícone ao lado do nome para ver a fórmula."

### 6.10 Régua de faixas (`ReguaFaixas`)

- **Tamanho:** 150px de largura (a coluna inteira) × 16px de altura, `position: relative`.
- **Trilho:** `position: absolute`, `left: 0`, `right: 0`, `top: 4px`, 8px de altura, cantos de 4px, `overflow: hidden`, `display: flex`.
- **Segmentos:** 3 `div`s com 8px de altura, sem espaço entre eles, larguras em % da escala.
  - Em maior melhor: `[risco: (limiteRisco − min) ÷ (max − min), atencao: (limiteBom − limiteRisco) ÷ (max − min), bom: (max − limiteBom) ÷ (max − min)]`.
  - Em menor melhor: `[bom: (limiteBom − min) ÷ (max − min), atencao: (limiteRisco − limiteBom) ÷ (max − min), risco: (max − limiteRisco) ÷ (max − min)]`.
  - Cada segmento na cor "Segmento da régua" do seu status.
- **Marcador:** `position: absolute`, `top: 0`, `left: {posicaoRegua}%`, 3px × 16px, `margin-left: -1px`, cantos de 2px, fundo #17202B.
- **Acessibilidade:** `role="img"` e `aria-label="{nome}: {valor formatado}, {status}. Bom {≥ ou ≤} {limiteBom}, risco {< ou >} {limiteRisco}."`.
- A régua sempre usa o período **atual**.

Conferência com o esboço: liquidez imediata 0,04 na escala de 0 a 0,5 deixa o marcador em 8%. Dívida no curto prazo 99,52% deixa o marcador em 99%.

### 6.11 Minigráfico (`Sparkline`)

- SVG com `width="96"`, `height="24"`, `viewBox="0 0 96 24"` e `aria-hidden="true"`.
- Uma `<polyline>` sem preenchimento, `stroke: #4E5968`, `stroke-width: 1.5`, `stroke-linejoin: round` e `stroke-linecap: round`.
- Pontos: os 12 `meses[i].avaliacao.valorGrafico`.
  - `x = i × 96 ÷ 11`.
  - `y = 22 − (v − mín) ÷ (máx − mín) × 20`, onde mín e máx são os da própria série, ignorando nulos.
  - Se máx = mín, a linha fica reta em `y = 12`.
- **Nulos quebram a linha:** desenhe uma `<polyline>` para cada trecho contínuo, com pelo menos 2 pontos.
- Com menos de 2 pontos numéricos no total, mostre "—" na cor #6B7684.

### 6.12 Pílula de status e variação

**`StatusPill`**
- `display: inline-block`, 12px, peso 500, `padding: 3px 8px`, cantos de 6px.
- Fundo e texto nas cores do status.
- Textos: **Bom**, **Atenção**, **Risco**, **Verificar**.
- O status **sempre aparece com texto**, nunca só com cor.

**`Variacao`**
- Texto de `formatarVariacao`.
- Cor:
  - `melhorou = true` → #1B6B43;
  - `false` → #A12A24;
  - nulo → #4E5968.
- Valor nulo → "—" em #4E5968.
- Tooltip: "Contra {rótulo da base}".

### 6.13 Tooltip da fórmula (`TooltipFormula`)

- Ícone de informação em SVG de traço, 16px, cor #6B7684, `cursor: help`, dentro de um `<button type="button">` sem estilo de botão e com `aria-label="Fórmula de {nome}"`.
- O tooltip abre no **hover e no foco do teclado**, e fecha com Esc. Use o componente de tooltip do projeto, se existir.
- Conteúdo:
  - **Linha 1:** a fórmula, em peso 500.
  - **Linha 2:** a explicação.
  - **Linha 3**, se houver faixas: "Bom: {≥ ou ≤} {limiteBom} · Atenção: entre {…} e {…} · Risco: {< ou >} {limiteRisco}", formatados na unidade do indicador.

### 6.14 Painel "O que explica a variação do lucro"

**Contêiner**
- Fundo branco, borda de 1px `borda-card`, cantos de 12px, `padding: 22px 24px`, flex em coluna com `gap: 14px`.
- Usa `<section id="variacao" aria-label="O que explica a variação do lucro">`.

**Cabeçalho:** flex com `wrap`, `gap: 10px` e `align-items: center`:
- `<h2>` "O que explica a variação do lucro", 18px, peso 600;
- subtítulo em 13px `texto-suave`: "Lucro líquido · {rótulo da base} → {rótulo atual} · R$ mil".

**Tabela**
- Caixa com `overflow-x: auto` e conteúdo com `min-width: 840px`.
- Grade: `grid-template-columns: minmax(220px, 1.6fr) 96px 96px 104px minmax(160px, 1fr) 96px`, com `gap: 12px` e `align-items: center`.
- **Cabeçalho:** `padding: 0 0 10px`, 12px, peso 500, `texto-suave`, `border-bottom: 1px solid borda-card`. Colunas: "Linha da DRE" | {rótulo da base} (à direita) | {rótulo atual} (à direita) | "Variação" (à direita) | "Peso na variação" | (vazio).
- **Linhas** (as de 4.9, na ordem): `min-height: 44px`, `border-top: 1px solid borda-linha`, 14px.
  1. o rótulo;
  2. o valor da base, em mil com 1 casa, à direita, `texto-comparacao`;
  3. o valor atual, em mil com 1 casa, à direita, peso 500;
  4. a variação, em mil com 1 casa e sinal, à direita, peso 600: verde (#1B6B43) se ≥ 0 e vermelha (#A12A24) se < 0;
  5. a barra de peso: um trilho de 8px de altura, fundo `trilho`, cantos de 4px, `position: relative`. Dentro, o preenchimento com 8px de altura, cantos de 4px, largura `{peso}%` e cor #7CC29A se a variação for ≥ 0 ou #E08A82 se for < 0;
  6. o botão "Ver contas", com aparência de link (sem fundo nem borda, 13px, peso 500, cor primária), alinhado à direita, que abre a gaveta (6.15).
- **Linha de total:** `min-height: 48px`, `border-top: 1px solid borda-forte`, peso 600. Conteúdo: "Lucro líquido", o lucro da base, o lucro atual (em #A12A24 se negativo) e a variação colorida. As colunas 5 e 6 ficam vazias.
- **Texto abaixo**, em 13px `texto-secundario`: "Cada linha abre as contas que a compõem e, dentro de cada conta, as partidas do mês."
- Os valores em "mil" usam `v ÷ 1000` com 1 casa: 187.530,96 → "187,5"; −75.670,44 → "−75,7".

### 6.15 Gaveta de contas e lançamentos (`GavetaContas`)

**Abertura:** abre ao clicar em "Ver contas". Use o Drawer do projeto, se existir.

**Estrutura**
- Painel à direita com 560px de largura (100% em telas menores que 640px), sobre um fundo escurecido.
- O foco vai para o título ao abrir e volta para o botão "Ver contas" ao fechar. Esc e clique no fundo escurecido fecham.
- **Cabeçalho:**
  - título "{rótulo da linha} · contas", 18px, peso 600;
  - subtítulo em 13px `texto-suave`: "{rótulo da base} → {rótulo atual} · R$";
  - botão de fechar, com `aria-label="Fechar"`.
- **Corpo:**
  - lista vinda de `/api/indicadores/variacao-contas`, com colunas Conta (código em 12px `texto-suave` acima da descrição em 14px) | Base | Atual | Variação (colorida);
  - valores com `moedaCompleta`;
  - ordem por `abs(variacao)` decrescente;
  - se houver `diferencaNaoExplicada`, uma última linha "Diferença não explicada" em itálico.
- **Ao clicar numa conta**, ela expande e mostra os lançamentos do mês pelo endpoint existente (5.6). Colunas: Data | Histórico | Débito | Crédito.
  - Se o período tiver mais de um mês, aparece um select de mês dentro da gaveta, começando no último mês do período.
  - Junto aparece o link "Abrir no fechamento", que leva para a tela de Fechamentos com a empresa, a conta e o mês já preenchidos, se a rota aceitar esses parâmetros.
- **Estados:** carregando (esqueleto com 5 linhas), erro ("Não foi possível carregar as contas." + botão "Tentar de novo") e vazio ("Nenhuma conta com movimento nesta linha.").

### 6.16 Matriz dos últimos 12 meses (`MatrizMensal`)

**Contêiner:** igual ao do 6.14, com `aria-label="Matriz dos últimos 12 meses"`.

**Cabeçalho:** `<h2>` "Matriz dos últimos 12 meses" + subtítulo em 13px `texto-suave`: "Cada célula na cor do status daquele mês".

**Grade**
- Caixa com `overflow-x: auto` e conteúdo com `min-width: 1040px`, flex em coluna com `gap: 4px`.
- Linha de cabeçalho: `grid-template-columns: minmax(190px, 1.6fr) repeat(12, minmax(66px, 1fr))` e `gap: 4px`.
  - Primeira célula: "Indicador".
  - Depois, os 12 `rotuloMes` centralizados, em 12px, peso 500, `texto-suave`. O último mês fica em `texto-principal`, peso 600.
- **Uma linha por indicador**, na ordem da tabela, sem títulos de grupo:
  - primeira célula: o nome em 13px `texto-principal`. Para o EBITDA, "EBITDA (R$ mil)";
  - depois, 12 células com `padding: 8px 4px`, cantos de 4px, texto centralizado em 12px e peso 500, fundo e texto nas cores do status daquele mês;
  - conteúdo da célula: o valor formatado ou o `textoCurto`. O EBITDA aparece em mil com 1 casa, sem "mil";
  - mês sem dados: "—", fundo #F7F8FA, texto #6B7684.
- Cada célula tem `title` com "{nome} · {rotuloMes}: {valor} · {status}".

**Legenda:** flex com `wrap`, `gap: 8px 16px`, 12px, `texto-secundario`. Contém as 4 pílulas (Bom, Atenção, Risco, Verificar) e o texto "Faixas definidas pela contabilidade".

### 6.17 Base de cálculo (`BaseCalculo`)

**Contêiner:** fundo branco, borda de 1px `borda-card`, cantos de 12px, `padding: 12px 24px`, flex em coluna com `gap: 12px`.

**Cabeçalho clicável**
- `<button type="button">` com largura total, `min-height: 48px`, sem fundo e sem borda, `justify-content: space-between`, `aria-expanded`.
- À esquerda:
  - "Base de cálculo", 18px, peso 600;
  - abaixo, "Valores usados nas fórmulas, em R$", 13px `texto-suave`.
- À direita: "Mostrar" ou "Ocultar", 14px, peso 500, cor primária.
- **Começa recolhido.** O estado aberto ou fechado fica salvo no `localStorage` (com try/catch).

**Conteúdo aberto**
- Grade com `grid-template-columns: repeat(auto-fit, minmax(min(100%, 380px), 1fr))`, `gap: 32px`, `padding: 4px 0 14px` e fonte de 14px.
- Cada item é uma linha flex com `justify-content: space-between`, `gap: 12px`, `padding: 8px 0` e `border-top: 1px solid borda-linha`. O valor fica em peso 500, com `moedaCompleta`.
- O título de cada coluna fica em 13px, peso 600, cor primária, com `padding-bottom: 6px`.

**Coluna "Balanço em {dataBalanco dd/mm/aaaa}":**
1. Ativo total
2. Ativo circulante
3. Disponível (caixa e equivalentes)
4. Estoques
5. Realizável a longo prazo
6. Passivo circulante
7. Passivo não circulante
8. Patrimônio líquido
9. Dívida bruta (empréstimos e financiamentos)
10. Dívida líquida

**Coluna "Resultado de {rotuloPeriodoLongo}":**
1. Receita líquida
2. (−) Custos. Com 0 e estoques > 0, mostre "—" e a etiqueta "verificar" em 12px #7A4A00 ao lado do rótulo
3. Lucro bruto
4. (−) Despesas operacionais, exceto D&A. Se `despesasPorDiferenca`, a etiqueta "por diferença" em 12px `texto-suave`
5. EBITDA
6. (−) Depreciação e amortização
7. EBIT (resultado operacional)
8. (+/−) Resultado financeiro
9. (−) IR e CS
10. Lucro líquido, em peso 600, na cor #A12A24 se negativo
11. EBITDA dos últimos 12 meses, com a etiqueta "base do Dív. líquida / EBITDA" e, se `mesesEbitda12m < 12`, "({m} meses)"

### 6.18 DFC

Mantenha a DFC atual logo abaixo, sem nenhuma mudança.

### 6.19 Carregamento, erro e vazio

- Use TanStack Query, com a chave `['indicadores-contador', codColigada, codFilial, inicio, fim]`.
- O botão **Atualizar** existente refaz a consulta com `atualizar=true`.
- **Carregando:** esqueletos com as mesmas medidas finais: 4 cards, a tabela com 13 linhas (barras cinza `trilho` no lugar dos números), o painel e a matriz. A página não pode "pular" quando os dados chegam.
- **Erro:** no lugar das seções 2 a 9, um card com "Não foi possível carregar os indicadores." e o botão "Tentar de novo". A DFC continua independente.
- **Empresa sem nenhum dado no período:** o card "Não há lançamentos contábeis para {empresa} em {período}."

### 6.20 Responsividade e acessibilidade

- Até 360px de largura não pode haver rolagem horizontal na página. Só as tabelas rolam, dentro das suas caixas. Cards e barra de filtros quebram linha.
- Toda a navegação funciona pelo teclado: abas, select, tooltips, links, gaveta e botão da base de cálculo.
- Contraste mínimo de 4,5:1 nos textos. As cores definidas aqui já atendem.
- Nenhuma informação depende só de cor: status com texto e variação com sinal.

---

## 7. Etapas de entrega

Pare ao fim de cada etapa (regra 2.7).

| # | Etapa | Entregas |
|---|---|---|
| 1 | Investigação e plano | Plano da regra 2.1. **Nenhuma edição** |
| 2 | Backend: núcleo | `ListarSaldosMensaisAsync`, montagem mensal, `CalculadoraIndicadores` extraída e usada pelo `CalcularAsync`, testes de regressão e de consistência (5.2) |
| 3 | Backend: avaliação e endpoint | `IndicadoresConfiguracao`, `AvaliadorIndicadores`, `ComparadorIndicadores`, pontos a verificar, explicação do lucro, endpoint 5.4, cache, testes (5.9), tempo de resposta medido |
| 4 | Front: base | Abas e URL, tipos, hook, `formatacao.ts` com testes, esqueletos, placeholder da diretoria |
| 5 | Front: topo | Cabeçalho da aba, barra "Comparar com", aviso de dados, pontos a verificar, cards de resumo |
| 6 | Front: tabela | Tabela completa com régua, minigráfico, pílulas, variação, notas, linha "a.a." e tooltip |
| 7 | Variação do lucro | Painel 6.14, endpoint 5.5, gaveta com contas e lançamentos |
| 8 | Matriz e base | Matriz 6.16, base de cálculo 6.17, estados 6.19, responsividade e acessibilidade 6.20 |
| 9 | Exportação | Endpoint 5.7 e botão "Exportar" |
| 10 | Revisão final | Percorrer os critérios de aceite da seção 8 e me mostrar o resultado de cada um |

---

## 8. Critérios de aceite

- [ ] As duas abas existem, a aba ativa fica na URL e os filtros globais valem para as duas.
- [ ] A ordem das seções da aba do contador é exatamente a da seção 3.
- [ ] A tabela tem as 8 colunas com as larguras da seção 6.9, os 3 grupos e os 13 indicadores na ordem e com os nomes da seção 4.3. O PCT não aparece.
- [ ] Régua, minigráfico, pílulas e variação seguem as medidas e cores das seções 6.10 a 6.12.
- [ ] Os status batem com as faixas da seção 4.3 e com os limites testados na seção 5.9.
- [ ] Todos os casos especiais da seção 4.5 aparecem corretamente na tabela, nos cards e na matriz.
- [ ] ROE e ROA mostram o valor do período e "≈ X% a.a." quando N < 12. O status usa o anualizado.
- [ ] Variações de percentuais aparecem em p.p., e a cor indica se melhorou ou piorou, conforme a direção.
- [ ] Trocar o "Comparar com" muda a coluna Variação, a linha dos cards e o painel do lucro sem nova chamada à API, e destaca o cabeçalho da coluna base.
- [ ] As colunas Anterior e Ano anterior, os minigráficos e a matriz mostram **dados reais**. Meses sem dados aparecem como "—", com o aviso.
- [ ] Na explicação do lucro, a soma das variações das linhas é igual à variação do lucro.
- [ ] A gaveta lista as contas da linha, com totais que batem, e abre os lançamentos do mês.
- [ ] A exportação gera o .xlsx com as 3 abas.
- [ ] Os valores atuais da tela continuam iguais aos de antes (regressão da seção 9).
- [ ] O tempo de resposta foi medido e informado.
- [ ] Build, lint e testes passam no back e no front.
- [ ] Não há dependência nova, não há dado ilustrativo e não há número de faixa fora de `IndicadoresConfiguracao`.

---

## 9. Casos de teste

### 9.1 AEPL Participações · coligada 17 · mar/2026 (N = 1)

**Base**, como entrada de teste unitário, considerando que só o mês de março tem dados no EBITDA 12m:

| Campo | Valor |
|---|---|
| ativoTotal | 34.087.932,98 |
| ativoCirculante | 24.960.794,97 |
| disponivel | 984.563,27 |
| estoques | 20.392.081,09 |
| realizavelLongoPrazo | 4.787.803,87 |
| passivoCirculante | 24.355.366,24 |
| passivoNaoCirculante | 116.549,27 |
| patrimonioLiquido | 9.616.017,47 |
| dividaBruta | 4.541.150,68 |
| dividaLiquida | 3.556.587,41 |
| receitaLiquida | 187.530,96 |
| custos | 0,00 |
| lucroBruto | 187.530,96 |
| despesasOperacionais | −103.395,23 |
| ebitda | 84.135,73 |
| depreciacaoAmortizacao | −67.633,78 |
| ebit | 16.501,95 |
| resultadoFinanceiro | −14.880,20 |
| resultadoAntesIr | 1.621,75 |
| irCs | −77.292,19 |
| outrosResultados | 0,00 |
| lucroLiquido | −75.670,44 |
| ebitda12m / mesesEbitda12m | 84.135,73 / 1 |
| DFC: variações não classificadas | −359.713,43 |

**Avaliações esperadas:**

| Indicador | Valor exibido | Status | Nota |
|---|---|---|---|
| Liquidez corrente | 1,02 | Atenção | — |
| Liquidez seca | 0,19 | Risco | Estoques são 82% do ativo circulante |
| Liquidez imediata | 0,04 | Risco | — |
| Liquidez geral | 1,22 | Bom | — |
| Endividamento geral | 71,79% | Atenção | — |
| Dívida no curto prazo | 99,52% | Risco | — |
| Dív. líquida / EBITDA 12m | 3,52x | Atenção | EBITDA anualizado com 1 mês |
| Dívida bruta / PL | 0,47x | Bom | — |
| ROE | −0,79% e "≈ −9,44% a.a." | Risco | — |
| ROA | −0,22% e "≈ −2,66% a.a." | Risco | — |
| Margem líquida | −40,35% | Risco | IR e CS maiores que o resultado antes dos impostos |
| Margem bruta | 100,00% | **Verificar** | Custos zerados no período: conferir o de-para (faixa: "Sem custos para comparar") |
| EBITDA | 84,1 mil | Bom | (faixa: "Sem faixa definida") |

**Cards esperados:** Liquidez corrente 1,02 (Atenção) · Endividamento geral 71,79% (Atenção) · Dív. líquida / EBITDA 12m 3,52x (Atenção) · Lucro líquido do período −75,7 mil (Risco).

**Pontos a verificar esperados**, com o título "2 pontos a verificar antes de fechar o mês":
- "Custos zerados com R$ 20,39 mi em estoque";
- "R$ 359,7 mil na DFC sem classificação".

**Explicação do lucro, valores atuais esperados (R$ mil):** Receita líquida 187,5 · Despesas operacionais −103,4 · Depreciação e amortização −67,6 · Resultado financeiro −14,9 · IR e CS −77,3 · Lucro líquido −75,7. A linha Custos some se também for 0 na base.

**Posições da régua esperadas:** Liquidez corrente 51,2% · Liquidez imediata 8,1% · Dívida no curto prazo 99% (limite superior) · Dívida bruta / PL 15,7%.

Na tela real, o Dív. líquida / EBITDA 12m vai usar o EBITDA real dos últimos 12 meses e pode ser diferente de 3,52x. Isso é esperado.

### 9.2 AGV Campinas Empreendimentos · coligada 1 · filial 1 · mar–set/2026 (N = 7): regressão

Estes são os valores que a tela atual mostra hoje. **Os valores numéricos atuais têm que continuar iguais.** Status e textos esperados com as regras novas:

| Indicador | Valor hoje | Exibição esperada | Status esperado |
|---|---|---|---|
| Liquidez corrente | 2,44 | 2,44 | Bom |
| Liquidez seca | 1,24 | 1,24 | Bom |
| Liquidez imediata | 0,17 | 0,17 | Atenção |
| Liquidez geral | 2,35 | 2,35 | Bom |
| Endividamento geral | 42,03% | 42,03% | Bom |
| Dívida no curto prazo | 61,60% | 61,60% | Atenção |
| Dív. líquida / EBITDA | −0,50x | **Caixa líquido**, com nota "Caixa supera a dívida em R$ …" | Bom |
| Dívida bruta / PL | 0,00x | "Sem dívida" se a dívida bruta for 0; senão, "0,00x" | Bom |
| PCT | 42,03% | **não aparece** | — |
| ROE | 14,58% | 14,58% e "≈ 24,99% a.a." (14,58 × 12 ÷ 7) | Bom |
| ROA | 8,45% | 8,45% e "≈ 14,49% a.a." | Bom |
| Margem líquida | 40,57% | 40,57% | Bom |
| Margem bruta | 94,59% | 94,59% | Bom |

O valor anualizado sai do número sem arredondar. Pode haver diferença de 0,01 no "a.a.".

### 9.3 Consistência série × cálculo atual

Para cada filtro abaixo, `periodos.atual` do endpoint novo tem que ser igual, ao centavo, ao resultado do `CalcularAsync` atual:
1. AEPL mar/26
2. AGV mar–set/26
3. Um intervalo que cruze dezembro, por exemplo AGV out/25–mar/26. Este caso testa a exclusão da apuração.

### 9.4 Rótulos

- `rotuloPeriodo('2026-03', '2026-03')` = "Mar/26"
- `rotuloPeriodo('2026-03', '2026-09')` = "Mar–Set/26"
- `rotuloPeriodo('2025-10', '2026-03')` = "Out/25–Mar/26"
- Período anterior de mar–set/26 = ago/25–fev/26 ("Ago/25–Fev/26")
- Ano anterior de mar–set/26 = mar–set/25 ("Mar–Set/25")

---

## Apêndice A. Código de referência do desenho (React + TypeScript + CSS)

Este é o código de **como desenhar** cada parte da aba. Use como base e adapte imports, nomes de hooks, roteador e padrão de estilo do projeto (CSS Modules, Tailwind etc.), **sem mudar medidas, cores, textos e comportamento**. O arquivo `referencia-visao-contador.html` (Apêndice B) desenha exatamente a mesma coisa em HTML e JavaScript puro, para abrir no navegador e comparar.

Regras que valem para todo o código abaixo:
- O front **não calcula** status, variação, posição da régua nem casos especiais. Tudo vem pronto da API (seção 5.4).
- Os gráficos são **SVG e CSS puros**, sem biblioteca.
- `.ind-raiz` carrega os tokens de cor. Ela vai no wrapper das abas **e** nos elementos renderizados no `body` (tooltip e gaveta), porque eles ficam fora da árvore.
- Troque o valor de `--ind-primaria` pelo token verde do sistema.

### A.1 `indicadores.css`

```css
/* ==========================================================================
   Indicadores · Visão do contador
   - .ind-raiz  → tokens (cores). Vai no wrapper das abas E nos portais (tooltip, gaveta).
   - .ind       → pilha vertical com 24px entre as seções.
   Troque o valor de --ind-primaria pelo token verde do sistema.
   ========================================================================== */

.ind-raiz {
  --ind-primaria: var(--cor-primaria, #0E5A45);
  --ind-texto: #17202B;
  --ind-texto-2: #4E5968;
  --ind-suave: #5B6573;
  --ind-comp: #3D4754;
  --ind-icone: #6B7684;
  --ind-borda: #E2E5EA;
  --ind-linha: #EEF0F3;
  --ind-borda-forte: #CBD1D8;
  --ind-trilho: #F0F2F5;
  --ind-vazio: #F7F8FA;

  --bom-fg: #1B6B43;      --bom-bg: #E3F2E9;      --bom-seg: #B5DCC4;
  --atencao-fg: #7A4A00;  --atencao-bg: #FCEFD6;  --atencao-seg: #F1D08E;
  --risco-fg: #A12A24;    --risco-bg: #FBE4E2;    --risco-seg: #ECB0AA;
  --verificar-fg: #4E5968; --verificar-bg: #EEF0F3; --verificar-seg: #DDE1E6;

  --melhorou: #1B6B43;
  --piorou: #A12A24;
  --neutro: #4E5968;
  --peso-mais: #7CC29A;
  --peso-menos: #E08A82;

  color: var(--ind-texto);
  font-variant-numeric: tabular-nums;
}
.ind-raiz *, .ind-raiz *::before, .ind-raiz *::after { box-sizing: border-box; }
.ind-raiz :focus-visible { outline: 2px solid var(--ind-primaria); outline-offset: 2px; }
.ind-raiz [hidden] { display: none !important; }

.ind { display: flex; flex-direction: column; gap: 24px; }

/* ---------- Utilitários ---------- */
.ind-card { background: #FFFFFF; border: 1px solid var(--ind-borda); border-radius: 12px; }
.ind-titulo-secao { margin: 0; font-size: 18px; font-weight: 600; color: var(--ind-texto); }
.ind-subtitulo { font-size: 13px; color: var(--ind-suave); }
.ind-texto-2 { font-size: 13px; color: var(--ind-texto-2); }
.ind-rolagem { overflow-x: auto; }
.ind-dir { text-align: right; }
.ind-forte { font-weight: 600; color: var(--ind-texto); }
.ind-comp { color: var(--ind-comp); }
.ind-forte-var { font-weight: 600; }
.ind-link {
  padding: 0; border: 0; background: none; font: inherit; font-size: 13px; font-weight: 500;
  color: var(--ind-primaria); text-decoration: none; cursor: pointer;
}
.ind-link:hover { text-decoration: underline; }
.ind-link.ind-dir { justify-self: end; }
.ind-estado {
  padding: 24px; display: flex; flex-wrap: wrap; align-items: center; gap: 12px 16px;
  font-size: 14px; color: var(--ind-texto-2);
}

/* ---------- Abas ---------- */
.ind-abas { display: flex; flex-wrap: wrap; gap: 4px; border-bottom: 1px solid var(--ind-borda); }
.ind-aba {
  display: flex; align-items: center; min-height: 44px; padding: 0 16px; margin-bottom: -1px;
  border-bottom: 2px solid transparent; font-size: 15px; color: var(--ind-texto-2); text-decoration: none;
}
.ind-aba[aria-current="page"] { font-weight: 600; color: var(--ind-primaria); border-bottom-color: var(--ind-primaria); }

/* ---------- Cabeçalho da aba (no projeto, mantenha o que já existe) ---------- */
.ind-cabecalho { display: flex; flex-direction: column; gap: 4px; }
.ind-titulo-aba { margin: 0; font-size: 20px; font-weight: 600; }

/* ---------- Barra "Comparar com" ---------- */
.ind-barra { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-end; }
.ind-campo { display: flex; flex-direction: column; gap: 6px; }
.ind-campo label { font-size: 12px; font-weight: 500; color: var(--ind-texto-2); }
.ind-select {
  height: 44px; min-width: 250px; padding: 0 12px; border: 1px solid var(--ind-borda-forte);
  border-radius: 8px; background: #FFFFFF; font: inherit; font-size: 14px; color: var(--ind-texto);
}
.ind-select--pequeno { height: 36px; min-width: 120px; }
.ind-espaco { flex: 1 1 0; }
.ind-botao {
  height: 44px; padding: 0 18px; border: 1px solid var(--ind-borda-forte); border-radius: 8px;
  background: #FFFFFF; font: inherit; font-size: 14px; font-weight: 500; color: var(--ind-texto); cursor: pointer;
}
.ind-botao:hover { background: var(--ind-vazio); }
.ind-botao:disabled { color: var(--ind-suave); cursor: default; }

/* ---------- Avisos ---------- */
.ind-aviso {
  display: flex; gap: 10px; align-items: center; padding: 12px 16px; border: 1px solid #C9D8EC;
  border-radius: 8px; background: #EEF3FA; font-size: 13px; line-height: 1.5; color: #1D3F6E;
}
.ind-aviso strong { font-weight: 600; }
.ind-pontos {
  display: flex; flex-wrap: wrap; gap: 10px 28px; align-items: center; padding: 14px 18px;
  border: 1px solid #F0D9A8; border-radius: 10px; background: #FFF8EB; font-size: 14px; color: #4A3B1C;
}
.ind-pontos__titulo { font-weight: 600; color: #7A4A00; }
.ind-pontos__link { margin-left: auto; font-weight: 500; color: var(--ind-primaria); }

/* ---------- Cards de resumo ---------- */
.ind-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 16px; }
.ind-resumo { padding: 18px 20px; display: flex; flex-direction: column; gap: 8px; min-width: 0; }
.ind-resumo__topo { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.ind-resumo__rotulo { font-size: 13px; font-weight: 500; color: var(--ind-texto-2); }
.ind-resumo__valor { font-size: 30px; font-weight: 600; letter-spacing: -0.01em; line-height: 1.2; }
.ind-resumo__comp { font-size: 13px; color: var(--ind-suave); }

/* ---------- Pílula de status ---------- */
.ind-pill {
  display: inline-block; padding: 3px 8px; border-radius: 6px; font-size: 12px; font-weight: 500;
  line-height: 1.4; white-space: nowrap;
}
.ind-pill--bom { background: var(--bom-bg); color: var(--bom-fg); }
.ind-pill--atencao { background: var(--atencao-bg); color: var(--atencao-fg); }
.ind-pill--risco { background: var(--risco-bg); color: var(--risco-fg); }
.ind-pill--verificar { background: var(--verificar-bg); color: var(--verificar-fg); }

/* ---------- Tabela de indicadores ---------- */
.ind-tabela-card { padding: 8px 24px 18px; }
.ind-tabela { min-width: 1040px; }
.ind-grade {
  display: grid;
  grid-template-columns: minmax(230px, 2fr) 150px 92px 92px 104px 92px 104px 92px;
  gap: 12px;
  align-items: center;
}
.ind-cab {
  padding: 14px 0 12px; border-bottom: 1px solid var(--ind-borda);
  font-size: 12px; font-weight: 500; color: var(--ind-suave);
}
.ind-cab .ind-cab-base { color: var(--ind-texto); font-weight: 600; }
.ind-grupo { padding: 22px 0 6px; font-size: 13px; font-weight: 600; color: var(--ind-primaria); letter-spacing: 0.02em; }
.ind-linha { padding: 12px 0; border-top: 1px solid var(--ind-linha); font-size: 14px; }
.ind-nome { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.ind-nome__linha { display: flex; align-items: center; gap: 6px; }
.ind-nome__texto { font-weight: 500; }
.ind-nota { font-size: 12px; color: #7A4A00; }
.ind-sem-regua { font-size: 12px; color: var(--ind-suave); }
.ind-especial { font-size: 13px; }
.ind-especial-comp { font-size: 12px; }
.ind-aa { font-size: 11px; font-weight: 400; color: var(--ind-suave); }
.ind-var { font-weight: 500; }
.ind-spark { display: flex; justify-content: flex-end; }
.ind-rodape { display: flex; flex-wrap: wrap; gap: 8px 20px; padding-top: 16px; font-size: 12px; color: var(--ind-suave); }

/* ---------- Régua de faixas ---------- */
.ind-regua { position: relative; width: 100%; height: 16px; }
.ind-regua__trilho {
  position: absolute; left: 0; right: 0; top: 4px; height: 8px;
  display: flex; border-radius: 4px; overflow: hidden;
}
.ind-regua__trilho > div { height: 8px; }
.ind-regua__marcador {
  position: absolute; top: 0; width: 3px; height: 16px; margin-left: -1px;
  border-radius: 2px; background: var(--ind-texto);
}

/* ---------- Tooltip da fórmula (renderizado no body) ---------- */
.ind-tip__botao {
  display: inline-flex; align-items: center; padding: 0; border: 0; background: none;
  color: var(--ind-icone); cursor: help; flex: none;
}
.ind-tip__caixa {
  box-sizing: border-box; position: absolute; z-index: 1000; width: 280px; padding: 10px 12px; border-radius: 8px;
  background: #17202B; color: #FFFFFF; font-size: 12px; line-height: 1.5;
  display: flex; flex-direction: column; gap: 4px; pointer-events: none;
}
.ind-tip__caixa strong { font-weight: 500; }

/* ---------- Painel "O que explica a variação do lucro" e matriz ---------- */
.ind-painel { padding: 22px 24px; display: flex; flex-direction: column; gap: 14px; }
.ind-painel__cab { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
.ind-tabela-lucro { min-width: 840px; }
.ind-grade-lucro {
  display: grid;
  grid-template-columns: minmax(220px, 1.6fr) 96px 96px 104px minmax(160px, 1fr) 96px;
  gap: 12px;
  align-items: center;
}
.ind-cab-lucro {
  padding: 0 0 10px; border-bottom: 1px solid var(--ind-borda);
  font-size: 12px; font-weight: 500; color: var(--ind-suave);
}
.ind-linha-lucro { min-height: 44px; border-top: 1px solid var(--ind-linha); font-size: 14px; }
.ind-total-lucro { min-height: 48px; border-top: 1px solid var(--ind-borda-forte); font-size: 14px; font-weight: 600; }
.ind-peso { position: relative; height: 8px; border-radius: 4px; background: var(--ind-trilho); }
.ind-peso__barra { position: absolute; left: 0; top: 0; height: 8px; border-radius: 4px; }

/* ---------- Matriz dos últimos 12 meses ---------- */
.ind-matriz { min-width: 1040px; display: flex; flex-direction: column; gap: 4px; }
.ind-grade-matriz {
  display: grid;
  grid-template-columns: minmax(190px, 1.6fr) repeat(12, minmax(66px, 1fr));
  gap: 4px;
  align-items: center;
  font-size: 12px;
}
.ind-cab-matriz { padding-bottom: 4px; font-weight: 500; color: var(--ind-suave); }
.ind-cab-matriz > span:not(:first-child) { text-align: center; }
.ind-cab-matriz > .ind-ultimo { color: var(--ind-texto); font-weight: 600; }
.ind-matriz__nome { font-size: 13px; color: var(--ind-texto); }
.ind-celula { padding: 8px 4px; border-radius: 4px; text-align: center; font-weight: 500; white-space: nowrap; }
.ind-celula--bom { background: var(--bom-bg); color: var(--bom-fg); }
.ind-celula--atencao { background: var(--atencao-bg); color: var(--atencao-fg); }
.ind-celula--risco { background: var(--risco-bg); color: var(--risco-fg); }
.ind-celula--verificar { background: var(--verificar-bg); color: var(--verificar-fg); }
.ind-celula--vazia { background: var(--ind-vazio); color: var(--ind-icone); }
.ind-legenda { display: flex; flex-wrap: wrap; gap: 8px 16px; align-items: center; font-size: 12px; color: var(--ind-texto-2); }

/* ---------- Base de cálculo ---------- */
.ind-base { padding: 12px 24px; display: flex; flex-direction: column; gap: 12px; }
.ind-base__botao {
  display: flex; align-items: center; justify-content: space-between; gap: 12px; width: 100%;
  min-height: 48px; padding: 0; border: 0; background: none; font: inherit; color: var(--ind-texto);
  text-align: left; cursor: pointer;
}
.ind-base__titulos { display: flex; flex-direction: column; gap: 2px; }
.ind-base__acao { font-size: 14px; font-weight: 500; color: var(--ind-primaria); }
.ind-base__grade {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 380px), 1fr));
  gap: 32px; padding: 4px 0 14px; font-size: 14px;
}
.ind-base__col { display: flex; flex-direction: column; }
.ind-base__col-titulo { padding-bottom: 6px; font-size: 13px; font-weight: 600; color: var(--ind-primaria); }
.ind-base__item { display: flex; justify-content: space-between; gap: 12px; padding: 8px 0; border-top: 1px solid var(--ind-linha); }
.ind-base__valor { font-weight: 500; white-space: nowrap; }
.ind-etiqueta { margin-left: 6px; font-size: 12px; font-weight: 400; }
.ind-etiqueta--verificar { color: #7A4A00; }
.ind-etiqueta--info { color: var(--ind-suave); }

/* ---------- Gaveta de contas (renderizada no body) ---------- */
.ind-gaveta__fundo {
  position: fixed; inset: 0; z-index: 900; display: flex; justify-content: flex-end;
  background: rgba(23, 32, 43, 0.45);
}
.ind-gaveta { width: 560px; max-width: 100%; height: 100%; display: flex; flex-direction: column; background: #FFFFFF; }
.ind-gaveta__cab {
  display: flex; justify-content: space-between; align-items: flex-start; gap: 12px;
  padding: 20px 24px; border-bottom: 1px solid var(--ind-borda);
}
.ind-gaveta__titulos { display: flex; flex-direction: column; gap: 2px; }
.ind-gaveta__titulos h2:focus { outline: none; }
.ind-gaveta__fechar {
  display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px;
  margin: -10px -10px 0 0; border: 0; border-radius: 8px; background: none; color: var(--ind-texto-2); cursor: pointer;
}
.ind-gaveta__fechar:hover { background: var(--ind-trilho); }
.ind-gaveta__corpo { flex: 1; overflow-y: auto; padding: 8px 24px 24px; }
.ind-grade-contas {
  display: grid; grid-template-columns: minmax(0, 1fr) 100px 100px 100px; gap: 12px; align-items: center; width: 100%;
}
.ind-cab-contas { padding: 10px 0; border-bottom: 1px solid var(--ind-borda); font-size: 12px; font-weight: 500; color: var(--ind-suave); }
.ind-linha-conta {
  min-height: 52px; padding: 8px 0; border: 0; border-top: 1px solid var(--ind-linha); background: none;
  font: inherit; font-size: 13px; color: inherit; text-align: left; cursor: pointer;
}
.ind-linha-conta:hover { background: var(--ind-vazio); }
.ind-conta { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.ind-conta__cod { font-size: 12px; color: var(--ind-suave); }
.ind-lancamentos {
  display: flex; flex-direction: column; gap: 10px; padding: 12px 0 16px 12px;
  border-top: 1px solid var(--ind-linha); font-size: 13px;
}
.ind-grade-lanc { display: grid; grid-template-columns: 84px minmax(0, 1fr) 96px 96px; gap: 10px; align-items: center; }
.ind-diferenca { font-style: italic; color: var(--ind-texto-2); }

/* ---------- Esqueleto de carregamento ---------- */
.ind-esq { display: block; border-radius: 4px; background: var(--ind-trilho); animation: ind-pulso 1.2s ease-in-out infinite; }
@keyframes ind-pulso { 50% { opacity: 0.55; } }
@media (prefers-reduced-motion: reduce) { .ind-esq { animation: none; } }

@media (max-width: 640px) {
  .ind-tabela-card, .ind-painel { padding-left: 16px; padding-right: 16px; }
  .ind-base { padding-left: 16px; padding-right: 16px; }
  .ind-select { min-width: 0; width: 100%; }
  .ind-campo { flex: 1 1 100%; }
}
```

### A.2 `tipos.ts`

```ts
export type Status = 'bom' | 'atencao' | 'risco' | 'verificar';
export type Unidade = 'numero' | 'percentual' | 'multiplo' | 'moeda';
export type Direcao = 'maior_melhor' | 'menor_melhor';
export type Comparar = 'anterior' | 'ano-anterior';

export const ROTULO_STATUS: Record<Status, string> = {
  bom: 'Bom',
  atencao: 'Atenção',
  risco: 'Risco',
  verificar: 'Verificar',
};

export interface Avaliacao {
  temDados: boolean;
  valor: number | null;
  valorAnualizado: number | null;
  valorGrafico: number | null;
  texto: string | null;
  textoCurto: string | null;
  status: Status;
  nota: string | null;
  posicaoRegua: number | null;
  motivoSemRegua: string | null;
}

export interface Variacao {
  valor: number | null;
  unidade: 'absoluta' | 'pp' | 'multiplo' | 'moeda';
  melhorou: boolean | null;
}

export interface IndicadorLinha {
  codigo: string;
  nome: string;
  formula: string;
  explicacao: string;
  unidade: Unidade;
  casas: number;
  direcao: Direcao;
  limiteRisco: number | null;
  limiteBom: number | null;
  escala: { min: number; max: number } | null;
  atual: Avaliacao;
  anterior: Avaliacao;
  anoAnterior: Avaliacao;
  variacaoAnterior: Variacao;
  variacaoAnoAnterior: Variacao;
  meses: { anoMes: string; avaliacao: Avaliacao }[];
}

export interface Grupo {
  codigo: string;
  titulo: string;
  indicadores: IndicadorLinha[];
}

export interface CardResumo {
  codigo: string;
  rotulo: string;
  unidade: Unidade;
  casas: number;
  atual: Avaliacao;
  anterior: Avaliacao;
  anoAnterior: Avaliacao;
  variacaoAnterior: Variacao;
  variacaoAnoAnterior: Variacao;
}

export interface LinhaLucro {
  codigo: string;
  rotulo: string;
  valorBase: number;
  valorAtual: number;
  variacao: number;
  peso: number;
}

export interface ExplicacaoLucro {
  linhas: LinhaLucro[];
  lucroBase: number;
  lucroAtual: number;
  variacaoLucro: number;
}

export interface Base {
  temDados: boolean;
  dataBalanco: string;
  ativoTotal: number;
  ativoCirculante: number;
  disponivel: number;
  estoques: number;
  realizavelLongoPrazo: number;
  passivoCirculante: number;
  passivoNaoCirculante: number;
  patrimonioLiquido: number;
  dividaBruta: number;
  dividaLiquida: number;
  receitaLiquida: number;
  custos: number;
  lucroBruto: number;
  despesasOperacionais: number;
  ebitda: number;
  depreciacaoAmortizacao: number;
  ebit: number;
  resultadoFinanceiro: number;
  resultadoAntesIr: number;
  irCs: number;
  outrosResultados: number;
  lucroLiquido: number;
  ebitda12m: number | null;
  mesesEbitda12m: number;
  despesasPorDiferenca: boolean;
}

export interface Periodo {
  inicio: string; // AAAA-MM
  fim: string;    // AAAA-MM
  rotulo: string; // "Mar/26", "Mar–Set/26"
  meses?: number;
}

export interface IndicadoresContador {
  empresa: { codColigada: number; codFilial: number | null; nome: string };
  periodo: Periodo;
  periodoAnterior: Periodo;
  periodoAnoAnterior: Periodo;
  dataBalanco: string;
  primeiroMesComDados: string | null;
  mesesSemDados: string[];
  mesesSerie: string[];
  grupos: Grupo[];
  cards: CardResumo[];
  pontosVerificar: { codigo: string; texto: string }[];
  explicacaoLucro: { anterior: ExplicacaoLucro; anoAnterior: ExplicacaoLucro };
  baseCalculo: { atual: Base; anterior: Base; anoAnterior: Base };
}

export interface ContaVariacao {
  codConta: string;
  descricao: string;
  valorBase: number;
  valorAtual: number;
  variacao: number;
}

export interface VariacaoContas {
  linha: string;
  rotulo: string;
  totalBase: number;
  totalAtual: number;
  contas: ContaVariacao[];
  diferencaNaoExplicada?: number | null;
}
```

### A.3 `formatacao.ts`

```ts
import type { Avaliacao, IndicadorLinha, Unidade, Variacao } from './tipos';

export const TRAVESSAO = '—';
const MESES = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

const cache = new Map<number, Intl.NumberFormat>();
function nf(casas: number): Intl.NumberFormat {
  let f = cache.get(casas);
  if (!f) {
    f = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });
    cache.set(casas, f);
  }
  return f;
}

// Converte -0 em 0, para nunca aparecer "-0,00".
const semZeroNegativo = (v: number) => (v === 0 ? 0 : v);

export function moedaAbreviada(v: number): string {
  v = semZeroNegativo(v);
  const a = Math.abs(v);
  if (a >= 1_000_000) return `${nf(2).format(v / 1_000_000)} mi`;
  if (a >= 1_000) return `${nf(1).format(v / 1_000)} mil`;
  return nf(0).format(v);
}

/** 34.087.932,98 · negativo entre parênteses: (75.670,44) */
export function moedaCompleta(v: number): string {
  const s = nf(2).format(Math.abs(v));
  return v < 0 ? `(${s})` : s;
}

/** +6.500,00 · -6.500,00 */
export function moedaSinal(v: number): string {
  v = semZeroNegativo(v);
  return `${v > 0 ? '+' : ''}${nf(2).format(v)}`;
}

/** Valor em R$ mil com 1 casa: 187.530,96 → "187,5" */
export const mil1 = (v: number) => nf(1).format(semZeroNegativo(v) / 1000);
export const mil1Sinal = (v: number) => `${v > 0 ? '+' : ''}${mil1(v)}`;

export function formatarValor(v: number | null, unidade: Unidade, casas: number): string {
  if (v === null || v === undefined) return TRAVESSAO;
  v = semZeroNegativo(v);
  switch (unidade) {
    case 'numero': return nf(casas).format(v);
    case 'percentual': return `${nf(casas).format(v)}%`;
    case 'multiplo': return `${nf(casas).format(v)}x`;
    case 'moeda': return moedaAbreviada(v);
  }
}

export function formatarVariacao(variacao: Variacao | null, casas: number): string {
  if (!variacao || variacao.valor === null) return TRAVESSAO;
  const v = semZeroNegativo(variacao.valor);
  const abs = Math.abs(v);
  const sinal = v > 0 ? '+' : v < 0 ? '-' : '';
  switch (variacao.unidade) {
    case 'absoluta': return sinal + nf(casas).format(abs);
    case 'pp': return `${sinal}${nf(casas).format(abs)} p.p.`;
    case 'multiplo': return `${sinal}${nf(casas).format(abs)}x`;
    case 'moeda': return sinal + moedaAbreviada(abs);
  }
}

/** Texto da avaliação: "—" sem dados, texto especial ou número formatado. */
export function textoAvaliacao(a: Avaliacao, unidade: Unidade, casas: number): string {
  if (!a.temDados) return TRAVESSAO;
  if (a.texto) return a.texto;
  return formatarValor(a.valor, unidade, casas);
}

/** Texto da célula da matriz. Moeda aparece em mil, sem sufixo. */
export function textoCelulaMatriz(a: Avaliacao, ind: IndicadorLinha): string {
  if (!a.temDados) return TRAVESSAO;
  if (a.textoCurto) return a.textoCurto;
  if (a.valor === null) return TRAVESSAO;
  if (ind.unidade === 'moeda') return mil1(a.valor);
  return formatarValor(a.valor, ind.unidade, ind.casas);
}

/** "2026-03" → "mar/26" */
export function rotuloMes(anoMes: string): string {
  const [ano, mes] = anoMes.split('-');
  return `${MESES[Number(mes) - 1]}/${ano.slice(2)}`;
}

const capitalizar = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);

/** "Mar/26" · "Mar–Set/26" · "Out/25–Mar/26" */
export function rotuloPeriodo(inicio: string, fim: string): string {
  if (inicio === fim) return capitalizar(rotuloMes(inicio));
  const [anoI, mesI] = inicio.split('-');
  const [anoF] = fim.split('-');
  if (anoI === anoF) return `${capitalizar(MESES[Number(mesI) - 1])}–${capitalizar(rotuloMes(fim))}`;
  return `${capitalizar(rotuloMes(inicio))}–${capitalizar(rotuloMes(fim))}`;
}

/** "mar/2026" · "mar/2026 a set/2026" */
export function rotuloPeriodoLongo(inicio: string, fim: string): string {
  const longo = (am: string) => {
    const [ano, mes] = am.split('-');
    return `${MESES[Number(mes) - 1]}/${ano}`;
  };
  return inicio === fim ? longo(inicio) : `${longo(inicio)} a ${longo(fim)}`;
}

/** "2026-03-31" → "31/03/2026" */
export function dataBr(iso: string): string {
  const [a, m, d] = iso.slice(0, 10).split('-');
  return `${d}/${m}/${a}`;
}

/** Lista "AAAA-MM" de inicio até fim, inclusive. */
export function mesesEntre(inicio: string, fim: string): string[] {
  let [ano, mes] = inicio.split('-').map(Number);
  const [anoFim, mesFim] = fim.split('-').map(Number);
  const lista: string[] = [];
  while (ano < anoFim || (ano === anoFim && mes <= mesFim)) {
    lista.push(`${ano}-${String(mes).padStart(2, '0')}`);
    mes += 1;
    if (mes > 12) {
      mes = 1;
      ano += 1;
    }
  }
  return lista;
}

/** Linha de faixas do tooltip: "Bom: ≥ 1,50 · Atenção: entre 1,00 e 1,50 · Risco: < 1,00" */
export function faixasTexto(ind: IndicadorLinha): string | null {
  if (ind.limiteBom === null || ind.limiteRisco === null || ind.escala === null) return null;
  const f = (v: number) => formatarValor(v, ind.unidade, ind.casas);
  if (ind.direcao === 'maior_melhor') {
    return `Bom: ≥ ${f(ind.limiteBom)} · Atenção: entre ${f(ind.limiteRisco)} e ${f(ind.limiteBom)} · Risco: < ${f(ind.limiteRisco)}`;
  }
  return `Bom: ≤ ${f(ind.limiteBom)} · Atenção: entre ${f(ind.limiteBom)} e ${f(ind.limiteRisco)} · Risco: > ${f(ind.limiteRisco)}`;
}
```

### A.4 `StatusPill.tsx` (pílula de status e texto da variação)

```tsx
import { ROTULO_STATUS, type Status, type Variacao } from '../tipos';
import { formatarVariacao } from '../formatacao';

export function StatusPill({ status }: { status: Status }) {
  return <span className={`ind-pill ind-pill--${status}`}>{ROTULO_STATUS[status]}</span>;
}

export function VariacaoTexto({
  variacao,
  casas,
  rotuloBase,
  className,
}: {
  variacao: Variacao;
  casas: number;
  rotuloBase: string;
  className?: string;
}) {
  const cor =
    variacao.valor === null || variacao.melhorou === null
      ? 'var(--neutro)'
      : variacao.melhorou
        ? 'var(--melhorou)'
        : 'var(--piorou)';
  return (
    <span className={className} style={{ color: cor }} title={`Contra ${rotuloBase}`}>
      {formatarVariacao(variacao, casas)}
    </span>
  );
}
```

### A.5 `ReguaFaixas.tsx`

```tsx
import { ROTULO_STATUS, type IndicadorLinha, type Status } from '../tipos';
import { formatarValor, textoAvaliacao } from '../formatacao';

/**
 * Régua de 150 × 16 px: trilho de 8 px com 3 segmentos (risco, atenção, bom)
 * proporcionais à escala, e um marcador vertical na posição do valor atual.
 * A posição já vem calculada da API (posicaoRegua, de 1 a 99).
 */
export function ReguaFaixas({ ind }: { ind: IndicadorLinha }) {
  const a = ind.atual;
  if (a.posicaoRegua === null || ind.escala === null || ind.limiteBom === null || ind.limiteRisco === null) {
    return <span className="ind-sem-regua">{a.motivoSemRegua ?? 'Sem dados'}</span>;
  }

  const { min, max } = ind.escala;
  const largura = (trecho: number) => `${((trecho / (max - min)) * 100).toFixed(2)}%`;

  const segmentos: { status: Status; largura: string }[] =
    ind.direcao === 'maior_melhor'
      ? [
          { status: 'risco', largura: largura(ind.limiteRisco - min) },
          { status: 'atencao', largura: largura(ind.limiteBom - ind.limiteRisco) },
          { status: 'bom', largura: largura(max - ind.limiteBom) },
        ]
      : [
          { status: 'bom', largura: largura(ind.limiteBom - min) },
          { status: 'atencao', largura: largura(ind.limiteRisco - ind.limiteBom) },
          { status: 'risco', largura: largura(max - ind.limiteRisco) },
        ];

  const maior = ind.direcao === 'maior_melhor';
  const f = (v: number) => formatarValor(v, ind.unidade, ind.casas);
  const rotulo =
    `${ind.nome}: ${textoAvaliacao(a, ind.unidade, ind.casas)}, ${ROTULO_STATUS[a.status]}. ` +
    `Bom ${maior ? '≥' : '≤'} ${f(ind.limiteBom)}, risco ${maior ? '<' : '>'} ${f(ind.limiteRisco)}.`;

  return (
    <div className="ind-regua" role="img" aria-label={rotulo}>
      <div className="ind-regua__trilho">
        {segmentos.map((s) => (
          <div key={s.status} style={{ width: s.largura, background: `var(--${s.status}-seg)` }} />
        ))}
      </div>
      <div className="ind-regua__marcador" style={{ left: `${a.posicaoRegua}%` }} />
    </div>
  );
}
```

### A.6 `Sparkline.tsx`

```tsx
/**
 * Minigráfico de 96 × 24 px com 12 pontos. x = i × 96 ÷ 11; y vai de 22 (mínimo) a 2 (máximo).
 * Valores nulos quebram a linha em trechos. Com menos de 2 pontos, mostra "—".
 */
export function Sparkline({ valores }: { valores: (number | null)[] }) {
  const numeros = valores.filter((v): v is number => v !== null);
  if (numeros.length < 2) return <span style={{ color: 'var(--ind-icone)' }}>—</span>;

  const minimo = Math.min(...numeros);
  const maximo = Math.max(...numeros);
  const y = (v: number) => (maximo === minimo ? 12 : 22 - ((v - minimo) / (maximo - minimo)) * 20);
  const x = (i: number) => (i * 96) / 11;

  const trechos: string[][] = [];
  let atual: string[] = [];
  valores.forEach((v, i) => {
    if (v === null) {
      if (atual.length) trechos.push(atual);
      atual = [];
      return;
    }
    atual.push(`${x(i).toFixed(1)},${y(v).toFixed(1)}`);
  });
  if (atual.length) trechos.push(atual);

  return (
    <svg width="96" height="24" viewBox="0 0 96 24" aria-hidden="true">
      {trechos
        .filter((t) => t.length >= 2)
        .map((t, k) => (
          <polyline
            key={k}
            points={t.join(' ')}
            fill="none"
            stroke="#4E5968"
            strokeWidth={1.5}
            strokeLinejoin="round"
            strokeLinecap="round"
          />
        ))}
    </svg>
  );
}
```

### A.7 `TooltipFormula.tsx`

O tooltip vai para o `body` (portal) porque a caixa da tabela tem `overflow-x: auto`, o que cortaria o tooltip. Se o projeto já tiver um componente de tooltip com portal, use o dele com este mesmo conteúdo.

```tsx
import { useId, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { IndicadorLinha } from '../tipos';
import { faixasTexto } from '../formatacao';

const LARGURA = 280;

export function TooltipFormula({ ind }: { ind: IndicadorLinha }) {
  const id = useId();
  const botao = useRef<HTMLButtonElement>(null);
  const [posicao, setPosicao] = useState<{ top: number; left: number } | null>(null);

  const abrir = () => {
    const r = botao.current?.getBoundingClientRect();
    if (!r) return;
    const limite = window.scrollX + document.documentElement.clientWidth - LARGURA - 8;
    setPosicao({ top: r.bottom + 6 + window.scrollY, left: Math.max(8, Math.min(r.left + window.scrollX, limite)) });
  };
  const fechar = () => setPosicao(null);
  const faixas = faixasTexto(ind);

  return (
    <>
      <button
        ref={botao}
        type="button"
        className="ind-tip__botao"
        aria-label={`Fórmula de ${ind.nome}`}
        aria-describedby={posicao ? id : undefined}
        onMouseEnter={abrir}
        onMouseLeave={fechar}
        onFocus={abrir}
        onBlur={fechar}
        onKeyDown={(e) => {
          if (e.key === 'Escape') fechar();
        }}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
          strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="9" />
          <path d="M12 11v5" />
          <path d="M12 8h.01" />
        </svg>
      </button>
      {posicao &&
        createPortal(
          <div role="tooltip" id={id} className="ind-raiz ind-tip__caixa" style={{ top: posicao.top, left: posicao.left }}>
            <strong>{ind.formula}</strong>
            <span>{ind.explicacao}</span>
            {faixas && <span>{faixas}</span>}
          </div>,
          document.body,
        )}
    </>
  );
}
```

### A.8 `TabelaIndicadores.tsx`

```tsx
import { Fragment } from 'react';
import type { Avaliacao, Comparar, IndicadoresContador, IndicadorLinha } from '../tipos';
import { formatarValor, rotuloPeriodoLongo } from '../formatacao';
import { ReguaFaixas } from './ReguaFaixas';
import { Sparkline } from './Sparkline';
import { StatusPill, VariacaoTexto } from './StatusPill';
import { TooltipFormula } from './TooltipFormula';

export function TabelaIndicadores({ dados, comparar }: { dados: IndicadoresContador; comparar: Comparar }) {
  const { periodo, periodoAnterior, periodoAnoAnterior } = dados;
  const rotuloBase = comparar === 'anterior' ? periodoAnterior.rotulo : periodoAnoAnterior.rotulo;
  const longo = (p: { inicio: string; fim: string }) => rotuloPeriodoLongo(p.inicio, p.fim);

  return (
    <section className="ind-card ind-tabela-card" aria-label="Indicadores por grupo">
      <div className="ind-rolagem">
        <div className="ind-tabela">
          <div className="ind-grade ind-cab">
            <span>Indicador</span>
            <span>Faixa</span>
            <span className="ind-dir" title={longo(periodo)}>{periodo.rotulo}</span>
            <span className={comparar === 'anterior' ? 'ind-dir ind-cab-base' : 'ind-dir'} title={longo(periodoAnterior)}>
              {periodoAnterior.rotulo}
            </span>
            <span className="ind-dir">Variação</span>
            <span className={comparar === 'ano-anterior' ? 'ind-dir ind-cab-base' : 'ind-dir'} title={longo(periodoAnoAnterior)}>
              {periodoAnoAnterior.rotulo}
            </span>
            <span className="ind-dir">12 meses</span>
            <span>Status</span>
          </div>

          {dados.grupos.map((g) => (
            <Fragment key={g.codigo}>
              <div className="ind-grupo">{g.titulo}</div>
              {g.indicadores.map((ind) => (
                <LinhaIndicador key={ind.codigo} ind={ind} comparar={comparar} rotuloBase={rotuloBase} />
              ))}
            </Fragment>
          ))}
        </div>
      </div>

      <div className="ind-rodape">
        <span>Dív. líquida / EBITDA usa o EBITDA dos últimos 12 meses.</span>
        <span>Passe o mouse no ícone ao lado do nome para ver a fórmula.</span>
      </div>
    </section>
  );
}

function LinhaIndicador({ ind, comparar, rotuloBase }: { ind: IndicadorLinha; comparar: Comparar; rotuloBase: string }) {
  const a = ind.atual;
  const variacao = comparar === 'anterior' ? ind.variacaoAnterior : ind.variacaoAnoAnterior;

  return (
    <div className="ind-grade ind-linha">
      {/* 1. Indicador */}
      <div className="ind-nome">
        <div className="ind-nome__linha">
          <span className="ind-nome__texto">{ind.nome}</span>
          <TooltipFormula ind={ind} />
        </div>
        {a.nota && <span className="ind-nota">{a.nota}</span>}
      </div>

      {/* 2. Faixa */}
      <ReguaFaixas ind={ind} />

      {/* 3. Atual (+ linha "a.a." para ROE e ROA quando N < 12) */}
      <div className="ind-dir">
        <ValorCelula a={a} ind={ind} atual />
        {a.valorAnualizado !== null && (
          <div className="ind-aa">≈ {formatarValor(a.valorAnualizado, 'percentual', 2)} a.a.</div>
        )}
      </div>

      {/* 4. Anterior */}
      <ValorCelula a={ind.anterior} ind={ind} />

      {/* 5. Variação contra a base escolhida */}
      <VariacaoTexto variacao={variacao} casas={ind.casas} rotuloBase={rotuloBase} className="ind-dir ind-var" />

      {/* 6. Ano anterior */}
      <ValorCelula a={ind.anoAnterior} ind={ind} />

      {/* 7. 12 meses */}
      <div className="ind-spark">
        <Sparkline valores={ind.meses.map((m) => m.avaliacao.valorGrafico)} />
      </div>

      {/* 8. Status */}
      <div>
        <StatusPill status={a.status} />
      </div>
    </div>
  );
}

function ValorCelula({ a, ind, atual = false }: { a: Avaliacao; ind: IndicadorLinha; atual?: boolean }) {
  const classe = atual ? 'ind-dir ind-forte' : 'ind-dir ind-comp';
  if (!a.temDados) return <div className={classe}>—</div>;
  if (a.texto) return <div className={`${classe} ${atual ? 'ind-especial' : 'ind-especial-comp'}`}>{a.texto}</div>;
  return <div className={classe}>{formatarValor(a.valor, ind.unidade, ind.casas)}</div>;
}
```

### A.9 `CardsResumo.tsx` (cards de resumo, pontos a verificar e aviso de dados)

```tsx
import type { CardResumo, Comparar } from '../tipos';
import { rotuloMes, textoAvaliacao } from '../formatacao';
import { StatusPill, VariacaoTexto } from './StatusPill';

export function CardsResumo({ cards, comparar, rotuloBase }: { cards: CardResumo[]; comparar: Comparar; rotuloBase: string }) {
  return (
    <div className="ind-cards">
      {cards.map((c) => {
        const base = comparar === 'anterior' ? c.anterior : c.anoAnterior;
        const variacao = comparar === 'anterior' ? c.variacaoAnterior : c.variacaoAnoAnterior;
        return (
          <div key={c.codigo} className="ind-card ind-resumo">
            <div className="ind-resumo__topo">
              <span className="ind-resumo__rotulo">{c.rotulo}</span>
              <StatusPill status={c.atual.status} />
            </div>
            <span className="ind-resumo__valor">{textoAvaliacao(c.atual, c.unidade, c.casas)}</span>
            <span className="ind-resumo__comp">
              {rotuloBase}: {textoAvaliacao(base, c.unidade, c.casas)}{' '}
              <VariacaoTexto variacao={variacao} casas={c.casas} rotuloBase={rotuloBase} className="ind-forte-var" />
            </span>
          </div>
        );
      })}
    </div>
  );
}

// Troque o <a> pelo Link do roteador do projeto, apontando para a tela Contas contábeis.
export function PontosVerificar({ itens, hrefContas }: { itens: { codigo: string; texto: string }[]; hrefContas: string }) {
  if (itens.length === 0) return null;
  const titulo =
    itens.length === 1
      ? '1 ponto a verificar antes de fechar o mês'
      : `${itens.length} pontos a verificar antes de fechar o mês`;
  return (
    <section className="ind-pontos" aria-label="Pontos a verificar">
      <span className="ind-pontos__titulo">{titulo}</span>
      {itens.map((p) => (
        <span key={p.codigo}>{p.texto}</span>
      ))}
      <a className="ind-pontos__link" href={hrefContas}>Revisar em Contas contábeis</a>
    </section>
  );
}

export function AvisoDados({ primeiroMes }: { primeiroMes: string }) {
  return (
    <div className="ind-aviso">
      <strong>Dados</strong>
      <span>
        Não há dados contábeis antes de {rotuloMes(primeiroMes)}. Comparações e meses sem dados aparecem como —.
      </span>
    </div>
  );
}
```

### A.10 `PainelVariacaoLucro.tsx`

```tsx
import type { ExplicacaoLucro, LinhaLucro } from '../tipos';
import { mil1, mil1Sinal } from '../formatacao';

const cor = (v: number) => (v >= 0 ? 'var(--melhorou)' : 'var(--piorou)');

export function PainelVariacaoLucro({
  explicacao,
  rotuloBase,
  rotuloAtual,
  onVerContas,
}: {
  explicacao: ExplicacaoLucro;
  rotuloBase: string;
  rotuloAtual: string;
  onVerContas: (linha: LinhaLucro) => void;
}) {
  return (
    <section id="variacao" className="ind-card ind-painel" aria-label="O que explica a variação do lucro">
      <div className="ind-painel__cab">
        <h2 className="ind-titulo-secao">O que explica a variação do lucro</h2>
        <span className="ind-subtitulo">Lucro líquido · {rotuloBase} → {rotuloAtual} · R$ mil</span>
      </div>

      <div className="ind-rolagem">
        <div className="ind-tabela-lucro">
          <div className="ind-grade-lucro ind-cab-lucro">
            <span>Linha da DRE</span>
            <span className="ind-dir">{rotuloBase}</span>
            <span className="ind-dir">{rotuloAtual}</span>
            <span className="ind-dir">Variação</span>
            <span>Peso na variação</span>
            <span />
          </div>

          {explicacao.linhas.map((l) => (
            <div key={l.codigo} className="ind-grade-lucro ind-linha-lucro">
              <span>{l.rotulo}</span>
              <span className="ind-dir ind-comp">{mil1(l.valorBase)}</span>
              <span className="ind-dir" style={{ fontWeight: 500 }}>{mil1(l.valorAtual)}</span>
              <span className="ind-dir" style={{ fontWeight: 600, color: cor(l.variacao) }}>{mil1Sinal(l.variacao)}</span>
              <div className="ind-peso">
                <div
                  className="ind-peso__barra"
                  style={{ width: `${l.peso.toFixed(1)}%`, background: l.variacao >= 0 ? 'var(--peso-mais)' : 'var(--peso-menos)' }}
                />
              </div>
              <button type="button" className="ind-link ind-dir" onClick={() => onVerContas(l)}>
                Ver contas
              </button>
            </div>
          ))}

          <div className="ind-grade-lucro ind-total-lucro">
            <span>Lucro líquido</span>
            <span className="ind-dir">{mil1(explicacao.lucroBase)}</span>
            <span className="ind-dir" style={{ color: explicacao.lucroAtual < 0 ? 'var(--piorou)' : undefined }}>
              {mil1(explicacao.lucroAtual)}
            </span>
            <span className="ind-dir" style={{ color: cor(explicacao.variacaoLucro) }}>{mil1Sinal(explicacao.variacaoLucro)}</span>
            <span />
            <span />
          </div>
        </div>
      </div>

      <span className="ind-texto-2">Cada linha abre as contas que a compõem e, dentro de cada conta, as partidas do mês.</span>
    </section>
  );
}
```

### A.11 `GavetaContas.tsx`

```tsx
import { Fragment, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { LinhaLucro, Periodo } from '../tipos';
import { mesesEntre, moedaCompleta, moedaSinal, rotuloMes } from '../formatacao';
import { useVariacaoContas, type FiltrosIndicadores } from '../api';

export function GavetaContas({
  linha,
  filtros,
  periodoAtual,
  periodoBase,
  onFechar,
}: {
  linha: LinhaLucro;
  filtros: FiltrosIndicadores;
  periodoAtual: Periodo;
  periodoBase: Periodo;
  onFechar: () => void;
}) {
  const titulo = useRef<HTMLHeadingElement>(null);
  const [contaAberta, setContaAberta] = useState<string | null>(null);
  const consulta = useVariacaoContas({
    ...filtros,
    linha: linha.codigo,
    inicio: periodoAtual.inicio,
    fim: periodoAtual.fim,
    inicioBase: periodoBase.inicio,
    fimBase: periodoBase.fim,
  });

  // Foco no título ao abrir, Esc fecha, foco volta para o botão "Ver contas" ao fechar.
  useEffect(() => {
    const anterior = document.activeElement as HTMLElement | null;
    titulo.current?.focus();
    const tecla = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onFechar();
    };
    document.addEventListener('keydown', tecla);
    return () => {
      document.removeEventListener('keydown', tecla);
      anterior?.focus();
    };
  }, [onFechar]);

  return createPortal(
    <div className="ind-raiz ind-gaveta__fundo" onMouseDown={onFechar}>
      <aside
        className="ind-gaveta"
        role="dialog"
        aria-modal="true"
        aria-labelledby="ind-gaveta-titulo"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <header className="ind-gaveta__cab">
          <div className="ind-gaveta__titulos">
            <h2 id="ind-gaveta-titulo" ref={titulo} tabIndex={-1} className="ind-titulo-secao">
              {linha.rotulo} · contas
            </h2>
            <span className="ind-subtitulo">{periodoBase.rotulo} → {periodoAtual.rotulo} · R$</span>
          </div>
          <button type="button" className="ind-gaveta__fechar" aria-label="Fechar" onClick={onFechar}>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
              strokeLinecap="round" aria-hidden="true">
              <path d="M6 6l12 12M18 6L6 18" />
            </svg>
          </button>
        </header>

        <div className="ind-gaveta__corpo">
          {consulta.isLoading && <EsqueletoLinhas quantidade={5} />}

          {consulta.isError && (
            <div className="ind-estado">
              Não foi possível carregar as contas.
              <button type="button" className="ind-botao" onClick={() => consulta.refetch()}>Tentar de novo</button>
            </div>
          )}

          {consulta.data && consulta.data.contas.length === 0 && (
            <div className="ind-estado">Nenhuma conta com movimento nesta linha.</div>
          )}

          {consulta.data && consulta.data.contas.length > 0 && (
            <>
              <div className="ind-grade-contas ind-cab-contas">
                <span>Conta</span>
                <span className="ind-dir">{periodoBase.rotulo}</span>
                <span className="ind-dir">{periodoAtual.rotulo}</span>
                <span className="ind-dir">Variação</span>
              </div>

              {consulta.data.contas.map((c) => {
                const aberta = contaAberta === c.codConta;
                return (
                  <Fragment key={c.codConta}>
                    <button
                      type="button"
                      className="ind-grade-contas ind-linha-conta"
                      aria-expanded={aberta}
                      onClick={() => setContaAberta(aberta ? null : c.codConta)}
                    >
                      <span className="ind-conta">
                        <span className="ind-conta__cod">{c.codConta}</span>
                        <span>{c.descricao}</span>
                      </span>
                      <span className="ind-dir ind-comp">{moedaCompleta(c.valorBase)}</span>
                      <span className="ind-dir">{moedaCompleta(c.valorAtual)}</span>
                      <span className="ind-dir" style={{ fontWeight: 600, color: c.variacao >= 0 ? 'var(--melhorou)' : 'var(--piorou)' }}>
                        {moedaSinal(c.variacao)}
                      </span>
                    </button>
                    {aberta && <LancamentosConta codConta={c.codConta} filtros={filtros} periodo={periodoAtual} />}
                  </Fragment>
                );
              })}

              {consulta.data.diferencaNaoExplicada != null && (
                <div className="ind-grade-contas ind-linha-conta ind-diferenca">
                  <span>Diferença não explicada</span>
                  <span />
                  <span />
                  <span className="ind-dir">{moedaSinal(consulta.data.diferencaNaoExplicada)}</span>
                </div>
              )}
            </>
          )}
        </div>
      </aside>
    </div>,
    document.body,
  );
}

/**
 * Lançamentos da conta no mês. `useLancamentosConta` e `linkFechamento` são para criar
 * a partir do que a tela de Fechamentos JÁ usa (ILancamentosTotvsService e a rota dela).
 * A linha-resumo (Data nula) não entra na lista. Ajuste os nomes dos campos ao DTO existente.
 */
function LancamentosConta({ codConta, filtros, periodo }: { codConta: string; filtros: FiltrosIndicadores; periodo: Periodo }) {
  const meses = mesesEntre(periodo.inicio, periodo.fim);
  const [mes, setMes] = useState(periodo.fim);
  const consulta = useLancamentosConta({ codColigada: filtros.codColigada, codFilial: filtros.codFilial, anoMes: mes, codConta });

  return (
    <div className="ind-lancamentos">
      {meses.length > 1 && (
        <div className="ind-campo">
          <label htmlFor={`ind-mes-${codConta}`}>Mês</label>
          <select id={`ind-mes-${codConta}`} className="ind-select ind-select--pequeno" value={mes} onChange={(e) => setMes(e.target.value)}>
            {meses.map((m) => (
              <option key={m} value={m}>{rotuloMes(m)}</option>
            ))}
          </select>
        </div>
      )}

      {consulta.isLoading && <EsqueletoLinhas quantidade={3} />}

      {consulta.data && (
        <>
          <div className="ind-grade-lanc ind-cab-contas">
            <span>Data</span>
            <span>Histórico</span>
            <span className="ind-dir">Débito</span>
            <span className="ind-dir">Crédito</span>
          </div>
          {consulta.data
            .filter((l) => l.data !== null)
            .map((l, i) => (
              <div key={i} className="ind-grade-lanc">
                <span>{new Date(l.data!).toLocaleDateString('pt-BR')}</span>
                <span>{l.historico}</span>
                <span className="ind-dir">{l.debito ? moedaCompleta(l.debito) : ''}</span>
                <span className="ind-dir">{l.credito ? moedaCompleta(l.credito) : ''}</span>
              </div>
            ))}
        </>
      )}

      {/* Monte o link com a rota da tela de Fechamentos, já com empresa, conta e mês. */}
      <a className="ind-link" href={linkFechamento(filtros, codConta, mes)}>Abrir no fechamento</a>
    </div>
  );
}

function EsqueletoLinhas({ quantidade }: { quantidade: number }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12, padding: '12px 0' }}>
      {Array.from({ length: quantidade }, (_, i) => (
        <span key={i} className="ind-esq" style={{ height: 14, width: `${90 - i * 8}%` }} />
      ))}
    </div>
  );
}
```

### A.12 `MatrizMensal.tsx`

```tsx
import { ROTULO_STATUS, type IndicadoresContador, type Status } from '../tipos';
import { rotuloMes, textoCelulaMatriz } from '../formatacao';
import { StatusPill } from './StatusPill';

const LEGENDA: Status[] = ['bom', 'atencao', 'risco', 'verificar'];

export function MatrizMensal({ dados }: { dados: IndicadoresContador }) {
  const indicadores = dados.grupos.flatMap((g) => g.indicadores);
  const ultimo = dados.mesesSerie.length - 1;

  return (
    <section className="ind-card ind-painel" aria-label="Matriz dos últimos 12 meses">
      <div className="ind-painel__cab">
        <h2 className="ind-titulo-secao">Matriz dos últimos 12 meses</h2>
        <span className="ind-subtitulo">Cada célula na cor do status daquele mês</span>
      </div>

      <div className="ind-rolagem">
        <div className="ind-matriz">
          <div className="ind-grade-matriz ind-cab-matriz">
            <span>Indicador</span>
            {dados.mesesSerie.map((m, i) => (
              <span key={m} className={i === ultimo ? 'ind-ultimo' : undefined}>{rotuloMes(m)}</span>
            ))}
          </div>

          {indicadores.map((ind) => (
            <div key={ind.codigo} className="ind-grade-matriz">
              <span className="ind-matriz__nome">{ind.unidade === 'moeda' ? `${ind.nome} (R$ mil)` : ind.nome}</span>
              {ind.meses.map(({ anoMes, avaliacao }) => {
                const texto = textoCelulaMatriz(avaliacao, ind);
                const classe = avaliacao.temDados ? `ind-celula ind-celula--${avaliacao.status}` : 'ind-celula ind-celula--vazia';
                const status = avaliacao.temDados ? ROTULO_STATUS[avaliacao.status] : 'sem dados';
                return (
                  <span key={anoMes} className={classe} title={`${ind.nome} · ${rotuloMes(anoMes)}: ${texto} · ${status}`}>
                    {texto}
                  </span>
                );
              })}
            </div>
          ))}
        </div>
      </div>

      <div className="ind-legenda">
        {LEGENDA.map((s) => (
          <StatusPill key={s} status={s} />
        ))}
        <span>Faixas definidas pela contabilidade</span>
      </div>
    </section>
  );
}
```

### A.13 `BaseCalculo.tsx`

```tsx
import { useState } from 'react';
import type { Base } from '../tipos';
import { dataBr, moedaCompleta, TRAVESSAO } from '../formatacao';

const CHAVE = 'indicadores.baseCalculoAberta';

const BALANCO: [keyof Base, string][] = [
  ['ativoTotal', 'Ativo total'],
  ['ativoCirculante', 'Ativo circulante'],
  ['disponivel', 'Disponível (caixa e equivalentes)'],
  ['estoques', 'Estoques'],
  ['realizavelLongoPrazo', 'Realizável a longo prazo'],
  ['passivoCirculante', 'Passivo circulante'],
  ['passivoNaoCirculante', 'Passivo não circulante'],
  ['patrimonioLiquido', 'Patrimônio líquido'],
  ['dividaBruta', 'Dívida bruta (empréstimos e financiamentos)'],
  ['dividaLiquida', 'Dívida líquida'],
];

function lerAberto(): boolean {
  try {
    return localStorage.getItem(CHAVE) === '1';
  } catch {
    return false;
  }
}

export function BaseCalculo({ base, rotuloResultado }: { base: Base; rotuloResultado: string }) {
  const [aberto, setAberto] = useState(lerAberto);

  const alternar = () => {
    const novo = !aberto;
    setAberto(novo);
    try {
      localStorage.setItem(CHAVE, novo ? '1' : '0');
    } catch {
      /* sem armazenamento: só não lembra */
    }
  };

  const custosVerificar = base.custos === 0 && base.estoques > 0;
  const textoEbitda12m =
    base.mesesEbitda12m < 12 ? `base do Dív. líquida / EBITDA (${base.mesesEbitda12m} meses)` : 'base do Dív. líquida / EBITDA';

  return (
    <section className="ind-card ind-base" aria-label="Base de cálculo">
      <button type="button" className="ind-base__botao" aria-expanded={aberto} onClick={alternar}>
        <span className="ind-base__titulos">
          <span className="ind-titulo-secao">Base de cálculo</span>
          <span className="ind-subtitulo">Valores usados nas fórmulas, em R$</span>
        </span>
        <span className="ind-base__acao">{aberto ? 'Ocultar' : 'Mostrar'}</span>
      </button>

      {aberto && (
        <div className="ind-base__grade">
          <div className="ind-base__col">
            <span className="ind-base__col-titulo">Balanço em {dataBr(base.dataBalanco)}</span>
            {BALANCO.map(([campo, rotulo]) => (
              <Item key={campo} rotulo={rotulo} valor={moedaCompleta(base[campo] as number)} />
            ))}
          </div>

          <div className="ind-base__col">
            <span className="ind-base__col-titulo">Resultado de {rotuloResultado}</span>
            <Item rotulo="Receita líquida" valor={moedaCompleta(base.receitaLiquida)} />
            <Item
              rotulo="(−) Custos"
              valor={custosVerificar ? TRAVESSAO : moedaCompleta(base.custos)}
              etiqueta={custosVerificar ? { texto: 'verificar', tipo: 'verificar' } : undefined}
            />
            <Item rotulo="Lucro bruto" valor={moedaCompleta(base.lucroBruto)} />
            <Item
              rotulo="(−) Despesas operacionais, exceto D&A"
              valor={moedaCompleta(base.despesasOperacionais)}
              etiqueta={base.despesasPorDiferenca ? { texto: 'por diferença', tipo: 'info' } : undefined}
            />
            <Item rotulo="EBITDA" valor={moedaCompleta(base.ebitda)} />
            <Item rotulo="(−) Depreciação e amortização" valor={moedaCompleta(base.depreciacaoAmortizacao)} />
            <Item rotulo="EBIT (resultado operacional)" valor={moedaCompleta(base.ebit)} />
            <Item rotulo="(+/−) Resultado financeiro" valor={moedaCompleta(base.resultadoFinanceiro)} />
            <Item rotulo="(−) IR e CS" valor={moedaCompleta(base.irCs)} />
            <Item rotulo="Lucro líquido" valor={moedaCompleta(base.lucroLiquido)} forte negativo={base.lucroLiquido < 0} />
            <Item
              rotulo="EBITDA dos últimos 12 meses"
              valor={base.ebitda12m === null ? TRAVESSAO : moedaCompleta(base.ebitda12m)}
              etiqueta={{ texto: textoEbitda12m, tipo: 'info' }}
            />
          </div>
        </div>
      )}
    </section>
  );
}

function Item({
  rotulo,
  valor,
  etiqueta,
  forte = false,
  negativo = false,
}: {
  rotulo: string;
  valor: string;
  etiqueta?: { texto: string; tipo: 'verificar' | 'info' };
  forte?: boolean;
  negativo?: boolean;
}) {
  return (
    <div className="ind-base__item">
      <span style={forte ? { fontWeight: 600 } : undefined}>
        {rotulo}
        {etiqueta && <span className={`ind-etiqueta ind-etiqueta--${etiqueta.tipo}`}>{etiqueta.texto}</span>}
      </span>
      <span className="ind-base__valor" style={{ fontWeight: forte ? 600 : 500, color: negativo ? 'var(--piorou)' : undefined }}>
        {valor}
      </span>
    </div>
  );
}
```

### A.14 `api.ts`

```ts
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { IndicadoresContador, VariacaoContas, Comparar } from './tipos';

// Use o cliente HTTP do projeto (o que já envia o JWT). `http` é ilustrativo.
declare const http: {
  get<T>(url: string): Promise<T>;
  getBlob(url: string): Promise<{ blob: Blob; nomeArquivo: string | null }>;
};

export interface FiltrosIndicadores {
  codColigada: number;
  codFilial: number | null;
  inicio: string; // AAAA-MM, a partir do FiltrosContext
  fim: string;
}

const chave = (f: FiltrosIndicadores) => ['indicadores-contador', f.codColigada, f.codFilial, f.inicio, f.fim] as const;

function parametros(f: FiltrosIndicadores, extra: Record<string, string> = {}) {
  const p = new URLSearchParams({ codColigada: String(f.codColigada), inicio: f.inicio, fim: f.fim, ...extra });
  if (f.codFilial !== null) p.set('codFilial', String(f.codFilial));
  return p.toString();
}

export function useIndicadoresContador(f: FiltrosIndicadores) {
  return useQuery({
    queryKey: chave(f),
    queryFn: () => http.get<IndicadoresContador>(`/api/indicadores/contador?${parametros(f)}`),
  });
}

/** Ligue no botão "Atualizar" que já existe: busca ignorando o cache do backend. */
export function useAtualizarIndicadoresContador() {
  const queryClient = useQueryClient();
  return async (f: FiltrosIndicadores) => {
    const dados = await http.get<IndicadoresContador>(`/api/indicadores/contador?${parametros(f, { atualizar: 'true' })}`);
    queryClient.setQueryData(chave(f), dados);
  };
}

export function useVariacaoContas(
  f: FiltrosIndicadores & { linha: string; inicioBase: string; fimBase: string },
) {
  return useQuery({
    queryKey: ['indicadores-variacao-contas', f.codColigada, f.codFilial, f.linha, f.inicio, f.fim, f.inicioBase, f.fimBase],
    queryFn: () =>
      http.get<VariacaoContas>(
        `/api/indicadores/variacao-contas?${parametros(f, { linha: f.linha, inicioBase: f.inicioBase, fimBase: f.fimBase })}`,
      ),
  });
}

export async function baixarExportacao(f: FiltrosIndicadores & { comparar: Comparar }) {
  const { blob, nomeArquivo } = await http.getBlob(`/api/indicadores/contador/exportar?${parametros(f, { comparar: f.comparar })}`);
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = nomeArquivo ?? `indicadores-${f.inicio}_${f.fim}.xlsx`;
  a.click();
  URL.revokeObjectURL(url);
}
```

### A.15 `VisaoContador.tsx` (barra de comparação, visão do contador e abas)

Os componentes abaixo podem ficar em arquivos separados. `useFiltrosIndicadores` é um hook a criar: ele lê o `FiltrosContext` e devolve `{ codColigada, codFilial, inicio, fim }` no formato `AAAA-MM`.

```tsx
import { useState, type MouseEvent } from 'react';
import { useSearchParams } from 'react-router-dom'; // adapte se o roteador for outro
import type { Comparar, IndicadoresContador, LinhaLucro } from '../tipos';
import { rotuloPeriodoLongo } from '../formatacao';
import { baixarExportacao, useIndicadoresContador } from '../api';
import { AvisoDados, CardsResumo, PontosVerificar } from './CardsResumo';
import { TabelaIndicadores } from './TabelaIndicadores';
import { PainelVariacaoLucro } from './PainelVariacaoLucro';
import { GavetaContas } from './GavetaContas';
import { MatrizMensal } from './MatrizMensal';
import { BaseCalculo } from './BaseCalculo';
import { VisaoDiretoria } from '../diretoria/VisaoDiretoria';

export function BarraComparacao({
  dados,
  comparar,
  onComparar,
  onExportar,
  exportando,
}: {
  dados: IndicadoresContador;
  comparar: Comparar;
  onComparar: (c: Comparar) => void;
  onExportar: () => void;
  exportando: boolean;
}) {
  return (
    <div className="ind-barra">
      <div className="ind-campo">
        <label htmlFor="ind-comparar">Comparar com</label>
        <select id="ind-comparar" className="ind-select" value={comparar} onChange={(e) => onComparar(e.target.value as Comparar)}>
          <option value="anterior">Período anterior ({dados.periodoAnterior.rotulo})</option>
          <option value="ano-anterior">Mesmo período do ano anterior ({dados.periodoAnoAnterior.rotulo})</option>
        </select>
      </div>
      <div className="ind-espaco" />
      <button type="button" className="ind-botao" onClick={onExportar} disabled={exportando}>
        {exportando ? 'Exportando…' : 'Exportar'}
      </button>
    </div>
  );
}

export function VisaoContador() {
  // Converta os filtros globais do FiltrosContext para { codColigada, codFilial, inicio, fim } (AAAA-MM).
  const filtros = useFiltrosIndicadores();
  const [params, setParams] = useSearchParams();
  const comparar: Comparar = params.get('comparar') === 'ano-anterior' ? 'ano-anterior' : 'anterior';
  const consulta = useIndicadoresContador(filtros);
  const [linhaAberta, setLinhaAberta] = useState<LinhaLucro | null>(null);
  const [exportando, setExportando] = useState(false);

  const mudarComparar = (c: Comparar) => {
    const p = new URLSearchParams(params);
    p.set('comparar', c);
    setParams(p, { replace: true }); // não refaz a consulta: só troca a base exibida
  };

  const exportar = async () => {
    setExportando(true);
    try {
      await baixarExportacao({ ...filtros, comparar });
    } finally {
      setExportando(false);
    }
  };

  if (consulta.isLoading) return <div className="ind"><EsqueletoContador /></div>;

  if (consulta.isError || !consulta.data) {
    return (
      <div className="ind">
        <div className="ind-card ind-estado">
          <span>Não foi possível carregar os indicadores.</span>
          <button type="button" className="ind-botao" onClick={() => consulta.refetch()}>Tentar de novo</button>
        </div>
        {/* A DFC atual continua aqui, independente. */}
      </div>
    );
  }

  const d = consulta.data;

  if (d.primeiroMesComDados === null || !d.baseCalculo.atual.temDados) {
    return (
      <div className="ind">
        <div className="ind-card ind-estado">
          Não há lançamentos contábeis para {d.empresa.nome} em {rotuloPeriodoLongo(d.periodo.inicio, d.periodo.fim)}.
        </div>
      </div>
    );
  }

  const periodoBase = comparar === 'anterior' ? d.periodoAnterior : d.periodoAnoAnterior;
  const explicacao = comparar === 'anterior' ? d.explicacaoLucro.anterior : d.explicacaoLucro.anoAnterior;

  return (
    <div className="ind">
      {/* Título "Índices financeiros" e subtítulo atuais da tela: mantenha como já estão. */}
      <BarraComparacao dados={d} comparar={comparar} onComparar={mudarComparar} onExportar={exportar} exportando={exportando} />
      {d.mesesSemDados.length > 0 && <AvisoDados primeiroMes={d.primeiroMesComDados} />}
      <PontosVerificar itens={d.pontosVerificar} hrefContas="/contas" />
      <CardsResumo cards={d.cards} comparar={comparar} rotuloBase={periodoBase.rotulo} />
      <TabelaIndicadores dados={d} comparar={comparar} />
      <PainelVariacaoLucro
        explicacao={explicacao}
        rotuloBase={periodoBase.rotulo}
        rotuloAtual={d.periodo.rotulo}
        onVerContas={setLinhaAberta}
      />
      <MatrizMensal dados={d} />
      <BaseCalculo base={d.baseCalculo.atual} rotuloResultado={rotuloPeriodoLongo(d.periodo.inicio, d.periodo.fim)} />
      {/* DFC atual, sem nenhuma mudança. */}

      {linhaAberta && (
        <GavetaContas
          linha={linhaAberta}
          filtros={filtros}
          periodoAtual={d.periodo}
          periodoBase={periodoBase}
          onFechar={() => setLinhaAberta(null)}
        />
      )}
    </div>
  );
}

function EsqueletoContador() {
  return (
    <>
      <div className="ind-cards">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="ind-card ind-resumo">
            <span className="ind-esq" style={{ width: '60%', height: 14 }} />
            <span className="ind-esq" style={{ width: '45%', height: 34 }} />
            <span className="ind-esq" style={{ width: '70%', height: 14 }} />
          </div>
        ))}
      </div>
      <section className="ind-card ind-tabela-card" aria-busy="true" aria-label="Carregando indicadores">
        <div className="ind-rolagem">
          <div className="ind-tabela">
            {Array.from({ length: 13 }, (_, i) => (
              <div key={i} className="ind-grade ind-linha">
                <span className="ind-esq" style={{ width: '70%', height: 14 }} />
                <span className="ind-esq" style={{ width: '100%', height: 8 }} />
                {Array.from({ length: 6 }, (_, j) => (
                  <span key={j} className="ind-esq" style={{ width: '80%', height: 14, justifySelf: 'end' }} />
                ))}
              </div>
            ))}
          </div>
        </div>
      </section>
    </>
  );
}

export function AbasIndicadores() {
  const [params, setParams] = useSearchParams();
  const visao = params.get('visao') === 'diretoria' ? 'diretoria' : 'contador';

  const ir = (v: 'contador' | 'diretoria') => (e: MouseEvent) => {
    e.preventDefault();
    const p = new URLSearchParams(params);
    p.set('visao', v);
    setParams(p);
  };

  return (
    <div className="ind-raiz ind">
      <nav className="ind-abas" aria-label="Visões">
        <a href="?visao=contador" className="ind-aba" aria-current={visao === 'contador' ? 'page' : undefined} onClick={ir('contador')}>
          Visão do contador
        </a>
        <a href="?visao=diretoria" className="ind-aba" aria-current={visao === 'diretoria' ? 'page' : undefined} onClick={ir('diretoria')}>
          Visão da diretoria
        </a>
      </nav>
      {visao === 'contador' ? <VisaoContador /> : <VisaoDiretoria />}
    </div>
  );
}
```

`VisaoDiretoria.tsx` nesta tarefa:

```tsx
export function VisaoDiretoria() {
  return (
    <div className="ind">
      <div className="ind-card ind-estado">A visão da diretoria será construída na próxima etapa.</div>
    </div>
  );
}
```

---

## Apêndice B. `referencia-visao-contador.html`

Arquivo único, em HTML + CSS + JavaScript puro, que desenha a aba inteira: abas, barra "Comparar com", pontos a verificar, cards, tabela com régua e minigráfico, tooltip da fórmula, painel do lucro, gaveta de contas, matriz e base de cálculo. Abra no navegador para ver o resultado final e testar as interações: trocar a comparação, passar o mouse no ícone de informação, clicar em "Ver contas", abrir a base de cálculo.

O script tem três partes:
1. **Exemplo de resposta da API:** monta um objeto no formato exato do contrato 5.4. Os valores de mar/2026 são da AEPL; comparações, 12 meses, explicação do lucro e contas da gaveta são **exemplo**. **Não copie estes números para o produto.**
2. **Formatação:** as mesmas funções de `formatacao.ts`.
3. **Desenho:** as mesmas estruturas, classes e regras dos componentes do Apêndice A.

Use esse arquivo como gabarito visual. Ao fim de cada etapa do front, compare a tela do sistema com ele lado a lado.
