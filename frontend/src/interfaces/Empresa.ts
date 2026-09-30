export interface Empresa {
  id: string;
  codColigada: number;
  codFilial: number;
  nome: string;
  nomeFantasia: string;
  nomeFilial: string;
  cgc: string;
  cidade: string;
  estado: string;
  /** Flag do painel (não existe no TOTVS). Inativa some dos comboboxes de Fechamentos. */
  ativo: boolean;
}
