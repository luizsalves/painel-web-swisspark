# Conciliação contábil

## O que é

Conciliar uma conta é mostrar que o saldo dela no razão está certo, explicando o que compõe esse saldo. Para cada empresa, filial, conta e mês, a conciliação responde:

- qual era o saldo anterior;
- quais débitos e créditos formaram o movimento do mês;
- qual é o saldo atual, o saldo do razão;
- quais lançamentos e documentos compõem esse saldo, como notas e títulos em aberto, pagamentos e adiantamentos;
- se a conta foi preparada e conferida, se tem pendência e se o fechamento foi finalizado.

## Saldo do razão, composição e diferença

- **Saldo do razão:** saldo anterior + débitos + créditos do mês. O sinal segue a natureza da conta: nas contas de ativo, o débito soma; nas demais, o crédito soma.
- **Composição do saldo:** o que explica o saldo, item por item. Por exemplo, notas de fornecedores ainda não pagas ou adiantamentos ainda sem nota.
- **Diferença:** saldo do razão − composição. Diferença zero quer dizer que o saldo está explicado. Diferença diferente de zero pede investigação: lançamento a mais ou a menos, documento sem vínculo ou conta classificada errado. [CONFIRMAR]

## Como era feito antes

Antes do painel, uma rotina automática copiava os dados do TOTVS para outro banco, montava o razão e a composição e entregava tudo numa planilha de Excel, com o controle de preparado, conferido, pendência e prioridade. A composição cobria principalmente:

- **fornecedores de materiais e serviços:** notas lançadas menos os pagamentos;
- **adiantamentos:** títulos de adiantamento baixados menos as notas vinculadas a eles.

A rotina também guardava uma fotografia dos lançamentos para apontar o que sumiu ou mudou entre uma execução e outra.

## Como é no painel hoje

- O painel lê o TOTVS direto, só para consulta, e calcula o saldo do razão de cada conta no Fechamento Mensal.
- O controle Pendente → Preparado → Conferido → Finalizado fica no próprio painel, com quem fez cada etapa e quando. Cada etapa pode ser revertida.
- A composição de fornecedores e adiantamentos já tem cálculo no painel, com a mesma lógica da rotina antiga, mas ainda não aparece em nenhuma tela. No Fechamento, a composição aparece igual ao saldo do razão e a diferença aparece zerada. **Não trate essa diferença zero como prova de que a conta está conciliada.**
- Nas empresas de filiais consolidadas, a conciliação é da empresa inteira, e não de cada filial.
- O Excel do fechamento traz o analítico do ano até o mês fechado, com o saldo anterior de cada mês.
