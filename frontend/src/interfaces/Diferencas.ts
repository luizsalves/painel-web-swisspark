export interface DiferencaEmpresa {
  empresa: string;
  contas: number;
  valorAbsoluto: number;
  valorLiquido: number;
}

export interface DiferencaConta {
  empresa: string;
  codColigada: number;
  codFilial: number;
  nomeFilial: string;
  codConta: string;
  descricao: string;
  saldoRazao: number;
  composicao: number;
  diferenca: number;
  documentos: number;
}

export interface Diferencas {
  anoMes: string;
  totalEmpresas: number;
  totalContas: number;
  valorTotal: number;
  calculadoEm: string;
  empresas: DiferencaEmpresa[];
  contas: DiferencaConta[];
}
