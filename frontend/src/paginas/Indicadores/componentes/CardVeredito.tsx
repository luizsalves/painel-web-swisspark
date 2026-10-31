import BotaoAnalisarTati from "../../../componentes/tati/BotaoAnalisarTati";
import { perguntasAnalisar } from "../../../componentes/tati/perguntasAnalisar";
import type { Detalhe } from "../ancoras";
import type { CodigoPilar, PilarDetalhado, Status } from "../regras";
import CardPilar from "./CardPilar";
import StatusPill from "./StatusPill";

/** Onde cada pilar se explica na visão do contador. */
const DETALHE_DO_PILAR: Record<CodigoPilar, Detalhe> = {
  resultado: "rentabilidade",
  caixa: "dfc",
  divida: "endividamento",
  curtoPrazo: "liquidez",
};

interface CardVereditoProps {
  status: Status;
  frase: string;
  descricaoPeriodo: string;
  pilares: PilarDetalhado[];
  onVerDetalhe: (detalhe: Detalhe) => void;
}

export default function CardVeredito({ status, frase, descricaoPeriodo, pilares, onVerDetalhe }: CardVereditoProps) {
  return (
    <section className="sp-ind-card sp-ind-veredito" aria-label="Saúde geral" data-card="veredito">
      <div className="sp-ind-veredito__topo">
        <StatusPill status={status} variante="grande" />
        <span>Saúde geral em {descricaoPeriodo} · o pior status entre os quatro pilares</span>
        <BotaoAnalisarTati assunto="a saúde geral" pergunta={perguntasAnalisar.saudeGeral()} />
      </div>
      <h2 className="sp-ind-veredito__frase">{frase}</h2>
      <div className="sp-ind-veredito__pilares">
        {pilares.map((pilar) => (
          <CardPilar key={pilar.codigo} pilar={pilar} onVerDetalhe={() => onVerDetalhe(DETALHE_DO_PILAR[pilar.codigo])} />
        ))}
      </div>
    </section>
  );
}
