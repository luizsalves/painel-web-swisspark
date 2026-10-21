# Indicadores financeiros

Este arquivo explica os indicadores da tela **Indicadores**. Use-o para explicar conceitos, fórmulas e regras do painel.

**Valores, status e faixas de uma empresa vêm sempre da ferramenta `ConsultarIndicadores`.** Nunca cite um número que não veio de uma ferramenta. Se este arquivo e a ferramenta divergirem, vale a ferramenta.

## As duas visões

- **Visão do contador.** É técnica e mostra todos os indicadores em três grupos (Liquidez, Endividamento, Rentabilidade e resultado), com fórmula, faixa, comparativos e status.
- **Visão da diretoria.** Resume a saúde da empresa em quatro pilares (Resultado, Caixa, Dívida e Contas de curto prazo), com um veredito geral e a lista "O que exige atenção". Não tem fórmulas e usa a linguagem do negócio.

Adapte a resposta a quem pergunta. Para o contador, cite a fórmula e os componentes. Para o diretor, diga o que o número significa para o negócio, sem jargão.

## Status

| Status | Significado |
|---|---|
| Bom | Dentro da faixa saudável |
| Atenção | Entre o limite de risco e o limite de bom |
| Risco | Fora da faixa saudável |
| Verificar | O número provavelmente está distorcido por um problema no dado. Não trate como bom nem como ruim: diga o que conferir |

Cada indicador tem uma **direção**: "maior é melhor" ou "menor é melhor". As faixas são iniciais e o contador pode revisá-las, por isso use sempre as que a ferramenta devolver.

## Liquidez

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| Liquidez corrente | Ativo circulante ÷ Passivo circulante | Maior é melhor | Quanto o ativo de curto prazo cobre as contas que vencem em até 12 meses. Abaixo de 1, ele não cobre |
| Liquidez seca | (Ativo circulante − Estoques) ÷ Passivo circulante | Maior é melhor | O mesmo, sem os estoques, que podem demorar a virar dinheiro. Uma diferença grande entre a corrente e a seca indica dependência dos estoques |
| Liquidez imediata | Disponível ÷ Passivo circulante | Maior é melhor | Quanto do passivo de curto prazo o caixa e as aplicações pagariam hoje |
| Liquidez geral | (Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante) | Maior é melhor | A capacidade de pagar todas as obrigações, de curto e de longo prazo |

## Endividamento

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| Endividamento geral | (Passivo circulante + Passivo não circulante) ÷ Ativo total | Menor é melhor | Quanto do ativo é financiado por terceiros |
| Dívida no curto prazo (CE) | Passivo circulante ÷ (Passivo circulante + Passivo não circulante) | Menor é melhor | Quanto das obrigações vence em até 12 meses |
| Dívida líquida / EBITDA 12m | (Dívida bruta − Caixa e aplicações) ÷ EBITDA dos últimos 12 meses | Menor é melhor | Quantos anos de geração de caixa da operação pagariam a dívida líquida |
| Dívida bruta / PL | Empréstimos e financiamentos (curto e longo prazo) ÷ Patrimônio líquido | Menor é melhor | O peso da dívida financeira em relação ao capital dos sócios |

## Rentabilidade e resultado (no período)

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| ROE | Lucro líquido ÷ Patrimônio líquido | Maior é melhor | O retorno sobre o capital dos sócios |
| ROA | Lucro líquido ÷ Ativo total | Maior é melhor | O retorno sobre todo o ativo |
| Margem bruta | Lucro bruto ÷ Receita líquida | Maior é melhor | Quanto sobra da receita depois dos custos |
| Margem líquida | Lucro líquido ÷ Receita líquida | Maior é melhor | Quanto sobra da receita depois de tudo, inclusive dos impostos |
| EBITDA | Resultado antes de juros, impostos, depreciação e amortização | Positivo é bom | A geração de caixa da operação, sem efeitos financeiros e contábeis |

ROE, ROA e as margens são do período escolhido. O status de ROE e ROA usa o valor **anualizado** (valor do período × 12 ÷ número de meses), e a tela mostra "≈ X% ao ano" embaixo do valor.

## Casos especiais

