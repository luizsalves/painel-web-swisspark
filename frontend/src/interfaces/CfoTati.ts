export interface MensagemCfoTati {
  autor: "usuario" | "tati";
  texto: string;
  horario: string;
  /** Período e empresa usados na pergunta — aparece como "Com base em" na resposta da Tati. */
  contexto?: string;
}

export interface PerguntarCfoTatiPayload {
  pergunta: string;
  anoMes: string;
  empresa?: string;
  historico: Array<{ autor: "usuario" | "tati"; texto: string }>;
}
