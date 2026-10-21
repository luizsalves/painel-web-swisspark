import { describe, expect, it } from "vitest";
import type { BaseCalculoIndicadores, BaseCalculoMensal } from "../../interfaces/Indicadores";
import { colunasDeComparacao, combinarBases, listarMeses, montarHistorico } from "./historico";
import { deslocarMes } from "./periodo";

const ZERADA: BaseCalculoIndicadores = {
  ativoTotal: 0, ativoCirculante: 0, disponivel: 0, estoques: 0, realizavelLongoPrazo: 0,
  passivoCirculante: 0, passivoNaoCirculante: 0, patrimonioLiquido: 0, dividaBruta: 0, dividaLiquida: 0,
  receitaLiquida: 0, custos: 0, lucroBruto: 0, resultadoFinanceiro: 0, impostoRendaContribuicao: 0,
  lucroLiquido: 0, ebit: 0, depreciacaoAmortizacao: 0, ebitda: 0,
};

/** Um mês com o ativo circulante crescendo 10 por mês e um lucro de 5. */
function mes(indice: number): BaseCalculoIndicadores {
  return {
    ...ZERADA,
    ativoTotal: 2000 + indice * 10, ativoCirculante: 1000 + indice * 10, passivoCirculante: 500,
    patrimonioLiquido: 1500, receitaLiquida: 100, custos: 40, lucroBruto: 60, lucroLiquido: 5, ebit: 8, ebitda: 10,
  };
}

/** Os 24 meses que a tela lê para o fechamento de mar/2026. */
function historicoAte(fim: string, base = mes): BaseCalculoMensal[] {
  return listarMeses(deslocarMes(fim, -23), fim).map((anoMes, indice) => ({ anoMes, baseCalculo: base(indice) }));
}

describe("combinarBases", () => {
  it("usa o balanço do último mês e soma o resultado", () => {
    const base = combinarBases([mes(0), mes(1), mes(2)]);
    expect(base.ativoCirculante).toBe(1020);
    expect(base.patrimonioLiquido).toBe(1500);
    expect(base.lucroLiquido).toBe(15);
    expect(base.receitaLiquida).toBe(300);
    expect(base.ebitda).toBe(30);
  });
});

describe("colunasDeComparacao", () => {
  it("compara um mês com o anterior e com o mesmo mês do ano anterior", () => {
    const colunas = colunasDeComparacao("2026-03", "2026-03", "anterior-e-ano-anterior");
    expect(colunas.comparada).toMatchObject({ inicio: "2026-02", fim: "2026-02", rotulo: "Fev/26" });
    expect(colunas.extra).toMatchObject({ inicio: "2025-03", fim: "2025-03", rotulo: "Mar/25" });
  });

  it("compara um trimestre com o trimestre anterior", () => {
    const colunas = colunasDeComparacao("2026-01", "2026-03", "anterior-e-ano-anterior");
    expect(colunas.comparada).toMatchObject({ inicio: "2025-10", fim: "2025-12", rotulo: "Out–Dez/25" });
    expect(colunas.extra?.rotulo).toBe("Jan–Mar/25");
  });

  it("sem a terceira coluna quando é só o período anterior", () => {
    expect(colunasDeComparacao("2026-03", "2026-03", "anterior").extra).toBeNull();
  });

  it("o acumulado do ano anterior é janeiro a dezembro", () => {
    const colunas = colunasDeComparacao("2026-01", "2026-03", "acumulado-ano-anterior");
    expect(colunas.comparada).toMatchObject({ inicio: "2025-01", fim: "2025-12", rotulo: "Jan–Dez/25" });
    expect(colunas.extra).toBeNull();
  });
});

describe("montarHistorico", () => {
  const colunas = colunasDeComparacao("2026-03", "2026-03", "anterior-e-ano-anterior");

  it("calcula o mês anterior, o do ano anterior e os 12 meses", () => {
    const historico = montarHistorico(historicoAte("2026-03"), "2026-03", colunas);
    // Fev/26 é o 23º mês (índice 22): ativo circulante 1220 ÷ passivo 500.
    expect(historico.comparada?.indicadores["liquidez-corrente"].valor).toBeCloseTo(2.44, 6);
    // Mar/25 é o índice 11: 1110 ÷ 500.
    expect(historico.extra?.indicadores["liquidez-corrente"].valor).toBeCloseTo(2.22, 6);
    expect(historico.meses.map((item) => item.anoMes)).toEqual(listarMeses("2025-04", "2026-03"));
    expect(historico.meses[11].indicadores?.["liquidez-corrente"].valor).toBeCloseTo(2.46, 6);
  });

  it("o acumulado do ano anterior soma o resultado dos 12 meses", () => {
    const anual = colunasDeComparacao("2026-01", "2026-03", "acumulado-ano-anterior");
    const historico = montarHistorico(historicoAte("2026-03"), "2026-03", anual);
    expect(historico.comparada?.base.lucroLiquido).toBe(60);
    expect(historico.comparada?.indicadores["margem-liquida"].valor).toBeCloseTo(0.05, 6);
  });

  it("mês sem lançamento nenhum fica sem valor, em vez de 'Caixa líquido R$ 0'", () => {
    // Empresa que começou em jun/2025: tudo antes zerado.
    const historico = montarHistorico(historicoAte("2026-03", (indice) => (indice < 14 ? ZERADA : mes(indice))), "2026-03", colunas);
    expect(historico.extra).toBeNull();
    expect(historico.meses[0].indicadores).toBeNull();
    expect(historico.meses[11].indicadores).not.toBeNull();
  });

  it("sem os meses do período comparado, a comparação fica vazia", () => {
    const historico = montarHistorico(historicoAte("2026-03").slice(-3), "2026-03", colunas);
    expect(historico.comparada).not.toBeNull();
    expect(historico.extra).toBeNull();
    expect(historico.meses.filter((item) => item.indicadores !== null)).toHaveLength(3);
  });
});
