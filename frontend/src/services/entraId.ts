import { InteractionRequiredAuthError, PublicClientApplication } from "@azure/msal-browser";
import type { AccountInfo, Configuration } from "@azure/msal-browser";

const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID ?? "";
const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID ?? "";
const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE ?? "";

/**
 * Enquanto a TI não devolver tenant, client id e escopo, a aplicação sobe do mesmo
 * jeito e a tela de login explica o que falta — em vez de quebrar na inicialização.
 */
export const entraConfigurado = Boolean(tenantId && clientId && apiScope);

export const MENSAGEM_ENTRA_NAO_CONFIGURADO =
  "Login com Microsoft ainda não configurado. Preencha VITE_ENTRA_TENANT_ID, " +
  "VITE_ENTRA_CLIENT_ID e VITE_ENTRA_API_SCOPE no arquivo .env.local.";

const configuracao: Configuration = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI ?? window.location.origin,
    postLogoutRedirectUri: `${window.location.origin}/login`,
  },
  cache: {
    // sessionStorage: a sessão morre ao fechar a aba. Em máquina compartilhada
    // isso vale mais do que a conveniência de continuar logado.
    cacheLocation: "sessionStorage",
  },
};

// O escopo tem que ser o da NOSSA API. É o erro clássico: mandar o ID token ou um
// token do Microsoft Graph para o backend e levar 401 por audience inválida.
const escoposDaApi = apiScope ? [apiScope] : [];

let instancia: PublicClientApplication | null = null;

function obterInstancia(): PublicClientApplication {
  if (!entraConfigurado) {
    throw new Error(MENSAGEM_ENTRA_NAO_CONFIGURADO);
  }

  instancia ??= new PublicClientApplication(configuracao);
  return instancia;
}

/** Precisa rodar uma vez antes de qualquer outra chamada ao MSAL. */
export async function inicializarEntra(): Promise<void> {
  if (!entraConfigurado) return;

  const msal = obterInstancia();
  await msal.initialize();

  const resultado = await msal.handleRedirectPromise();

  if (resultado?.account) {
    msal.setActiveAccount(resultado.account);
    return;
  }

  if (!msal.getActiveAccount()) {
    const [primeiraConta] = msal.getAllAccounts();
    if (primeiraConta) msal.setActiveAccount(primeiraConta);
  }
}

export function obterContaAtiva(): AccountInfo | null {
  if (!entraConfigurado || !instancia) return null;
  return instancia.getActiveAccount();
}

export async function entrarComMicrosoft(): Promise<void> {
  const msal = obterInstancia();
  const resultado = await msal.loginPopup({ scopes: escoposDaApi, prompt: "select_account" });
  msal.setActiveAccount(resultado.account);
}

/**
 * Token de acesso para a API do painel. Renova em silêncio; só abre popup quando
 * a Microsoft exige interação de novo (MFA expirado, senha trocada).
 */
export async function obterAccessToken(): Promise<string | null> {
  if (!entraConfigurado || !instancia) return null;

  const conta = instancia.getActiveAccount();
  if (!conta) return null;

  try {
    const resultado = await instancia.acquireTokenSilent({ account: conta, scopes: escoposDaApi });
    return resultado.accessToken;
  } catch (erro) {
    if (erro instanceof InteractionRequiredAuthError) {
      const resultado = await instancia.acquireTokenPopup({ scopes: escoposDaApi });
      return resultado.accessToken;
    }

    throw erro;
  }
}

/** Limpa só o cache local — usado quando a API recusa o acesso. */
export async function limparSessaoLocal(): Promise<void> {
  if (!entraConfigurado || !instancia) return;
  await instancia.clearCache();
}

/** Logout de verdade: encerra também a sessão na Microsoft. */
export async function sairDoEntra(): Promise<void> {
  if (!entraConfigurado || !instancia) {
    window.location.assign("/login");
    return;
  }

  const conta = instancia.getActiveAccount();
  await instancia.logoutRedirect({ account: conta ?? undefined });
}
