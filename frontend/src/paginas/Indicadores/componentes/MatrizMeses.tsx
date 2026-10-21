import { INDICADORES } from "../config";
import type { HistoricoIndicadores } from "../historico";
import { rotuloMes } from "../periodo";
import { ROTULO_STATUS, textoDoValor } from "../regras";
import type { Status } from "../regras";
import StatusPill from "./StatusPill";

const LEGENDA: Status[] = ["bom", "atencao", "risco", "verificar"];

interface MatrizMesesProps {
  /** Os 12 meses até o fim do período; nulo enquanto o histórico carrega ou se ele falhou. */
  meses: HistoricoIndicadores["meses"] | null;
  /** O que aparece no lugar da matriz enquanto ela não tem os meses. */
  mensagem: string;
}

/** Cada indicador mês a mês nos últimos 12 meses, cada célula na cor do status daquele mês. */
export default function MatrizMeses({ meses, mensagem }: MatrizMesesProps) {
  return (
    <section className="sp-ind-card sp-ind-matriz" aria-label="Matriz dos últimos 12 meses">
      <header className="sp-ind-matriz__topo">
        <h2>Matriz dos últimos 12 meses</h2>
        <span>Cada célula na cor do status daquele mês</span>
      </header>

      {!meses ? (
        <p className="sp-ind-matriz__mensagem">{mensagem}</p>
      ) : (
        <>
          <div className="sp-ind-matriz__rolagem">
            <table className="sp-ind-matriz__tabela">
              <thead>
                <tr>
                  <th scope="col">Indicador</th>
                  {meses.map(({ anoMes }, indice) => (
                    <th key={anoMes} scope="col" className={indice === meses.length - 1 ? "sp-ind-matriz__mes-atual" : undefined}>
                      {rotuloMes(anoMes)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {INDICADORES.map((config) => (
                  <tr key={config.codigo}>
                    <th scope="row">{config.nome}</th>
                    {meses.map(({ anoMes, indicadores }) => {
                      const indicador = indicadores?.[config.codigo];
                      return (
                        <td key={anoMes}>
                          {indicador ? (
                            <span className={`sp-ind-matriz__celula sp-ind-matriz__celula--${indicador.status}`}
                              title={`${config.nome} em ${rotuloMes(anoMes)}: ${ROTULO_STATUS[indicador.status]}`}>
                              {textoDoValor(indicador)}
                              {/* A cor não é o único sinal do status. */}
                              <span className="visually-hidden"> ({ROTULO_STATUS[indicador.status]})</span>
                            </span>
                          ) : (
                            <span className="sp-ind-matriz__celula sp-ind-matriz__celula--vazia" title="Sem lançamentos nesse mês">—</span>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <footer className="sp-ind-matriz__legenda">
            {LEGENDA.map((status) => <StatusPill key={status} status={status} />)}
            <span>
              Cada mês sozinho: ROE, ROA, margens e EBITDA são os do mês, e a Dív. líquida / EBITDA usa o EBITDA do mês × 12.
            </span>
          </footer>
        </>
      )}
    </section>
  );
}
