import api from "./api";
import type { PerguntarCfoTatiNaTelaPayload, PerguntarCfoTatiPayload } from "../interfaces/CfoTati";

/** A pergunta vai com todos os filtros da tela (período, filial, conta e status) para o contexto da Tati. */
export async function perguntarCfoTati(payload: PerguntarCfoTatiPayload): Promise<string> {
  const { data } = await api.post<{ resposta: string }>("/agente-contabil/chat/filtros", payload);
  return data.resposta;
}

/** A pergunta pelo ícone da Tati: os filtros da tela aberta e qual tela (e aba) é. */
export async function perguntarCfoTatiNaTela(payload: PerguntarCfoTatiNaTelaPayload): Promise<string> {
  const { data } = await api.post<{ resposta: string }>("/agente-contabil/chat/tela", payload);
  return data.resposta;
}
