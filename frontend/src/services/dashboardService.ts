import api from "./api";
import type { DashboardConta, DashboardResumo } from "../interfaces/Dashboard";

/** Resumo por empresa de `inicio` a `fim` (AAAA-MM, inclusive), somado mês a mês. */
export async function obterResumoEmpresas(inicio: string, fim: string, signal?: AbortSignal): Promise<Record<string, DashboardResumo>> {
  const { data } = await api.get<Record<string, DashboardResumo>>("/dashboard/periodo/empresas", {
    params: { inicio, fim }, signal,
  });
  return data;
}

/**
 * Contas da empresa (nome, como vem em obterResumoEmpresas) de `inicio` a `fim`:
 * uma linha por mês, com o status da conta naquele mês.
 */
export async function obterContasDaEmpresa(inicio: string, fim: string, empresa: string, signal?: AbortSignal): Promise<DashboardConta[]> {
  const { data } = await api.get<DashboardConta[]>("/dashboard/periodo/contas", {
    params: { inicio, fim, empresa }, signal,
  });
  return data;
}

export async function obterResumoDashboard(anoMes: string, empresa: string): Promise<DashboardResumo> {
  const { data } = await api.get<DashboardResumo>("/dashboard/resumo", {
    params: { anoMes, empresa: empresa || undefined },
  });
  return data;
}

/** Mês mais recente com algum fechamento registrado — null se a tabela estiver vazia. */
export async function obterUltimoPeriodoComDados(): Promise<string | null> {
  const { data } = await api.get<{ anoMes: string | null }>("/dashboard/ultimo-periodo");
  return data.anoMes;
}
