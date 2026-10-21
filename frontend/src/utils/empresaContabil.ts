import type { EmpresaContabil } from "../interfaces/ContaContabil";

/** A filial que representa "todas" no fechamento das empresas de filiais consolidadas. */
export const FILIAL_CONSOLIDADA = 0;

// A identidade da opção preserva o nome mesmo quando a coligada se repete.
export function chaveEmpresaContabil(empresa: EmpresaContabil): string {
  return `${empresa.codColigada}:${empresa.nome}`;
}

export function coligadaDaEmpresa(valor: string): number {
  return Number(valor.split(":", 1)[0]);
}
