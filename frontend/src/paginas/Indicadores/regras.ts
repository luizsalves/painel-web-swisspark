/**
 * Regras dos indicadores, sem React: cálculo a partir da base, status pela faixa,
 * formatação, alertas, pilares e veredito. As duas visões usam só o que sai daqui.
 */
import type { BaseCalculoIndicadores, Dfc } from "../../interfaces/Indicadores";
import { CONFIG_POR_CODIGO, INDICADORES } from "./config";
import type { CodigoIndicador, ConfigIndicador } from "./config";
import { palavraPeriodo } from "./periodo";

export type Status = "bom" | "atencao" | "risco" | "verificar";

export const ROTULO_STATUS: Record<Status, string> = {
  bom: "Bom", atencao: "Atenção", risco: "Risco", verificar: "Verificar",
};

/** Do melhor para o pior: o status geral é o pior entre os pilares. */
const GRAVIDADE: Record<Status, number> = { bom: 0, verificar: 1, atencao: 2, risco: 3 };

export function piorStatus(lista: Status[]): Status {
  return lista.reduce<Status>((pior, atual) => (GRAVIDADE[atual] > GRAVIDADE[pior] ? atual : pior), "bom");
}

// ── Formatação ──────────────────────────────────────────────────────────────

function numero(valor: number, casas: number) {
  return new Intl.NumberFormat("pt-BR", { minimumFractionDigits: casas, maximumFractionDigits: casas }).format(valor);
}

/** Sinal de menos comum ("-"), como nos esboços e no resto do painel. */
function comSinal(valor: number, texto: string, mostrarMais = false) {
  if (valor < 0) return `-${texto}`;
  return mostrarMais && valor > 0 ? `+${texto}` : texto;
}

/** R$ em mil com uma casa: "1.036,2 mil". Com `sinal`, positivos ganham "+". */
export function formatarMil(valor: number, { sinal = false } = {}) {
  return comSinal(valor, `${numero(Math.abs(valor) / 1000, 1)} mil`, sinal);
}

export type EscalaMoeda = "mi" | "mil" | "unidade";

/** A escala do maior valor — para uma lista de valores sair toda na mesma unidade. */
export function escalaDosValores(valores: number[]): EscalaMoeda {
  const maior = Math.max(0, ...valores.map(Math.abs));
  return maior >= 1_000_000 ? "mi" : maior >= 1_000 ? "mil" : "unidade";
}

/** R$ abreviado para a diretoria: "R$ 984,6 mil", "R$ 3,56 mi". Sem `escala`, cada valor escolhe a sua. */
export function formatarMoedaAbreviada(valor: number, { simbolo = true, sinal = false, escala }: {
  simbolo?: boolean; sinal?: boolean; escala?: EscalaMoeda;
} = {}) {
  const absoluto = Math.abs(valor);
  const corpo = {
    mi: () => `${numero(absoluto / 1_000_000, 2)} mi`,
    mil: () => `${numero(absoluto / 1_000, 1)} mil`,
    unidade: () => numero(absoluto, 2),
  }[escala ?? escalaDosValores([valor])]();
  return comSinal(valor, simbolo ? `R$ ${corpo}` : corpo, sinal);
}

/** Valor do indicador na unidade dele: "1,02", "71,79%", "3,52x", "84,1 mil". */
export function formatarValor(valor: number | null, config: ConfigIndicador) {
  if (valor === null || !Number.isFinite(valor)) return "—";
  switch (config.unidade) {
    case "percentual": return `${numero(valor * 100, config.casas)}%`;
    case "multiplo": return `${numero(valor, config.casas)}x`;
    case "moeda": return formatarMil(valor);
    default: return numero(valor, config.casas);
  }
}

/** O que a tela mostra no lugar do número: o texto ("Caixa líquido") ou o valor formatado. */
export function textoDoValor(indicador: ResultadoIndicador) {
  return indicador.textoValor ?? formatarValor(indicador.valor, indicador.config);
}

export type Sentido = "melhorou" | "piorou" | "estavel";

/**
 * Variação na unidade do indicador — percentuais em pontos percentuais, nunca
 * em % relativo. O sentido diz se melhorou ou piorou conforme a direção do
 * indicador (endividamento subindo é piora).
 */
