/** Rótulos e deslocamentos de períodos AAAA-MM usados pelas duas visões. */

const MESES_CURTOS = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
const MESES_EXTENSO = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro",
  "outubro", "novembro", "dezembro"];

function partes(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  return { ano, mes };
}

/** Quantos meses o período tem, contando os dois extremos. */
export function mesesDoPeriodo(inicio: string, fim: string) {
  const a = partes(inicio);
  const b = partes(fim);
  return (b.ano - a.ano) * 12 + (b.mes - a.mes) + 1;
}

export function deslocarMes(anoMes: string, meses: number) {
  const { ano, mes } = partes(anoMes);
  const total = ano * 12 + (mes - 1) + meses;
  return `${Math.floor(total / 12)}-${String((total % 12) + 1).padStart(2, "0")}`;
}

/** Os N meses imediatamente antes do período. */
export function periodoAnterior(inicio: string, fim: string) {
  const meses = mesesDoPeriodo(inicio, fim);
  return { inicio: deslocarMes(inicio, -meses), fim: deslocarMes(fim, -meses) };
}

export function mesmoPeriodoDoAnoAnterior(inicio: string, fim: string) {
  return { inicio: deslocarMes(inicio, -12), fim: deslocarMes(fim, -12) };
}

/** "Mar/26". */
export function rotuloMes(anoMes: string) {
  const { ano, mes } = partes(anoMes);
  return `${MESES_CURTOS[mes - 1]}/${String(ano).slice(2)}`;
}

/** "Mar/26", "Jan–Mar/26" ou "Out/25–Mar/26": cabe numa coluna estreita. */
export function rotuloPeriodo(inicio: string, fim: string) {
  if (inicio === fim) return rotuloMes(inicio);
  const a = partes(inicio);
  const b = partes(fim);
  return a.ano === b.ano ? `${MESES_CURTOS[a.mes - 1]}–${rotuloMes(fim)}` : `${rotuloMes(inicio)}–${rotuloMes(fim)}`;
}

/** "mar/2026" ou "jan a mar/2026", para frases. */
export function descreverPeriodo(inicio: string, fim: string) {
  const a = partes(inicio);
  const b = partes(fim);
  const mes = (p: { ano: number; mes: number }) => MESES_CURTOS[p.mes - 1].toLowerCase();
  if (inicio === fim) return `${mes(b)}/${b.ano}`;
  return a.ano === b.ano ? `${mes(a)} a ${mes(b)}/${b.ano}` : `${mes(a)}/${a.ano} a ${mes(b)}/${b.ano}`;
}

/** "Março de 2026" ou "Janeiro a março de 2026", para subtítulos. */
export function descreverPeriodoExtenso(inicio: string, fim: string) {
  const a = partes(inicio);
  const b = partes(fim);
  const texto = inicio === fim
    ? `${MESES_EXTENSO[b.mes - 1]} de ${b.ano}`
    : a.ano === b.ano
      ? `${MESES_EXTENSO[a.mes - 1]} a ${MESES_EXTENSO[b.mes - 1]} de ${b.ano}`
      : `${MESES_EXTENSO[a.mes - 1]} de ${a.ano} a ${MESES_EXTENSO[b.mes - 1]} de ${b.ano}`;
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

export function nomeDoMes(anoMes: string) {
  return MESES_EXTENSO[partes(anoMes).mes - 1];
}

/** "mês" quando o período tem um mês só; senão "período". */
export function palavraPeriodo(meses: number) {
  return meses === 1 ? "mês" : "período";
}
