import type { Alerta } from "../regras";
import StatusPill from "./StatusPill";

export default function ListaAlertas({ alertas }: { alertas: Alerta[] }) {
  return (
    <section className="sp-ind-card sp-ind-alertas" aria-label="O que exige atenção">
      <h2>O que exige atenção</h2>
      {alertas.length === 0 ? (
        <p className="sp-ind-alertas__vazio">Nenhum ponto exige atenção neste fechamento.</p>
      ) : (
        <ul>
          {alertas.map((alerta) => (
            <li key={alerta.texto}>
              <StatusPill status={alerta.severidade} variante="fixa" />
              <span>{alerta.texto}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
