export type UserRole = "Administrador" | "Gerente" | "Analista";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  /** Cadastrado, mas ainda não entrou nenhuma vez (entra_object_id nulo no backend). */
  primeiroAcessoPendente: boolean;
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
  /** True quando o tenant/client id do Entra ID ainda não foram configurados. */
  entraIndisponivel: boolean;
  /** Motivo da recusa de acesso, quando houver (não cadastrado, desativado). */
  erroDeAcesso: string | null;
  entrarComMicrosoft: () => Promise<ResultadoLogin>;
  logout: () => void;
}
