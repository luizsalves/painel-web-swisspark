import { describe, expect, it } from "vitest";
import { descreverFonte, lerRespostaTati, rotuloDoMes } from "./respostaTati";
import { formatarValorGrafico } from "./formatarGrafico";

const resposta = (grafico: string) =>
  `A liquidez melhorou no trimestre.\n\n\`\`\`grafico\n${grafico}\n\`\`\`\n\n\`\`\`opcoes\n["Ver o balanço", "Ver a DFC"]\n\`\`\``;

describe("lerRespostaTati", () => {
  it("lê o contrato novo de linha, com buraco onde o mês não tem dados", () => {
    const { corpo, opcoes, grafico } = lerRespostaTati(resposta(JSON.stringify({
      tipo: "linha",
      titulo: "Liquidez corrente",
      unidade: "indice",
      fonte: { ferramenta: "consultar_historico_indicadores", empresa: "EMPRESA TESTE LTDA", periodo: "2026-07 a 2026-09" },
      eixoX: ["2026-07", "2026-08", "2026-09"],
      series: [{ nome: "Liquidez corrente", valores: [1.1, null, 1.3] }],
    })));

    expect(corpo).toBe("A liquidez melhorou no trimestre.");
    expect(opcoes).toEqual(["Ver o balanço", "Ver a DFC"]);
    expect(grafico).toEqual({
      tipo: "linha",
      titulo: "Liquidez corrente",
      unidade: "indice",
      fonte: { ferramenta: "consultar_historico_indicadores", empresa: "EMPRESA TESTE LTDA", periodo: "2026-07 a 2026-09" },
      categorias: ["2026-07", "2026-08", "2026-09"],
      series: [{ nome: "Liquidez corrente", valores: [1.1, null, 1.3] }],
    });
  });

  it("lê colunas com mais de uma série e descarta a série de tamanho errado", () => {
    const { grafico } = lerRespostaTati(resposta(JSON.stringify({
      tipo: "colunas",
      titulo: "Balanço",
      unidade: "R$",
      fonte: { ferramenta: "consultar_balanco" },
      eixoX: ["Ativo", "Passivo"],
      series: [{ nome: "Setembro", valores: [100, 60] }, { nome: "Agosto", valores: [90, 50] }, { nome: "Errada", valores: [1] }],
    })));

    expect(grafico?.tipo).toBe("colunas");
    expect(grafico?.series.map((s) => s.nome)).toEqual(["Setembro", "Agosto"]);
  });

  it("lê o formato antigo como barras de uma série", () => {
    const { grafico } = lerRespostaTati(resposta('{"titulo": "Backlog por empresa", "itens": [{"label": "A", "valor": 12}, {"label": "Demais empresas", "valor": 4}]}'));

    expect(grafico).toEqual({
      tipo: "barras",
      titulo: "Backlog por empresa",
      unidade: null,
      fonte: null,
      categorias: ["A", "Demais empresas"],
      series: [{ nome: "Backlog por empresa", valores: [12, 4] }],
    });
  });

  it("resposta cortada no meio do gráfico não desenha nada e mantém as opções", () => {
    // O backend já tira o bloco cortado; se algum chegar, a tela não tenta aproveitar pedaço dele.
    const cortada = 'Veja o backlog.\n\n```grafico\n{"titulo": "Backlog", "itens": [{"label": "A", "valor": 12}, {"label": "C", "va\n\n```opcoes\n["Ver o balanço"]\n```';

    const { corpo, opcoes, grafico } = lerRespostaTati(cortada);

    expect(grafico).toBeNull();
    expect(corpo).toBe("Veja o backlog.");
    expect(opcoes).toEqual(["Ver o balanço"]);
  });

  it("bloco sem fechamento seguido das opções: lê o mesmo JSON que o backend conferiu e mantém as opções", () => {
    const texto = 'Veja.\n\n```grafico\n{"titulo": "Backlog", "itens": [{"label": "A", "valor": 12}]}\n\n```opcoes\n["a", "b"]\n```\n{"label": "B", "valor": 999}';

    const { opcoes, grafico } = lerRespostaTati(texto);

    // O 999 solto depois das opções não entra no gráfico (o caso V1 do árbitro).
    expect(grafico?.series[0].valores).toEqual([12]);
    expect(opcoes).toEqual(["a", "b"]);
  });

  it("não desenha bloco inválido, mas tira ele do texto", () => {
    for (const invalido of ["{nada}", "[1, 2]", '{"tipo": "pizza", "eixoX": ["a"], "series": [{"nome": "x", "valores": [1]}]}', '{"tipo": "linha", "eixoX": [], "series": []}']) {
      const { corpo, grafico } = lerRespostaTati(resposta(invalido));
      expect(grafico).toBeNull();
      expect(corpo).toBe("A liquidez melhorou no trimestre.");
    }
  });

  it("lê tipo e unidade como o backend: aparados, e tipo vazio ou nulo é o formato antigo", () => {
    const series = { eixoX: ["2026-08", "2026-09"], series: [{ nome: "Liquidez", valores: [1.1, 1.2] }] };
    const comEspacos = lerRespostaTati(resposta(JSON.stringify({ tipo: " linha ", unidade: " R$ ", titulo: "T", ...series }))).grafico;
    expect(comEspacos?.tipo).toBe("linha");
    expect(comEspacos?.unidade).toBe("R$");

    for (const tipo of [null, "", "  "]) {
      const { grafico } = lerRespostaTati(resposta(JSON.stringify({ tipo, titulo: "Backlog", itens: [{ label: "A", valor: 3 }] })));
      expect(grafico?.tipo).toBe("barras");
      expect(grafico?.series[0].valores).toEqual([3]);
    }
  });

  it("resposta sem gráfico nem opções volta só com o texto", () => {
    expect(lerRespostaTati("Só texto.")).toEqual({ corpo: "Só texto.", opcoes: [], grafico: null });
  });
});

