/**
 * As comparações e os 12 meses da Visão do contador, a partir da base de cada mês
 * (GET /api/indicadores/mensal). Sem React, como as regras.
 */
import type { BaseCalculoIndicadores, BaseCalculoMensal } from "../../interfaces/Indicadores";
import { deslocarMes, mesesDoPeriodo, mesmoPeriodoDoAnoAnterior, periodoAnterior, rotuloPeriodo } from "./periodo";
import { calcularIndicadores } from "./regras";
import type { Indicadores } from "./regras";

/**
 * Quantos meses o histórico lê, terminando no mês final do período: os 12 do minigráfico
 * e da matriz, e mais 12 para trás, que cobrem o período anterior, o mesmo período do ano
 * anterior e o ano anterior inteiro (o período dos filtros nunca passa de um ano).
 */
export const MESES_DO_HISTORICO = 24;

export type OpcaoComparacao = "anterior-e-ano-anterior" | "anterior" | "acumulado-ano-anterior";

export interface PeriodoComparado {
  inicio: string;
  fim: string;
  rotulo: string;
}

/** A coluna comparada com o atual (a da variação) e, numa das opções, o mesmo período do ano anterior. */
export interface ColunasComparacao {
  comparada: PeriodoComparado;
  extra: PeriodoComparado | null;
}

export function colunasDeComparacao(inicio: string, fim: string, opcao: OpcaoComparacao): ColunasComparacao {
  const comRotulo = (periodo: { inicio: string; fim: string }) => ({ ...periodo, rotulo: rotuloPeriodo(periodo.inicio, periodo.fim) });
  if (opcao === "acumulado-ano-anterior") {
    const ano = Number(fim.slice(0, 4)) - 1;
    return { comparada: comRotulo({ inicio: `${ano}-01`, fim: `${ano}-12` }), extra: null };
  }
  return {
    comparada: comRotulo(periodoAnterior(inicio, fim)),
    extra: opcao === "anterior-e-ano-anterior" ? comRotulo(mesmoPeriodoDoAnoAnterior(inicio, fim)) : null,
  };
}

/** Os meses AAAA-MM de `inicio` a `fim`, inclusive. */
export function listarMeses(inicio: string, fim: string) {
  return Array.from({ length: Math.max(0, mesesDoPeriodo(inicio, fim)) }, (_, indice) => deslocarMes(inicio, indice));
}

/**
 * A base de um período a partir das bases dos meses dele, em ordem: o balanço é o do
 * último mês e o resultado se soma — a mesma conta que o backend faz para o período inteiro.
 */
export function combinarBases(bases: BaseCalculoIndicadores[]): BaseCalculoIndicadores {
  const ultima = bases[bases.length - 1];
  const somar = (campo: keyof BaseCalculoIndicadores) => bases.reduce((total, base) => total + base[campo], 0);
  return {
    ativoTotal: ultima.ativoTotal,
    ativoCirculante: ultima.ativoCirculante,
    disponivel: ultima.disponivel,
    estoques: ultima.estoques,
    realizavelLongoPrazo: ultima.realizavelLongoPrazo,
    passivoCirculante: ultima.passivoCirculante,
    passivoNaoCirculante: ultima.passivoNaoCirculante,
    patrimonioLiquido: ultima.patrimonioLiquido,
    dividaBruta: ultima.dividaBruta,
    dividaLiquida: ultima.dividaLiquida,
    receitaLiquida: somar("receitaLiquida"),
    custos: somar("custos"),
    lucroBruto: somar("lucroBruto"),
    resultadoFinanceiro: somar("resultadoFinanceiro"),
    impostoRendaContribuicao: somar("impostoRendaContribuicao"),
    lucroLiquido: somar("lucroLiquido"),
    ebit: somar("ebit"),
    depreciacaoAmortizacao: somar("depreciacaoAmortizacao"),
    ebitda: somar("ebitda"),
  };
}

/** Tudo zerado: a empresa ainda não tinha lançamentos — melhor "—" do que "Caixa líquido R$ 0,00". */
function semDados(base: BaseCalculoIndicadores) {
  return Object.values(base).every((valor) => valor === 0);
}

export interface Comparado {
  base: BaseCalculoIndicadores;
  indicadores: Indicadores;
}

export interface HistoricoIndicadores {
  comparada: Comparado | null;
  extra: Comparado | null;
  /** Os 12 meses até o fim do período, cada um sozinho (o resultado é o do mês). */
  meses: { anoMes: string; indicadores: Indicadores | null }[];
}

/** Os indicadores das colunas de comparação e dos 12 meses; nulo onde falta mês no histórico. */
export function montarHistorico(mensal: BaseCalculoMensal[], fim: string, colunas: ColunasComparacao): HistoricoIndicadores {
  const porMes = new Map(mensal.map((mes) => [mes.anoMes, mes.baseCalculo]));
  const calcular = (inicioPeriodo: string, fimPeriodo: string): Comparado | null => {
    const meses = listarMeses(inicioPeriodo, fimPeriodo);
    const bases = meses.map((anoMes) => porMes.get(anoMes)).filter((base): base is BaseCalculoIndicadores => base !== undefined);
    if (bases.length === 0 || bases.length !== meses.length) return null;
    const base = combinarBases(bases);
    return semDados(base) ? null : { base, indicadores: calcularIndicadores(base, bases.length) };
  };

  return {
    comparada: calcular(colunas.comparada.inicio, colunas.comparada.fim),
    extra: colunas.extra && calcular(colunas.extra.inicio, colunas.extra.fim),
    meses: listarMeses(deslocarMes(fim, -11), fim).map((anoMes) => ({
      anoMes,
      indicadores: calcular(anoMes, anoMes)?.indicadores ?? null,
    })),
  };
}
