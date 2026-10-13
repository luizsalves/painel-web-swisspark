import api from "./api";
import type { Balanco } from "../interfaces/Balanco";

const RECURSO = "/balanco";

/** Balanço patrimonial no último dia do mês (AAAA-MM). */
export async function carregarBalanco(params: {
  mes: string;
  codColigada: number;
  codFilial?: number;
}): Promise<Balanco> {
  const { data } = await api.get<Balanco>(RECURSO, { params });
  return data;
}

/** Planilha .xls do balanço que está na tela — o backend não recalcula nada. */
export async function exportarBalancoExcel(dados: {
  empresa: string;
  codColigada: number;
  filial?: string;
  mes: string;
  balanco: Balanco;
}): Promise<Blob> {
  const { data } = await api.post<Blob>(`${RECURSO}/exportar-excel`, dados, { responseType: "blob" });
  return data;
}
