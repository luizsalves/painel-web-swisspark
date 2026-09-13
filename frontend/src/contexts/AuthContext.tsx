import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import {
  carregarUsuarioAtual,
  login as autenticar,
  sair,
  trocarSenha as trocarSenhaRequest,
} from "../services/authService";
import { obterSessao } from "../services/authStorage";
import {
  EVENTO_ACESSO_NEGADO,
  EVENTO_SESSAO_EXPIRADA,
  extrairMensagemDeErro,
} from "../services/api";
import type { AuthContextValue, ResultadoLogin, User } from "../interfaces/Auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [erroDeAcesso, setErroDeAcesso] = useState<string | null>(null);

  const encerrarSessao = useCallback((mensagem: string | null) => {
    sair();
    setUser(null);
    setErroDeAcesso(mensagem);
  }, []);

  // Recupera a sessão ao abrir o app. O token guardado diz quem somos, mas quem
  // manda é a bi_users — por isso o perfil vem sempre da API, nunca do storage.
  useEffect(() => {
    let cancelado = false;

    async function restaurarSessao() {
      if (!obterSessao()) {
        if (!cancelado) setIsLoading(false);
        return;
      }

      try {
        const usuarioAtual = await carregarUsuarioAtual();
        if (!cancelado) setUser(usuarioAtual);
      } catch (erro) {
        if (!cancelado) {
          encerrarSessao(extrairMensagemDeErro(erro, "Não foi possível restaurar sua sessão."));
        }
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
        setUser(await carregarUsuarioAtual());
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

  const login = useCallback(async (email: string, senha: string): Promise<ResultadoLogin> => {
    setErroDeAcesso(null);

    try {
      setUser(await autenticar(email, senha));
      return { success: true };
    } catch (erro) {
      const mensagem = extrairMensagemDeErro(erro, "Não foi possível entrar. Tente novamente.");
      encerrarSessao(mensagem);
      return { success: false, message: mensagem };
    }
  }, [encerrarSessao]);

  const trocarSenha = useCallback(async (senhaAtual: string, novaSenha: string) => {
    await trocarSenhaRequest(senhaAtual, novaSenha);

    // A troca derruba o must_change_password no banco; recarregar é o que libera
    // o resto do painel para esta sessão.
    setUser(await carregarUsuarioAtual());
  }, []);

  const logout = useCallback(() => {
    encerrarSessao(null);
  }, [encerrarSessao]);

  const podeAcessarModulo = useCallback(
    (moduloKey: string) =>
      Boolean(user?.isSuperAdmin) ||
      Boolean(user?.modulos.some((modulo) => modulo.moduleKey === moduloKey)),
    [user]
  );

  // Quem administra algum módulo vê a tela de Usuários. Quais usuários ela
  // mostra é o backend que decide, módulo a módulo.
  const administraAlgumModulo =
    Boolean(user?.isSuperAdmin) ||
    Boolean(user?.modulos.some((modulo) => modulo.role === "Administrador"));

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      precisaTrocarSenha: user?.mustChangePassword ?? false,
      erroDeAcesso,
      podeAcessarModulo,
      administraAlgumModulo,
      login,
      trocarSenha,
      logout,
    }),
    [user, isLoading, erroDeAcesso, podeAcessarModulo, administraAlgumModulo, login, trocarSenha, logout]
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
