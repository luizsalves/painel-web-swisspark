# Testes da CFO Tati — fase 4

Rodados em 21/10/2026 com as instruções v2 e a base de conhecimento (4 arquivos, 22.856 caracteres).

A rodada completa usou a v2 de antes da correção da regra de filial (veja o 3b). Depois da correção, foram refeitas as perguntas 2, 3 e 3b. Elas foram refeitas de novo quando a descrição do `codFilial` na `consultar_indicadores` ficou igual à regra nova. Nenhuma das outras perguntas tinha filial no filtro, então a correção não as afeta.

## Como foi rodado

- **Código testado.** As perguntas passaram pelo mesmo código do `POST /api/agente-contabil/chat/filtros`, o `HarnessService.ExecutarComFiltrosAsync`. Ele rodou num console local, com a configuração da API, contra os bancos e o modelo reais. Ficou de fora só a camada HTTP com o login de superadministrador, porque não havia token.
- **Ferramenta chamada.** Saiu do log do harness (`chamando ferramenta …`).
- **Contexto da tela.** O período foi sempre 2026-03. Nas perguntas sobre a AEPL, a empresa do filtro era a AEPL PARTICIPACOES LTDA (coligada 17), com a filial "Todas". "AEPL" sozinho combina com quatro empresas do cadastro (teste 2b), e os valores esperados na tabela do prompt são os dessa empresa. Nas outras perguntas, o filtro estava em "todas".
- **Conversa.** Cada pergunta foi uma conversa nova, sem histórico.

## Resultado

| # | Pergunta | Ferramenta | Resumo da resposta | Resultado |
|---|---|---|---|---|
| 1 | Oi | nenhuma | Saudação com as 4 opções, nesta ordem: produtividade, conciliação, fechamento de uma conta e "Ver a saúde financeira da empresa". Não apareceu uma 5ª opção. | ok |
| 2 | Como está a saúde financeira da AEPL em março de 2026? | consultar_indicadores | Começa pela empresa, o período e a abrangência (todas as filiais da coligada). Depois vêm o veredito Risco e os 4 pilares: Caixa Bom, Dívida Atenção (3,5x o EBITDA), Contas de curto prazo Atenção (1,02) e Resultado Risco. Por fim, o que pesa, com os 3 alertas, e os 2 pontos a verificar. Os números conferem com o JSON da ferramenta. | ok |
| 3 | Qual a liquidez corrente da AEPL em março de 2026? | consultar_indicadores | 1,02, Atenção, com as faixas da ferramenta (Risco abaixo de 1,0, Bom a partir de 1,5). Explica a dependência dos estoques (82% do ativo circulante, seca 0,19). | ok |
| 4 | O que é liquidez seca? | nenhuma | Explica a fórmula, a direção e o aviso de estoques acima da metade do ativo circulante, tudo da base. Uma das opções ("a empresa mais crítica do período") é algo que nenhuma ferramenta faz. | ok, com ressalva |
| 5 | Por que a margem bruta da AEPL está em 100%? | consultar_indicadores | Status Verificar: custos zerados no mês, com R$ 20,39 mi em estoque. Diz que é custo não classificado e manda conferir o de-para; o ajuste se pede ao time do painel. Uma opção ("sem o efeito do custo zerado") não é possível com as ferramentas. | ok, com ressalva |
| 6 | Por que a AEPL teve prejuízo em março se a operação foi bem? | consultar_indicadores | Mostra a cascata: receita, custos zerados, despesas, EBITDA, depreciação e resultado financeiro. O resultado antes do IR é positivo (R$ 1,6 mil), e o IR e a CS (R$ 77,3 mil) levam ao prejuízo de R$ 75,7 mil. | ok |
| 7 | Qual o saldo da conta 1.1.1.02.01.0002 da AEPL em março? | consultar_saldo | Traz o saldo inicial, os débitos, os créditos e o saldo final da conta (filial 1). Os débitos, os créditos e o saldo final batem com o Balancete de 2026-03, em valor absoluto: a ferramenta antiga mostra os créditos com sinal negativo. Não chamou a consultar_indicadores. | ok |
| 8 | A dívida da AEPL está alta? | consultar_indicadores | Dívida líquida / EBITDA de 3,52x, Atenção (faixa entre 2,5x e 4x), explicada como cerca de 3,5 anos de geração de caixa. Dívida bruta / PL de 0,47x (Bom). O problema apontado é o perfil: 99,52% do passivo vence no curto prazo (Risco). | ok |
| 9 | Por que o PCT sumiu da tela? | nenhuma | Saiu das duas visões porque é igual ao endividamento geral. Continua na aba "Índices financeiros". | ok |
| 10 | O que significa o status Verificar? | nenhuma | Problema no dado, ou indicador que não dá para calcular. Cita os casos da base e a orientação: conferir com a contabilidade e pedir o ajuste do de-para ao time do painel. Incluiu "valor na DFC sem classificação" entre os casos de Verificar; na base, isso é um ponto a verificar e um alerta de informação, não um status. | ok |
| 11 | Como está a produtividade do time? | nenhuma | Pergunta se é preparação, conferência ou fechamento, com as três opções, como manda a regra. A ferramenta foi testada no 11b. | ok |
| 12 | Qual o backlog da AEPL? | consultar_status_backlog | A ferramenta antiga responde: 215 contas em 2026-03, todas pendentes, explicadas com a frase de "Pendente". | ok |
| 13 | Qual a liquidez corrente da AEPL em março de 2030? | consultar_indicadores | Diz que não há dados para 2030-03 e não projeta. Diz também que "o período mais recente que posso consultar é o da tela", o que está errado: a ferramenta aceita até o mês corrente. | ok, com ressalva |
| 14 | Qual o ROE da Empresa Inexistente? | consultar_indicadores | Diz que não encontrou a empresa e pede a razão social. Uma das opções inventa o nome "SwissPark", o que a regra das opções proíbe. | ok, com ressalva |
| 15 | Me explica a tela de Fechamento Mensal | nenhuma | Descreve, a partir do 01-painel.md: abas, ações, perfis, o "Possui pendência?" que não é salvo, as Observações que não são salvas e a composição que não aparece na tela (diferença sempre 0). | ok |
| 16 | O que é de-para? | nenhuma | Definição do glossário. Explica que não tem tela de edição e dá a orientação da contabilidade e do time do painel. | ok |
| 17 | A margem foi de 7,6% para 7,4%. Caiu quanto? | nenhuma | 0,2 p.p. Cita também a queda relativa (cerca de 2,6%). | ok |
| 18 | Resume para o diretor a situação da AEPL em março, em três frases | consultar_indicadores | Três frases em linguagem de negócio, sem fórmula: caixa e EBITDA, prejuízo pelo IR e CS, perfil da dívida e os dois pontos a conferir. | ok |
| 19 | Quanto a AEPL vai faturar no ano que vem? | nenhuma | Diz que não faz projeções e oferece os dados realizados. | ok |

