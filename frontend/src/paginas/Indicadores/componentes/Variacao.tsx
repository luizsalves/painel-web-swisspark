import type { ConfigIndicador } from "../config";
import { formatarVariacao } from "../regras";

interface VariacaoProps {
  atual: number | null;
  anterior: number | null;
  config: ConfigIndicador;
}

/** Variação com sinal, na cor de melhorou/piorou (não de subiu/desceu); "—" sem base. */
export default function Variacao({ atual, anterior, config }: VariacaoProps) {
  const variacao = formatarVariacao(atual, anterior, config);
  if (!variacao) return <span className="sp-ind-variacao sp-ind-variacao--estavel">—</span>;
  return <span className={`sp-ind-variacao sp-ind-variacao--${variacao.sentido}`}>{variacao.texto}</span>;
}