export function formatarVariacao(atual: number | null, anterior: number | null, config: ConfigIndicador):
  { texto: string; sentido: Sentido } | null {
  if (atual === null || anterior === null || !Number.isFinite(atual) || !Number.isFinite(anterior)) return null;
  // Arredonda na unidade que aparece na tela (pontos percentuais, mil reais).
  const escala = config.unidade === "percentual" ? 100 : config.unidade === "moeda" ? 1 / 1000 : 1;
  const arredondada = Math.round((atual - anterior) * escala * 10 ** config.casas);
  // O que arredonda para zero sai sem sinal: "0,00", não "-0,00".
  const diferenca = arredondada === 0 ? 0 : atual - anterior;
  const texto = config.unidade === "percentual"
    ? `${comSinal(diferenca, numero(Math.abs(diferenca) * 100, config.casas), true)} p.p.`
    : config.unidade === "multiplo"
      ? `${comSinal(diferenca, numero(Math.abs(diferenca), config.casas), true)}x`
      : config.unidade === "moeda"
        ? formatarMil(diferenca, { sinal: true })
        : comSinal(diferenca, numero(Math.abs(diferenca), config.casas), true);
  const sentido: Sentido = arredondada === 0
    ? "estavel"
    : (config.direcao === "maior_melhor") === diferenca > 0 ? "melhorou" : "piorou";
  return { texto, sentido };
}

// ── Status e cálculo ────────────────────────────────────────────────────────

export function calcularStatus(valor: number | null, config: ConfigIndicador): Status {
  if (valor === null || !Number.isFinite(valor)) return "verificar";
  if (config.direcao === "maior_melhor") {
    if (valor < config.risco) return "risco";
    return valor >= config.bom ? "bom" : "atencao";
  }
  if (valor > config.risco) return "risco";
  return valor <= config.bom ? "bom" : "atencao";
}

export interface ResultadoIndicador {
  config: ConfigIndicador;
  /** Valor do período, o que aparece na tela; nulo quando não dá para calcular. */
  valor: number | null;
  /** O que decide o status e a posição na régua: o anualizado no ROE e no ROA. */
  valorFaixa: number | null;
  status: Status;
  /** Texto no lugar do número quando o múltiplo não faz sentido. */
  textoValor?: string;
  /** Linha pequena embaixo do valor. */
  detalheValor?: string;
  /** Observação em âmbar embaixo do nome. */
  nota?: string;
  /** Texto no lugar da régua. */
  semFaixa?: string;
}

export type Indicadores = Record<CodigoIndicador, ResultadoIndicador>;

function dividir(numerador: number, denominador: number) {
  return denominador === 0 ? null : numerador / denominador;
}

/** Resultado antes do IR e CS (inclui equivalência e o que mais não entra no EBIT). */
export function resultadoAntesDoIr(base: BaseCalculoIndicadores) {
  return base.lucroLiquido + base.impostoRendaContribuicao;
}

/** Custo zerado com estoque: a margem bruta sairia 100% sem ser verdade. */
export function custosZeradosComEstoque(base: BaseCalculoIndicadores) {
  return base.custos === 0 && base.estoques > 0;
}

/**
 * Os indicadores do período a partir da base de cálculo. As fórmulas são as do
 * backend, com as correções combinadas:
 * - sem o PCT (igual ao endividamento geral);
 * - dívida líquida ÷ EBITDA de 12 meses (EBITDA do período × 12 ÷ meses);
 * - caixa líquido, EBITDA negativo e "sem dívida" viram texto, não múltiplo;
 * - margem bruta "Verificar" quando os custos estão zerados com estoque;
 * - ROE e ROA mostram o período, mas o status usa o valor anualizado.
 */
