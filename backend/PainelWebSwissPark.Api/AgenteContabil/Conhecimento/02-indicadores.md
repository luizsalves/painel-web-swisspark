# Indicadores financeiros

Este arquivo explica os indicadores da tela **Indicadores**. Use-o para explicar conceitos, fórmulas e regras do painel.

**Valores, status e faixas de uma empresa vêm sempre da ferramenta `consultar_indicadores`.** Nunca cite um número que não veio de uma ferramenta. Se este arquivo e a ferramenta divergirem, vale a ferramenta.

## As visões

A tela tem três abas:

- **Índices financeiros.** É a aba que abre primeiro: o painel mais antigo, com os índices em cartões, a base de cálculo e a DFC. Ela ainda mostra o PCT e calcula a Dívida líquida / EBITDA com o EBITDA do próprio período, sem anualizar, por isso esse número pode ser bem diferente do da visão do contador.
- **Visão do contador.** É técnica e mostra todos os indicadores em três grupos (Liquidez, Endividamento, Rentabilidade e resultado), com fórmula, faixa, comparativos, minigráfico dos 12 meses e status. Tem ainda a base de cálculo, a DFC e a matriz dos últimos 12 meses.
- **Visão da diretoria.** Resume a saúde da empresa em quatro pilares (Resultado, Caixa, Dívida e Contas de curto prazo), com um veredito geral e a lista "O que exige atenção". Não tem fórmulas e usa a linguagem do negócio.

Adapte a resposta a quem pergunta. Para o contador, cite a fórmula e os componentes. Para o diretor, diga o que o número significa para o negócio, sem jargão.

## Status

| Status | Significado |
|---|---|
| Bom | Dentro da faixa saudável |
| Atenção | Entre o limite de risco e o limite de bom |
| Risco | Fora da faixa saudável |
| Verificar | O número provavelmente está distorcido por um problema no dado, ou não dá para calcular (por exemplo, sem receita no período não existe margem). Não trate como bom nem como ruim: diga o que conferir |

Cada indicador tem uma **direção**: "maior é melhor" ou "menor é melhor". As faixas são as iniciais: estão fixas no painel, não dá para mudá-las pela tela, e a contabilidade ainda vai revisá-las. Use sempre as que a ferramenta devolver.

## Liquidez

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| Liquidez corrente | Ativo circulante ÷ Passivo circulante | Maior é melhor | Quanto o ativo de curto prazo cobre as contas que vencem em até 12 meses. Abaixo de 1, ele não cobre |
| Liquidez seca | (Ativo circulante − Estoques) ÷ Passivo circulante | Maior é melhor | O mesmo, sem os estoques, que podem demorar a virar dinheiro. Uma diferença grande entre a corrente e a seca indica dependência dos estoques. Quando os estoques passam da metade do ativo circulante, a tela avisa ao lado do nome |
| Liquidez imediata | Disponível ÷ Passivo circulante | Maior é melhor | Quanto do passivo de curto prazo o caixa e os equivalentes de caixa (o disponível) pagariam hoje |
| Liquidez geral | (Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante) | Maior é melhor | A capacidade de pagar todas as obrigações, de curto e de longo prazo |

## Endividamento

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| Endividamento geral | (Passivo circulante + Passivo não circulante) ÷ Ativo total | Menor é melhor | Quanto do ativo é financiado por terceiros |
| Dívida no curto prazo (CE) | Passivo circulante ÷ (Passivo circulante + Passivo não circulante) | Menor é melhor | Quanto das obrigações vence em até 12 meses |
| Dívida líquida / EBITDA 12m | (Dívida bruta − Disponível) ÷ EBITDA de 12 meses | Menor é melhor | Quantos anos de geração de caixa da operação pagariam a dívida líquida |
| Dívida bruta / PL | Empréstimos e financiamentos (curto e longo prazo) ÷ Patrimônio líquido | Menor é melhor | O peso da dívida financeira em relação ao capital dos sócios |

## Rentabilidade e resultado (no período)

| Indicador | Fórmula | Direção | O que mostra |
|---|---|---|---|
| ROE | Lucro líquido ÷ Patrimônio líquido | Maior é melhor | O retorno sobre o capital dos sócios |
| ROA | Lucro líquido ÷ Ativo total | Maior é melhor | O retorno sobre todo o ativo |
| Margem bruta | Lucro bruto ÷ Receita líquida | Maior é melhor | Quanto sobra da receita depois dos custos |
| Margem líquida | Lucro líquido ÷ Receita líquida | Maior é melhor | Quanto sobra da receita depois de tudo, inclusive dos impostos |
| EBITDA | EBIT + depreciação e amortização. O EBIT não inclui o resultado financeiro, o IR e a CS nem a equivalência patrimonial | Zero ou positivo é bom | A geração de caixa da operação, sem efeitos financeiros e contábeis |

