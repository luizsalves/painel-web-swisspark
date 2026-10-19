const LARGURA = 96;
const ALTURA = 24;

/** Minigráfico dos 12 meses: só a linha, na escala da própria série. Sem série, "—". */
export default function Sparkline({ serie }: { serie: (number | null)[] | null }) {
  const valores = serie?.filter((valor): valor is number => valor !== null && Number.isFinite(valor)) ?? [];
  if (!serie || valores.length < 2) return <span className="sp-ind-vazio">—</span>;

  const minimo = Math.min(...valores);
  const amplitude = Math.max(...valores) - minimo || 1;
  const pontos = serie
    .map((valor, indice) => valor === null ? null
      : `${((indice * LARGURA) / (serie.length - 1)).toFixed(1)},${(ALTURA - 2 - ((valor - minimo) / amplitude) * (ALTURA - 4)).toFixed(1)}`)
    .filter(Boolean)
    .join(" ");

  return (
    <svg className="sp-ind-sparkline" width={LARGURA} height={ALTURA} viewBox={`0 0 ${LARGURA} ${ALTURA}`} aria-hidden="true">
      <polyline points={pontos} fill="none" strokeWidth="1.5" strokeLinejoin="round" strokeLinecap="round" />
    </svg>
  );
}
