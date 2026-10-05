import api from "./api";
import type { BalanceteConta } from "../interfaces/Balancete";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../interfaces/ContaContabil";

const RECURSO = "/balancete";

export async function listarEmpresasBalancete(): Promise<EmpresaContabil[]> {
  const { data } = await api.get<EmpresaContabil[]>(`${RECURSO}/empresas`);
  return data;
}

export async function listarFiliaisBalancete(codColigada?: number): Promise<ColigadaFilialContabil[]> {
  const { data } = await api.get<ColigadaFilialContabil[]>(`${RECURSO}/filiais`, { params: { codColigada } });
  return data;
}

export async function listarContasBalancete(params: { codColigada?: number; codFilial?: number }): Promise<ContaContabilTotvs[]> {
  const { data } = await api.get<ContaContabilTotvs[]>(`${RECURSO}/contas`, { params });
  return data;
}

export async function carregarBalancete(params: {
  anoMes: string;
  codColigada?: number;
  codFilial?: number;
  codConta?: string;
}): Promise<BalanceteConta[]> {
  const { data } = await api.get<BalanceteConta[]>(RECURSO, { params });
  return data;
}
