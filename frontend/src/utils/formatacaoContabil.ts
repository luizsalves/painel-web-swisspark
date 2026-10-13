export const numeroContabil = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatoData = new Intl.DateTimeFormat("pt-BR");

/** Valor em R$ sem símbolo; zero aparece como "—", como nos demonstrativos. */
export function formatarValor(valor: number) {
  return valor === 0 ? "—" : numeroContabil.format(valor);
}

/** Classe da célula numérica, em vermelho quando negativa. */
export function classeNumero(valor: number) {
  return valor < 0 ? "sp-indicators__number sp-indicators__number--negative" : "sp-indicators__number";
}

/** Último dia do mês AAAA-MM, em dd/mm/aaaa. */
export function ultimoDiaDoMes(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  // Dia 0 do mês seguinte = último dia do mês.
  return formatoData.format(new Date(ano, mes, 0));
}

/** Último dia do mês AAAA-MM por extenso: "31 de dezembro de 2025". */
export function ultimoDiaPorExtenso(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long", year: "numeric" }).format(new Date(ano, mes, 0));
}

/** Último dia do mês anterior a AAAA-MM — a data do saldo inicial do período. */
export function vesperaDoMes(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  return formatoData.format(new Date(ano, mes - 1, 0));
}

/** AAAA-MM em mm/aaaa. */
export function mesAno(anoMes: string) {
  const [ano, mes] = anoMes.split("-");
  return `${mes}/${ano}`;
}
