import axios from "axios";
import { configuracao } from "./configuracao";
import { obterAccessToken } from "./authStorage";

/**
 * Disparado quando a API recusa o token (401): expirado, inválido, ou o usuário
 * não existe mais. A sessão local não vale mais nada.
 */
export const EVENTO_SESSAO_EXPIRADA = "painel:sessao-expirada";

/**
 * Disparado quando o token é válido mas a API recusa (403). Pode ser acesso
 * desativado na bi_users, senha provisória pendente, ou apenas perfil
 * insuficiente para aquele recurso — o AuthContext confirma em /auth/me antes
 * de encerrar a sessão.
 */
export const EVENTO_ACESSO_NEGADO = "painel:acesso-negado";

const api = axios.create({
  baseURL: configuracao.apiUrl,
});

api.interceptors.request.use((config) => {
  const token = obterAccessToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (erro: unknown) => {
    const status = obterStatus(erro);
    const url = obterUrl(erro);

    // O 401 do próprio login é "e-mail ou senha incorretos", não sessão expirada.
    if (status === 401 && !url.includes("/auth/login")) {
      window.dispatchEvent(new CustomEvent(EVENTO_SESSAO_EXPIRADA));
    }

    // /auth/me também devolve 403 para quem foi desativado, mas ali o AuthContext
    // já trata a resposta direto — avisar duas vezes só confunde.
    if (status === 403 && !url.includes("/auth/me")) {
      window.dispatchEvent(
        new CustomEvent(EVENTO_ACESSO_NEGADO, { detail: extrairMensagemDeErro(erro) })
      );
    }

    return Promise.reject(erro);
  }
);

function obterStatus(erro: unknown): number | undefined {
  return axios.isAxiosError(erro) ? erro.response?.status : undefined;
}

function obterUrl(erro: unknown): string {
  return (axios.isAxiosError(erro) ? erro.config?.url : undefined) ?? "";
}

/**
 * Texto que a tela mostra. O backend manda { "error": "..." } com mensagem escrita
 * para o usuário final; o resto é rede ou falha inesperada.
 */
export function extrairMensagemDeErro(erro: unknown, padrao = "Não foi possível concluir a operação."): string {
  if (!axios.isAxiosError(erro)) return padrao;

  const corpo = erro.response?.data as { error?: string } | undefined;
  if (corpo?.error) return corpo.error;

  if (erro.response === undefined) {
    return "Não foi possível falar com o servidor. Verifique sua conexão.";
  }

  return padrao;
}

export default api;
