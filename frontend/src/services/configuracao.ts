/**
 * O Vite embute as variáveis VITE_* no bundle durante o build. Numa imagem Docker
 * isso amarraria a imagem a uma URL: cada ambiente exigiria uma imagem diferente.
 *
 * Por isso a configuração é lida em tempo de execução, de um /config.js que o
 * container gera no start a partir de variáveis de ambiente. Fora do container
 * (npm run dev) o arquivo vem vazio e valem as VITE_* do .env.local.
 */
interface ConfiguracaoDeExecucao {
  apiUrl?: string;
}

declare global {
  interface Window {
    __PAINEL_CONFIG__?: ConfiguracaoDeExecucao;
  }
}

const deExecucao: ConfiguracaoDeExecucao = window.__PAINEL_CONFIG__ ?? {};

export const configuracao = {
  /** Base da API. "/api" funciona tanto no proxy do Vite quanto no nginx do container. */
  apiUrl: (deExecucao.apiUrl?.trim() || import.meta.env.VITE_API_URL?.trim() || "/api").trim(),
};
