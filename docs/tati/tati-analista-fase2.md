# Tati Analista — Parte IV: Prompt de execução da Fase 2

> Fase 2 — Resposta com gráfico
> Continuação do livro `tati-analista.md`. Base: Fase 1 concluída na `feature/tati-modulos` (commits 0473fe5 e 2fbc95b) e PR #9 já na main.

## Índice da Parte IV

- Onde estamos (o que a Fase 1 e o PR #9 deixaram)
- O que a Fase 2 entrega
- Regras invioláveis
- Capítulo 2.0 — Preparação
- Capítulo 2.1 — Investigação (para e espera aprovação)
- Capítulo 2.2 — Ferramenta de histórico
- Capítulo 2.3 — Contrato do gráfico e validação no backend
- Capítulo 2.4 — Instruções v4
- Capítulo 2.5 — Gráficos no front
- Capítulo 2.6 — Testes
- Capítulo 2.7 — Relatório final
- Apêndice C — Perguntas de teste da Fase 2
- Apêndice D — Decisões que são do Luiz

---

> Copie tudo a partir daqui e cole no agente do VS Code.

---

Você vai executar a **Fase 2 do projeto Tati Analista** no Painel Web SwissPark. Leia este prompt inteiro antes de começar.

## Onde estamos

- A Tati (`backend/PainelWebSwissPark.Api/AgenteContabil/`) tem 9 ferramentas, instruções `instrucoes-v3.md` (padrão), limites no `appsettings` (6 idas ao modelo, 90 s) e log/proteção por ferramenta. Registro da Fase 1 em `docs/tati/testes-tati-analista-fase1.md`.
- **Gráfico hoje** (veio do PR #9):
  - A Tati escreve um bloco ```` ```grafico ```` no texto: `{"titulo": "...", "itens": [{"label": "...", "valor": 12}]}`.
  - O front (`frontend/src/paginas/IaPlanning/index.tsx`, função `lerRespostaTati` e componente `GraficoTatiCard`) tira o bloco do texto e desenha **barras horizontais feitas à mão em CSS**, na área de gráficos acima da conversa. Não há biblioteca de gráficos no `package.json`.
  - A v3 só permite gráfico de **backlog e produtividade**, no máximo 1 por resposta e 15 itens.
  - **Ninguém confere** se os números do bloco vieram de uma ferramenta. É o modelo que copia os números para dentro do bloco.
- O `IIndicadoresMensaisService.CalcularAsync(inicio, fim, codColigada, codFilial, ct)` já devolve a base mês a mês dos indicadores (usado no histórico da tela Indicadores).

## O que a Fase 2 entrega

1. Gráficos também de **Indicadores, Balanço, DFC e Balancete**, além de backlog e produtividade.
2. Três formatos: **barras** (o atual), **colunas** (comparar categorias ou períodos) e **linha** (evolução no tempo).
3. Uma ferramenta de **histórico de indicadores** para os gráficos de linha.
4. **Validação no backend**: um gráfico só chega na tela se todos os números dele saíram de uma ferramenta chamada naquela resposta. Se não, o bloco é removido e o motivo vai para o log.
5. **Fonte embaixo de cada gráfico**: módulo, empresa e período.
6. O formato antigo (`titulo` + `itens`) continua funcionando.

## Regras invioláveis

1. Não altere services, repositories, controllers nem regras de cálculo. Ferramenta só chama service.
2. Nenhum número inventado: o validador do backend é a garantia, não só a instrução.
3. Não mude telas fora da CFO Tati, exceto o texto do 4º assunto da barra lateral (item do Capítulo 2.5).
4. Não apague arquivos. Não mexa na v1, v2 e v3 das instruções: crie a v4.
5. Siga o padrão das ferramentas da Fase 1 (`LocalizarEmpresa`, retorno com `empresa`, `coligada`, `abrangencia`, `periodo`, `semDados`, erro sem exceção).
6. Não faça commit nem push sem eu pedir.
7. Ao fim de cada capítulo, chame o árbitro (`.claude/agents/arbitro-tati.md`) com este prompt como especificação. Só siga se aprovar ou aprovar com ressalvas.

---

## Capítulo 2.0 — Preparação

1. `git status --short --branch`, `git log --oneline -5`, `gh pr list --state all --limit 5`.
2. Se a `feature/tati-modulos` **já foi mesclada** na main: crie `feature/tati-graficos` a partir da `origin/main`. Se **não foi**: **pare e me pergunte** se abro o PR dela primeiro ou se crio a nova branch a partir dela.
3. Se houver arquivo não commitado, pare e me pergunte.
4. Atualize o árbitro: a descrição e o "leia primeiro" dele devem apontar para este prompt (Parte IV) e para `docs/tati/testes-tati-analista-fase1.md`. Só isso, sem mudar as regras dele.
5. Linha de base: `dotnet build` e `dotnet test` no backend; `npm run build`, `npx vitest run` e `npx oxlint` no frontend. Anote erros, avisos e testes.

## Capítulo 2.1 — Investigação (pare e espere minha aprovação)

Traga um resumo curto com:

1. **Histórico dos indicadores:** como a tela Indicadores transforma `BaseCalculoMensalViewModel` em valores e status por mês. A regra está no backend (`RegrasIndicadores`?) ou só no front? Qual método você vai reaproveitar para não duplicar regra.
2. **Tempo:** meça o `IIndicadoresMensaisService` para AEPL Participações (coligada 17) e AGV Campinas (coligada 1) com 6 e 12 meses.
3. **Gráficos que já existem no front:** sparkline, matriz mensal, barras da Visão do contador ou outros componentes reaproveitáveis. Diga se dá para fazer linha e colunas sem biblioteca nova.
4. **Fluxo da resposta:** onde no `HarnessService` o texto final sai, e onde guardar os resultados das ferramentas da resposta para o validador consultar.
5. **Testes do front:** se o vitest já cobre `lerRespostaTati` ou outra parte da tela.
6. **Plano:** classes e componentes que vai criar ou mudar, e as dúvidas.

**Pare aqui e espere eu responder "ok".**

## Capítulo 2.2 — Ferramenta de histórico

Crie `ConsultarHistoricoIndicadores` em `AgenteContabil/Ferramentas/`:

- **Quando usar (descrição para o modelo):** evolução mês a mês de um ou mais indicadores financeiros de uma empresa, para responder "como evoluiu" ou montar gráfico de linha.
- **Parâmetros:** `empresa` (+ `codColigada`/`codFilial` opcionais, como as outras), `mesInicial` e `mesFinal` (`AAAA-MM`), `indicadores` (lista opcional; sem ela, todos).
- **Teto:** no máximo 12 meses por chamada. Pedido maior → erro claro pedindo um período menor.
- **Saída enxuta:** por indicador → `nome`, `unidade`, e lista `{ mes, valor, status }`. Mês sem dados → `valor: null` com o motivo, nunca zero.
- Use **a mesma regra da tela** para valor e status. Se a regra só existir no front, **pare e me avise** antes de escrever qualquer cálculo.
- Registre no `HarnessService` e no `Program.cs` como as outras.

Árbitro ao fim do capítulo.

## Capítulo 2.3 — Contrato do gráfico e validação no backend

### Contrato novo (o bloco continua sendo ```` ```grafico ````)

```json
{
  "tipo": "linha",
  "titulo": "Liquidez corrente — AEPL Participações",
  "unidade": "x",
  "fonte": { "ferramenta": "consultar_historico_indicadores", "empresa": "AEPL PARTICIPACOES LTDA", "periodo": "2025-10 a 2026-09" },
  "eixoX": ["2025-10", "2025-11", "2025-12"],
  "series": [ { "nome": "Liquidez corrente", "valores": [1.03, 1.10, 1.20] } ]
}
```

- `tipo`: `barras` (horizontal, ranking), `colunas` (categorias ou períodos lado a lado), `linha` (tempo).
- `unidade`: `R$`, `%`, `x` (índice) ou `contas`. O front formata; o modelo manda número puro.
- `barras` aceita o formato antigo `itens: [{label, valor}]`. `colunas` e `linha` usam `eixoX` + `series`.
- Sem `tipo` → trata como `barras` no formato antigo (compatibilidade).
- Limites: 1 gráfico por resposta; `barras` até 15 itens; `colunas` até 12 categorias e 3 séries; `linha` até 12 pontos e 3 séries. `valores` aceita `null` (buraco no gráfico).

### Validador (backend)

Crie `ValidadorGraficoTati` (classe pura, fácil de testar) e chame no `HarnessService` antes de devolver o texto:

1. Guarde os JSONs devolvidos por cada ferramenta **nesta resposta**.
2. Se a resposta tiver bloco `grafico`:
   - JSON inválido ou fora dos limites → remove o bloco.
   - `fonte.ferramenta` não foi chamada nesta resposta → remove.
   - **Cada número** do gráfico precisa existir nos resultados das ferramentas desta resposta (compare arredondando para 2 casas; aceite também a soma de "Demais" quando a ferramenta devolveu `demais`). Algum número sem origem → remove.
3. Bloco removido → log de aviso com o motivo (sem os valores) e o texto segue sem gráfico. Não chame o modelo de novo.
4. Bloco válido → passa sem alteração.

Árbitro ao fim do capítulo.

## Capítulo 2.4 — Instruções v4

1. Copie a v3 para `instrucoes-v4.md`, mude o padrão no `appsettings` para v4 e inclua no `.csproj`. A v3 continua no build.
2. Seção **Gráficos** reescrita:
   - Gráfico permitido para **todos os módulos**, sempre com números de ferramenta desta resposta.
   - **Quando incluir:** o usuário pediu gráfico, evolução, tendência ou comparação. Fora disso, não force.
   - **Qual tipo:** evolução no tempo → `linha` com `consultar_historico_indicadores`; atual × anterior ou comparação de grupos (blocos do balanço, atividades da DFC) → `colunas`; ranking (backlog por empresa, produtividade, maiores contas de um grupo do balancete) → `barras`.
   - Sempre preencher `fonte` com a ferramenta, empresa e período usados.
   - Os exemplos de backlog e produtividade da v3 continuam valendo.
   - No texto, 2 a 4 frases sobre o que chama atenção. Não repetir a lista do gráfico.
   - Avisar que o gráfico só aparece se os números baterem com a ferramenta (para o modelo não "ajustar" valor).
3. Corrija as 3 pendências de texto da Fase 1 (registradas em `docs/tati/testes-tati-analista-fase1.md`):
   - nunca citar para o usuário o nome interno de uma ferramenta;
   - saldo com sinal contrário: só sugerir conferir com a contabilidade, sem arriscar causa;
   - receita acumulada no balancete: deixar claro que é acumulado do ano, não resultado.

Árbitro ao fim do capítulo.

## Capítulo 2.5 — Gráficos no front

1. Em `lerRespostaTati`: aceite o contrato novo e o antigo. Mantenha a recuperação de resposta cortada.
2. Componentes de gráfico para `colunas` e `linha`, no mesmo visual do `GraficoTatiCard` (mesmas variáveis de cor e card). **Prefira SVG próprio ou componente que já exista** (Capítulo 2.1). Biblioteca nova só com minha aprovação.
3. Formatação pt-BR por `unidade`: `R$ 1.234.567,89` (com abreviação "mi"/"mil" no eixo), `12,5%`, `1,20x`. `null` = buraco, nunca zero.
4. Tooltip ou valor visível em cada ponto/coluna. Legenda quando houver mais de uma série.
5. Linha de **fonte** embaixo do gráfico: "Fonte: Indicadores · AEPL Participações · out/2025 a set/2026".
6. Funciona em tela estreita (sem rolagem horizontal da página) e nos temas claro/escuro, se a tela tiver os dois.
7. Ajuste o texto do 4º assunto da barra lateral para ficar igual ao que a Tati oferece na v4.

Árbitro ao fim do capítulo.

## Capítulo 2.6 — Testes

1. **Backend (xUnit):**
   - `ConsultarHistoricoIndicadores`: saída no formato, mês sem dados → `null`, mais de 12 meses → erro, empresa não encontrada → erro.
   - `ValidadorGraficoTati`: bloco válido passa; número sem origem → removido; ferramenta não chamada → removido; JSON quebrado → removido; acima dos limites → removido; formato antigo válido passa; "Demais" com soma passa.
2. **Frontend (vitest):** `lerRespostaTati` com contrato novo, antigo, cortado e inválido.
3. Build e testes sem novas falhas nem novos avisos em relação à linha de base.
4. **Teste manual** pelo console da Fase 1 (backend) **e** na tela, com a API e o front rodando (login superadmin: me avise quando chegar aqui). Use o Apêndice C e monte a tabela "pergunta → tipo de gráfico → valores do gráfico → valor na tela → bate?". Tire print de cada gráfico e salve em `docs/tati/fase2-prints/`.

Árbitro ao fim do capítulo.

## Capítulo 2.7 — Relatório final

1. Arquivos criados e alterados.
2. Build e testes contra a linha de base (backend e frontend).
3. Tabela do teste manual + prints.
4. Quantas vezes o validador removeu gráfico no teste manual e por quê.
5. Tempos das perguntas com histórico (AEPL e AGV).
6. Pendências, riscos e decisões que precisam de mim (separadas das técnicas).

Chame o árbitro uma última vez e pare. Não faça commit.

---

## Apêndice C — Perguntas de teste da Fase 2

Use AEPL Participações (coligada 17) e AGV Campinas (coligada 1), set/2026.

1. Mostra a evolução da liquidez corrente da AEPL nos últimos 12 meses. *(linha)*
2. Compara margem líquida e endividamento da AEPL nos últimos 6 meses. *(linha, 2 séries, unidades diferentes → espera 2 respostas ou só 1 série; ver Apêndice D)*
3. Gráfico dos indicadores da AEPL: este mês contra o anterior. *(colunas)*
4. Mostra em gráfico ativo, passivo exigível e PL da AGV. *(colunas)*
5. Gráfico do fluxo de caixa da AEPL por atividade. *(colunas)*
6. Quais as maiores contas do ativo circulante da AGV? Faz um gráfico. *(barras)*
7. Gráfico do backlog por empresa. *(barras, formato antigo — regressão)*
8. Ranking de produtividade de quem fechou contas. *(barras — regressão)*
9. Qual a liquidez corrente da AEPL? *(sem gráfico: não foi pedido)*
10. Teste do validador: no console, injete uma resposta com um número que nenhuma ferramenta devolveu. *(gráfico removido + log)*

## Apêndice D — Decisões que são do Luiz

- Biblioteca de gráficos: SVG próprio (recomendado, sem dependência nova) ou Recharts?
- Linha com unidades diferentes no mesmo gráfico (% e x): proibir e fazer um gráfico por unidade (recomendado) ou usar dois eixos?
- Teto de 12 meses no histórico está bom?
- Gráficos da conversa: continuam só na área de cima (como hoje) ou também dentro da mensagem da Tati?
