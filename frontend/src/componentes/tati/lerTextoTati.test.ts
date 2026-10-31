import { describe, expect, it } from "vitest";
import { lerBlocos, lerTrechos } from "./lerTextoTati";

describe("lerBlocos", () => {
  it("separa título, lista, tabela e parágrafo", () => {
    const texto = [
      "Resumo do mês da EMPRESA TESTE LTDA.",
      "Segunda linha do parágrafo.",
      "",
      "### Resultado",
      "- Receita em alta",
      "- Custos estáveis",
      "",
      "| Conta | Valor |",
      "|---|---:|",
      "| Caixa | 1,00 |",
      "| Bancos | 2,00 |",
      "",
      "1. Primeiro",
      "2) Segundo",
    ].join("\n");

    expect(lerBlocos(texto)).toEqual([
      { tipo: "paragrafo", linhas: ["Resumo do mês da EMPRESA TESTE LTDA.", "Segunda linha do parágrafo."] },
      { tipo: "titulo", texto: "Resultado" },
      { tipo: "lista", ordenada: false, itens: ["Receita em alta", "Custos estáveis"] },
      { tipo: "tabela", cabecalho: ["Conta", "Valor"], linhas: [["Caixa", "1,00"], ["Bancos", "2,00"]] },
      { tipo: "lista", ordenada: true, itens: ["Primeiro", "Segundo"] },
    ]);
  });

  it("linha com barras sem o separador não vira tabela, e a linha horizontal some", () => {
    expect(lerBlocos("Ativo | Passivo\n---\n**Total** do mês")).toEqual([
      { tipo: "paragrafo", linhas: ["Ativo | Passivo"] },
      { tipo: "paragrafo", linhas: ["**Total** do mês"] },
    ]);
  });

  it("texto simples continua um parágrafo só, com as quebras de linha", () => {
    expect(lerBlocos("Uma linha.\nOutra linha.")).toEqual([{ tipo: "paragrafo", linhas: ["Uma linha.", "Outra linha."] }]);
  });
});

describe("lerTrechos", () => {
  it("lê o negrito e o link do número para o card", () => {
    expect(lerTrechos("Receita de [R$ 1,2 mi](card:resultado/receitaLiquida) e **lucro de [R$ 3 mil](card:resultado/lucroLiquido)**.")).toEqual([
      { tipo: "texto", texto: "Receita de " },
      { tipo: "card", texto: "R$ 1,2 mi", alvo: "resultado/receitaLiquida" },
      { tipo: "texto", texto: " e " },
      { tipo: "negrito", trechos: [{ tipo: "texto", texto: "lucro de " }, { tipo: "card", texto: "R$ 3 mil", alvo: "resultado/lucroLiquido" }] },
      { tipo: "texto", texto: "." },
    ]);
  });

  it("rótulo em negrito dentro do link perde os asteriscos", () => {
    expect(lerTrechos("[**1,42**](card:indicador/LIQ_CORRENTE)")).toEqual([
      { tipo: "card", texto: "1,42", alvo: "indicador/LIQ_CORRENTE" },
    ]);
  });

  it("link que não é para card fica como texto", () => {
    expect(lerTrechos("Veja [o site](https://exemplo.com).")).toEqual([{ tipo: "texto", texto: "Veja [o site](https://exemplo.com)." }]);
  });
});