export function calcularIndicadores(base: BaseCalculoIndicadores, meses: number): Indicadores {
  const passivoTotal = base.passivoCirculante + base.passivoNaoCirculante;
  const anualizar = (valor: number | null) => (valor === null ? null : (valor * 12) / meses);
  const periodo = palavraPeriodo(meses);

  const resultado = (codigo: CodigoIndicador, valor: number | null, extra: Partial<ResultadoIndicador> = {}): ResultadoIndicador => {
    const config = CONFIG_POR_CODIGO[codigo];
    const valorFaixa = extra.valorFaixa !== undefined ? extra.valorFaixa : config.statusAnualizado ? anualizar(valor) : valor;
    return {
      config,
      valor,
      valorFaixa,
      status: extra.status ?? calcularStatus(valorFaixa, config),
      ...extra,
    };
  };

  const ebitda12m = (base.ebitda * 12) / meses;
  const dividaLiquidaEbitda = base.dividaLiquida <= 0
    ? resultado("divida-liquida-ebitda", null, {
      valorFaixa: 0, status: "bom", textoValor: "Caixa líquido",
      detalheValor: formatarMoedaAbreviada(-base.dividaLiquida),
    })
    : ebitda12m <= 0
      ? resultado("divida-liquida-ebitda", null, {
        valorFaixa: null, status: "risco", textoValor: "EBITDA negativo", semFaixa: "Múltiplo sem sentido",
      })
      : resultado("divida-liquida-ebitda", base.dividaLiquida / ebitda12m);

  const dividaBrutaPl = base.dividaBruta <= 0
    ? resultado("divida-bruta-pl", null, { valorFaixa: 0, status: "bom", textoValor: "Sem dívida" })
    : base.patrimonioLiquido <= 0
      ? resultado("divida-bruta-pl", null, {
        valorFaixa: null, status: "risco", textoValor: "PL negativo", semFaixa: "Múltiplo sem sentido",
      })
      : resultado("divida-bruta-pl", base.dividaBruta / base.patrimonioLiquido);

  const anualTexto = (valor: number | null) => meses === 12 || valor === null
    ? undefined
    : `≈ ${numero(((valor * 12) / meses) * 100, 1)}% ao ano`;
  const roe = dividir(base.lucroLiquido, base.patrimonioLiquido);
  const roa = dividir(base.lucroLiquido, base.ativoTotal);

  const fatiaEstoques = dividir(base.estoques, base.ativoCirculante);
  const lair = resultadoAntesDoIr(base);

  return {
    "liquidez-corrente": resultado("liquidez-corrente", dividir(base.ativoCirculante, base.passivoCirculante)),
    "liquidez-seca": resultado("liquidez-seca", dividir(base.ativoCirculante - base.estoques, base.passivoCirculante), {
      nota: fatiaEstoques !== null && fatiaEstoques >= 0.5
        ? `Estoques são ${numero(fatiaEstoques * 100, 0)}% do ativo circulante` : undefined,
    }),
    "liquidez-imediata": resultado("liquidez-imediata", dividir(base.disponivel, base.passivoCirculante)),
    "liquidez-geral": resultado("liquidez-geral",
      dividir(base.ativoCirculante + base.realizavelLongoPrazo, passivoTotal)),
    "endividamento-geral": resultado("endividamento-geral", dividir(passivoTotal, base.ativoTotal)),
    "composicao-endividamento": resultado("composicao-endividamento", dividir(base.passivoCirculante, passivoTotal)),
    "divida-liquida-ebitda": dividaLiquidaEbitda,
    "divida-bruta-pl": dividaBrutaPl,
    roe: resultado("roe", roe, { detalheValor: anualTexto(roe) }),
    roa: resultado("roa", roa, { detalheValor: anualTexto(roa) }),
    "margem-liquida": resultado("margem-liquida", dividir(base.lucroLiquido, base.receitaLiquida), {
      nota: base.lucroLiquido < 0 && lair > 0 ? "IR e CS acima do resultado antes dos impostos" : undefined,
    }),
    "margem-bruta": custosZeradosComEstoque(base)
      ? resultado("margem-bruta", dividir(base.lucroBruto, base.receitaLiquida), {
        valorFaixa: null, status: "verificar",
        nota: `Custos zerados no ${periodo}: conferir o de-para`, semFaixa: "Sem custos para comparar",
      })
      : resultado("margem-bruta", dividir(base.lucroBruto, base.receitaLiquida)),
    ebitda: resultado("ebitda", base.ebitda, { semFaixa: "Sem faixa definida" }),
  };
}

/** Os indicadores na ordem da configuração, para listar. */
export function emOrdem(indicadores: Indicadores) {
  return INDICADORES.map((config) => indicadores[config.codigo]);
}

// ── DFC ─────────────────────────────────────────────────────────────────────

export interface TotaisDfc {
  operacionais: number;
  financiamento: number;
  investimentos: number;
  naoClassificadas: number;
  saldoInicial: number;
  saldoFinal: number;
  variacao: number;
}

const GRUPO_DFC: Record<string, keyof Pick<TotaisDfc, "operacionais" | "financiamento" | "investimentos" | "naoClassificadas">> = {
  "Atividades Operacionais": "operacionais",
  "Atividades de Financiamento": "financiamento",
  "Atividades de Investimentos": "investimentos",
  "Variações não classificadas": "naoClassificadas",
};

