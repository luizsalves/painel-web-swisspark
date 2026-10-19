/**
 * Faixas e textos de cada indicador — o único lugar com esses números. São as
 * faixas iniciais; a contabilidade vai revisá-las (e, numa fase seguinte, elas
 * passam a vir do banco, editáveis em Configurações).
 *
 * Percentuais ficam em fração (0,72 = 72%), como o cálculo devolve: limites e
 * escala usam a mesma unidade do valor.
 */

export type Unidade = "numero" | "percentual" | "multiplo" | "moeda";
export type Direcao = "maior_melhor" | "menor_melhor";
export type GrupoIndicador = "liquidez" | "endividamento" | "rentabilidade";

export type CodigoIndicador =
  | "liquidez-corrente" | "liquidez-seca" | "liquidez-imediata" | "liquidez-geral"
  | "endividamento-geral" | "composicao-endividamento" | "divida-liquida-ebitda" | "divida-bruta-pl"
  | "roe" | "roa" | "margem-liquida" | "margem-bruta" | "ebitda";

export interface ConfigIndicador {
  codigo: CodigoIndicador;
  grupo: GrupoIndicador;
  nome: string;
  nomeDiretoria: string;
  formula: string;
  explicacao: string;
  unidade: Unidade;
  casas: number;
  direcao: Direcao;
  /** maior_melhor: abaixo disso é Risco. menor_melhor: acima disso é Risco. */
  risco: number;
  /** maior_melhor: a partir disso é Bom. menor_melhor: até isso é Bom. Entre os dois, Atenção. */
  bom: number;
  /** Escala da régua de faixas; nula quando o indicador não tem régua. */
  escala: { min: number; max: number } | null;
  /** O status (e a régua) usam o valor anualizado: valor do período × 12 ÷ meses. */
  statusAnualizado?: boolean;
}

export const GRUPOS: { codigo: GrupoIndicador; titulo: string }[] = [
  { codigo: "liquidez", titulo: "Liquidez" },
  { codigo: "endividamento", titulo: "Endividamento" },
  { codigo: "rentabilidade", titulo: "Rentabilidade e resultado" },
];

