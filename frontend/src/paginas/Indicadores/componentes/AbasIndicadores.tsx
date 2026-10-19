import { VISOES } from "../visoes";
import type { Visao } from "../visoes";

interface AbasIndicadoresProps {
  ativa: Visao;
  onTrocar: (visao: Visao) => void;
}

export default function AbasIndicadores({ ativa, onTrocar }: AbasIndicadoresProps) {
  return (
    <div className="sp-ind-abas" role="tablist" aria-label="Visões dos indicadores">
      {VISOES.map(({ visao, rotulo }) => (
        <button key={visao} type="button" role="tab" aria-selected={ativa === visao}
          className={ativa === visao ? "sp-ind-abas__aba sp-ind-abas__aba--ativa" : "sp-ind-abas__aba"}
          onClick={() => onTrocar(visao)}>
          {rotulo}
        </button>
      ))}
    </div>
  );
}
