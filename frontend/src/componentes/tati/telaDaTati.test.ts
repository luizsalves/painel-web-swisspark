import { describe, expect, it } from "vitest";
import { filtrosPadrao } from "../../utils/filtrosPainel";
import type { FiltrosPainel } from "../../utils/filtrosPainel";
import { descreverContexto, pedidoDaTela, telaDaTati } from "./telaDaTati";

const filtros = (parcial: Partial<FiltrosPainel>): FiltrosPainel => ({ ...filtrosPadrao(), ...parcial });

// Empresa da coligada 17, filial 3, conta e status escolhidos, julho a setembro.
const comTudo = filtros({
  empresa: "17:EMPRESA TESTE LTDA", empresaNome: " EMPRESA TESTE LTDA ", filial: "3", conta: "1.1.01", status: "Pendente",
  ano: 2026, mesInicial: 7, mesFinal: 9,
});

describe("telaDaTati", () => {
  it("lê a aba do Balancete e a visão dos Indicadores pelo endereço", () => {
    expect(telaDaTati("/balancete", "")).toMatchObject({ tela: "Balancete", aba: "Balancete Resumo" });
    expect(telaDaTati("/balancete", "?aba=balanco")).toMatchObject({ tela: "Balancete", aba: "Balanço" });
    expect(telaDaTati("/balancete", "?aba=dfc")).toMatchObject({ tela: "Balancete", aba: "Balanço DFC" });
    expect(telaDaTati("/indicadores", "")).toMatchObject({ tela: "Indicadores", aba: "Índices financeiros" });
    expect(telaDaTati("/indicadores", "?visao=diretoria")).toMatchObject({ tela: "Indicadores", aba: "Visão da diretoria" });
    expect(telaDaTati("/dashboard/", "")).toEqual({ tela: "Produtividade", campos: ["empresa", "filial", "conta", "periodo", "status"] });
  });

  it("some na tela da Tati, no login e em endereço desconhecido", () => {
    expect(telaDaTati("/ia-planning", "")).toBeNull();
    expect(telaDaTati("/login", "")).toBeNull();
    expect(telaDaTati("/trocar-senha", "")).toBeNull();
    expect(telaDaTati("/", "")).toBeNull();
  });
});

describe("pedidoDaTela", () => {
  it("na Produtividade vai tudo: período, empresa, coligada, filial, conta e status", () => {
    expect(pedidoDaTela(telaDaTati("/dashboard", "")!, comTudo, false)).toEqual({
      tela: "Produtividade", aba: undefined, anoMes: "2026-09", anoMesInicial: "2026-07",
      empresa: "EMPRESA TESTE LTDA", codColigada: 17, codFilial: 3, codConta: "1.1.01", status: "Pendente",
    });
  });

  it("no Balanço vai só o que a aba usa: período, empresa e filial", () => {
    expect(pedidoDaTela(telaDaTati("/balancete", "?aba=balanco")!, comTudo, false)).toEqual({
      tela: "Balancete", aba: "Balanço", anoMes: "2026-09", anoMesInicial: "2026-07",
      empresa: "EMPRESA TESTE LTDA", codColigada: 17, codFilial: 3, codConta: undefined, status: undefined,
    });
  });

  it("nas telas de um mês só vai o mês final, sem o inicial", () => {
    const pedido = pedidoDaTela(telaDaTati("/balancete", "")!, comTudo, false);
    expect(pedido.anoMes).toBe("2026-09");
    expect(pedido.anoMesInicial).toBeUndefined();
    expect(pedido.codConta).toBe("1.1.01");
  });

  it("empresa consolidada não manda filial, e sem empresa não vão filial nem conta", () => {
    expect(pedidoDaTela(telaDaTati("/dashboard", "")!, comTudo, true).codFilial).toBeUndefined();
    const semEmpresa = pedidoDaTela(telaDaTati("/dashboard", "")!, { ...comTudo, empresa: null }, false);
    expect(semEmpresa).toMatchObject({ empresa: undefined, codColigada: undefined, codFilial: undefined, codConta: undefined, status: "Pendente" });
  });

  it("período de um mês não manda o mês inicial", () => {
    const umMes = filtros({ ano: 2026, mesInicial: 9, mesFinal: 9 });
    expect(pedidoDaTela(telaDaTati("/indicadores", "")!, umMes, false)).toMatchObject({ anoMes: "2026-09", anoMesInicial: undefined });
  });
});

describe("descreverContexto", () => {
  it("mostra a tela, a aba e os filtros que a tela usa", () => {
    expect(descreverContexto(telaDaTati("/balancete", "?aba=balanco")!, comTudo, false))
      .toBe("Balancete · Balanço · EMPRESA TESTE LTDA · filial 3 · Jul a Set de 2026");
    expect(descreverContexto(telaDaTati("/fechamentos", "")!, comTudo, true))
      .toBe("Fechamentos · EMPRESA TESTE LTDA · conta 1.1.01 · Set de 2026 · Pendente");
    expect(descreverContexto(telaDaTati("/indicadores", "")!, filtros({ ano: 2026 }), false))
      .toBe("Indicadores · Índices financeiros · Todas as empresas · Ano de 2026");
  });
});
