import type { ModuloDoUsuario, UserRole } from "./Auth";

export interface Usuario {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  isSuperAdmin: boolean;
  /** Senha provisória: definida por um administrador e ainda não trocada. */
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
  avatarInitials: string;
  modulos: ModuloDoUsuario[];
}

/** Um módulo que quem está logado pode conceder. */
export interface ModuloGerenciavel {
  key: string;
  name: string;
}

export interface VinculoDeModulo {
  moduleKey: string;
  role: UserRole;
}

export interface NovoUsuario {
  name: string;
  email: string;
  modulos: VinculoDeModulo[];
  senhaInicial: string;
}

export interface AlteracaoUsuario {
  name: string;
  modulos: VinculoDeModulo[];
}
