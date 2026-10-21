# Tati: base de conhecimento e ferramenta de indicadores

## Contexto

A Tati é a assistente do painel. Ela usa o DeepSeek e mora em `backend/PainelWebSwissPark.Api/AgenteContabil/`. Ela é formada por três partes:

- `Instrucoes/instrucoes-v1.md`: o que ela sabe e como se comporta;
- `Ferramentas/`: as consultas de dados que ela pode fazer (hoje são 5: saldo, fechamento de conta, backlog geral, backlog por empresa e produtividade);
- `HarnessService.cs` (e `ChatEndpoint.cs`): monta o prompt, registra as ferramentas e conversa com o modelo.

Hoje ela só responde sobre produtividade por três motivos:

1. a instrução diz que tudo gira em torno dos assuntos atuais;
2. ela não tem ferramenta de indicadores;
3. a instrução manda responder "não tenho essa informação" para dados de outros módulos.

**Objetivo:** fazer a Tati conhecer o painel e as regras dos indicadores, e conseguir consultar os indicadores de uma empresa. São três mudanças:

1. uma **base de conhecimento** em arquivos .md, carregada junto com as instruções;
2. uma **ferramenta `ConsultarIndicadores`**;
3. um **ajuste nas instruções**, liberando explicações de conceitos.

O rascunho do principal arquivo de conhecimento está em `docs/tati/02-indicadores.md`.

---

## Regras de trabalho

1. Trabalhe numa branch nova, `feature/tati-conhecimento`, criada a partir da branch em que está o código dos Indicadores (hoje `feat/indicadores-duas-visoes`). Os passos estão em "Preparação".
2. Não faça commit sem eu pedir.
3. Ao fim de cada fase, chame o árbitro e siga o que está em "Revisão pelo árbitro", abaixo.
4. Na fase 0, só leia. Fora a preparação, não edite nada antes de o árbitro aprovar o plano.
5. Siga os padrões do projeto: Controller → Service → Interface, injeção de dependência e o mesmo formato das ferramentas existentes (o modelo é `ConsultarSaldo`). Não adicione dependências.
6. Não mude o comportamento da tela de Indicadores nem das 5 ferramentas existentes.
7. Não invente regra de negócio. Se o rascunho divergir do código, me mostre a divergência e pergunte.
8. Não coloque credenciais, nomes de pessoas nem dados de clientes nos arquivos de conhecimento.

---

## Revisão pelo árbitro

O árbitro é um subagente revisor, definido em `.claude/agents/arbitro-tati.md`. Ele confere cada fase com independência e devolve um veredito. Ao fim de cada fase:

1. **Chame o subagente `arbitro-tati`** passando o número da fase, o hash do commit de partida (anotado na Preparação), a lista de arquivos alterados e um resumo do que você fez. Se o subagente não aparecer nesta sessão (acontece quando a pasta `.claude/agents` acabou de ser criada), chame um subagente `general-purpose` e passe como instruções o conteúdo inteiro de `.claude/agents/arbitro-tati.md`, deixando claro que ele não pode editar arquivos.
2. **REPROVADO:** corrija os bloqueantes e chame o árbitro de novo. No máximo 3 rodadas. Se ainda estiver reprovado, pare e me mostre o último veredito.
3. **APROVADO ou APROVADO COM RESSALVAS, sem "Decisões para o usuário":** siga direto para a próxima fase, sem esperar por mim. Antes de seguir, me mostre só o veredito e o resumo de 3 linhas do árbitro.
4. **Com "Decisões para o usuário":** pare e me faça as perguntas, com as opções. Siga depois que eu responder.
5. **Fim da fase 4:** pare sempre e me mostre o relatório final e o último veredito.
6. Não discuta com o árbitro nem o convença a mudar o veredito. Se discordar de um bloqueante, me mostre os dois lados e pare.

---

## Preparação

Faça estes passos, nesta ordem, antes da fase 0:

