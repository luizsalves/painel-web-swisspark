export interface ContaContabilTotvs {
  id: string;
  codColigada: number;
  codConta: string;
  reduzido: string;
  descricao: string;
  /** Flag do painel (não existe no TOTVS). Inativa some do combobox de Fechamentos. */
  ativo: boolean;
}
