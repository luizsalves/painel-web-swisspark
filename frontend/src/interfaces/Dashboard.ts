// "Backlog" aqui é o rótulo mostrado no combobox de status do mock de Contas
// Contábeis — não tem relação com o DashboardResumo (esse vem de verdade do
// backend, cruzando Empresas x Contas Contábeis x bi_fechamento_validacao).
export type ReconciliationStatus =
  | "Backlog"
  | "Preparado"
  | "Pré-conferido"
  | "Conferido"
  | "Pendência"
  | "Fechado";

/**
 * Resumo do backlog de conciliação de um período (GET /api/dashboard/resumo):
 * das combinações de filial ativa x conta contábil ativa esperadas, quantas
 * estão em cada etapa do fechamento.
 */
export interface DashboardResumo {
  backlogTotal: number;
  preparados: number;
  conferidos: number;
  fechados: number;
  pendentes: number;
}

export type StatusFechamentoConta = "Pendente" | "Preparado" | "Conferido" | "Fechado";

/** Uma conta de uma filial no período (GET /api/dashboard/contas), com o status atual. */
export interface DashboardConta {
  /** Mês (AAAA-MM) a que o status se refere. */
  anoMes: string;
  codColigada: number;
  codFilial: number;
  /** "1 - Nome fantasia". */
  filial: string;
  codConta: string;
  reduzido: string;
  descricao: string;
  status: StatusFechamentoConta;
  /** Nulos enquanto a conta está pendente. */
  ultimaAcaoPor: string | null;
  ultimaAcaoEm: string | null;
}
