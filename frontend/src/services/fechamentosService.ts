import api from "./api";
import type { Fechamento } from "../interfaces/Fechamento";

export interface FechamentoFiltro {
  empresaId: number;
  contaContabilId: number;
  /** Formato AAAA-MM, ex.: "2026-01" */
  periodo: string;
}

export async function getFechamento(filtro: FechamentoFiltro): Promise<Fechamento> {
  const { data } = await api.get<Fechamento>("/fechamentos", {
    params: {
      empresaId: filtro.empresaId,
      contaContabilId: filtro.contaContabilId,
      periodo: filtro.periodo,
    },
  });
  return data;
}