/** Na ordem em que aparecem na tabela do contador. */
export const INDICADORES: ConfigIndicador[] = [
  {
    codigo: "liquidez-corrente", grupo: "liquidez", nome: "Liquidez corrente", nomeDiretoria: "Contas de curto prazo",
    formula: "Ativo circulante ÷ Passivo circulante",
    explicacao: "Acima de 1, os ativos de curto prazo cobrem as dívidas que vencem em até 1 ano.",
    unidade: "numero", casas: 2, direcao: "maior_melhor", risco: 1.0, bom: 1.5, escala: { min: 0, max: 2 },
  },
  {
    codigo: "liquidez-seca", grupo: "liquidez", nome: "Liquidez seca", nomeDiretoria: "Curto prazo sem os estoques",
    formula: "(Ativo circulante − Estoques) ÷ Passivo circulante",
    explicacao: "Visão mais conservadora: tira os estoques, que demoram a virar caixa.",
    unidade: "numero", casas: 2, direcao: "maior_melhor", risco: 0.5, bom: 1.0, escala: { min: 0, max: 1.5 },
  },
  {
    codigo: "liquidez-imediata", grupo: "liquidez", nome: "Liquidez imediata", nomeDiretoria: "Caixa para o curto prazo",
    formula: "Disponível ÷ Passivo circulante",
    explicacao: "Quanto das dívidas de curto prazo o caixa e equivalentes pagam hoje.",
    unidade: "numero", casas: 2, direcao: "maior_melhor", risco: 0.1, bom: 0.3, escala: { min: 0, max: 0.5 },
  },
  {
    codigo: "liquidez-geral", grupo: "liquidez", nome: "Liquidez geral", nomeDiretoria: "Cobertura de todas as dívidas",
    formula: "(Ativo circulante + Realizável a longo prazo) ÷ (Passivo circulante + Passivo não circulante)",
    explicacao: "Visão de longo prazo, além dos 12 meses.",
    unidade: "numero", casas: 2, direcao: "maior_melhor", risco: 1.0, bom: 1.2, escala: { min: 0, max: 2 },
  },
  {
    codigo: "endividamento-geral", grupo: "endividamento", nome: "Endividamento geral", nomeDiretoria: "Ativos financiados por terceiros",
    formula: "Passivo total ÷ Ativo total",
    explicacao: "A parte dos ativos da empresa que foi financiada por terceiros.",
    unidade: "percentual", casas: 2, direcao: "menor_melhor", risco: 0.8, bom: 0.6, escala: { min: 0, max: 1 },
  },
  {
    codigo: "composicao-endividamento", grupo: "endividamento", nome: "Dívida no curto prazo", nomeDiretoria: "Dívidas que vencem em 12 meses",
    formula: "Passivo circulante ÷ Passivo total (composição do endividamento)",
    explicacao: "A parte das dívidas que vence em até 12 meses.",
    unidade: "percentual", casas: 2, direcao: "menor_melhor", risco: 0.7, bom: 0.4, escala: { min: 0, max: 1 },
  },
  {
    codigo: "divida-liquida-ebitda", grupo: "endividamento", nome: "Dív. líquida / EBITDA 12m", nomeDiretoria: "Anos de geração de caixa para pagar a dívida",
    formula: "(Dívida bruta − Caixa) ÷ EBITDA de 12 meses (EBITDA do período × 12 ÷ meses do período)",
    explicacao: "Quantos anos de geração operacional de caixa levariam para quitar a dívida.",
    unidade: "multiplo", casas: 2, direcao: "menor_melhor", risco: 4.0, bom: 2.5, escala: { min: 0, max: 6 },
  },
  {
    codigo: "divida-bruta-pl", grupo: "endividamento", nome: "Dívida bruta / PL", nomeDiretoria: "Dívida em relação ao capital próprio",
    formula: "Dívida bruta ÷ Patrimônio líquido",
    explicacao: "A proporção entre o capital de terceiros e o capital próprio.",
    unidade: "multiplo", casas: 2, direcao: "menor_melhor", risco: 2.0, bom: 1.0, escala: { min: 0, max: 3 },
  },
  {
    codigo: "roe", grupo: "rentabilidade", nome: "ROE", nomeDiretoria: "Retorno para os sócios",
    formula: "Lucro líquido ÷ Patrimônio líquido",
    explicacao: "Retorno sobre o patrimônio líquido no período. A faixa compara o valor anualizado.",
    unidade: "percentual", casas: 2, direcao: "maior_melhor", risco: 0, bom: 0.12, escala: { min: -0.1, max: 0.3 },
    statusAnualizado: true,
  },
  {
    codigo: "roa", grupo: "rentabilidade", nome: "ROA", nomeDiretoria: "Retorno sobre os ativos",
    formula: "Lucro líquido ÷ Ativo total",
    explicacao: "Retorno sobre o ativo total no período. A faixa compara o valor anualizado.",
    unidade: "percentual", casas: 2, direcao: "maior_melhor", risco: 0, bom: 0.04, escala: { min: -0.05, max: 0.1 },
    statusAnualizado: true,
  },
  {
    codigo: "margem-liquida", grupo: "rentabilidade", nome: "Margem líquida", nomeDiretoria: "Lucro a cada R$ 100 vendidos",
    formula: "Lucro líquido ÷ Receita líquida",
    explicacao: "Quanto sobra de lucro a cada R$ 1 de venda no período.",
    unidade: "percentual", casas: 2, direcao: "maior_melhor", risco: 0, bom: 0.1, escala: { min: -0.4, max: 0.4 },
  },
  {
    codigo: "margem-bruta", grupo: "rentabilidade", nome: "Margem bruta", nomeDiretoria: "Sobra depois dos custos",
    formula: "Lucro bruto ÷ Receita líquida",
    explicacao: "O que sobra da receita depois do custo dos produtos e serviços.",
    unidade: "percentual", casas: 2, direcao: "maior_melhor", risco: 0.2, bom: 0.4, escala: { min: 0, max: 1 },
  },
  {
    codigo: "ebitda", grupo: "rentabilidade", nome: "EBITDA", nomeDiretoria: "Geração de caixa da operação",
    formula: "EBIT + Depreciação + Amortização",
    explicacao: "Desempenho operacional puro, sem juros, impostos, depreciação e amortização.",
    unidade: "moeda", casas: 1, direcao: "maior_melhor", risco: 0, bom: 0, escala: null,
  },
];

export const CONFIG_POR_CODIGO = Object.fromEntries(INDICADORES.map((item) => [item.codigo, item])) as
  Record<CodigoIndicador, ConfigIndicador>;
