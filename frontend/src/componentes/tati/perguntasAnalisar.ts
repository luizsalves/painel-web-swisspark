/**
 * As perguntas prontas dos botões "Analisar", por tipo de card. Dizem só o assunto e as análises
 * pedidas (período anterior, mesmo período do ano passado, composição, maiores variações): os
 * números a Tati busca nas ferramentas, com a empresa, o período e a tela que vão junto.
 */

const COMPARACOES = "compare com o período anterior e com o mesmo período do ano passado";
const COMPARACOES_DO_MES = "compare com o mês anterior e com o mesmo mês do ano passado";

export const perguntasAnalisar = {
  indicador: (nome: string) =>
    `Analise o indicador “${nome}”: o valor e o status, ${COMPARACOES}, e explique o que mais pesou na variação.`,
  pilar: (nome: string) =>
    `Analise o pilar “${nome}” da saúde financeira: o status, os indicadores que o formam, ${COMPARACOES}, e o que explica a mudança.`,
  saudeGeral: () =>
    `Analise a saúde financeira geral da empresa: o veredito e os quatro pilares, ${COMPARACOES}, e o que mais exige atenção.`,
  resultado: () =>
    `Analise o resultado, da receita ao lucro: a composição, ${COMPARACOES}, e o que mais pesou.`,
  caixa: () =>
    `Analise de onde veio e para onde foi o caixa no período (DFC): operação, investimentos e financiamentos, ${COMPARACOES}, e as contas que mais pesaram.`,
  divida: () =>
    `Analise a dívida da empresa: dívida bruta, caixa, dívida líquida e o múltiplo sobre o EBITDA anualizado, o que vence no curto prazo, e ${COMPARACOES}.`,
  evolucao: () =>
    "Analise a evolução dos indicadores nos últimos 12 meses: as tendências, os meses fora da curva e o que mudou.",
  balanco: () =>
    `Analise o balanço patrimonial no fim do período: a composição do ativo, do passivo e do patrimônio líquido, e as maiores variações — ${COMPARACOES_DO_MES}.`,
  ladoDoBalanco: (lado: string) =>
    `Analise o ${lado} do balanço no fim do período: a composição e as maiores variações — ${COMPARACOES_DO_MES}.`,
  dfc: () =>
    `Analise a DFC do período: quanto a operação gerou ou consumiu de caixa, os investimentos e os financiamentos, ${COMPARACOES}, e as contas que mais pesaram.`,
  balancete: () =>
    "Analise o balancete do mês: os grupos com maior saldo, as maiores movimentações do mês e o que chama atenção.",
  conciliacao: () =>
    "Analise a conciliação do período: o backlog por etapa e as empresas com mais contas pendentes.",
  etapaDaConciliacao: (etapa: string) =>
    `Analise “${etapa}” na conciliação do período: quantas contas são, em quais empresas e o que falta.`,
  fechamentoDaConta: () =>
    "Analise o fechamento desta conta no mês: o status, quem preparou, conferiu e fechou, e o saldo do razão.",
};
