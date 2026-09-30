import api from "./api";
import type { Diferencas } from "../interfaces/Diferencas";

export async function obterDiferencas(anoMes: string, empresa?: string): Promise<Diferencas> {
  const { data } = await api.get<Diferencas>("/diferencas", { params: { anoMes, empresa: empresa || undefined } });
  return data;
}
