# Tati Analista — Fase 1: teste manual (Apêndice A)

Data: 27/10/2026. Branch `feature/tati-modulos`, instruções v3, 9 ferramentas, limites de 6 idas ao modelo e 60 s (90 s na 3ª e na 4ª rodada, por decisão do Luiz).

## Como foi feito

- **Caminho:** o mesmo código do `POST /api/agente-contabil/chat/filtros` (`HarnessService.ExecutarComFiltrosAsync`), chamado por um console fora do repositório, com a configuração da API (appsettings + keys.json) e a DeepSeek de verdade. Só a camada HTTP e o login ficaram de fora — escolha do Luiz, para não precisar de token.
- **Contexto da tela:** setembro/2026; empresa AEPL PARTICIPACOES LTDA (coligada 17) nas perguntas da AEPL, AGV CAMPINAS EMPREENDIMENTOS LTDA (coligada 1) nas da AGV e "todas" nas de produtividade.
- **"Valor na tela":** a saída das próprias ferramentas, rodadas fora da Tati, só para leitura, logo depois do teste. Não é print da tela. Ela prova que a Tati repetiu a ferramenta sem mudar número. Que a ferramenta bate com a tela vem de outro lugar: as ferramentas só repassam o que devolvem os services que a tela também chama (`IIndicadoresService`, `IBalanceteService`, `IBalancoService`), o que foi conferido no capítulo 1.2, e os relatórios batem entre si (ativo, passivo e disponível iguais no balancete, no balanço, na DFC e nos indicadores).
- O valor anterior dos indicadores usa a mesma base da coluna "período anterior" da tela (`/api/indicadores/mensal` + `combinarBases`): as 19 parcelas conferiram na AEPL (3 períodos, um deles atravessando o ano) e na AGV Campinas.

## Resultado

| # | Pergunta | Ferramenta chamada | Valor da Tati | Valor na tela | Bate? | Tempo |
|---|---|---|---|---|---|---|
| 1 | Liquidez corrente da AEPL em set/2026 | consultar_indicadores (`indicador`) | 1,20, Atenção; ago/2026 1,03 (+0,17, melhorou) | 1,20 Atenção; anterior 1,03 | sim | 7,1 s |
| 2 | Indicadores da AEPL em atenção ou crítico | consultar_indicadores | Risco: liquidez seca 0,29, imediata 0,00, dívida no curto prazo 99,60%, ROE -0,63%, ROA -0,18%, margem líquida -24,45%. Atenção: liquidez corrente 1,20, endividamento 71,95%, dívida líquida/EBITDA 3,38x. Verificar: margem bruta 100% | os mesmos 13 status e valores | sim (2ª rodada) | 7,3 s |
| 3 | Caixa/bancos no balancete da AGV em set/2026 | consultar_balancete (`conta` 1.1.1) | Disponível R$ 2.220.122,40 = caixa R$ 3.910,64 + bancos R$ 72.931,97 + aplicações R$ 2.143.279,79 | os mesmos | sim | 5,8 s |
| 4 | Balancete da AEPL | consultar_balancete | 47 linhas, sem corte; ativo R$ 31.268.796,29; lucros acumulados R$ 142.302,82 credor | 47 linhas, `truncado: false`; os mesmos saldos | sim, com ressalva (2ª rodada; ver Observações) | 6,4 s |
| 4b | Balancete da AGV Campinas (para testar o corte) | consultar_balancete | truncado: 50 de 52; ativo R$ 49.740.091,42; sugeriu filtrar por 1.1 ou 2.1 | 52 linhas, `truncado: true`; os mesmos saldos | sim | 9,8 s |
| 5 | Total do ativo e do passivo da AEPL em set/2026 | consultar_balanco | ativo R$ 31.268.796,29; passivo exigível R$ 22.498.208,84; PL R$ 8.770.587,45; passivo + PL = ativo | os mesmos; diferença 0,00 | sim | 4,0 s |
| 6 | Caixa operacional da AEPL na DFC de set/2026 | consultar_dfc | -R$ 9.616,74; caixa de R$ 55.685,47 para R$ 47.450,93; 10 contas fora do de-para | os mesmos | sim | 5,1 s |
| 7 | PL da AEPL × mês anterior | consultar_balanco | R$ 8.770.587,45 × R$ 8.827.428,74 (queda de R$ 56.841,29) | os mesmos | sim | 3,5 s |
| 8 | O que é dívida líquida/EBITDA? | nenhuma | conceito, casos especiais; "é só a explicação do conceito, não um dado da empresa" | — | sim (sem número) | 2,9 s |
| 9 | Faturamento da AEPL em 2015 | consultar_indicadores (jan–dez/2015) | receita líquida zero, "não é ausência de dado", com o ponto a verificar dos custos zerados | receita líquida 0; há saldos em dez/2015 | sim (2ª rodada) | 3,9 s |
| 9b | Faturamento da AEPL em 2014 (sem dados) | consultar_indicadores | "não há dados… não tenho o faturamento de 2014" | `semDados: true` | sim | 3,3 s |
| 10 | Produtividade da equipe este mês | consultar_status_backlog | perguntou qual ação (preparação, conferência ou fechamento) e deu o backlog: 18.566 contas, 3 fechadas | backlog 18.566, 3 fechadas | sim | 5,5 s |
| 10b | Quem mais fechou contas este mês | consultar_produtividade_time | a 1ª do ranking, com 4 contas (a única com fechamentos) | total 4, uma pessoa com 4 | sim | 3,2 s |

