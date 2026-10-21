import { describe, expect, it } from "vitest";
import type { BaseCalculoIndicadores, Dfc } from "../../interfaces/Indicadores";
import { CONFIG_POR_CODIGO } from "./config";
import {
  calcularIndicadores, calcularStatus, escalaDosValores, formatarMoedaAbreviada, formatarValor, formatarVariacao, gerarAlertas,
  gerarVeredito, montarCascata, montarPilares, pontosAVerificar, totaisDaDfc,
} from "./regras";
import type { DadosPainel } from "./regras";

// AEPL Participações (coligada 17), mar/2026 — o caso de teste combinado com a contabilidade.
const AEPL: BaseCalculoIndicadores = {
  ativoTotal: 34_087_932.98,
  ativoCirculante: 24_960_794.97,
  disponivel: 984_563.27,
  estoques: 20_392_081.09,
  realizavelLongoPrazo: 4_787_803.87,
  passivoCirculante: 24_355_366.24,
  passivoNaoCirculante: 116_549.27,
  patrimonioLiquido: 9_616_017.47,
  dividaBruta: 4_541_150.68,
  dividaLiquida: 4_541_150.68 - 984_563.27,
  receitaLiquida: 187_530.96,
  custos: 0,
  lucroBruto: 187_530.96,
  resultadoFinanceiro: -14_880.2,
  impostoRendaContribuicao: 77_292.19,
  lucroLiquido: -75_670.44,
  ebit: 16_501.95,
  depreciacaoAmortizacao: 67_633.78,
  ebitda: 16_501.95 + 67_633.78,
};

const DFC_AEPL: Dfc = {
  grupos: [
    { nome: "Atividades Operacionais", total: 1_036_235.8, linhas: [] },
    { nome: "Atividades de Financiamento", total: 457_259.39, linhas: [] },
    { nome: "Atividades de Investimentos", total: -308_800, linhas: [] },
    { nome: "Variações não classificadas", total: -359_713.43, linhas: [] },
  ],
  saldoInicialCaixa: 159_581.51,
  saldoFinalCaixa: 984_563.27,
  variacaoCaixa: 984_563.27 - 159_581.51,
};

function dadosAepl(base = AEPL, meses = 1): DadosPainel {
  return { base, indicadores: calcularIndicadores(base, meses), dfc: totaisDaDfc(DFC_AEPL), meses };
}

describe("calcularIndicadores — caso AEPL mar/2026", () => {
  const ind = calcularIndicadores(AEPL, 1);
  const texto = (codigo: keyof typeof ind) => formatarValor(ind[codigo].valor, ind[codigo].config);

  it.each([
    ["liquidez-corrente", "1,02", "atencao"],
    ["liquidez-seca", "0,19", "risco"],
    ["liquidez-imediata", "0,04", "risco"],
    ["liquidez-geral", "1,22", "bom"],
    ["endividamento-geral", "71,79%", "atencao"],
    ["composicao-endividamento", "99,52%", "risco"],
    ["divida-liquida-ebitda", "3,52x", "atencao"],
    ["divida-bruta-pl", "0,47x", "bom"],
    ["roe", "-0,79%", "risco"],
    ["roa", "-0,22%", "risco"],
    ["margem-liquida", "-40,35%", "risco"],
    ["margem-bruta", "100,00%", "verificar"],
    ["ebitda", "84,1 mil", "bom"],
  ] as const)("%s = %s (%s)", (codigo, valor, status) => {
    expect(texto(codigo)).toBe(valor);
    expect(ind[codigo].status).toBe(status);
  });

  it("anualiza o EBITDA no múltiplo da dívida", () => {
    expect(ind["divida-liquida-ebitda"].valor).toBeCloseTo(3_556_587.41 / (84_135.73 * 12), 6);
  });

  it("mostra ROE e ROA do período com o anualizado embaixo", () => {
    expect(ind.roe.detalheValor).toBe("≈ -9,4% ao ano");
    expect(ind.roe.valorFaixa).toBeCloseTo(ind.roe.valor! * 12, 10);
  });

  it("marca a margem bruta como Verificar quando os custos estão zerados com estoque", () => {
    expect(ind["margem-bruta"].nota).toBe("Custos zerados no mês: conferir o de-para");
    expect(ind["margem-bruta"].semFaixa).toBe("Sem custos para comparar");
  });

  it("não traz o PCT", () => {
    expect(Object.keys(ind)).not.toContain("pct");
  });

  it("anota o peso dos estoques na liquidez seca", () => {
    expect(ind["liquidez-seca"].nota).toBe("Estoques são 82% do ativo circulante");
  });
});

