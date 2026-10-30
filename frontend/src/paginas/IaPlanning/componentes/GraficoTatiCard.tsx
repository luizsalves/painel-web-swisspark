import { Component, lazy, Suspense, type ReactNode } from "react";
import { X } from "lucide-react";
import { descreverFonte, type GraficoTatiDados } from "../respostaTati";

// O Recharts só é baixado quando o primeiro gráfico aparece, e só na tela da Tati.
const GraficoRecharts = lazy(() => import("./GraficoRecharts"));

/**
 * Se o Recharts não baixar (deploy novo com a aba aberta, rede caindo) ou falhar ao desenhar, só
 * o card mostra o aviso: sem isso o React desmonta a tela inteira e a conversa se perde.
 */
class ProtecaoDoDesenho extends Component<{ children: ReactNode }, { falhou: boolean }> {
  state = { falhou: false };

  static getDerivedStateFromError() {
    return { falhou: true };
  }

  render() {
    return this.state.falhou ? <p className="ia-grafico-tati__carregando">Não consegui desenhar o gráfico.</p> : this.props.children;
  }
}

export interface GraficoTati extends GraficoTatiDados {
  id: number;
  horario: string;
}

/** O card de um gráfico da Tati: título, hora, o desenho e, embaixo, de onde vieram os números. */
export default function GraficoTatiCard({ grafico, onRemover }: { grafico: GraficoTati; onRemover: () => void }) {
  const fonte = descreverFonte(grafico.fonte);

  return (
    <div className="sp-card ia-grafico-tati" id={`grafico-tati-${grafico.id}`}>
      <div className="ia-grafico-tati__topo">
        <div>
          <h2 className="sp-card__title">{grafico.titulo}</h2>
          <small>Criado pela Tati às {grafico.horario}</small>
        </div>
        <button type="button" onClick={onRemover} aria-label={`Remover o gráfico ${grafico.titulo}`}>
          <X size={16} />
        </button>
      </div>
      <div className="ia-grafico-tati__desenho" role="img" aria-label={grafico.titulo}>
        <ProtecaoDoDesenho>
          <Suspense fallback={<p className="ia-grafico-tati__carregando">Carregando o gráfico…</p>}>
            <GraficoRecharts grafico={grafico} />
          </Suspense>
        </ProtecaoDoDesenho>
      </div>
      {fonte && <p className="ia-grafico-tati__fonte">{fonte}</p>}
    </div>
  );
}
