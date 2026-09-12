import type { UserRole } from "./Auth";

export interface Usuario {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  /** Cadastrado, mas ainda sem primeiro login — o oid do Entra ainda não foi vinculado. */
  primeiroAcessoPendente: boolean;
  avatarInitials: string;
}

export interface NovoUsuario {
  name: string;
  email: string;
  role: UserRole;
}

export interface AlteracaoUsuario {
  name: string;
  role: UserRole;
}