describe("casos especiais do múltiplo da dívida", () => {
  it("dívida líquida ≤ 0 vira caixa líquido, Bom", () => {
    const base = { ...AEPL, dividaBruta: 500_000, dividaLiquida: 500_000 - 984_563.27 };
    const ind = calcularIndicadores(base, 1)["divida-liquida-ebitda"];
    expect(ind).toMatchObject({ valor: null, status: "bom", textoValor: "Caixa líquido", detalheValor: "R$ 484,6 mil" });
  });

  it("EBITDA ≤ 0 com dívida líquida > 0 vira EBITDA negativo, Risco", () => {
    const base = { ...AEPL, ebit: -80_000, ebitda: -12_366.22 };
    expect(calcularIndicadores(base, 1)["divida-liquida-ebitda"]).toMatchObject({ status: "risco", textoValor: "EBITDA negativo" });
  });

  it("dívida bruta zero vira Sem dívida, Bom", () => {
    const base = { ...AEPL, dividaBruta: 0, dividaLiquida: -984_563.27 };
    expect(calcularIndicadores(base, 1)["divida-bruta-pl"]).toMatchObject({ status: "bom", textoValor: "Sem dívida" });
  });

  it("período de 3 meses anualiza com × 12 ÷ 3", () => {
    const ind = calcularIndicadores(AEPL, 3)["divida-liquida-ebitda"];
    expect(ind.valor).toBeCloseTo(3_556_587.41 / (84_135.73 * 4), 6);
  });
});

describe("calcularStatus", () => {
  it("maior é melhor: o limite de Bom já é Bom; abaixo do risco é Risco", () => {
    const lc = CONFIG_POR_CODIGO["liquidez-corrente"];
    expect(calcularStatus(1.5, lc)).toBe("bom");
    expect(calcularStatus(1.0, lc)).toBe("atencao");
    expect(calcularStatus(0.99, lc)).toBe("risco");
  });

  it("menor é melhor: acima do risco é Risco; até o limite de Bom é Bom", () => {
    const eg = CONFIG_POR_CODIGO["endividamento-geral"];
    expect(calcularStatus(0.6, eg)).toBe("bom");
    expect(calcularStatus(0.8, eg)).toBe("atencao");
    expect(calcularStatus(0.81, eg)).toBe("risco");
  });

  it("sem valor é Verificar", () => {
    expect(calcularStatus(null, CONFIG_POR_CODIGO["liquidez-corrente"])).toBe("verificar");
  });
});

describe("formatação", () => {
  it("abrevia R$ em mil e mi", () => {
    expect(formatarMoedaAbreviada(984_563.27)).toBe("R$ 984,6 mil");
    expect(formatarMoedaAbreviada(3_556_587.41)).toBe("R$ 3,56 mi");
    expect(formatarMoedaAbreviada(-75_670.44, { simbolo: false })).toBe("-75,7 mil");
  });

  it("numa lista, todos os valores saem na escala do maior", () => {
    const escala = escalaDosValores([4_541_150.68, 984_563.27, 3_556_587.41]);
    expect(formatarMoedaAbreviada(984_563.27, { escala })).toBe("R$ 0,98 mi");
  });

  it("variação de percentual é em pontos percentuais e a cor segue a direção", () => {
    const margem = CONFIG_POR_CODIGO["margem-liquida"];
    expect(formatarVariacao(0.074, 0.076, margem)).toEqual({ texto: "-0,20 p.p.", sentido: "piorou" });
    const eg = CONFIG_POR_CODIGO["endividamento-geral"];
    expect(formatarVariacao(0.7179, 0.714, eg)).toEqual({ texto: "+0,39 p.p.", sentido: "piorou" });
  });

  it("variação de múltiplo e de moeda fica na unidade", () => {
    expect(formatarVariacao(3.52, 3.39, CONFIG_POR_CODIGO["divida-liquida-ebitda"])?.texto).toBe("+0,13x");
    expect(formatarVariacao(84_100, 81_000, CONFIG_POR_CODIGO.ebitda)).toEqual({ texto: "+3,1 mil", sentido: "melhorou" });
  });

  it("variação que arredonda para zero sai sem sinal", () => {
    expect(formatarVariacao(0.041, 0.043, CONFIG_POR_CODIGO["liquidez-imediata"])).toEqual({ texto: "0,00", sentido: "estavel" });
    expect(formatarVariacao(0.99521, 0.99518, CONFIG_POR_CODIGO["composicao-endividamento"]))
      .toEqual({ texto: "0,00 p.p.", sentido: "estavel" });
    expect(formatarVariacao(84_120, 84_100, CONFIG_POR_CODIGO.ebitda)).toEqual({ texto: "0,0 mil", sentido: "estavel" });
  });

  it("sem base de comparação não há variação", () => {
    expect(formatarVariacao(1.02, null, CONFIG_POR_CODIGO["liquidez-corrente"])).toBeNull();
  });
});

