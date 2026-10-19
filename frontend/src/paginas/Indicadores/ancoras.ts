/** Âncoras da visão do contador, para onde o "Ver no detalhe" da diretoria rola. */
export type Detalhe = "liquidez" | "endividamento" | "rentabilidade" | "dfc";

export function idDoDetalhe(detalhe: Detalhe) {
  return detalhe === "dfc" ? "ind-dfc" : `ind-grupo-${detalhe}`;
}

export function lerDetalhe(valor: string | null): Detalhe | null {
  return valor === "liquidez" || valor === "endividamento" || valor === "rentabilidade" || valor === "dfc" ? valor : null;
}
