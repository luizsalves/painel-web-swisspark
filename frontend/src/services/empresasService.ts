import api from "./api";
import type { Empresa } from "../interfaces/Empresa";

const RECURSO = "/empresas";

export async function listarEmpresas(somenteBsb = false): Promise<Empresa[]> {
  const { data } = await api.get<Empresa[]>(RECURSO, { params: { somenteBsb } });
  return data;
}

/** Não mexe no TOTVS (GFILIAL é somente leitura) — grava só o flag do painel. */
export async function alterarStatusEmpresa(
  codColigada: number,
  codFilial: number,
  ativo: boolean
): Promise<Empresa> {
  const { data } = await api.patch<Empresa>(`${RECURSO}/${codColigada}/${codFilial}/status`, { ativo });
  return data;
}
