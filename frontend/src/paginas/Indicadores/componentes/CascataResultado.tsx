import type { DegrauCascata } from "../regras";

/** Altura da área das barras; o contêiner tem 30 px a mais para o valor da barra mais alta. */
const ALTURA = 220;
const FOLGA = 0.08;

const numeroMil = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

function emMil(valor: number, sinalMais: boolean) {
  const texto = numeroMil.format(Math.abs(valor) / 1000);
  return valor < 0 ? `-${texto}` : sinalMais && valor > 0 ? `+${texto}` : texto;
}

function corDaBarra(degrau: DegrauCascata) {
  if (degrau.tipo === "total") return degrau.valor < 0 ? "negativo" : "positivo";
  return degrau.valor < 0 ? "reduz" : "aumenta";
}

/**
 * Cascata vertical da receita ao lucro: os totais nascem do zero e os passos
 * flutuam entre o acumulado anterior e o novo. Valores em R$ mil.
 */
export default function CascataResultado({ degraus }: { degraus: DegrauCascata[] }) {
  const extremos = degraus.flatMap((degrau) => [degrau.de, degrau.ate]);
  const menor = Math.min(0, ...extremos);
  const maior = Math.max(0, ...extremos);
  const folga = (maior - menor || 1) * FOLGA;
  const minimo = menor - folga;
  const maximo = maior + folga;
  const altura = (valor: number) => ((valor - minimo) / (maximo - minimo)) * ALTURA;

  const descricao = degraus.map((degrau) => `${degrau.rotulo} ${emMil(degrau.valor, degrau.tipo === "passo")} mil`).join("; ");

  return (
    <div className="sp-ind-cascata">
      <div className="sp-ind-cascata__grafico" role="img" aria-label={`Da receita ao lucro, em R$ mil: ${descricao}`}>
        <div className="sp-ind-cascata__zero" style={{ bottom: `${altura(0)}px` }} />
        {degraus.map((degrau) => {
          const base = altura(Math.min(degrau.de, degrau.ate));
          const tamanho = Math.max(2, altura(Math.max(degrau.de, degrau.ate)) - base);
          return (
            <div key={degrau.rotulo} className="sp-ind-cascata__coluna">
              <div className={`sp-ind-cascata__barra sp-ind-cascata__barra--${corDaBarra(degrau)}`}
                style={{ bottom: `${base}px`, height: `${tamanho}px` }} />
              <span className={degrau.valor < 0 ? "sp-ind-cascata__valor sp-ind-negativo" : "sp-ind-cascata__valor"}
                style={{ bottom: `${base + tamanho + 6}px` }}>
                {emMil(degrau.valor, degrau.tipo === "passo")}
              </span>
            </div>
          );
        })}
      </div>
      <div className="sp-ind-cascata__rotulos" aria-hidden="true">
        {degraus.map((degrau) => <span key={degrau.rotulo}>{degrau.rotulo}</span>)}
      </div>
    </div>
  );
}
