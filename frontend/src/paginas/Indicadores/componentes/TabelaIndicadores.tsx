import { Info } from "lucide-react";
import { idDoDetalhe } from "../ancoras";
import { GRUPOS } from "../config";
import type { HistoricoIndicadores } from "../historico";
import { emOrdem, textoDoValor } from "../regras";
import type { Indicadores, ResultadoIndicador } from "../regras";
import ReguaFaixas from "./ReguaFaixas";
import Sparkline from "./Sparkline";
import StatusPill from "./StatusPill";
import Variacao from "./Variacao";

export interface RotulosComparacao {
  atual: string;
  /** O período comparado com o atual — o da variação. */
  comparada: string;
  /** A terceira coluna (o mesmo período do ano anterior); nula quando a comparação escolhida não a mostra. */
  extra: string | null;
}

interface TabelaIndicadoresProps {
  indicadores: Indicadores;
  rotulos: RotulosComparacao;
  /** Nulo enquanto o histórico carrega (ou se ele falhou): as comparações mostram "—". */
  historico: HistoricoIndicadores | null;
  /** "mês" ou "período", para o título do grupo de rentabilidade. */
  periodo: string;
  nota: string;
}

function ValorAtual({ indicador }: { indicador: ResultadoIndicador }) {
  return (
    <span className="sp-ind-tabela__atual">
      <strong>{textoDoValor(indicador)}</strong>
      {indicador.detalheValor && <small>{indicador.detalheValor}</small>}
    </span>
  );
}

function ValorComparado({ indicador }: { indicador: ResultadoIndicador | undefined }) {
  return <span className="sp-ind-num sp-ind-tabela__comparacao">{indicador ? textoDoValor(indicador) : "—"}</span>;
}

/**
 * Indicadores por grupo, com a régua de faixas, o atual, as comparações, o
 * minigráfico dos 12 meses e o status.
 */
export default function TabelaIndicadores({ indicadores, rotulos, historico, periodo, nota }: TabelaIndicadoresProps) {
  const linhas = emOrdem(indicadores);
  const comExtra = rotulos.extra !== null;

  return (
    <section className={`sp-ind-card sp-ind-tabela${comExtra ? "" : " sp-ind-tabela--sem-extra"}`} aria-label="Indicadores por grupo">
      <div className="sp-ind-tabela__rolagem">
        <div className="sp-ind-tabela__conteudo">
          <div className="sp-ind-tabela__linha sp-ind-tabela__linha--cabecalho">
            <span>Indicador</span>
            <span>Faixa</span>
            <span className="sp-ind-num">{rotulos.atual}</span>
            <span className="sp-ind-num">{rotulos.comparada}</span>
            <span className="sp-ind-num">Variação</span>
            {comExtra && <span className="sp-ind-num">{rotulos.extra}</span>}
            <span className="sp-ind-num">12 meses</span>
            <span>Status</span>
          </div>
          {GRUPOS.map((grupo) => (
            <div key={grupo.codigo} id={idDoDetalhe(grupo.codigo)} className="sp-ind-tabela__grupo">
              <div className="sp-ind-tabela__titulo-grupo">
                {grupo.codigo === "rentabilidade" ? `${grupo.titulo} · no ${periodo}` : grupo.titulo}
              </div>
              {linhas.filter((linha) => linha.config.grupo === grupo.codigo).map((linha) => {
                const codigo = linha.config.codigo;
                const comparado = historico?.comparada?.indicadores[codigo];
                return (
                  <div key={codigo} className="sp-ind-tabela__linha">
                    <div className="sp-ind-tabela__nome">
                      <div>
                        <span>{linha.config.nome}</span>
                        <span className="sp-ind-tabela__info" tabIndex={0} role="img"
                          aria-label={`${linha.config.formula}. ${linha.config.explicacao}`}
                          title={`${linha.config.formula}\n${linha.config.explicacao}`}>
                          <Info size={16} aria-hidden="true" />
                        </span>
                      </div>
                      {linha.nota && <small>{linha.nota}</small>}
                    </div>
                    <ReguaFaixas indicador={linha} />
                    <ValorAtual indicador={linha} />
                    <ValorComparado indicador={comparado} />
                    <span className="sp-ind-num">
                      <Variacao atual={linha.valor} anterior={comparado?.valor ?? null} config={linha.config} />
                    </span>
                    {comExtra && <ValorComparado indicador={historico?.extra?.indicadores[codigo]} />}
                    <span className="sp-ind-num">
                      <Sparkline serie={historico ? historico.meses.map((mes) => mes.indicadores?.[codigo].valor ?? null) : null} />
                    </span>
                    <span><StatusPill status={linha.status} /></span>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>
      <div className="sp-ind-tabela__rodape">
        <span>{nota}</span>
        <span>Passe o mouse no ícone ao lado do nome para ver a fórmula.</span>
      </div>
    </section>
  );
}