### Testes extras, das ressalvas do árbitro na fase 3

| # | Pergunta e contexto | Ferramenta | Resumo da resposta | Resultado |
|---|---|---|---|---|
| 2b | A pergunta 2 com o filtro de empresa em "todas" | consultar_indicadores | A ferramenta devolveu as quatro empresas que contêm "AEPL", e a Tati ofereceu as quatro como opções, com os nomes vindos da ferramenta. | ok |
| 3b | "Qual a liquidez corrente da empresa?", com a filial 1 no filtro | consultar_indicadores | **1ª rodada:** ignorou a filial do filtro e consultou todas as filiais. Disse isso na resposta, mas não seguiu o filtro. **Correção:** na v2, a filial passou a ser a que o usuário pedir ou, se ele não disser, a do contexto da tela. **2ª e 3ª rodadas:** "filial 1, março/2026", 1,02, Atenção. A 3ª rodada já tinha a descrição do `codFilial` alinhada e pôs um "R$" indevido no título. Nas duas rodadas, as perguntas 2 e 3 foram refeitas e não mudaram. | falhou → corrigido → ok |
| 11b | Quem mais fechou contas em março de 2026? | consultar_produtividade_time | Ranking com 1 usuário e 1 conta fechada, sem gráfico (não foi pedido) e sem oferta de resumo para a diretoria. | ok |

**Placar:** 19 de 19 perguntas ok, quatro delas com ressalva. Nos testes extras, o 3b falhou e passou depois da correção.

## Ressalvas para acompanhar

- **Opções fora do alcance.** Nas perguntas 4, 5 e 14, uma opção ofereceu algo que as ferramentas não fazem ou inventou um nome de empresa. As duas coisas já são proibidas pelas instruções (v1 e v2), então é desobediência pontual do modelo, não falta de regra.
- **Período mais recente.** Na pergunta 13, a Tati afirmou que o período mais recente é o da tela, o que está errado. Nenhuma ferramenta diz qual é o último mês com dados.
- **Filial que não existe.** Na revisão, o árbitro pôs no filtro uma filial que a AEPL PARTICIPACOES não tem. A ferramenta avisou que a filial não existe. A Tati trocou sozinha para a filial 1 e respondeu sem contar isso ao usuário. O filtro da tela só oferece filiais existentes, então o caso é raro.
- **Opções inventadas também na revisão.** Num teste do árbitro com a AEPL AGROPECUARIA, uma opção citou uma conta que nenhuma ferramenta trouxe.
- **Gráfico.** Nenhuma resposta sobre indicadores trouxe gráfico. Na fase 2, ainda com a v1, a Tati tinha inventado notas num gráfico de pilares.
