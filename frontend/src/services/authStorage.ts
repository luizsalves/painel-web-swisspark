/**
 * Guarda o token da sessão.
 *
 * sessionStorage, não localStorage: a sessão morre ao fechar a aba. Em máquina
 * compartilhada isso vale mais do que a conveniência de continuar logado.
 *
 * Todo acesso é protegido: em janela anônima ou com armazenamento bloqueado os
 * acessores lançam, e a aplicação precisa continuar funcionando mesmo assim.
 */
const CHAVE = "painel-swiss-park:sessao";

export interface SessaoArmazenada {
  accessToken: string;
  expiraEm: string;
}

export function guardarSessao(sessao: SessaoArmazenada): void {
  try {
    window.sessionStorage.setItem(CHAVE, JSON.stringify(sessao));
  } catch {
    // Sem armazenamento a sessão vale só enquanto a página estiver aberta.
  }
}

export function obterSessao(): SessaoArmazenada | null {
  try {
    const bruto = window.sessionStorage.getItem(CHAVE);
    if (!bruto) return null;

    const sessao = JSON.parse(bruto) as SessaoArmazenada;
    if (!sessao?.accessToken) return null;

    // Token vencido não vale a viagem até a API.
    if (sessao.expiraEm && new Date(sessao.expiraEm).getTime() <= Date.now()) {
      limparSessao();
      return null;
    }

    return sessao;
  } catch {
    return null;
  }
}

export function obterAccessToken(): string | null {
  return obterSessao()?.accessToken ?? null;
}

export function limparSessao(): void {
  try {
    window.sessionStorage.removeItem(CHAVE);
  } catch {
    // Nada a fazer: sem armazenamento, não havia o que limpar.
  }
}
