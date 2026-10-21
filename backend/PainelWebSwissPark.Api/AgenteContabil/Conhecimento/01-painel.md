# O painel

O Painel SwissPark é o sistema de conciliação contábil do grupo. Ele lê os lançamentos do TOTVS RM, só para consulta, e organiza o fechamento mensal das contas de cada empresa e filial, os relatórios contábeis e os indicadores financeiros. Cada pessoa vê só as telas dos módulos e perfis que tem.

## Filtros

O botão **Filtros**, no alto das telas, abre uma gaveta com empresa, filial, conta contábil, ano, mês ou período de meses e status do fechamento. Os filtros valem para todas as telas: cada tela usa os que fazem sentido para ela e avisa quando um filtro não se aplica.

Algumas empresas fecham com todas as filiais juntas (filiais consolidadas). Nelas, no Fechamento e na Produtividade, a filial escolhida não vale, e a gaveta avisa isso.

## Perfis

- **Analista:** prepara e realiza o fechamento das contas.
- **Gerente e Administrador:** fazem o mesmo e também conferem. O Administrador cuida dos usuários dos módulos que administra.
- **Superadministrador:** acesso a tudo, inclusive à CFO Tati.

## Produtividade

- **Para que serve:** acompanhar o andamento do fechamento no período.
- **O que se vê:** o backlog (fechamentos esperados), quantos estão preparados, conferidos, pendentes e finalizados, a meta e o andamento por empresa, e as listas "Resta preparar" e "A conferir", que abrem as contas de cada empresa. Num período de vários meses, cada conta conta uma vez por mês.
- **Quem usa:** quem tem o módulo de conciliação.

## Fechamento Mensal

- **Para que serve:** conciliar e fechar uma conta contábil de uma empresa e filial num mês.
- **O que se vê:** o saldo anterior, o saldo do razão, a composição do saldo e a diferença; o "Possui pendência?", que não é salvo e começa marcado como SIM; o status atual e o responsável; e o fluxo Pendente → Preparado → Conferido → Finalizado, com quem fez cada etapa e quando. A composição do saldo ainda não é mostrada nesta tela: ela aparece igual ao saldo do razão, e a diferença aparece zerada.
- **Abas:**
  - **Lançamentos:** as partidas da conta no mês (data, IdPartida, histórico, valor e se é débito ou crédito), com filtros.
  - **Pendências:** ainda mostra exemplos fixos; não está ligada aos dados.
  - **Observações:** o texto digitado fica só na tela e não é salvo.
  - **Histórico:** as partidas da conta mês a mês, em até 12 meses contando o do fechamento, com saldo anterior, débitos, créditos e saldo de cada mês, e exportação para Excel.
- **Ações:**
  - Preparar, Conferir, Realizar fechamento e Reverter (desfaz a etapa atual).
  - Gerar Excel: de janeiro até o mês do fechamento, com uma aba RAZAO com o analítico e uma aba por mês.
  - Gerar PDF: só o mês aberto.
- **Quem usa:** quem tem o módulo de conciliação. O Analista prepara e fecha; Gerente e Administrador também conferem. Para reverter, é preciso o perfil que faria a etapa desfeita.

## Balancete

- **Para que serve:** consultar saldos e demonstrações contábeis.
- **O que se vê:** três abas. Balancete Resumo, com saldos e movimentos por empresa, filial, conta e período. Balanço, com o balanço patrimonial no fim do período. Balanço DFC, com a demonstração do fluxo de caixa pelo método indireto. As duas últimas exportam para PDF e Excel.
- **Quem usa:** qualquer pessoa com acesso ao painel.

## Indicadores

- **Para que serve:** ver a saúde financeira da empresa: liquidez, endividamento, rentabilidade e resultado, calculados dos saldos contábeis.
- **O que se vê:** três abas (Índices financeiros, Visão do contador e Visão da diretoria), explicadas no arquivo de indicadores. Usa os filtros de empresa, filial e período.
- **Quem usa:** qualquer pessoa com acesso ao painel.

## Empresas

- **Para que serve:** consultar as empresas e filiais do TOTVS que entram na conciliação.
- **O que se vê:** a lista de empresas e filiais, com a opção de desativar ou reativar cada uma no painel. Empresa desativada sai do backlog da Produtividade e da CFO Tati.
- **Quem usa:** quem tem o módulo de conciliação.

## Contas Contábeis

- **Para que serve:** escolher quais contas entram na conciliação.
- **O que se vê:** as contas com lançamentos, com a opção de desativar ou reativar cada uma. Conta desativada sai do backlog da Produtividade e da CFO Tati. Esta tela não muda a classificação das contas (o de-para).
- **Quem usa:** quem tem o módulo de conciliação.

## Usuários

- **Para que serve:** controlar quem entra no painel.
- **O que se vê:** as pessoas cadastradas, os módulos de cada uma, o perfil em cada módulo (Analista, Gerente ou Administrador), se estão ativas e a redefinição de senha.
- **Quem usa:** administradores, nos módulos que administram, e o superadministrador.

## CFO Tati • IA

- **Para que serve:** conversar com a Tati, a assistente de IA do painel.
- **O que se vê:** os mesmos filtros da Produtividade, a área dos gráficos que a Tati cria, em cima, e o chat, embaixo. Os gráficos da Tati são do backlog e da produtividade do fechamento, e não os indicadores financeiros da tela Indicadores.
- **Quem usa:** o superadministrador.

## Entrada

Para entrar, a pessoa usa o login e a senha. Quando a senha é provisória, o painel pede uma nova antes de liberar as telas.

## Ainda não disponíveis

No menu aparecem Relatórios, Pendências, Configurações e Auditoria, mas desativados: essas telas ainda não existem.
