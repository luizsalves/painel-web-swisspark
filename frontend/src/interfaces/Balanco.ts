/** linha comum; grupo = Disponível, Realizável...; bloco = Ativo Circulante...; total = ATIVO/PASSIVO. */
export type TipoLinhaBalanco = "linha" | "grupo" | "bloco" | "total";

export interface BalancoComposicao {
  /** Conta analítica, ou o grupo da DRE ("3", "4"...) nas linhas de resultado. */
  codConta: string;
  descricao: string;
  valor: number;
}

export interface BalancoLinha {
  tipo: TipoLinhaBalanco;
  descricao: string;
  valor: number;
  composicao: BalancoComposicao[];
}

export interface Balanco {
  ativo: BalancoLinha[];
  passivo: BalancoLinha[];
  totalAtivo: number;
  totalPassivo: number;
}
