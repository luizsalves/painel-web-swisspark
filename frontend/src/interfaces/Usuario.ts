import type { UserRole } from "./Auth";

export interface Usuario {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  /** Senha provisória: definida por um administrador e ainda não trocada. */
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
  avatarInitials: string;
}

export interface NovoUsuario {
  name: string;
  email: string;
  role: UserRole;
  senhaInicial: string;
}

export interface AlteracaoUsuario {
  name: string;
  role: UserRole;
}
