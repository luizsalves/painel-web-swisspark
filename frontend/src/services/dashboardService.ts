import api from "./api";
import type { DashboardResumo } from "../interfaces/Dashboard";

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
