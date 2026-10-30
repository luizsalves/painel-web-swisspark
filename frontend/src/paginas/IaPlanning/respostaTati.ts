/**
 * A resposta da CFO Tati em partes: o texto, as opções numeradas (```opcoes) e o gráfico
 * (```grafico). O gráfico vem no contrato novo (tipo, unidade, fonte, eixoX, series) ou no antigo
 * (titulo, itens), e sai daqui sempre no mesmo formato para a tela desenhar. Quem garante que os
 * números vieram de uma ferramenta é o backend (ValidadorGraficoTati); aqui só se lê — e do mesmo
 * jeito que ele confere: o primeiro bloco ```grafico, lido antes das opções, sem tentar
 * aproveitar bloco cortado (o backend já tira todo bloco que não é um JSON inteiro).
 */

export type TipoGrafico = "barras" | "colunas" | "linha";
export type UnidadeGrafico = "R$" | "%" | "x" | "indice" | "contas";

export interface SerieGrafico {
  nome: string;
  /** null é buraco no gráfico (mês sem dados), nunca zero. */
  valores: (number | null)[];
}

export interface FonteGrafico {
  ferramenta?: string;
  empresa?: string;
  periodo?: string;
}

export interface GraficoTatiDados {
  tipo: TipoGrafico;
  titulo: string;
  /** Sem unidade (formato antigo): número puro, como as contagens do backlog. */
  unidade: UnidadeGrafico | null;
  fonte: FonteGrafico | null;
  /** As categorias (barras e colunas) ou os meses (linha). */
  categorias: string[];
  series: SerieGrafico[];
}

export interface RespostaTati {
  corpo: string;
  opcoes: string[];
  grafico: GraficoTatiDados | null;
}

const TIPOS: TipoGrafico[] = ["barras", "colunas", "linha"];
const UNIDADES: UnidadeGrafico[] = ["R$", "%", "x", "indice", "contas"];

// O fechamento ``` é opcional: se a resposta vier cortada, o bloco ainda some do texto. A
// expressão do gráfico é a mesma do ValidadorGraficoTati.
const BLOCO_OPCOES = /```opcoes\s*([\s\S]*?)(?:```|$)/;
const BLOCO_GRAFICO = /```grafico\s*([\s\S]*?)(?:```|$)/;
const BLOCOS_QUE_FICAM = ["opcoes", "grafico"];

function lerJson(bruto: string): unknown {
  try {
    return JSON.parse(bruto);
  } catch {
    return null;
  }
}

function texto(valor: unknown): string | null {
  return typeof valor === "string" && valor.trim() !== "" ? valor.trim() : null;
}

/** A unidade como o backend confere: aparada, e só uma das conhecidas. */
function lerUnidade(valor: unknown): UnidadeGrafico | null {
  const unidade = texto(valor);
  return UNIDADES.find((u) => u === unidade) ?? null;
}

/**
 * Tira o bloco do texto. Bloco sem a cerca de fechamento termina nas crases que abrem o bloco
 * seguinte (```opcoes): essas ficam, como no backend.
 */
function removerBloco(texto: string, bloco: RegExpMatchArray): string {
  const inicio = bloco.index ?? 0;
  const fim = inicio + bloco[0].length;
  const abreOutro = bloco[0].endsWith("```") && BLOCOS_QUE_FICAM.some((nome) => texto.startsWith(nome, fim));
  return texto.slice(0, inicio) + texto.slice(abreOutro ? fim - 3 : fim);
}

/** Formato antigo: barras com itens [{label, valor}]. */
function lerItens(dados: Record<string, unknown>): GraficoTatiDados | null {
  if (!Array.isArray(dados.itens)) return null;
  const itens = dados.itens
    .map((item: { label?: unknown; valor?: unknown }) => ({ label: String(item?.label ?? ""), valor: Number(item?.valor) }))
    .filter((item) => item.label && Number.isFinite(item.valor));
  if (itens.length === 0) return null;

  const titulo = texto(dados.titulo) ?? "Gráfico da Tati";
  return {
    tipo: "barras",
    titulo,
    unidade: lerUnidade(dados.unidade),
    fonte: lerFonte(dados.fonte),
    categorias: itens.map((item) => item.label),
    series: [{ nome: titulo, valores: itens.map((item) => item.valor) }],
  };
}