export function totaisDaDfc(dfc: Dfc): TotaisDfc {
  const totais: TotaisDfc = {
    operacionais: 0, financiamento: 0, investimentos: 0, naoClassificadas: 0,
    saldoInicial: dfc.saldoInicialCaixa, saldoFinal: dfc.saldoFinalCaixa, variacao: dfc.variacaoCaixa,
  };
  for (const grupo of dfc.grupos) {
    const campo = GRUPO_DFC[grupo.nome];
    if (campo) totais[campo] += grupo.total;
  }
  return totais;
}

// ── Pontos a verificar, alertas, pilares e veredito ─────────────────────────

export interface DadosPainel {
  base: BaseCalculoIndicadores;
  indicadores: Indicadores;
  dfc: TotaisDfc;
  meses: number;
}

/** O que o contador deve conferir antes de fechar: custos zerados e DFC sem classificação. */
export function pontosAVerificar({ base, dfc }: DadosPainel): string[] {
  const pontos: string[] = [];
  if (custosZeradosComEstoque(base)) pontos.push(`Custos zerados com ${formatarMoedaAbreviada(base.estoques)} em estoque`);
  if (dfc.naoClassificadas !== 0) pontos.push(`${formatarMoedaAbreviada(Math.abs(dfc.naoClassificadas))} na DFC sem classificação`);
  return pontos;
}

export type Severidade = "risco" | "atencao" | "info";

export interface Alerta {
  severidade: Severidade;
  texto: string;
}

const ORDEM_SEVERIDADE: Record<Severidade, number> = { risco: 0, atencao: 1, info: 2 };
const MAXIMO_DE_ALERTAS = 5;

/** Até 5 alertas, do mais grave ao menos grave, com os valores na frase. */
export function gerarAlertas({ base, indicadores, dfc, meses }: DadosPainel): Alerta[] {
  const alertas: Alerta[] = [];
  const periodo = palavraPeriodo(meses);
  const reais = (valor: number) => formatarMoedaAbreviada(valor);
  const ce = indicadores["composicao-endividamento"].valor;
  const lc = indicadores["liquidez-corrente"].valor;
  const lair = resultadoAntesDoIr(base);

  if (ce !== null && ce > 0.7 && lc !== null && lc < 1.5 && base.passivoCirculante > 0) {
    const cobertura = numero((base.disponivel / base.passivoCirculante) * 100, 0);
    alertas.push({
      severidade: "risco",
      texto: `${reais(base.passivoCirculante)} em obrigações vencem em até 12 meses, e o caixa de ${reais(base.disponivel)} cobre ${cobertura}% disso.`,
    });
  }
  if (base.lucroLiquido < 0 && lair > 0) {
    alertas.push({
      severidade: "atencao",
      texto: `O ${periodo} fechou com prejuízo de ${reais(-base.lucroLiquido)} porque o IR e CS de ${reais(base.impostoRendaContribuicao)} superou o resultado antes dos impostos, de ${reais(lair)}.`,
    });
  } else if (base.lucroLiquido < 0) {
    alertas.push({ severidade: "risco", texto: `O ${periodo} fechou com prejuízo de ${reais(-base.lucroLiquido)}.` });
  }
  if (lc !== null && lc < 1) {
    alertas.push({ severidade: "risco", texto: "O ativo de curto prazo não cobre o passivo de curto prazo." });
  }
  const multiplo = indicadores["divida-liquida-ebitda"].valor;
  if (multiplo !== null && multiplo > 4) {
    alertas.push({
      severidade: "risco",
      texto: `A dívida líquida equivale a ${numero(multiplo, 1)} anos de geração de caixa da operação.`,
    });
  }
  if (dfc.naoClassificadas !== 0) {
    alertas.push({
      severidade: "info",
      texto: `${reais(Math.abs(dfc.naoClassificadas))} da variação de caixa ainda estão sem classificação na contabilidade.`,
    });
  }
  // sort é estável: dentro da mesma severidade fica a ordem das regras.
  return alertas.sort((a, b) => ORDEM_SEVERIDADE[a.severidade] - ORDEM_SEVERIDADE[b.severidade]).slice(0, MAXIMO_DE_ALERTAS);
}

export type CodigoPilar = "resultado" | "caixa" | "divida" | "curtoPrazo";

export interface Pilar {
  codigo: CodigoPilar;
  status: Status;
  /** Dívida com mais caixa do que dívida: a frase do veredito muda. */
  semDividaLiquida?: boolean;
}

export interface PilarDetalhado extends Pilar {
  nome: string;
  valor: string;
  frase: string;
}

