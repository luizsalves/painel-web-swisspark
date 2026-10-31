import { describe, expect, it } from "vitest";
import { destinoDoCard } from "./cardsDaTati";

describe("destinoDoCard", () => {
  it("leva cada tipo de alvo à tela, à aba e ao card", () => {
    expect(destinoDoCard("indicador/LIQ_CORRENTE")).toEqual({ caminho: "/indicadores", busca: { visao: "contador" }, ancora: "indicador-liquidez-corrente" });
    expect(destinoDoCard("indicador/div_liq_ebitda_12m")?.ancora).toBe("indicador-divida-liquida-ebitda");
    expect(destinoDoCard("pilar/curtoPrazo")).toEqual({ caminho: "/indicadores", busca: { visao: "diretoria" }, ancora: "pilar-curtoPrazo" });
    expect(destinoDoCard("veredito")?.ancora).toBe("veredito");
    expect(destinoDoCard("resultado/lucroLiquido")).toEqual({ caminho: "/indicadores", busca: { visao: "diretoria" }, ancora: "resultado" });
    expect(destinoDoCard("resultado")?.ancora).toBe("resultado");
    expect(destinoDoCard("dfc/operacional")).toEqual({ caminho: "/indicadores", busca: { visao: "contador" }, ancora: "dfc" });
    expect(destinoDoCard("balanco/ativoCirculante")).toEqual({ caminho: "/balancete", busca: { aba: "balanco" }, ancora: "balanco-ativo" });
    expect(destinoDoCard("balanco/patrimonioLiquido")?.ancora).toBe("balanco-passivo");
    expect(destinoDoCard("balanco/passivo")?.ancora).toBe("balanco-passivo");
    expect(destinoDoCard("conciliacao/pendentes")).toEqual({ caminho: "/dashboard", busca: {}, ancora: "conciliacao-pendentes" });
    expect(destinoDoCard("conciliacao/empresas")?.ancora).toBe("conciliacao-empresas");
  });

  it("os 13 indicadores da Tati têm card na tela", () => {
    const codigos = [
      "LIQ_CORRENTE", "LIQ_SECA", "LIQ_IMEDIATA", "LIQ_GERAL", "END_GERAL", "END_CURTO_PRAZO", "DIV_LIQ_EBITDA_12M",
      "DIV_BRUTA_PL", "ROE", "ROA", "MARGEM_LIQUIDA", "MARGEM_BRUTA", "EBITDA",
    ];
    expect(codigos.every((codigo) => destinoDoCard(`indicador/${codigo}`) !== null)).toBe(true);
  });

  it("alvo desconhecido não tem destino", () => {
    for (const alvo of ["indicador/NAO_EXISTE", "pilar/outro", "veredito/x", "balanco/outro", "tela/qualquer", "dfc/operacional/x", ""]) {
      expect(destinoDoCard(alvo)).toBeNull();
    }
  });
});