/** Formato novo: eixoX + series, cada série do tamanho do eixo. */
function lerSeries(dados: Record<string, unknown>, tipo: TipoGrafico): GraficoTatiDados | null {
  const categorias = Array.isArray(dados.eixoX) ? dados.eixoX.map((x) => String(x)) : [];
  if (categorias.length === 0 || !Array.isArray(dados.series)) return null;

  const series = dados.series
    .map((serie: { nome?: unknown; valores?: unknown }) => ({
      nome: texto(serie?.nome) ?? "",
      valores: Array.isArray(serie?.valores)
        ? serie.valores.map((valor) => (typeof valor === "number" && Number.isFinite(valor) ? valor : null))
        : [],
    }))
    .filter((serie) => serie.nome && serie.valores.length === categorias.length);
  if (series.length === 0) return null;

  return {
    tipo,
    titulo: texto(dados.titulo) ?? "Gráfico da Tati",
    unidade: lerUnidade(dados.unidade),
    fonte: lerFonte(dados.fonte),
    categorias,
    series,
  };
}

function lerFonte(valor: unknown): FonteGrafico | null {
  if (typeof valor !== "object" || valor === null) return null;
  const fonte = valor as Record<string, unknown>;
  const lida = { ferramenta: texto(fonte.ferramenta) ?? undefined, empresa: texto(fonte.empresa) ?? undefined, periodo: texto(fonte.periodo) ?? undefined };
  return lida.ferramenta || lida.empresa || lida.periodo ? lida : null;
}

/** O gráfico do bloco, no formato da tela; null se o bloco não der um gráfico desenhável. */
export function lerGrafico(bruto: string): GraficoTatiDados | null {
  const lido = lerJson(bruto);
  if (typeof lido !== "object" || lido === null || Array.isArray(lido)) return null;
  const dados = lido as Record<string, unknown>;

  // Sem tipo (ausente, vazio ou que não é texto), o formato antigo (barras com itens), como no backend.
  const tipoInformado = texto(dados.tipo);
  if (tipoInformado === null) return lerItens(dados);
  const tipo = TIPOS.find((t) => t === tipoInformado);
  if (!tipo) return null;
  return tipo === "barras" && Array.isArray(dados.itens) ? lerItens(dados) : lerSeries(dados, tipo);
}

export function lerRespostaTati(textoDaResposta: string): RespostaTati {
  let corpo = textoDaResposta;
  let opcoes: string[] = [];
  let grafico: GraficoTatiDados | null = null;

  // O gráfico antes das opções, como o backend: a tela desenha exatamente o bloco que foi conferido.
  const blocoGrafico = corpo.match(BLOCO_GRAFICO);
  if (blocoGrafico) {
    grafico = lerGrafico(blocoGrafico[1]);
    corpo = removerBloco(corpo, blocoGrafico);
  }

  const blocoOpcoes = corpo.match(BLOCO_OPCOES);
  if (blocoOpcoes) {
    corpo = corpo.replace(BLOCO_OPCOES, "");
    const lista = lerJson(blocoOpcoes[1]);
    if (Array.isArray(lista)) {
      opcoes = lista.filter((item): item is string => typeof item === "string" && item.trim() !== "");
    }
  }

  return { corpo: corpo.trim(), opcoes, grafico };
}

const MODULOS: Record<string, string> = {
  consultar_indicadores: "Indicadores",
  consultar_historico_indicadores: "Indicadores",
  consultar_balanco: "Balanço",
  consultar_balancete: "Balancete",
  consultar_dfc: "DFC",
  consultar_status_backlog: "Conciliação",
  consultar_backlog_por_empresa: "Conciliação",
  consultar_produtividade_time: "Produtividade",
  consultar_saldo: "Fechamentos",
  consultar_fechamento: "Fechamentos",
};

const MESES = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

/** "2025-10" → "out/2025"; outro texto passa como veio. */
export function rotuloDoMes(anoMes: string): string {
  const partes = /^(\d{4})-(\d{2})$/.exec(anoMes.trim());
  const mes = partes ? Number(partes[2]) : 0;
  return partes && mes >= 1 && mes <= 12 ? `${MESES[mes - 1]}/${partes[1]}` : anoMes;
}

/** "Fonte: Indicadores · EMPRESA TESTE LTDA · out/2025 a set/2026"; null sem fonte. */
export function descreverFonte(fonte: FonteGrafico | null): string | null {
  if (!fonte) return null;
  const periodo = fonte.periodo?.replace(/\d{4}-\d{2}/g, (anoMes) => rotuloDoMes(anoMes));
  const partes = [fonte.ferramenta && (MODULOS[fonte.ferramenta] ?? "CFO Tati"), fonte.empresa, periodo].filter(Boolean);
  return partes.length > 0 ? `Fonte: ${partes.join(" · ")}` : null;
}
