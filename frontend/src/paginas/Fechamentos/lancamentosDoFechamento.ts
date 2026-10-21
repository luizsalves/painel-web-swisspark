import api from "../../services/api";
import type { LancamentoTotvs } from "../../interfaces/LancamentoTotvs";
import { FILIAL_CONSOLIDADA } from "../../utils/empresaContabil";

export interface ChaveLancamentos {
  /** Nome da empresa (GFILIAL.NOME) — é por ele que o consolidado junta as filiais. */
  empresa: string;
  codColigada: number;
  codFilial: number;
  codConta: string;
  anoMes: string;
}

/** Lançamentos da conta no mês com a linha-resumo; na filial 0, os de todas as filiais da empresa. */
export async function carregarLancamentos(
  { empresa, codColigada, codFilial, codConta, anoMes }: ChaveLancamentos, signal?: AbortSignal,
): Promise<LancamentoTotvs[]> {
  const { data } = codFilial === FILIAL_CONSOLIDADA
    ? await api.get<LancamentoTotvs[]>("/lancamentos/consolidado", { params: { codColigada, empresa, anoMes, codConta }, signal })
    : await api.get<LancamentoTotvs[]>("/lancamentos", { params: { codColigada, codFilial, anoMes, codConta }, signal });
  return data;
}

export function somarMeses(anoMes: string, meses: number) {
  const [ano, mes] = anoMes.split("-").map(Number);
  const total = ano * 12 + (mes - 1) + meses;
  return `${Math.floor(total / 12)}-${String((total % 12) + 1).padStart(2, "0")}`;
}

export function mesesDoPeriodo(inicio: string, fim: string) {
  const meses: string[] = [];
  for (let anoMes = inicio; anoMes <= fim; anoMes = somarMeses(anoMes, 1)) meses.push(anoMes);
  return meses;
}

/** A linha-resumo (data nula) traz os saldos; as demais são as partidas do mês. */
function resumirMes(linhas: LancamentoTotvs[]) {
  const detalhe = linhas.filter((linha) => linha.data !== null);
  const resumo = linhas.find((linha) => linha.data === null);
  return {
    detalhe,
    semResumo: !resumo,
    saldoAnterior: resumo?.saldoAnterior ?? 0,
    saldoMes: resumo?.saldoRazao ?? 0,
    debitos: detalhe.reduce((total, linha) => total + linha.debitos, 0),
    creditos: detalhe.reduce((total, linha) => total + linha.creditos, 0),
  };
}

/**
 * O resumo de meses seguidos, do mais antigo ao mais novo. Mês sem lançamento não traz a
 * linha-resumo, mas o saldo não mudou nele: repete o do mês anterior (o primeiro da lista,
 * sem mês antes para olhar, fica zerado).
 */
export function resumirMeses(listas: LancamentoTotvs[][]) {
  let saldo = 0;
  return listas.map((linhas) => {
    const { semResumo, ...mes } = resumirMes(linhas);
    if (semResumo) {
      mes.saldoAnterior = saldo;
      mes.saldoMes = saldo;
    }
    saldo = mes.saldoMes;
    return mes;
  });
}

/** Como a partida aparece na tabela (e no Excel): mesmo sinal da aba Lançamentos. */
export function descreverPartida(linha: LancamentoTotvs) {
  return {
    data: linha.data ? new Date(linha.data).toLocaleDateString("pt-BR") : "",
    idPartida: linha.idPartida != null ? String(linha.idPartida) : "",
    historico: linha.complemento || linha.reduzido || "",
    valor: linha.debitos !== 0 ? linha.debitos : linha.creditos,
    tipo: linha.debitos !== 0 ? "Débito" : "Crédito",
  };
}

/** A empresa como sai no Excel: lá a filial 0 não diria nada a quem lê. */
export function descreverEmpresa(empresa: string, codFilial: number) {
  return codFilial === FILIAL_CONSOLIDADA ? `${empresa} (filiais consolidadas)` : empresa;
}

export function descreverFilial(codFilial: number) {
  return codFilial === FILIAL_CONSOLIDADA ? "Todas (consolidado)" : String(codFilial);
}
