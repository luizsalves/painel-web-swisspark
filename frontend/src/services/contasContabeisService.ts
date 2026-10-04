import api from "./api";
import type { ContaContabilTotvs } from "../interfaces/ContaContabil";

const RECURSO = "/contascontabeis";

export async function listarContasContabeis(params?: { somenteBsb?: boolean; codColigada?: number }): Promise<ContaContabilTotvs[]> {
  const { data } = await api.get<ContaContabilTotvs[]>(RECURSO, { params });
  return data;
}

/** Não mexe no TOTVS (CCONTA é somente leitura) — grava só o flag do painel. */
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
