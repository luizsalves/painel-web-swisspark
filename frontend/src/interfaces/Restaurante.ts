export interface Prato {
  id: number;
  nome: string;
  tag: string;
  descricao: string;
  imagem: string;
}

export interface Restaurante {
  id: number;
  nome: string;
  pratos: Prato[];
}