Extras: "Oi" → as 4 opções, a 4ª agora "Ver a saúde financeira e as demonstrações da empresa"; pergunta sobre futebol → recusa educada e as opções do painel; "total do ativo" sem empresa no contexto → pergunta qual empresa. Nenhuma das três chamou ferramenta.

## O que a 1ª rodada mostrou e o que mudou na v3

| # | Na 1ª rodada | Mudança na v3 |
|---|---|---|
| 2 | Sugeriu conferir o caixa porque a liquidez imediata deu 0,00 (R$ 47,5 mil ÷ R$ 22,4 mi é mesmo 0,002) | Só apontar problema no dado quando a ferramenta apontar; 0,00 arredondado não é erro |
| 4 | Chamou de "prejuízos acumulados" um saldo credor (que é lucro) e de "resultado do mês" o saldo das contas de resultado (que é acumulado); calculou de cabeça a diferença ativo − passivo | Sinal de Lucros/Prejuízos Acumulados; contas de resultado acumuladas desde o encerramento; não somar nem subtrair valores das ferramentas para criar número novo |
| 9 | Disse "não tenho esse dado" e, na mesma resposta, que a receita foi zero | Sem `semDados`, zero é dado: informar o zero |

As perguntas 2, 4, 4b e 9 foram refeitas depois da mudança; as demais não dependiam dela.

## 3ª rodada: decisões do Luiz depois do relatório

Decisões (27/10/2026, aceitando as recomendações do relatório final):
- a Tati pode apontar, como leitura dela, um grupo com saldo de sinal contrário à natureza (ativo credor, passivo devedor);
- tempo limite de 90 s;
- PR #9 mesclado antes desta branch, e as 2 frases dele foram para a v3 (tela nova da Tati);
- corrigir na v3 o "movimento do mês" bruto e explicar o PL do balancete diferente do balanço.

| # | Pergunta | Ferramenta | O que mudou na resposta | Números | Tempo |
|---|---|---|---|---|---|
| 2 | Indicadores da AEPL em atenção ou crítico | consultar_indicadores | os mesmos status e valores; nenhum alarme que a ferramenta não deu | batem | 6,7 s |
| 4 | Balancete da AEPL | consultar_balancete | aponta o 1.2.2 Investimentos credor como "observação de leitura (não é alerta da ferramenta)"; explica que o PL do balancete difere do balanço porque o resultado ainda está nas contas 3 em diante; não chama mais de "movimento do mês" o crédito bruto | batem (conferidas também as linhas 1.2.1, 2.2, 3.1.1, 3.1.2, 5.4, 6 e 7) | 7,7 s |
| 4b | Balancete da AGV Campinas | consultar_balancete | truncado (50 de 52); a mesma explicação do PL; disse "os débitos e créditos acima são brutos" sem ter mostrado débitos e créditos, e "mostrei as 50 contas" mostrando cerca de 20 grupos (corrigido na 4ª rodada) | batem | 33,8 s (28,4 s só na consulta ao banco) |

## 4ª rodada: correção da v3 depois da revisão do árbitro

