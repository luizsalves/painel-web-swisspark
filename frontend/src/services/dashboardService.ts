import api from "./api";
import type { DashboardConta, DashboardResumo } from "../interfaces/Dashboard";

// As rotas "consolidado" contam as empresas de filiais consolidadas (AEPL Agropecuária,
// AGV Participações) uma vez por conta, na filial 0, como o fechamento delas; as
// demais empresas saem iguais às rotas por filial.

/** Resumo por empresa de `inicio` a `fim` (AAAA-MM, inclusive), somado mês a mês. */
export async function obterResumoEmpresas(inicio: string, fim: string, signal?: AbortSignal): Promise<Record<string, DashboardResumo>> {
  const { data } = await api.get<Record<string, DashboardResumo>>("/dashboard/periodo/consolidado/empresas", {
    params: { inicio, fim }, signal,
  });
  return data;
}

/**
 * Contas da empresa (nome, como vem em obterResumoEmpresas) de `inicio` a `fim`:
 * uma linha por mês, com o status da conta naquele mês.
 */
export async function obterContasDaEmpresa(inicio: string, fim: string, empresa: string, signal?: AbortSignal): Promise<DashboardConta[]> {
  const { data } = await api.get<DashboardConta[]>("/dashboard/periodo/consolidado/contas", {
    params: { inicio, fim, empresa }, signal,
  });
  return data;
}

/** Mês mais recente com algum fechamento registrado — null se a tabela estiver vazia. */
export async function obterUltimoPeriodoComDados(): Promise<string | null> {
  const { data } = await api.get<{ anoMes: string | null }>("/dashboard/ultimo-periodo");
  return data.anoMes;
}
