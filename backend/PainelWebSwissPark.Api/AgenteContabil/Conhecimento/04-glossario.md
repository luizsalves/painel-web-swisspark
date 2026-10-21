# Glossário

Termos que aparecem nas telas e nas respostas. O que está marcado com [CONFIRMAR] foi deduzido e ainda precisa de confirmação.

## Empresa e conta

- **TOTVS RM:** o sistema de gestão de onde vêm os dados contábeis. O painel só lê, nunca grava nele.
- **Coligada:** cada empresa do grupo no TOTVS, identificada por um código.
- **Filial:** um estabelecimento da coligada. Uma empresa pode ter várias filiais.
- **SCP:** sociedade em conta de participação. No TOTVS, aparece como uma filial da coligada. [CONFIRMAR]
- **Empresa:** no painel, o nome da coligada como está no TOTVS (a razão social). O mesmo nome pode aparecer em mais de uma coligada.
- **Filiais consolidadas:** empresas que fecham com todas as filiais juntas. O fechamento delas vale para a empresa inteira.
- **Plano de contas:** a lista de contas contábeis. Existe um plano padrão, comum ao grupo; uma coligada pode ter descrições próprias para as contas.
- **Conta contábil:** o código de uma conta do plano, como 1.1.1.01.0001. O primeiro dígito dá o grupo: 1 é ativo, 2 é passivo e patrimônio líquido, e de 3 a 8 são contas de resultado.
- **Reduzido:** o código curto da conta no TOTVS.
- **De-para:** a regra fixa que diz em que linha do balanço, da DRE ou da DFC entra cada conta. Fica no painel, sem tela de edição; ajustes são pedidos ao time do painel.

## Período e lançamentos

- **Período:** um mês ou um intervalo de meses dentro do mesmo ano.
- **Competência:** o mês a que o lançamento pertence. No painel, é o mês escolhido nos filtros. [CONFIRMAR]
- **Lançamento e partida:** o registro de um débito ou de um crédito numa conta. O IdPartida identifica a partida no TOTVS.
- **Débito e crédito:** os dois lados de um lançamento. No painel, o valor aparece com o sinal da natureza da conta: nas contas de ativo, o débito soma; nas demais, o crédito soma.
- **Razão:** a lista de lançamentos de uma conta.
- **Saldo anterior:** o saldo acumulado da conta antes do mês.
- **Saldo do razão:** saldo anterior + débitos + créditos do período.
- **Composição do saldo:** o que explica o saldo da conta, item por item, como notas e títulos em aberto.
- **Diferença:** saldo do razão − composição do saldo.
- **Apuração do resultado:** os lançamentos de encerramento que zeram as contas de resultado contra o patrimônio líquido. Os Indicadores e a DFC não os contam no resultado do período; no Balancete e no razão do Fechamento eles aparecem normalmente.

## Fechamento

- **Fechamento:** o trabalho mensal de conferir e encerrar cada conta: preparar, conferir e realizar o fechamento.
- **Pendente:** nenhuma ação registrada na conta no período.
- **Preparado:** alguém preparou a conta e registrou os saldos.
- **Conferido:** um Gerente ou Administrador revisou a preparação.
- **Finalizado:** o fechamento foi realizado. No banco, o status se chama Fechado.
- **Reverter:** desfaz a etapa atual e volta um passo.
- **Pendência:** algo a resolver antes de fechar a conta. No Fechamento, o "Possui pendência?" não é salvo: vale só para a tela e para o PDF e o Excel do mês aberto. A aba Pendências ainda não está ligada aos dados.
- **Backlog:** os fechamentos esperados no período: cada conta ativa de cada filial ativa, uma vez por mês. Nas empresas de filiais consolidadas, a conta entra uma vez pela empresa inteira.
- **Produtividade (tela):** o backlog e o andamento do fechamento no período.
- **Produtividade do time:** quantas contas cada pessoa preparou, conferiu ou finalizou. Nenhuma tela mostra esse ranking; só a Tati o consulta.

## Relatórios

- **Balancete:** os saldos e os movimentos das contas num período.
- **Balanço patrimonial:** o ativo, o passivo e o patrimônio líquido no último dia do período.
- **DRE:** a demonstração do resultado: receitas, custos, despesas e lucro do período.
- **DFC:** a demonstração do fluxo de caixa pelo método indireto: de onde veio e para onde foi o caixa nas atividades operacionais, de investimento e de financiamento.
- **Variações não classificadas:** a parte da variação de caixa que vem de contas fora do de-para da DFC.

## Indicadores

- **Disponível:** caixa e equivalentes de caixa.
- **Dívida bruta:** empréstimos e financiamentos de curto e longo prazo.
- **Dívida líquida:** dívida bruta − disponível.
- **EBIT:** o resultado operacional, antes do resultado financeiro, do IR e da CS e sem a equivalência patrimonial.
- **EBITDA:** o EBIT mais a depreciação e a amortização.
- **Resultado antes do IR:** o lucro antes do imposto de renda e da contribuição social.
- **IR e CS:** imposto de renda e contribuição social sobre o lucro.
- **Equivalência patrimonial:** o resultado que vem das participações em outras empresas. [CONFIRMAR]
- **Anualizado:** o valor do período × 12 ÷ número de meses.
- **p.p. (pontos percentuais):** a diferença entre dois percentuais. De 7,6% para 7,4% é uma queda de 0,2 p.p.
- **Faixa:** os limites que separam Bom, Atenção e Risco em cada indicador.
- **Pilar:** um dos quatro grandes temas da visão da diretoria: Resultado, Caixa, Dívida e Contas de curto prazo.
