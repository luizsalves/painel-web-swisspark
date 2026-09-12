import api from "./api";
import type { User } from "../interfaces/Auth";

/**
 * Quem a API entende que somos: o token do Entra cruzado com a bi_users.
 * Responde 403 quando a pessoa não está cadastrada ou foi desativada — é a
 * resposta que o front usa para explicar o motivo em vez de só barrar.
 */
export async function carregarUsuarioAtual(): Promise<User> {
  const { data } = await api.get<User>("/auth/me");
  return data;
}