ROE, ROA e as margens são do período escolhido. O status de ROE e ROA usa o valor **anualizado** (valor do período × 12 ÷ número de meses). Quando o período não tem 12 meses, a tela mostra "≈ X% ao ano" embaixo do valor.

## Casos especiais

- **PCT foi retirado das visões do contador e da diretoria.** A participação de capitais de terceiros é matematicamente igual ao endividamento geral, porque passivo + PL = ativo. Os dois davam sempre o mesmo valor. Ela ainda aparece na aba Índices financeiros.
- **Dívida líquida ≤ 0.** A empresa tem mais caixa do que dívida. O painel mostra "Caixa líquido" com status Bom, e não um múltiplo negativo.
- **EBITDA de 12 meses ≤ 0 com dívida líquida > 0.** O múltiplo perde o sentido. O painel mostra "EBITDA negativo" com status Risco.
- **Dívida bruta zero.** Dívida bruta / PL aparece como "Sem dívida", com status Bom.
- **Patrimônio líquido ≤ 0 com dívida.** Dívida bruta / PL aparece como "PL negativo", com status Risco, porque o múltiplo perde o sentido.
- **Custos zerados com estoque > 0.** A margem bruta fica com status Verificar. Uma margem de 100% nesse caso não é bom resultado: é sinal de custo não classificado. O que fazer está em "Pontos a verificar".
- **Prejuízo com resultado antes do IR positivo.** A margem líquida ganha a nota "IR e CS acima do resultado antes dos impostos": o prejuízo veio dos impostos, e não da operação.
- **EBITDA de 12 meses estimado.** O indicador anualiza o período: EBITDA do período × 12 ÷ número de meses. Num período de 12 meses, é o EBITDA real dos 12 meses. Diga isso ao explicar o número.

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
| Resultado | Lucro líquido do período | Margem líquida. Sem receita no período, o sinal do lucro: positivo é Bom, negativo é Risco, zero é Atenção |
| Caixa | Saldo de caixa no fim do período | Caixa das atividades operacionais (DFC): positivo é Bom, negativo é Risco, zero é Atenção |
| Dívida | Dívida líquida / EBITDA 12m, ou "Sem dívida líquida" | Dívida líquida / EBITDA 12m |
| Contas de curto prazo | Liquidez corrente | Liquidez corrente |

### Veredito

O status geral é **o pior status entre os quatro pilares**, nesta ordem de gravidade: Bom, Verificar, Atenção e Risco. Assim, o topo nunca mostra "Bom" com um risco escondido embaixo.

A frase do veredito começa pelos pilares bons, usa "mas" e termina com os pilares ruins, do menos grave ao mais grave. Exemplo: "A operação gera caixa, mas a dívida pede atenção e o período fechou com prejuízo."

### O que exige atenção

O painel mostra até cinco alertas, do mais grave ao menos grave, entre estes:

- **Dívida no curto prazo acima do limite de risco, com liquidez corrente abaixo do limite de bom** (Risco). O alerta diz quanto vence em até 12 meses e quanto o caixa cobre disso.
- **Prejuízo com resultado antes do IR positivo** (Atenção). O prejuízo veio do IR e da CS, e não da operação.
- **Prejuízo nos outros casos** (Risco).
- **Liquidez corrente abaixo de 1** (Risco). O ativo de curto prazo não cobre o passivo de curto prazo.
- **Dívida líquida / EBITDA 12m acima do limite de risco** (Risco). A dívida líquida equivale a muitos anos de geração de caixa.
- **Valor da DFC sem classificação** (Informação). Parte da variação de caixa ainda não tem classificação.

## Pontos a verificar

São problemas no dado, e não na empresa. Aparecem numa faixa no topo da visão do contador:

- custos zerados com estoque;
- valor na DFC sem classificação: contas com movimento que o de-para da DFC não cobre.

O que orientar: conferir com a contabilidade a classificação das contas. Se a conta estiver fora do de-para do painel, pedir o ajuste ao time do painel. O de-para fica no próprio painel e não tem tela de edição: a faixa tem o link "Revisar em Contas contábeis", mas essa tela só ativa e desativa contas.

## Como responder

- **"Como está a empresa X?"** Consulte os indicadores. Comece pelo veredito e pelos pilares e depois cite os alertas. Diga a empresa e o período.
- **"Por que deu prejuízo?"** Siga a cascata do resultado: receita, custos, despesas, EBITDA, depreciação, resultado financeiro, equivalência e outros (quando houver), IR e CS e lucro líquido. Mostre em que passo o resultado virou.
- **"O que é tal indicador?"** Explique a fórmula e o significado. Não precisa consultar dados, a menos que a pessoa peça o valor de uma empresa.
- **Status Verificar.** Explique o que provavelmente está errado e o que conferir antes de interpretar o número.
- **Sem dados para o período ou empresa pedidos.** Diga isso. Não estime nem invente.
