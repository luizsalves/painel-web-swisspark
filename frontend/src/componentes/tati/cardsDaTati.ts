/**
 * Para onde leva cada número do resumo da Tati: a tela, a aba e o card, pelo alvo do link
 * ([R$ 1,2 mi](card:resultado/receitaLiquida)). É a mesma regra do backend (ValidadorReferenciasTati,
 * que confere o número com o card) e a lista de alvos das instruções da Tati (seção "Resumo do mês").
 * Cada card leva o data-card com a âncora.
 */
import type { CodigoIndicador } from "../../paginas/Indicadores/config";

export interface DestinoDoCard {
  caminho: string;
  busca: Record<string, string>;
  /** O data-card do card na tela. */
  ancora: string;
}

// Os códigos dos indicadores no backend (RegrasIndicadores, os que a Tati vê) e na tela (config.ts).
const INDICADORES: Record<string, CodigoIndicador> = {
  LIQ_CORRENTE: "liquidez-corrente",
  LIQ_SECA: "liquidez-seca",
  LIQ_IMEDIATA: "liquidez-imediata",
  LIQ_GERAL: "liquidez-geral",
  END_GERAL: "endividamento-geral",
  END_CURTO_PRAZO: "composicao-endividamento",
  DIV_LIQ_EBITDA_12M: "divida-liquida-ebitda",
  DIV_BRUTA_PL: "divida-bruta-pl",
  ROE: "roe",
  ROA: "roa",
  MARGEM_LIQUIDA: "margem-liquida",
  MARGEM_BRUTA: "margem-bruta",
  EBITDA: "ebitda",
};

const PILARES = ["resultado", "caixa", "divida", "curtoPrazo"];
const CHAVES_DO_ATIVO = ["ativo", "ativoCirculante", "ativoNaoCirculante"];
const CHAVES_DO_PASSIVO = ["passivo", "passivoExigivel", "patrimonioLiquido", "passivoMaisPl", "passivoCirculante", "passivoNaoCirculante"];
const CAMPOS_DA_CONCILIACAO = ["backlogTotal", "preparados", "conferidos", "pendentes", "fechados", "empresas"];

// Os indicadores e a DFC completos estão na visão do contador; pilares, veredito e a cascata, na da diretoria.
const CONTADOR = { visao: "contador" };
const DIRETORIA = { visao: "diretoria" };

/** O destino do alvo; null para alvo desconhecido (o número aparece sem link). */
export function destinoDoCard(alvo: string): DestinoDoCard | null {
  const [tipo, chave = "", ...resto] = alvo.split("/");
  if (resto.length > 0) return null;

  switch (tipo) {
    case "indicador": {
      const codigo = INDICADORES[chave.toUpperCase()];
      return codigo ? { caminho: "/indicadores", busca: CONTADOR, ancora: `indicador-${codigo}` } : null;
    }
    case "pilar":
      return PILARES.includes(chave) ? { caminho: "/indicadores", busca: DIRETORIA, ancora: `pilar-${chave}` } : null;
    case "veredito":
      return chave === "" ? { caminho: "/indicadores", busca: DIRETORIA, ancora: "veredito" } : null;
    // A cascata e a DFC são um card cada: o campo não muda o destino.
    case "resultado":
      return { caminho: "/indicadores", busca: DIRETORIA, ancora: "resultado" };
    case "dfc":
      return { caminho: "/indicadores", busca: CONTADOR, ancora: "dfc" };
    case "balanco":
      if (CHAVES_DO_ATIVO.includes(chave)) return { caminho: "/balancete", busca: { aba: "balanco" }, ancora: "balanco-ativo" };
      return CHAVES_DO_PASSIVO.includes(chave) ? { caminho: "/balancete", busca: { aba: "balanco" }, ancora: "balanco-passivo" } : null;
    case "conciliacao":
      return CAMPOS_DA_CONCILIACAO.includes(chave) ? { caminho: "/dashboard", busca: {}, ancora: `conciliacao-${chave}` } : null;
    default:
      return null;
  }
}