A v3 da 3ª rodada dizia que o PL do balancete é sempre menor que o do balanço, o que só vale com lucro no ano; a AEPL, com prejuízo, tem o PL do balancete maior (R$ 9.723.544,33 × R$ 8.770.587,45 no balanço). A v3 passou a dizer: menor com lucro, maior com prejuízo, explicando sem calcular a diferença; contas redutoras e Lucros/Prejuízos Acumulados não contam como "sinal contrário"; a diferença do PL não pede conferência; débitos e créditos só são explicados quando citados; resumo é avisado.

| # | Pergunta | Ferramenta | Resposta | Números | Tempo |
|---|---|---|---|---|---|
| 4 | Balancete da AEPL | consultar_balancete | resumo avisado ("até o nível 3, 47 linhas, sem corte"); 1.2.2 Investimentos credor apontado como leitura dela; PL do balancete explicado sem conta e sem pedir conferência | batem | 7,9 s |
| 4b | Balancete da AGV Campinas | consultar_balancete | "a ferramenta trouxe as 50 contas de maior saldo de um total de 52 — o que segue é um resumo"; tabela com saldo anterior, débitos, créditos e saldo atual; débitos e créditos ditos brutos | batem (conferidas as linhas 1, 1.1.1, 1.1.2, 1.2, 2.1, 2.2, 2.3, 3, 5 e 8) | 46,8 s (39,9 s só na consulta ao banco) |
| — | Por que o PL do balancete da AEPL é diferente do PL do balanço? | nenhuma | conceito: o resultado não encerrado fica nas contas 3 em diante; menor com lucro, maior com prejuízo; diferença normal; oferece trazer os dois valores | sem número | 2,5 s |

## Observações

- Pergunta 4 do Apêndice A ("espera truncado"): com o padrão até o nível 3, a AEPL tem 47 linhas e não corta; o corte foi testado com a AGV (4b) e nos testes unitários.
- Pergunta 9 do Apêndice A ("espera não tenho esse dado"): a AEPL Participações tem saldos em dez/2015 e receita zero no ano, então a resposta certa é "zero". Sem dado nenhum, a pergunta 9b (2014).
- Tempos: na 1ª e na 2ª rodada, de 1,5 s a 9,8 s; na 3ª e na 4ª, o banco estava lento e o balancete da AGV levou 33,8 s e 46,8 s (28,4 s e 39,9 s só na consulta). Em medições fora da Tati, a DFC da AGV levou até 44,6 s e uma consulta de indicadores da AGV, 65 s. Por isso o limite passou de 60 s para 90 s (`AgenteContabil:TempoLimiteSegundos`).
- Em 2026-09 todos os lançamentos da coligada 1 estão na filial 1; as filiais das SCPs não têm movimento próprio.
- Pergunta 4, 2ª rodada: os números conferiam, mas a Tati apontou por conta própria o grupo 1.2.2 Investimentos com saldo credor, o que a regra da v3 ainda não permitia, e citou como "movimento do mês" da receita o crédito bruto (R$ 238.085,24; a receita líquida do mês, da `consultar_indicadores`, desconta os débitos de R$ 13.568,66). As duas coisas foram resolvidas na 3ª rodada, acima.
- Ajustes de texto para a próxima versão das instruções (não mudam número):
  - na 4ª rodada, a Tati escreveu para o usuário "use `consultar_balanco`" (nome interno da ferramenta) em vez de "o balanço";
  - ao apontar o 1.2.2 Investimentos credor, arriscou causas ("pode ser baixa de investimento ou lançamento na conta errada"); a v3 só autoriza sugerir conferir com a contabilidade;
  - na 4b, citou o saldo credor da receita (R$ 12.009.602,70) ao falar do resultado do ano, o que pode ser lido como se fosse o resultado.
- Cada pergunta de cada rodada foi feita uma vez; a resposta do modelo varia de uma vez para outra.
- Pendência para quando o front puder mudar: a barra lateral da tela nova (PR #9) manda "Ver a saúde financeira da empresa" como 4º assunto, e a v3 oferece "Ver a saúde financeira e as demonstrações da empresa". As duas funcionam; só o texto difere.
- Perguntas 10 e 10b: "3 fechadas" no backlog e "4 contas" na produtividade não se contradizem. O backlog conta quantas contas estão hoje com status Fechado; a produtividade conta as ações de fechamento registradas no mês. São medidas diferentes, e a Tati não deve somar nem comparar uma com a outra.
- As saídas de referência (inclusive backlog, produtividade e a conferência das 19 parcelas) ficaram na pasta da sessão, fora do repositório.
