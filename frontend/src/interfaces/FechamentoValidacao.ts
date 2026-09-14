export type StatusFechamento = "Pendente" | "Preparado" | "Conferido" | "Fechado";

export interface FechamentoValidacaoStatus {
  status: StatusFechamento;
  prioridade: number | null;
  saldoAnterior: number | null;
  saldoAtual: number | null;

  preparadoPorId: string | null;
  preparadoPorNome: string | null;
  preparadoEm: string | null;

  conferidoPorId: string | null;
  conferidoPorNome: string | null;
  conferidoEm: string | null;

  fechadoPorId: string | null;
  fechadoPorNome: string | null;
  fechadoEm: string | null;

  pendenciaTexto: string | null;
  pendenciaResponsavelId: string | null;
  pendenciaResponsavelNome: string | null;
  pendenciaRegistradaEm: string | null;
}

export interface ChaveFechamento {
  codColigada: number;
  codFilial: number;
  codConta: string;
  anoMes: string;
}
