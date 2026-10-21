export interface MensagemCfoTati {
  autor: "usuario" | "tati";
  texto: string;
  horario: string;
  /** Período e empresa usados na pergunta — aparece como "Com base em" na resposta da Tati. */
  contexto?: string;
}

export interface PerguntarCfoTatiPayload {
  pergunta: string;
  /** Último mês do período (AAAA-MM). */
  anoMes: string;
  /** Primeiro mês do período, quando ele tem mais de um. */
  anoMesInicial?: string;
  empresa?: string;
  codColigada?: number;
  codFilial?: number;
  codConta?: string;
  status?: string;
  historico: Array<{ autor: "usuario" | "tati"; texto: string }>;
}