1. **Confira o git** com `git status --short --branch`.
   - A branch de partida deve ser a que contém as duas visões dos Indicadores (hoje `feat/indicadores-duas-visoes`). Se você estiver em outra, pare e me pergunte.
   - Quando escrevi este prompt, havia muito trabalho dos Indicadores sem commit nessa branch, inclusive em `HarnessService.cs` e `ChatEndpoint.cs`, que este trabalho também vai alterar. Se começarmos por cima disso, as mudanças se misturam e o árbitro não consegue separar o que é de cada trabalho.
   - Se houver qualquer arquivo pendente (modificado ou novo) além de `docs/prompt-tati-conhecimento.md`, `docs/tati/` e `.claude/`, **pare e me avise antes de fazer qualquer outra coisa**. Diga quantos arquivos são, liste-os em poucas linhas e me pergunte se pode fazer um **commit local de salvamento** de tudo na branch atual, sem `push`, com a mensagem "WIP: indicadores duas visões e documentos da Tati". Antes de perguntar, confira a lista: se algum arquivo parecer credencial, chave, `.env`, senha, dump de banco ou dado de cliente, avise, porque ele não deve entrar no commit. Só faça o commit depois do meu "sim".
   - Se não houver nada pendente (ou depois do commit), siga para o passo 2.
2. **Crie a branch** `feature/tati-conhecimento` a partir da branch atual: `git switch -c feature/tati-conhecimento`.
3. **Registre a linha de base.** Anote o hash do commit de partida (`git rev-parse HEAD`). Rode `dotnet build` e, se houver testes, `dotnet test` no backend e anote o resultado. Assim o árbitro separa o que já estava quebrado do que este trabalho quebrar. Se o build já falhar antes de você alterar qualquer coisa, pare e me mostre o erro.
4. **Copie** `docs/tati/arbitro-tati.md` para `.claude/agents/arbitro-tati.md` na raiz do projeto, criando as pastas se não existirem. Não altere o conteúdo.

---

## Fase 0: investigação (só leitura)

Leia e me explique:

1. **`HarnessService.cs`:** como monta o prompt de sistema, quando lê o `instrucoes-v1.md`, como registra as ferramentas (construtor, lista `ferramentas`, `switch` em `ExecutarFerramentaAsync`) e como envia as definições das ferramentas ao DeepSeek. Diga também se existe algo que muda a cada requisição (data, hora, nome do usuário) **antes** das instruções no prompt.
2. **`ChatEndpoint.cs`.**
3. **As 5 ferramentas**, em especial o `ConsultarSaldo` e o jeito como elas convertem o nome da empresa em coligada e filial.
4. **O `instrucoes-v1.md` inteiro**, incluindo a saudação e a regra das opções que viram botões.
5. **`IndicadoresService.CalcularAsync`:** os parâmetros, o ViewModel de saída, quais indicadores ele calcula e com quais fórmulas.
6. **Onde ficam hoje as faixas, a direção e o status dos indicadores:** no front (`indicadores/config.ts` ou parecido), no backend ou em lugar nenhum. Isso define como faremos a fase 2.
7. **Se a tela de Indicadores já tem as duas visões** (contador e diretoria) ou ainda é a versão antiga.
8. **Se existem** `docs/processo_conciliacao_contabil_totvs_rm.md` e `docs/prompt-visao-contador.md`. O segundo é a especificação completa da visão do contador (dados, regras de negócio e contratos de API) e substituiu o antigo `prompt-indicadores-duas-visoes.md`. Leia-o e use-o para conferir o rascunho `docs/tati/02-indicadores.md`.
9. **As rotas e os menus do front**, para escrever o `01-painel.md`.
10. **Os documentos** `docs/04_PROJETO_AGENTE_CONTABIL.md` (o desenho original da Tati) e `docs/contexto-projeto-swisspark.md`. Aponte o que neles muda o plano.

**Entregue:**

- um resumo do que encontrou;
- o plano das fases 1 a 4, com os arquivos que vai criar ou alterar;
- a lista de divergências entre `docs/tati/02-indicadores.md` e o código, indicador por indicador (fórmula, direção, casos especiais).

Chame o árbitro (veja "Revisão pelo árbitro").

---

## Fase 1: base de conhecimento e carregamento

### 1.1 Arquivos

Crie a pasta `AgenteContabil/Conhecimento/` com:

