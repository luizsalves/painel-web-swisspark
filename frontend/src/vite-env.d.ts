/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base da API do painel. Padrão "/api" (proxy do Vite em desenvolvimento). */
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
