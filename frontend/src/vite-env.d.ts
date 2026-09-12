/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base da API do painel. Padrão "/api" (proxy do Vite em desenvolvimento). */
  readonly VITE_API_URL?: string;

  /** Diretório (tenant) do Entra ID. Fornecido pela TI. */
  readonly VITE_ENTRA_TENANT_ID?: string;

  /** Id do aplicativo registrado no Entra ID. Fornecido pela TI. */
  readonly VITE_ENTRA_CLIENT_ID?: string;

  /**
   * Escopo da API do painel, no formato api://<client-id>/access_as_user.
   * Precisa ser o escopo da própria API: pedir o ID token ou um escopo do
   * Microsoft Graph faz o backend recusar com 401 por audience inválida.
   */
  readonly VITE_ENTRA_API_SCOPE?: string;

  /** URI de redirecionamento cadastrada no Entra. Padrão: origem da página. */
  readonly VITE_ENTRA_REDIRECT_URI?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
