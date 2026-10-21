import { describe, expect, it, vi } from "vitest";
import type { LancamentoTotvs } from "../../interfaces/LancamentoTotvs";

// O módulo também chama a API; aqui só entram as contas dos meses.
vi.mock("../../services/api", () => ({ default: {} }));
const { mesesDoPeriodo, resumirMeses } = await import("./lancamentosDoFechamento");

function linha(dados: Partial<LancamentoTotvs>): LancamentoTotvs {
  return {
    anoMes: "", data: "2026-09-21T00:00:00", empresa: "", codColigada: 1, reduzido: "43", codConta: "2.1.1.03.01.0001",
    codFilial: 1, nomeFilial: "", complemento: "", documento: null, idLancamento: null, idPartida: null, usuario: null,
    debitos: 0, creditos: 0, saldoRazao: null, saldoAnterior: null, ...dados,
  };
}

const resumo = (saldoAnterior: number, saldoRazao: number) => linha({ data: null, saldoAnterior, saldoRazao });

describe("mesesDoPeriodo", () => {
  it("vai de janeiro ao mês do fechamento", () => {
    expect(mesesDoPeriodo("2026-01", "2026-03")).toEqual(["2026-01", "2026-02", "2026-03"]);
  });
});

describe("resumirMeses", () => {
  it("usa a linha-resumo de cada mês", () => {
    const [setembro] = resumirMeses([[
      linha({ debitos: -64.69 }), linha({ debitos: -1351.87 }), linha({ creditos: 1468.68 }), resumo(1416.56, 1468.68),
    ]]);
    expect(setembro.saldoAnterior).toBe(1416.56);
    expect(setembro.saldoMes).toBe(1468.68);
    expect(setembro.debitos).toBeCloseTo(-1416.56, 2);
    expect(setembro.creditos).toBe(1468.68);
    expect(setembro.detalhe).toHaveLength(3);
  });

  it("repete o saldo do mês anterior num mês sem lançamento", () => {
    const [julho, agosto, setembro] = resumirMeses([
      [linha({ creditos: 100 }), resumo(1000, 1100)],
      [],
      [linha({ creditos: 50 }), resumo(1100, 1150)],
    ]);
    expect(julho.saldoMes).toBe(1100);
    expect(agosto).toMatchObject({ saldoAnterior: 1100, saldoMes: 1100, debitos: 0, creditos: 0, detalhe: [] });
    expect(setembro.saldoAnterior).toBe(1100);
  });
});
