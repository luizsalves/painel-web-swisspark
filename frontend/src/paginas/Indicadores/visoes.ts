/** "indices" é o painel de índices que já existia; as outras duas são as visões novas. */
export type Visao = "indices" | "contador" | "diretoria";

export const VISOES: { visao: Visao; rotulo: string }[] = [
  { visao: "indices", rotulo: "Índices financeiros" },
  { visao: "contador", rotulo: "Visão do contador" },
  { visao: "diretoria", rotulo: "Visão da diretoria" },
];

/** Aba que abre sem ?visao na URL: o painel que já existia. Depois pode depender do perfil do usuário. */
export const VISAO_PADRAO: Visao = "indices";

export function lerVisao(valor: string | null): Visao {
  return VISOES.find((item) => item.visao === valor)?.visao ?? VISAO_PADRAO;
}
