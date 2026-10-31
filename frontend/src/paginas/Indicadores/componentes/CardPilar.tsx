import BotaoAnalisarTati from "../../../componentes/tati/BotaoAnalisarTati";
import { perguntasAnalisar } from "../../../componentes/tati/perguntasAnalisar";
import type { PilarDetalhado } from "../regras";
import StatusPill from "./StatusPill";

interface CardPilarProps {
  pilar: PilarDetalhado;
  onVerDetalhe: () => void;
}

export default function CardPilar({ pilar, onVerDetalhe }: CardPilarProps) {
  return (
    <div className="sp-ind-pilar">
      <div className="sp-ind-pilar__topo">
        <span>{pilar.nome}<BotaoAnalisarTati assunto={`o pilar ${pilar.nome}`} pergunta={perguntasAnalisar.pilar(pilar.nome)} /></span>
        <StatusPill status={pilar.status} />
      </div>
      <strong className="sp-ind-pilar__valor">{pilar.valor}</strong>
      <span className="sp-ind-pilar__frase">{pilar.frase}</span>
      {/* "Ano anterior: X" entra com o histórico (fase 2). */}
      <button type="button" className="sp-ind-link sp-ind-pilar__detalhe" onClick={onVerDetalhe}>Ver no detalhe</button>
    </div>
  );
}