- **`02-indicadores.md`:** copie o rascunho de `docs/tati/02-indicadores.md`. Corrija só o que o código mostrou ser diferente e liste cada correção para mim. Se a tela ainda não tem as duas visões, mantenha o arquivo assim mesmo, porque ele explica conceitos, e me avise.
- **`01-painel.md`:** o que é o painel e para que serve cada tela, com base nas rotas e telas reais. Inclua Dashboard, Empresas, Contas Contábeis, Usuários, Relatórios, Pendências, Configurações, Auditoria, Fechamento Mensal (Lançamentos, Pendências, Observações, Histórico) e Indicadores. Para cada tela, escreva para que serve, o que se vê nela e quem usa. Descreva o que **existe hoje**, sem detalhes de implementação.
- **`03-conciliacao.md`:** uma versão condensada de `docs/processo_conciliacao_contabil_totvs_rm.md`. Se o arquivo não existir, pule e me avise.
- **`04-glossario.md`:** os termos que aparecem nas telas e no código (coligada, filial, competência, Conferido, pendência, backlog, de-para, fechamento etc.), com uma definição de uma ou duas linhas. Marque com `[CONFIRMAR]` o que você deduziu sem ter certeza.

**Tamanho:** o conjunto inteiro deve ficar em até cerca de 4.000 palavras, porque ele vai em toda pergunta. Me informe o total.

### 1.2 Build

No `.csproj`, garanta que os arquivos vão para a saída do build:

```xml
<ItemGroup>
  <None Update="AgenteContabil\Conhecimento\**\*.md" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Use o mesmo mecanismo que já leva o `instrucoes-v1.md` para a saída, se for diferente deste.

### 1.3 Carregamento no HarnessService

- Leia todos os `.md` da pasta em **ordem alfabética**, no mesmo momento em que as instruções são lidas (na subida da API).
- Monte o prompt de sistema nesta ordem: instruções, depois um título `# Base de conhecimento`, depois os arquivos separados por `---`, depois o que mais já existir.
- **O começo do prompt deve ser idêntico em todas as requisições.** Nada de data, hora ou dado do usuário antes da base de conhecimento. O DeepSeek reaproveita em cache o começo repetido, o que reduz o custo e o tempo. Se hoje existe algo variável antes das instruções, me avise antes de mudar.
- Na subida, registre no log quantos arquivos e quantos caracteres foram carregados.
- Se a pasta não existir, siga sem erro e registre um aviso no log.

Chame o árbitro (veja "Revisão pelo árbitro").

---

## Fase 2: ferramenta `ConsultarIndicadores`

### 2.1 A classe

Crie `Ferramentas/ConsultarIndicadores.cs`, no mesmo formato do `ConsultarSaldo`:

- **Parâmetros:** a empresa (pelo nome, convertida em coligada e filial como as outras ferramentas fazem) e o período, no mesmo formato de período que as outras ferramentas usam.
- Chama `IndicadoresService.CalcularAsync`.
- Devolve um **JSON enxuto**, e não o ViewModel inteiro:

```json
{
  "empresa": "AEPL Participações",
  "coligada": 17,
  "filial": 1,
  "periodo": { "inicio": "2026-03-01", "fim": "2026-03-31", "meses": 1 },
  "statusGeral": "risco",
  "pilares": [
    { "pilar": "Resultado", "valorFormatado": "−R$ 75,7 mil", "status": "risco" }
  ],
  "indicadores": [
    {
      "codigo": "LIQ_CORRENTE",
      "grupo": "Liquidez",
      "nome": "Liquidez corrente",
      "valor": 1.02,
      "valorFormatado": "1,02",
      "status": "atencao",
      "direcao": "maior_melhor",
      "faixas": { "risco": 1.0, "bom": 1.5 },
      "formula": "Ativo circulante ÷ Passivo circulante",
      "nota": null
    }
  ],
  "pontosAVerificar": ["Custos zerados com R$ X em estoque"],
  "resultado": {
    "receitaLiquida": 0, "ebitda": 0, "resultadoAntesIR": 0, "irCs": 0, "lucroLiquido": -75670.44
  },
  "caixaFinal": 984563.27
}
```

(Os zeros em `resultado` são só para mostrar o formato.)

Se o ViewModel não tiver algum desses dados, **não invente**. Devolva o que existe e me diga o que faltou.

A filial do exemplo é ilustrativa. Use o valor real.