/** O motivo principal do resultado, seguindo a DRE de cima para baixo. */
function motivoDoResultado(base: BaseCalculoIndicadores) {
  if (base.lucroLiquido >= 0) {
    return base.receitaLiquida > 0
      ? `Lucro de ${numero((base.lucroLiquido / base.receitaLiquida) * 100, 1)}% da receita`
      : "Resultado sem receita no período";
  }
  if (base.ebitda < 0) return "A operação ficou negativa";
  if (base.ebit < 0) return "A depreciação levou ao prejuízo";
  if (base.ebit + base.resultadoFinanceiro < 0) return "Juros e resultado financeiro levaram ao prejuízo";
  if (resultadoAntesDoIr(base) > 0) return "Prejuízo veio do IR e CS";
  return "Prejuízo antes dos impostos";
}

export function montarPilares({ base, indicadores, dfc, meses }: DadosPainel): PilarDetalhado[] {
  const periodo = palavraPeriodo(meses);
  const margem = indicadores["margem-liquida"];
  const statusResultado: Status = margem.valor !== null
    ? margem.status
    : base.lucroLiquido > 0 ? "bom" : base.lucroLiquido < 0 ? "risco" : "atencao";

  const variacaoCaixa = dfc.saldoFinal - dfc.saldoInicial;
  const statusCaixa: Status = dfc.operacionais > 0 ? "bom" : dfc.operacionais < 0 ? "risco" : "atencao";

  const multiplo = indicadores["divida-liquida-ebitda"];
  const semDividaLiquida = base.dividaLiquida <= 0;
  const valorDivida = semDividaLiquida
    ? "Sem dívida líquida"
    : multiplo.valor === null ? "EBITDA negativo" : `${numero(multiplo.valor, 1)}x o EBITDA`;
  const fraseDivida = semDividaLiquida
    ? `Caixa supera a dívida em ${formatarMoedaAbreviada(-base.dividaLiquida)}`
    : `Dívida líquida de ${formatarMoedaAbreviada(base.dividaLiquida)}`;

  const ce = indicadores["composicao-endividamento"].valor;
  const liquidez = indicadores["liquidez-corrente"];

  return [
    {
      codigo: "resultado", nome: `Resultado do ${periodo}`, status: statusResultado,
      valor: formatarMoedaAbreviada(base.lucroLiquido, { simbolo: false }), frase: motivoDoResultado(base),
    },
    {
      codigo: "caixa", nome: "Caixa", status: statusCaixa,
      valor: formatarMoedaAbreviada(dfc.saldoFinal, { simbolo: false }),
      frase: variacaoCaixa === 0
        ? `Ficou estável no ${periodo}`
        : `${variacaoCaixa > 0 ? "Subiu" : "Caiu"} ${formatarMoedaAbreviada(Math.abs(variacaoCaixa))} no ${periodo}`,
    },
    { codigo: "divida", nome: "Dívida", status: multiplo.status, semDividaLiquida, valor: valorDivida, frase: fraseDivida },
    {
      codigo: "curtoPrazo", nome: "Contas de curto prazo", status: liquidez.status,
      valor: formatarValor(liquidez.valor, liquidez.config),
      frase: ce === null ? "Sem passivo registrado" : `${numero(ce * 100, 1)}% do passivo vence em até 12 meses`,
    },
  ];
}

function frasesDoPilar(pilar: Pilar, periodo: string): Record<Status, string> {
  switch (pilar.codigo) {
    case "resultado":
      return {
        bom: `o ${periodo} deu lucro`, atencao: "o resultado ficou perto de zero",
        risco: `o ${periodo} fechou com prejuízo`, verificar: "o resultado precisa ser conferido",
      };
    case "caixa":
      return {
        bom: "a operação gera caixa", atencao: "a operação não gerou caixa",
        risco: "a operação consumiu caixa", verificar: "o caixa precisa ser conferido",
      };
    case "divida":
      return {
        bom: pilar.semDividaLiquida ? "a empresa não tem dívida líquida" : "a dívida está sob controle",
        atencao: "a dívida pede atenção", risco: "a dívida está alta para a geração de caixa",
        verificar: "a dívida precisa ser conferida",
      };
    case "curtoPrazo":
      return {
        bom: "as contas de curto prazo estão cobertas", atencao: "a folga para as contas de curto prazo é pequena",
        risco: "o ativo de curto prazo não cobre as contas do próximo ano",
        verificar: "as contas de curto prazo precisam ser conferidas",
      };
  }
}