describe("visão da diretoria — caso AEPL", () => {
  const dados = dadosAepl();
  const pilares = montarPilares(dados);

  it("pilares: Resultado Risco, Caixa Bom, Dívida Atenção, Curto prazo Atenção", () => {
    expect(pilares.map((p) => [p.codigo, p.status])).toEqual([
      ["resultado", "risco"], ["caixa", "bom"], ["divida", "atencao"], ["curtoPrazo", "atencao"],
    ]);
    expect(pilares.map((p) => p.valor)).toEqual(["-75,7 mil", "984,6 mil", "3,5x o EBITDA", "1,02"]);
    expect(pilares[0].frase).toBe("Prejuízo veio do IR e CS");
    expect(pilares[1].frase).toBe("Subiu R$ 825,0 mil no mês");
    expect(pilares[3].frase).toBe("99,5% do passivo vence em até 12 meses");
  });

  it("veredito: Risco, começando pelo que está bem", () => {
    expect(gerarVeredito(pilares, { meses: 1 })).toEqual({
      status: "risco",
      frase: "A operação gera caixa, mas a dívida pede atenção, a folga para as contas de curto prazo é pequena e o mês fechou com prejuízo.",
    });
  });

  it("veredito sem pilar bom não usa \"mas\"", () => {
    const ruins = pilares.map((p) => ({ ...p, status: "risco" as const }));
    expect(gerarVeredito(ruins, { meses: 3 }).frase).toBe(
      "O período fechou com prejuízo, a operação consumiu caixa, a dívida está alta para a geração de caixa e o ativo de curto prazo não cobre as contas do próximo ano.");
  });

  it("alertas esperados, do mais grave ao menos grave", () => {
    expect(gerarAlertas(dados)).toEqual([
      { severidade: "risco", texto: "R$ 24,36 mi em obrigações vencem em até 12 meses, e o caixa de R$ 984,6 mil cobre 4% disso." },
      { severidade: "atencao", texto: "O mês fechou com prejuízo de R$ 75,7 mil porque o IR e CS de R$ 77,3 mil superou o resultado antes dos impostos, de R$ 1,6 mil." },
      { severidade: "info", texto: "R$ 359,7 mil da variação de caixa ainda estão sem classificação na contabilidade." },
    ]);
  });

  it("pontos a verificar do contador", () => {
    expect(pontosAVerificar(dados)).toEqual(["Custos zerados com R$ 20,39 mi em estoque", "R$ 359,7 mil na DFC sem classificação"]);
  });
});

describe("montarCascata", () => {
  it("sai da receita e fecha no lucro líquido", () => {
    const degraus = montarCascata(AEPL);
    expect(degraus.map((d) => d.rotulo)).toEqual([
      "Receita líquida", "Despesas", "EBITDA", "Depreciação", "Juros e financeiro", "IR e CS", "Lucro líquido",
    ]);
    expect(degraus[1].valor).toBeCloseTo(-103_395.23, 2);
    expect(degraus[degraus.length - 2].ate).toBeCloseTo(AEPL.lucroLiquido, 2);
  });

  it("acrescenta custos e equivalência só quando existem", () => {
    const base = { ...AEPL, custos: 50_000, lucroBruto: 137_530.96, lucroLiquido: -45_670.44 };
    const rotulos = montarCascata(base).map((d) => d.rotulo);
    expect(rotulos).toContain("Custos");
    expect(rotulos).toContain("Equivalência e outros");
  });
});
