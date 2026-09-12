/**
 * O Vite embute as variáveis VITE_* no bundle durante o build. Numa imagem Docker
 * isso amarraria a imagem a um tenant e a uma URL: cada ambiente exigiria uma
 * imagem diferente, e não daria para construir nada antes de a TI devolver os
 * dados do Entra.
 *
 * Por isso a configuração é lida em tempo de execução, de um /config.js que o
 * container gera no start a partir de variáveis de ambiente. Fora do container
 * (npm run dev) o arquivo vem vazio e valem as VITE_* do .env.local.
 */
interface ConfiguracaoDeExecucao {
  apiUrl?: string;
  entraTenantId?: string;
  entraClientId?: string;
  entraApiScope?: string;
  entraRedirectUri?: string;
}

declare global {
  interface Window {
    __PAINEL_CONFIG__?: ConfiguracaoDeExecucao;
  }
}

const deExecucao: ConfiguracaoDeExecucao = window.__PAINEL_CONFIG__ ?? {};

function resolver(runtime: string | undefined, build: string | undefined, padrao = ""): string {
  return (runtime?.trim() || build?.trim() || padrao).trim();
}

export const configuracao = {
  /** Base da API. "/api" funciona tanto no proxy do Vite quanto no nginx do container. */
  apiUrl: resolver(deExecucao.apiUrl, import.meta.env.VITE_API_URL, "/api"),
  entraTenantId: resolver(deExecucao.entraTenantId, import.meta.env.VITE_ENTRA_TENANT_ID),
  entraClientId: resolver(deExecucao.entraClientId, import.meta.env.VITE_ENTRA_CLIENT_ID),
  entraApiScope: resolver(deExecucao.entraApiScope, import.meta.env.VITE_ENTRA_API_SCOPE),
  entraRedirectUri: resolver(
    deExecucao.entraRedirectUri,
    import.meta.env.VITE_ENTRA_REDIRECT_URI,
    window.location.origin
  ),
};
