export interface LancamentoTotvs {
  anoMes: string;
  data: string | null;
  empresa: string;
  codColigada: number;
  reduzido: string;
  codConta: string;
  codFilial: number;
  nomeFilial: string;
  complemento: string;
  documento: string | null;
  idLancamento: number | null;
  idPartida: number | null;
  usuario: string | null;
  debitos: number;
  creditos: number;
  /** Só preenchido na linha-resumo (data nula): saldo anterior ao período + débitos + créditos do período. */
  saldoRazao: number | null;
  /** Só preenchido na linha-resumo (data nula): saldo acumulado antes do período. */
  saldoAnterior: number | null;
}
