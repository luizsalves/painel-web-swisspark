import api from "./api";
import { guardarSessao, limparSessao } from "./authStorage";
import type { User } from "../interfaces/Auth";

interface RespostaLogin {
  accessToken: string;
  expiraEm: string;
  usuario: User;
}

/**
 * Troca e-mail e senha por um token. A senha é conferida no backend contra o
 * hash PBKDF2 da bi_users — nunca existe senha em texto no banco.
 */
export async function login(email: string, senha: string): Promise<User> {
  const { data } = await api.post<RespostaLogin>("/auth/login", {
    email: email.trim(),
    password: senha,
  });

  guardarSessao({ accessToken: data.accessToken, expiraEm: data.expiraEm });

  return data.usuario;
}

/**
 * Quem a API entende que somos. Responde 403 quando o acesso foi desativado —
 * é a resposta que o front usa para explicar o motivo em vez de só barrar.
 */
export async function carregarUsuarioAtual(): Promise<User> {
  const { data } = await api.get<User>("/auth/me");
  return data;
}

/** Troca da própria senha. Também é o caminho de quem entrou com senha provisória. */
export async function trocarSenha(senhaAtual: string, novaSenha: string): Promise<void> {
  await api.post("/auth/trocar-senha", { senhaAtual, novaSenha });
}

export function sair(): void {
  limparSessao();
}