### 2.2 Status, faixas e casos especiais

Aplique as regras de `02-indicadores.md` (status, casos especiais, pilares e veredito). As faixas iniciais são estas:

| Indicador | Direção | Risco | Atenção | Bom |
|---|---|---|---|---|
| Liquidez corrente | maior melhor | < 1,0 | 1,0 a 1,5 | ≥ 1,5 |
| Liquidez seca | maior melhor | < 0,5 | 0,5 a 1,0 | ≥ 1,0 |
| Liquidez imediata | maior melhor | < 0,1 | 0,1 a 0,3 | ≥ 0,3 |
| Liquidez geral | maior melhor | < 1,0 | 1,0 a 1,2 | ≥ 1,2 |
| Endividamento geral | menor melhor | > 80% | 60% a 80% | ≤ 60% |
| Dívida no curto prazo (CE) | menor melhor | > 70% | 40% a 70% | ≤ 40% |
| Dív. líquida / EBITDA 12m | menor melhor | > 4,0x | 2,5x a 4,0x | ≤ 2,5x |
| Dívida bruta / PL | menor melhor | > 2,0x | 1,0x a 2,0x | ≤ 1,0x |
| ROE (anualizado) | maior melhor | < 0% | 0% a 12% | ≥ 12% |
| ROA (anualizado) | maior melhor | < 0% | 0% a 4% | ≥ 4% |
| Margem bruta | maior melhor | < 20% | 20% a 40% | ≥ 40% |
| Margem líquida | maior melhor | < 0% | 0% a 10% | ≥ 10% |
| EBITDA | maior melhor | < 0 | — | ≥ 0 |

O lugar onde as faixas estão hoje (fase 0, item 6) decide o caminho:

- **Se já estão no backend:** use-as.
- **Se estão só no front:** crie no backend uma classe pura `RegrasIndicadores`, sem acesso a banco, com a mesma tabela de faixas, a direção, os casos especiais, os pilares e o veredito. Escreva testes unitários para ela. **Não altere o front nesta fase.** Anote a duplicação como pendência no relatório final, porque mais tarde as duas pontas devem ler da mesma fonte.

### 2.3 Registro

- Registre a classe na injeção de dependência, onde as outras ferramentas estão.
- No `HarnessService`, inclua a classe no construtor, na lista `ferramentas` e no `switch` de `ExecutarFerramentaAsync`.
- Escreva a **descrição da ferramenta** (a que o DeepSeek lê) deixando claro quando usá-la e a diferença para o `ConsultarSaldo`. O `ConsultarSaldo` serve para o saldo de uma conta. O `ConsultarIndicadores` serve para a saúde financeira, os índices, a liquidez, o endividamento, a rentabilidade e "como está a empresa".

### 2.4 Caso de teste: AEPL (coligada 17), março de 2026

| Item | Valor esperado | Status esperado |
|---|---|---|
| Liquidez corrente | 1,02 | Atenção |
| Liquidez seca | 0,19 | Risco |
| Liquidez imediata | 0,04 | Risco |
| Liquidez geral | 1,22 | Bom |
| Endividamento geral | 71,8% | Atenção |
| Dívida no curto prazo | 99,5% | Risco |
| Dív. líquida / EBITDA 12m | ≈ 3,5x | Atenção |
| Dívida bruta / PL | 0,47x | Bom |
| ROE no período | −0,79% | Risco |
| ROA no período | −0,22% | Risco |
| Margem líquida | −40,4% | Risco |
| Margem bruta | 100% | **Verificar** (custos zerados) |
| Lucro líquido | −R$ 75.670,44 | — |
| Caixa no fim | R$ 984.563,27 | — |

Rode a ferramenta com esses dados e me mostre o JSON. Se algum valor não bater, me mostre a diferença e a causa provável antes de corrigir.

Chame o árbitro (veja "Revisão pelo árbitro").

---

## Fase 3: instruções

Copie `instrucoes-v1.md` para `instrucoes-v2.md` e faça o `HarnessService` carregar a v2. Mantenha a v1 sem alterações, para podermos voltar se precisar.

Na v2:

