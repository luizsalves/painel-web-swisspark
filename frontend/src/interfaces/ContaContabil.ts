export interface ContaContabilTotvs {
  id: string;
  codColigada: number;
  codConta: string;
  codFiliais: number[];
  reduzido: string;
  descricao: string;
  /** Flag do painel (não existe no TOTVS). Inativa some do combobox de Fechamentos. */
  ativo: boolean;
}

export interface ColigadaFilialContabil {
  codColigada: number;
  codFilial: number;
}

export interface EmpresaContabil {
  codColigada: number;
  nome: string;
}
