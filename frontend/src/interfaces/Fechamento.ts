export interface Lancamento {
  data: string;
  historico: string;
  documento: string;
  numero: string;
  valor: number;
  tipo: "Debito" | "Credito";
}

export interface PendenciaFechamento {
  id: number;
  titulo: string;
  descricao: string;
  prioridade: string;
}

/** Espelha PainelWebSwissPark.Api.DTOs.ViewModels.FechamentoViewModel */
export interface Fechamento {
  empresaId: number;
  empresa: string;
  contaContabilId: number;
  contaContabilCodigo: string;
  contaContabilDescricao: string;
  periodo: string;

  saldoRazao: number;
  composicaoSaldo: number;
  diferenca: number;
  possuiPendencia: boolean;

  status: string;
  prioridade: string;
  responsavel: string;
  atualizadoEm: string;

  lancamentos: Lancamento[];
  pendencias: PendenciaFechamento[];
}