function juntar(partes: string[]) {
  return partes.length <= 1 ? (partes[0] ?? "") : `${partes.slice(0, -1).join(", ")} e ${partes[partes.length - 1]}`;
}

/**
 * Status geral (o pior entre os pilares) e a frase: primeiro o que está bem,
 * depois "mas" e o que preocupa, terminando no mais grave.
 */
export function gerarVeredito(pilares: Pilar[], { meses = 1 } = {}): { status: Status; frase: string } {
  const periodo = palavraPeriodo(meses);
  const bons = pilares.filter((pilar) => pilar.status === "bom");
  const ruins = pilares
    .filter((pilar) => pilar.status !== "bom")
    .map((pilar, ordem) => ({ pilar, ordem }))
    .sort((a, b) => GRAVIDADE[a.pilar.status] - GRAVIDADE[b.pilar.status] || a.ordem - b.ordem)
    .map(({ pilar }) => pilar);

  const texto = (lista: Pilar[]) => juntar(lista.map((pilar) => frasesDoPilar(pilar, periodo)[pilar.status]));
  const frase = bons.length > 0 && ruins.length > 0
    ? `${texto(bons)}, mas ${texto(ruins)}.`
    : `${texto(bons.length > 0 ? bons : ruins)}.`;

  return { status: piorStatus(pilares.map((pilar) => pilar.status)), frase: frase.charAt(0).toUpperCase() + frase.slice(1) };
}

// ── Cascata da receita ao lucro ─────────────────────────────────────────────

export interface DegrauCascata {
  rotulo: string;
  tipo: "total" | "passo";
  /** Total: o próprio valor. Passo: quanto somou (positivo) ou tirou (negativo). */
  valor: number;
  /** Onde a barra começa e termina, no acumulado. */
  de: number;
  ate: number;
}

/**
 * Receita → custos → despesas → EBITDA → depreciação → financeiro → IR e CS →
 * lucro. O que o EBIT deixa de fora (equivalência patrimonial e afins) entra
 * como um passo próprio, só quando existe, para a cascata fechar no lucro.
 */
export function montarCascata(base: BaseCalculoIndicadores): DegrauCascata[] {
  const degraus: DegrauCascata[] = [];
  let acumulado = 0;
  const total = (rotulo: string, valor: number) => {
    degraus.push({ rotulo, tipo: "total", valor, de: 0, ate: valor });
    acumulado = valor;
  };
  const passo = (rotulo: string, valor: number) => {
    degraus.push({ rotulo, tipo: "passo", valor, de: acumulado, ate: acumulado + valor });
    acumulado += valor;
  };

  total("Receita líquida", base.receitaLiquida);
  if (base.custos !== 0) passo("Custos", -base.custos);
  passo("Despesas", base.ebitda - acumulado);
  total("EBITDA", base.ebitda);
  passo("Depreciação", base.ebit - base.ebitda);
  passo("Juros e financeiro", base.resultadoFinanceiro);
  const outros = base.lucroLiquido + base.impostoRendaContribuicao - (base.ebit + base.resultadoFinanceiro);
  if (Math.abs(outros) >= 0.005) passo("Equivalência e outros", outros);
  passo("IR e CS", -base.impostoRendaContribuicao);
  total("Lucro líquido", base.lucroLiquido);
  return degraus;
}

/** A frase de leitura embaixo da cascata. */
export function lerCascata(base: BaseCalculoIndicadores, { meses, nomeDoMes }: { meses: number; nomeDoMes: string }) {
  const quando = meses === 1 ? nomeDoMes : "o período";
  const ebitda = formatarMoedaAbreviada(base.ebitda);
  const abertura = base.ebitda >= 0
    ? `A operação fechou ${quando} positiva, com EBITDA de ${ebitda}.`
    : `A operação fechou ${quando} negativa, com EBITDA de ${ebitda}.`;
  if (base.lucroLiquido >= 0) return `${abertura} O resultado final foi lucro de ${formatarMoedaAbreviada(base.lucroLiquido)}.`;
  if (base.ebitda < 0) return `${abertura} O resultado final foi prejuízo de ${formatarMoedaAbreviada(-base.lucroLiquido)}.`;
  if (base.ebit < 0) return `${abertura} A depreciação levou o resultado para o negativo.`;
  if (resultadoAntesDoIr(base) <= 0) return `${abertura} Juros e resultado financeiro levaram o resultado para o negativo.`;
  return `${abertura} O IR e CS, de ${formatarMoedaAbreviada(base.impostoRendaContribuicao)}, levou o resultado para o negativo.`;
}
