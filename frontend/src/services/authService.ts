import api from "./api";
import { storeUser } from "./authStorage";
import { MOCK_USER } from "./mockApi";
import type { User } from "../interfaces/Auth";

export interface LoginResult {
  success: boolean;
  message?: string;
  user?: User;
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Login temporário sem backend: aceita qualquer e-mail válido com senha de 4+ caracteres.
 * Trocar por uma chamada real a POST /api/auth/login assim que o endpoint existir no backend.
 */
export async function login(email: string, password: string): Promise<LoginResult> {
  await delay(600);

  if (!EMAIL_REGEX.test(email.trim())) {
    return { success: false, message: "Informe um e-mail válido." };
  }

  if (password.length < 4) {
    return { success: false, message: "A senha deve ter pelo menos 4 caracteres." };
  }

  const user: User = { ...MOCK_USER, email: email.trim() };

  storeUser(user);
  return { success: true, user };
}

/**
 * Chama o endpoint de autenticação do backend C#.
 * O endpoint POST /api/auth/login ainda não existe na API — usar no lugar do login mock acima
 * assim que a autenticação real for implementada no backend.
 */
export async function loginViaApi(email: string, password: string): Promise<LoginResult> {
  try {
    const { data } = await api.post<User>("/api/auth/login", { email, password });
    storeUser(data);
    return { success: true, user: data };
  } catch {
    return { success: false, message: "Não foi possível entrar. Verifique suas credenciais." };
  }
}

/** Envia um código de recuperação de senha. Endpoint ainda não implementado no backend. */
export async function sendRecoveryCode(contact: string): Promise<{ success: boolean }> {
  try {
    await api.post("/api/auth/recovery", { contact });
    return { success: true };
  } catch {
    return { success: false };
  }
}
