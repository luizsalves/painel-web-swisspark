import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { carregarUsuarioAtual } from "../services/authService";
import {
  EVENTO_ACESSO_NEGADO,
  EVENTO_SESSAO_EXPIRADA,
  extrairMensagemDeErro,
} from "../services/api";
import {
  entraConfigurado,
  entrarComMicrosoft as entrarNoEntra,
  inicializarEntra,
  limparSessaoLocal,
  obterContaAtiva,
  sairDoEntra,
} from "../services/entraId";
import type { AuthContextValue, ResultadoLogin, User } from "../interfaces/Auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/** Fechar o popup do Entra não é erro — não vale assustar quem só desistiu. */
const CODIGOS_DE_DESISTENCIA = ["user_cancelled", "popup_window_error", "interaction_in_progress"];

function ehDesistenciaDoUsuario(erro: unknown): boolean {
  const codigo = (erro as { errorCode?: string } | null)?.errorCode;
  return typeof codigo === "string" && CODIGOS_DE_DESISTENCIA.includes(codigo);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [erroDeAcesso, setErroDeAcesso] = useState<string | null>(null);

  const encerrarSessao = useCallback((mensagem: string | null) => {
    setUser(null);
    setErroDeAcesso(mensagem);
    void limparSessaoLocal();
  }, []);

  // Recupera a sessão ao abrir o app: o MSAL guarda a conta, mas quem manda é a
  // bi_users — por isso o perfil vem sempre da API, nunca do armazenamento local.
  useEffect(() => {
    let cancelado = false;

    async function restaurarSessao() {
      try {
        await inicializarEntra();

        if (obterContaAtiva()) {
          const usuarioAtual = await carregarUsuarioAtual();
          if (!cancelado) setUser(usuarioAtual);
        }
      } catch (erro) {
        if (cancelado) return;
        encerrarSessao(extrairMensagemDeErro(erro, "Não foi possível restaurar sua sessão."));
      } finally {
        if (!cancelado) setIsLoading(false);
      }
    }

    void restaurarSessao();

    return () => {
      cancelado = true;
    };
  }, [encerrarSessao]);

  // Revogação chegando do backend. Um 403 pode ser acesso desativado ou só perfil
  // insuficiente para aquele recurso; /auth/me é quem desempata.
  useEffect(() => {
    function aoExpirarSessao() {
      encerrarSessao("Sua sessão expirou. Entre novamente.");
    }

    async function aoNegarAcesso(evento: Event) {
      const motivo = (evento as CustomEvent<string>).detail;

      try {
        const usuarioAtual = await carregarUsuarioAtual();
        setUser(usuarioAtual);
      } catch {
        encerrarSessao(motivo ?? "Seu acesso ao painel foi revogado.");
      }
    }

    const aoNegarAcessoHandler = (evento: Event) => void aoNegarAcesso(evento);

    window.addEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirarSessao);
    window.addEventListener(EVENTO_ACESSO_NEGADO, aoNegarAcessoHandler);

    return () => {
      window.removeEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirarSessao);
      window.removeEventListener(EVENTO_ACESSO_NEGADO, aoNegarAcessoHandler);
    };
  }, [encerrarSessao]);

  const entrarComMicrosoft = useCallback(async (): Promise<ResultadoLogin> => {
    setErroDeAcesso(null);

    try {
      await entrarNoEntra();

      const usuarioAtual = await carregarUsuarioAtual();
      setUser(usuarioAtual);

      return { success: true };
    } catch (erro) {
      if (ehDesistenciaDoUsuario(erro)) {
        return { success: false };
      }

      const mensagem = extrairMensagemDeErro(erro, "Não foi possível entrar. Tente novamente.");
      encerrarSessao(mensagem);

      return { success: false, message: mensagem };
    }
  }, [encerrarSessao]);

  const logout = useCallback(() => {
    setUser(null);
    setErroDeAcesso(null);
    void sairDoEntra();
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      entraIndisponivel: !entraConfigurado,
      erroDeAcesso,
      entrarComMicrosoft,
      logout,
    }),
    [user, isLoading, erroDeAcesso, entrarComMicrosoft, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth deve ser usado dentro de um AuthProvider");
  }
  return context;
}
