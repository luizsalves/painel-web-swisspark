export type UserRole = "Administrador" | "Gerente" | "Analista";

/** Chaves dos módulos. Espelha a tabela bi_modules. */
export const MODULO_CONCILIACAO = "conciliacao";

export interface ModuloDoUsuario {
  moduleKey: string;
  moduleName: string;
  role: UserRole;
}

export interface User {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  /** Enxerga e administra todos os módulos. */
  isSuperAdmin: boolean;
  /** A senha atual foi definida por um administrador e ainda não foi trocada. */
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
  avatarInitials: string;
  /** Os módulos a que a pessoa pertence, com o perfil em cada um. */
  modulos: ModuloDoUsuario[];
}

export interface ResultadoLogin {
  success: boolean;
  message?: string;
}

export interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  /** Entrou, mas está com senha provisória: só pode trocar a senha. */
  precisaTrocarSenha: boolean;
  /** Motivo da recusa de acesso, quando houver. */
  erroDeAcesso: string | null;
  /** Pertence ao módulo — ou é superadministrador. */
  podeAcessarModulo: (moduloKey: string) => boolean;
  /** Administra algum módulo. É o que libera a tela de Usuários. */
  administraAlgumModulo: boolean;
  login: (email: string, senha: string) => Promise<ResultadoLogin>;
  trocarSenha: (senhaAtual: string, novaSenha: string) => Promise<void>;
  logout: () => void;
}
