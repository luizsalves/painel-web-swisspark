import api from "./api";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../interfaces/ContaContabil";

const RECURSO = "/contascontabeis";

export async function listarContasContabeis(params?: { somenteBsb?: boolean; codColigada?: number; codFilial?: number }): Promise<ContaContabilTotvs[]> {
  const { data } = await api.get<ContaContabilTotvs[]>(RECURSO, { params });
  return data;
}

export async function listarEmpresasContabeis(): Promise<EmpresaContabil[]> {
  const { data } = await api.get<EmpresaContabil[]>(`${RECURSO}/empresas`);
  return data;
}

export async function listarColigadasFiliaisContabeis(codColigada?: number): Promise<ColigadaFilialContabil[]> {
  const { data } = await api.get<ColigadaFilialContabil[]>(`${RECURSO}/coligadas-filiais`, { params: { codColigada } });
  return data;
}

/** Não mexe no TOTVS — grava só o flag do painel. */
export async function alterarStatusContaContabil(
  codColigada: number,
  codConta: string,
  ativo: boolean
): Promise<ContaContabilTotvs> {
  const { data } = await api.patch<ContaContabilTotvs>(
    `${RECURSO}/${codColigada}/${encodeURIComponent(codConta)}/status`,
    { ativo }
  );
  return data;
}
