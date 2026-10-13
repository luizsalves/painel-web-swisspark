import api from "./api";
import type { DashboardConta, DashboardResumo } from "../interfaces/Dashboard";

export async function obterResumoEmpresas(anoMes: string, signal?: AbortSignal): Promise<Record<string, DashboardResumo>> {
  const { data } = await api.get<Record<string, DashboardResumo>>("/dashboard/empresas", { params: { anoMes }, signal });
  return data;
}

/** Contas da empresa (nome, como vem em obterResumoEmpresas) no período, com o status de cada uma. */
export async function obterContasDaEmpresa(anoMes: string, empresa: string, signal?: AbortSignal): Promise<DashboardConta[]> {
  const { data } = await api.get<DashboardConta[]>("/dashboard/contas", { params: { anoMes, empresa }, signal });
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
