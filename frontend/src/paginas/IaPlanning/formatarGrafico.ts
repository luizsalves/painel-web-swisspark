import type { UnidadeGrafico } from "./respostaTati";

const numero = (casas: number) => new Intl.NumberFormat("pt-BR", { minimumFractionDigits: casas, maximumFractionDigits: casas });
const inteiro = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 0 });
const ateDuasCasas = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 });

/** O número sem sinal e o sinal à parte, só quando o que aparece não é zero: -0,004 vira "0,00", não "-0,00". */
function comSinal(valor: number, formato: Intl.NumberFormat): [sinal: string, absoluto: string] {
  const absoluto = formato.format(Math.abs(valor));
  return [valor < 0 && absoluto !== formato.format(0) ? "-" : "", absoluto];
}

/**
 * R$ com o sinal comum na frente ("-R$ 9.616,74"), como no resto do painel. No eixo, a escala
 * é escolhida pelo valor já arredondado: 999.999 é "R$ 1,0 mi", não "R$ 1.000,0 mil".
 */
function reais(valor: number, curto: boolean): string {
  const absoluto = Math.abs(valor);
  const [escala, sufixo, casas]: [number, string, number] = !curto ? [1, "", 2] : absoluto >= 999_950 ? [1_000_000, " mi", 1] : absoluto >= 999.5 ? [1_000, " mil", 1] : [1, "", 0];
  const [sinal, mostrado] = comSinal(valor / escala, numero(casas));
  return `${sinal}R$ ${mostrado}${sufixo}`;
}

/**
 * O valor do gráfico em pt-BR, pela unidade: "R$ 1.234.567,89" (no eixo, "R$ 1,2 mi"), "12,5%",
 * "1,20x", "1,20" (índice) e "12" (contas). null vira "—": é buraco, não zero.
 */
export function formatarValorGrafico(valor: number | null, unidade: UnidadeGrafico | null, curto = false): string {
  if (valor === null || !Number.isFinite(valor)) return "—";
  if (unidade === "R$") return reais(valor, curto);

  const formato = unidade === "%" ? numero(curto ? 1 : 2) : unidade === "x" || unidade === "indice" ? numero(2) : unidade === "contas" ? inteiro : ateDuasCasas;
  const [sinal, mostrado] = comSinal(valor, formato);
  const sufixo = unidade === "%" ? "%" : unidade === "x" ? "x" : "";
  return `${sinal}${mostrado}${sufixo}`;
}