describe("fonte e formatação", () => {
  it("descreve a fonte com o módulo, a empresa e os meses em pt-BR", () => {
    expect(descreverFonte({ ferramenta: "consultar_historico_indicadores", empresa: "EMPRESA TESTE LTDA", periodo: "2025-10 a 2026-09" }))
      .toBe("Fonte: Indicadores · EMPRESA TESTE LTDA · out/2025 a set/2026");
    expect(descreverFonte({ ferramenta: "consultar_dfc" })).toBe("Fonte: DFC");
    expect(descreverFonte(null)).toBeNull();
    expect(rotuloDoMes("2026-13")).toBe("2026-13");
  });

  it("formata pela unidade, com null como buraco", () => {
    expect(formatarValorGrafico(1234567.89, "R$")).toBe("R$ 1.234.567,89");
    expect(formatarValorGrafico(1234567.89, "R$", true)).toBe("R$ 1,2 mi");
    expect(formatarValorGrafico(-9616.74, "R$", true)).toBe("-R$ 9,6 mil");
    expect(formatarValorGrafico(12.5, "%")).toBe("12,50%");
    expect(formatarValorGrafico(1.2, "x")).toBe("1,20x");
    expect(formatarValorGrafico(1.2, "indice")).toBe("1,20");
    expect(formatarValorGrafico(12, "contas")).toBe("12");
    expect(formatarValorGrafico(null, "indice")).toBe("—");
  });

  it("no eixo, escolhe a escala pelo valor arredondado e não mostra -0", () => {
    expect(formatarValorGrafico(999.6, "R$", true)).toBe("R$ 1,0 mil");
    expect(formatarValorGrafico(999.4, "R$", true)).toBe("R$ 999");
    expect(formatarValorGrafico(999_999, "R$", true)).toBe("R$ 1,0 mi");
    expect(formatarValorGrafico(-999_999, "R$", true)).toBe("-R$ 1,0 mi");
    expect(formatarValorGrafico(-0.004, "%")).toBe("0,00%");
    expect(formatarValorGrafico(-0.004, "R$")).toBe("R$ 0,00");
    expect(formatarValorGrafico(-12.5, "%", true)).toBe("-12,5%");
  });
});