1. **Troque** a frase que diz que tudo gira em torno dos assuntos atuais por algo assim: "Estes são os assuntos em que você consulta dados. Além deles, você pode explicar conceitos contábeis, as regras dos indicadores e o funcionamento do painel, usando a base de conhecimento."
2. **Acrescente** "Indicadores e saúde financeira" à lista de assuntos principais.
3. **Saudação:** inclua "Saúde financeira da empresa" entre as opções. Se a regra exige exatamente três opções, me mostre como ficariam e qual sairia, e pergunte antes de mudar.
4. **Em "Regras de dados", acrescente:**
   - quando usar o `ConsultarIndicadores` e quando usar o `ConsultarSaldo`;
   - números, status e faixas vêm sempre de uma ferramenta, nunca da base de conhecimento nem de estimativa;
   - se a base e a ferramenta divergirem, vale a ferramenta;
   - com status "Verificar", explique o que conferir antes de interpretar o número;
   - sempre diga a empresa e o período da resposta.
5. **Restrinja** a regra "não tenho essa informação" a **dados** de módulos sem ferramenta. Para perguntas de conceito, ela explica.
6. **Público:** se o harness já recebe o perfil do usuário, use-o para responder no nível do contador ou no da diretoria. Se não recebe, responda no nível do contador e ofereça um resumo para a diretoria. Não crie mecanismo novo de perfil.

Reinicie a API depois das mudanças, porque as instruções e a base são lidas na subida.

Chame o árbitro (veja "Revisão pelo árbitro").

---

## Fase 4: testes e relatório

Rode as perguntas abaixo contra a API local e registre o resultado em `docs/tati/testes-tati.md`, com a pergunta, a ferramenta chamada, um resumo da resposta e "ok" ou "falhou".

| # | Pergunta | O que se espera |
|---|---|---|
| 1 | Oi | Saudação com a nova opção de saúde financeira |
| 2 | Como está a saúde financeira da AEPL em março de 2026? | `ConsultarIndicadores`. Começa pelo veredito e pelos pilares, depois os alertas |
| 3 | Qual a liquidez corrente da AEPL em março de 2026? | 1,02, Atenção |
| 4 | O que é liquidez seca? | Explica, sem chamar ferramenta |
| 5 | Por que a margem bruta da AEPL está em 100%? | Status Verificar: custos zerados, conferir o de-para |
| 6 | Por que a AEPL teve prejuízo em março se a operação foi bem? | Cascata do resultado: o IR e a CS viraram o resultado |
| 7 | Qual o saldo da conta {uma conta real} da AEPL em março? | `ConsultarSaldo`, e não `ConsultarIndicadores` |
| 8 | A dívida da AEPL está alta? | ≈ 3,5x o EBITDA, Atenção, explicado em anos de geração de caixa |
| 9 | Por que o PCT sumiu da tela? | Explica que é igual ao endividamento geral |
| 10 | O que significa o status Verificar? | Explica, sem ferramenta |
| 11 | Como está a produtividade do time? | A ferramenta antiga continua funcionando |
| 12 | Qual o backlog da AEPL? | A ferramenta antiga continua funcionando |
| 13 | Qual a liquidez corrente da AEPL em março de 2030? | Diz que não há dados. Não inventa |
| 14 | Qual o ROE da Empresa Inexistente? | Diz que não encontrou a empresa |
| 15 | Me explica a tela de Fechamento Mensal | Usa o `01-painel.md` |
| 16 | O que é de-para? | Usa o glossário |
| 17 | A margem foi de 7,6% para 7,4%. Caiu quanto? | 0,2 p.p. |
| 18 | Resume para o diretor a situação da AEPL em março, em três frases | Linguagem de negócio, sem fórmula |
| 19 | Quanto a AEPL vai faturar no ano que vem? | Não faz previsão nem inventa número |

**Relatório final:**

- o que mudou, arquivo por arquivo;
- o tamanho da base de conhecimento (palavras e caracteres);
- o resultado dos testes;
- as pendências. Por exemplo: faixas duplicadas entre front e back, itens `[CONFIRMAR]` do glossário e as próximas ferramentas possíveis (Balancete, Balanço e Diferenças, que já têm services prontos).

Depois do relatório, chame o árbitro uma última vez e pare, mesmo que ele aprove. Não faça commit.
