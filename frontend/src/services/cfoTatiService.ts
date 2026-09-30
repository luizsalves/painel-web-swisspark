import api from "./api";
import type { PerguntarCfoTatiPayload } from "../interfaces/CfoTati";

export async function perguntarCfoTati(payload: PerguntarCfoTatiPayload): Promise<string> {
  const { data } = await api.post<{ resposta: string }>("/agente-contabil/chat", payload);
  return data.resposta;
}
