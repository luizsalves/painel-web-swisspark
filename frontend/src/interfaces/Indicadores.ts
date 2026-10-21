export type FormatoIndice = "indice" | "percentual" | "vezes" | "moeda";

export interface IndiceFinanceiro {
  codigo: string;
  categoria: string;
  nome: string;
  formula: string;
  interpretacao: string;
  formato: FormatoIndice;
  /** Nulo quando o denominador é zero. */
  valor: number | null;
}

/** Saldos do balanço no fim do período e resultado acumulado no período. */
export interface BaseCalculoIndicadores {
  ativoTotal: number;
  ativoCirculante: number;
  disponivel: number;
  estoques: number;
  realizavelLongoPrazo: number;
  passivoCirculante: number;
  passivoNaoCirculante: number;
  patrimonioLiquido: number;
  dividaBruta: number;
  dividaLiquida: number;
  receitaLiquida: number;
  custos: number;
  lucroBruto: number;
  resultadoFinanceiro: number;
  impostoRendaContribuicao: number;
  lucroLiquido: number;
  ebit: number;
  depreciacaoAmortizacao: number;
  ebitda: number;
}

/** A base de um mês (GET /api/indicadores/mensal): o balanço no último dia e o resultado do mês. */
export interface BaseCalculoMensal {
  /** AAAA-MM. */
  anoMes: string;
  baseCalculo: BaseCalculoIndicadores;
}

export interface DfcComposicao {
  codConta: string;
  descricao: string;
  medida: string;
  valor: number;
}

export interface DfcLinha {
  descricao: string;
  valor: number;
  /** Contas que movimentaram e não têm linha definida no de-para da DFC. */
  naoClassificada: boolean;
  composicao: DfcComposicao[];
}

export interface DfcGrupo {
  nome: string;
  total: number;
  linhas: DfcLinha[];
}

export interface Dfc {
  grupos: DfcGrupo[];
  saldoInicialCaixa: number;
  saldoFinalCaixa: number;
  variacaoCaixa: number;
}

export interface Indicadores {
  baseCalculo: BaseCalculoIndicadores;
  indices: IndiceFinanceiro[];
  dfc: Dfc;
}