- **PCT foi retirado.** A participação de capitais de terceiros é matematicamente igual ao endividamento geral, porque passivo + PL = ativo. Os dois davam sempre o mesmo valor.
- **Dívida líquida ≤ 0.** A empresa tem mais caixa do que dívida. O painel mostra "Caixa líquido" com status Bom, e não um múltiplo negativo.
- **EBITDA ≤ 0 com dívida líquida > 0.** O múltiplo perde o sentido. O painel mostra "EBITDA negativo" com status Risco.
- **Dívida bruta = 0.** Dívida bruta / PL aparece como "Sem dívida", com status Bom.
- **Custos zerados com estoque > 0.** A margem bruta fica com status Verificar. Uma margem de 100% nesse caso não é bom resultado: é sinal de custo não classificado. O que fazer é conferir o de-para das contas de custo.
- **EBITDA 12m estimado.** Enquanto não houver o histórico de 12 meses, o EBITDA 12m é estimado anualizando o período (EBITDA do período × 12 ÷ número de meses). Diga isso ao explicar o número.

## Regras de comparação

1. **Fluxo é diferente de saldo.** Itens da DRE (receita, lucro) se comparam por período, por exemplo março contra fevereiro. Itens do balanço (caixa, dívida) se comparam pelo saldo na data, por exemplo 31/03 contra 28/02.
2. **Percentual varia em pontos percentuais.** Uma margem que vai de 7,6% para 7,4% caiu 0,2 p.p., e não 2,6%.
3. **A cor indica se melhorou ou piorou, e não se subiu ou desceu.** Endividamento subindo é piora. Receita subindo é melhora.
4. **Base zero ou negativa.** A variação vai em reais ou na unidade do indicador, nunca em percentual. Exemplo: um resultado que passou de prejuízo para lucro.
5. **Para a diretoria,** prefira comparar os últimos 12 meses ou o acumulado do ano contra o mesmo período do ano anterior. Mês contra mês é ruidoso: o IR trimestral, por exemplo, pesa num mês só.

## Visão da diretoria

### Pilares

| Pilar | Valor mostrado | De onde vem o status |
|---|---|---|
| Resultado | Lucro líquido do período | Margem líquida |
| Caixa | Saldo de caixa no fim do período | Caixa das atividades operacionais (DFC): positivo é Bom, negativo é Risco |
| Dívida | Dívida líquida / EBITDA 12m, ou "Sem dívida líquida" | Dívida líquida / EBITDA 12m |
| Contas de curto prazo | Liquidez corrente | Liquidez corrente |

### Veredito

O status geral é **o pior status entre os quatro pilares**. Assim, o topo nunca mostra "Bom" com um risco escondido embaixo.

A frase do veredito começa pelos pilares bons, usa "mas" e termina com os pilares ruins. Exemplo: "A operação gera caixa, mas a dívida pede atenção e o período fechou com prejuízo."

### O que exige atenção

São até cinco alertas, ordenados por gravidade:

- **Muitas obrigações vencendo em até 12 meses, com liquidez corrente abaixo da faixa boa** (Risco). O alerta diz quanto vence e quanto o caixa cobre disso.
- **Prejuízo com resultado antes do IR positivo** (Atenção). O prejuízo veio do IR e da CS, e não da operação.
- **Prejuízo nos outros casos** (Risco).
- **Liquidez corrente abaixo de 1** (Risco). O ativo de curto prazo não cobre o passivo de curto prazo.
- **Dívida líquida / EBITDA 12m acima do limite de risco** (Risco). A dívida líquida equivale a muitos anos de geração de caixa.

## Pontos a verificar

São problemas no dado, e não na empresa. Aparecem numa faixa no topo da visão do contador:

- custos zerados com estoque;
- valor na DFC sem classificação;
- contas sem de-para.

A correção é feita em **Contas contábeis**, revisando o de-para.

## Como responder

- **"Como está a empresa X?"** Consulte os indicadores. Comece pelo veredito e pelos pilares e depois cite os alertas. Diga a empresa e o período.
- **"Por que deu prejuízo?"** Siga a cascata do resultado: receita, custos, despesas, EBITDA, resultado financeiro, IR e CS e lucro líquido. Mostre em que passo o resultado virou.
- **"O que é tal indicador?"** Explique a fórmula e o significado. Não precisa consultar dados, a menos que a pessoa peça o valor de uma empresa.
- **Status Verificar.** Explique o que provavelmente está errado e o que conferir antes de interpretar o número.
- **Sem dados para o período ou empresa pedidos.** Diga isso. Não estime nem invente.
