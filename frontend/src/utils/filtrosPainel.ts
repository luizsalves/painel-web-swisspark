import type { StatusFechamentoConta } from "../interfaces/Dashboard";

/** Filtros principais do painel, compartilhados entre as telas. */
export interface FiltrosPainel {
  /** Chave de chaveEmpresaContabil; nulo enquanto nenhuma empresa foi escolhida. */
  empresa: string | null;
  empresaNome: string;
  /** Código da filial; vazio = todas. */
  filial: string;
  /** Código da conta contábil; vazio = todas. */
  conta: string;
  contaDescricao: string;
  ano: number;
  /** 1 a 12, inclusive. */
  mesInicial: number;
  mesFinal: number;
  /** Etapa do fechamento da conta no mês; vazio = todas. */
  status: StatusFechamentoConta | "";
}

/**
 * Filtros que uma tela usa — os demais continuam valendo para as outras telas.
 * "periodo" é o intervalo de meses; "mes" é para telas de um único mês, que
 * usam o mês final do período.
 */
export type CampoFiltro = "empresa" | "filial" | "conta" | "periodo" | "mes" | "status";

export const MESES = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

/** Opções do filtro de status, na ordem em que aparecem na gaveta. */
export const STATUS_FILTRO: { valor: StatusFechamentoConta; rotulo: string }[] = [
  { valor: "Preparado", rotulo: "Preparado" },
  { valor: "Conferido", rotulo: "Conferido" },
  { valor: "Pendente", rotulo: "Pendente" },
  { valor: "Fechado", rotulo: "Finalizado" },
];

const CHAVE_ARMAZENAMENTO = "sp-filtros-painel";

export function filtrosPadrao(): FiltrosPainel {
  return {
    empresa: null, empresaNome: "", filial: "", conta: "", contaDescricao: "",
    ano: new Date().getFullYear() - 1, mesInicial: 1, mesFinal: 12, status: "",
  };
}

/** Período no formato AAAA-MM que os endpoints recebem. */
export function periodoDosFiltros(filtros: FiltrosPainel) {
  const mes = (valor: number) => String(valor).padStart(2, "0");
  return { inicio: `${filtros.ano}-${mes(filtros.mesInicial)}`, fim: `${filtros.ano}-${mes(filtros.mesFinal)}` };
}

/** Mês de referência (AAAA-MM) das telas de um único mês: o mês final do período. */
export function mesDosFiltros(filtros: FiltrosPainel) {
  return `${filtros.ano}-${String(filtros.mesFinal).padStart(2, "0")}`;
}

export function descreverMes(filtros: FiltrosPainel) {
  return `${MESES[filtros.mesFinal - 1]} de ${filtros.ano}`;
}

export function descreverStatus(filtros: FiltrosPainel) {
  return STATUS_FILTRO.find((opcao) => opcao.valor === filtros.status)?.rotulo ?? "Todos";
}

export function descreverPeriodo(filtros: FiltrosPainel) {
  if (filtros.mesInicial === 1 && filtros.mesFinal === 12) return `Ano de ${filtros.ano}`;
  const inicial = MESES[filtros.mesInicial - 1];
  if (filtros.mesInicial === filtros.mesFinal) return `${inicial} de ${filtros.ano}`;
  return `${inicial} a ${MESES[filtros.mesFinal - 1]} de ${filtros.ano}`;
}

/** Quantos dos filtros usados pela tela fogem do padrão — o número do botão do cabeçalho. */
export function contarFiltrosAtivos(filtros: FiltrosPainel, campos: CampoFiltro[]) {
  const padrao = filtrosPadrao();
  const ativos: Record<CampoFiltro, boolean> = {
    empresa: filtros.empresa !== null,
    filial: filtros.filial !== "",
    conta: filtros.conta !== "",
    periodo: filtros.ano !== padrao.ano || filtros.mesInicial !== 1 || filtros.mesFinal !== 12,
    mes: filtros.ano !== padrao.ano || filtros.mesFinal !== 12,
    status: filtros.status !== "",
  };
  return campos.filter((campo) => ativos[campo]).length;
}

export function lerFiltrosSalvos(): FiltrosPainel {
  const padrao = filtrosPadrao();
  try {
    const salvo = JSON.parse(window.localStorage.getItem(CHAVE_ARMAZENAMENTO) ?? "null") as Partial<FiltrosPainel> | null;
    if (!salvo) return padrao;
    const mesValido = (valor: unknown) => typeof valor === "number" && valor >= 1 && valor <= 12;
    const filtros: FiltrosPainel = {
      empresa: typeof salvo.empresa === "string" ? salvo.empresa : null,
      empresaNome: typeof salvo.empresaNome === "string" ? salvo.empresaNome : "",
      filial: typeof salvo.filial === "string" ? salvo.filial : "",
      conta: typeof salvo.conta === "string" ? salvo.conta : "",
      contaDescricao: typeof salvo.contaDescricao === "string" ? salvo.contaDescricao : "",
      ano: typeof salvo.ano === "number" ? salvo.ano : padrao.ano,
      mesInicial: mesValido(salvo.mesInicial) ? salvo.mesInicial! : 1,
      mesFinal: mesValido(salvo.mesFinal) ? salvo.mesFinal! : 12,
      status: STATUS_FILTRO.find((opcao) => opcao.valor === salvo.status)?.valor ?? "",
    };
    return filtros.mesInicial <= filtros.mesFinal ? filtros : { ...filtros, mesInicial: 1, mesFinal: 12 };
  } catch {
    return padrao;
  }
}

export function salvarFiltros(filtros: FiltrosPainel) {
  try {
    window.localStorage.setItem(CHAVE_ARMAZENAMENTO, JSON.stringify(filtros));
  } catch {
    // Sem armazenamento (aba anônima, bloqueio): os filtros valem só até recarregar.
  }
}
