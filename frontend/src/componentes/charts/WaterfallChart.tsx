interface WaterfallDatum {
  label: string;
  value: number;
  tipo: "total" | "diminuicao";
}

interface WaterfallChartProps {
  /** Deve começar e terminar com um item tipo "total" — os do meio vão descontando dele. */
  data: readonly WaterfallDatum[];
}

export default function WaterfallChart({ data }: WaterfallChartProps) {
  const maximo = Math.max(...data.filter((item) => item.tipo === "total").map((item) => item.value), 1);

  let acumulado = 0;
  const barras = data.map((item) => {
    if (item.tipo === "total") {
      acumulado = item.value;
      return { ...item, base: 0, topo: item.value };
    }

    const topo = acumulado;
    acumulado -= item.value;
    return { ...item, base: acumulado, topo };
  });

  return (
    <div className="sp-waterfall">
      <div className="sp-waterfall__bars">
        {barras.map((item) => (
          <div className="sp-waterfall__column" key={item.label}>
            <div className="sp-waterfall__bar-track">
              <div
                className="sp-waterfall__bar"
                style={{
                  bottom: `${(item.base / maximo) * 100}%`,
                  height: `${Math.max(((item.topo - item.base) / maximo) * 100, item.value > 0 ? 1.5 : 0)}%`,
                  background: item.tipo === "total" ? "var(--color-green-dark)" : "var(--color-warning)",
                }}
              />
            </div>
            <span className="sp-waterfall__value">{item.value}</span>
            <span className="sp-waterfall__label">{item.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
