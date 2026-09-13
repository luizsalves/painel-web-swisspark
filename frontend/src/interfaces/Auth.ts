export type UserRole = "Administrador" | "Gerente" | "Analista";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  /** A senha atual foi definida por um administrador e ainda não foi trocada. */
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
  avatarInitials: string;
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
  login: (email: string, senha: string) => Promise<ResultadoLogin>;
  trocarSenha: (senhaAtual: string, novaSenha: string) => Promise<void>;
  logout: () => void;
}
