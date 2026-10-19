import type { DashboardConta, StatusFechamentoConta } from "../../interfaces/Dashboard";

/** Recorte das contas pelos filtros da gaveta; campo ausente = todas. */
export interface FiltroContas {
  codColigada?: number;
  codFilial?: number;
  codConta?: string;
  status?: StatusFechamentoConta;
}

export const SEM_FILTRO: FiltroContas = {};

export function contaPassaNoFiltro(conta: DashboardConta, filtro: FiltroContas) {
  return (filtro.codColigada === undefined || conta.codColigada === filtro.codColigada)
    && (filtro.codFilial === undefined || conta.codFilial === filtro.codFilial)
    && (!filtro.codConta || conta.codConta === filtro.codConta)
    && (!filtro.status || conta.status === filtro.status);
}
