import api from "./api";
import type { Dfc, Indicadores } from "../interfaces/Indicadores";

const RECURSO = "/indicadores";

export async function carregarIndicadores(params: {
  inicio: string;
  fim: string;
  codColigada: number;
  codFilial?: number;
}): Promise<Indicadores> {
  const { data } = await api.get<Indicadores>(RECURSO, { params });
  return data;
}

/** Planilha .xls da DFC que está na tela — o backend não recalcula nada. */
export async function exportarDfcExcel(dados: {
  empresa: string;
  codColigada: number;
  filial?: string;
  inicio: string;
  fim: string;
  dfc: Dfc;
}): Promise<Blob> {
  const { data } = await api.post<Blob>(`${RECURSO}/dfc/exportar-excel`, dados, { responseType: "blob" });
  return data;
}
