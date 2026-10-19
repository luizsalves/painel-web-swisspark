import { Info } from "lucide-react";
import { idDoDetalhe } from "../ancoras";
import { GRUPOS } from "../config";
import { emOrdem, formatarValor } from "../regras";
import type { Indicadores, ResultadoIndicador } from "../regras";
import ReguaFaixas from "./ReguaFaixas";
import Sparkline from "./Sparkline";
import StatusPill from "./StatusPill";
import Variacao from "./Variacao";

export interface RotulosComparacao {
  atual: string;
  anterior: string;
  anoAnterior: string;
}

interface TabelaIndicadoresProps {
  indicadores: Indicadores;
  rotulos: RotulosComparacao;
  /** "mês" ou "período", para o título do grupo de rentabilidade. */
  periodo: string;
  nota: string;
}

function ValorAtual({ indicador }: { indicador: ResultadoIndicador }) {
  return (
    <span className="sp-ind-tabela__atual">
      <strong>{indicador.textoValor ?? formatarValor(indicador.valor, indicador.config)}</strong>
      {indicador.detalheValor && <small>{indicador.detalheValor}</small>}
    </span>
  );
}

/**
 * Indicadores por grupo, com a régua de faixas, o atual, as comparações, o
 * minigráfico de 12 meses e o status. As comparações e a série chegam na fase 2;
 * até lá mostram "—".
 */
export default function TabelaIndicadores({ indicadores, rotulos, periodo, nota }: TabelaIndicadoresProps) {
  const linhas = emOrdem(indicadores);

  return (
    <section className="sp-ind-card sp-ind-tabela" aria-label="Indicadores por grupo">
      <div className="sp-ind-tabela__rolagem">
        <div className="sp-ind-tabela__conteudo">
          <div className="sp-ind-tabela__linha sp-ind-tabela__linha--cabecalho">
            <span>Indicador</span>
            <span>Faixa</span>
            <span className="sp-ind-num">{rotulos.atual}</span>
            <span className="sp-ind-num">{rotulos.anterior}</span>
            <span className="sp-ind-num">Variação</span>
            <span className="sp-ind-num">{rotulos.anoAnterior}</span>
            <span className="sp-ind-num">12 meses</span>
            <span>Status</span>
          </div>
          {GRUPOS.map((grupo) => (
            <div key={grupo.codigo} id={idDoDetalhe(grupo.codigo)} className="sp-ind-tabela__grupo">
              <div className="sp-ind-tabela__titulo-grupo">
                {grupo.codigo === "rentabilidade" ? `${grupo.titulo} · no ${periodo}` : grupo.titulo}
              </div>
              {linhas.filter((linha) => linha.config.grupo === grupo.codigo).map((linha) => (
                <div key={linha.config.codigo} className="sp-ind-tabela__linha">
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
                  <span className="sp-ind-num sp-ind-tabela__comparacao">—</span>
                  <span className="sp-ind-num"><Variacao atual={linha.valor} anterior={null} config={linha.config} /></span>
                  <span className="sp-ind-num sp-ind-tabela__comparacao">—</span>
                  <span className="sp-ind-num"><Sparkline serie={null} /></span>
                  <span><StatusPill status={linha.status} /></span>
                </div>
              ))}
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
