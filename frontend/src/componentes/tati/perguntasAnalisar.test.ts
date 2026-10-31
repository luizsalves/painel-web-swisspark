import { describe, expect, it } from "vitest";
import { filtrosPadrao } from "../../utils/filtrosPainel";
import { perguntaDoResumo, perguntasAnalisar } from "./perguntasAnalisar";

describe("perguntaDoResumo", () => {
  it("é o resumo do mês com um mês só e o do período com vários, como no backend", () => {
    expect(perguntaDoResumo({ ...filtrosPadrao(), ano: 2026, mesInicial: 9, mesFinal: 9 })).toBe("Resumo do mês");
    expect(perguntaDoResumo({ ...filtrosPadrao(), ano: 2026, mesInicial: 7, mesFinal: 9 })).toBe("Resumo do período");
  });
});

describe("perguntasAnalisar", () => {
  it("cita o card e pede as comparações do livro: período anterior e mesmo período do ano passado", () => {
    const pergunta = perguntasAnalisar.indicador("Liquidez corrente");
    expect(pergunta).toContain("“Liquidez corrente”");
    expect(pergunta).toContain("período anterior");
    expect(pergunta).toContain("mesmo período do ano passado");
    expect(perguntasAnalisar.pilar("Caixa")).toContain("pilar “Caixa”");
  });

  it("no balanço, que é a posição de um mês, compara com o mês anterior e o mesmo mês do ano passado", () => {
    expect(perguntasAnalisar.balanco()).toContain("mês anterior e com o mesmo mês do ano passado");
    expect(perguntasAnalisar.ladoDoBalanco("ativo")).toMatch(/^Analise o ativo do balanço/);
  });

  it("toda pergunta começa com “Analise” e não traz número: os números vêm das ferramentas", () => {
    const todas = [
      perguntasAnalisar.indicador("Margem líquida"), perguntasAnalisar.pilar("Dívida"), perguntasAnalisar.saudeGeral(),
      perguntasAnalisar.resultado(), perguntasAnalisar.caixa(), perguntasAnalisar.divida(), perguntasAnalisar.balanco(),
      perguntasAnalisar.ladoDoBalanco("passivo"), perguntasAnalisar.dfc(), perguntasAnalisar.balancete(),
      perguntasAnalisar.conciliacao(), perguntasAnalisar.etapaDaConciliacao("Resta preparar"), perguntasAnalisar.fechamentoDaConta(),
    ];
    for (const pergunta of todas) {
      expect(pergunta).toMatch(/^Analise /);
      expect(pergunta).not.toMatch(/\d/);
    }
    // A evolução é a única com número: os 12 meses do histórico.
    expect(perguntasAnalisar.evolucao()).toContain("últimos 12 meses");
  });
});
