import type { StatusPriorityDatum } from "../../interfaces/Priority";

const SERIES: { key: keyof StatusPriorityDatum["breakdown"]; label: string; color: string }[] = [
  { key: "alta", label: "Alta", color: "var(--color-red)" },
  { key: "media", label: "Média", color: "var(--color-warning)" },
  { key: "regular", label: "Regular", color: "var(--color-green)" },
  { key: "baixa", label: "Baixa", color: "var(--color-green-dark)" },
];

interface StackedBarChartProps {
  data: StatusPriorityDatum[];
}

export default function StackedBarChart({ data }: StackedBarChartProps) {
  const totals = data.map((item) =>
    SERIES.reduce((sum, series) => sum + item.breakdown[series.key], 0)
  );
  const maxTotal = Math.max(...totals, 1);

  return (
    <div className="sp-stacked-chart">
      <div className="sp-stacked-chart__bars">
        {data.map((item, index) => {
          const total = totals[index];
          return (
            <div className="sp-stacked-chart__column" key={item.status}>
              <div className="sp-stacked-chart__bar-track">
                <div
                  className="sp-stacked-chart__bar"
                  style={{ height: `${(total / maxTotal) * 100}%` }}
                >
                  {SERIES.slice()
                    .reverse()
                    .map((series) => {
                      const value = item.breakdown[series.key];
                      if (value === 0 || total === 0) return null;
                      return (
                        <div
                          key={series.key}
                          className="sp-stacked-chart__segment"
                          style={{
                            height: `${(value / total) * 100}%`,
                            background: series.color,
                          }}
                          title={`${series.label}: ${value}`}
                        />
                      );
                    })}
                </div>
              </div>
              <span className="sp-stacked-chart__value">{total}</span>
              <span className="sp-stacked-chart__label">{item.status}</span>
            </div>
          );
        })}
      </div>

      <div className="sp-chart-legend">
        {SERIES.map((series) => (
          <div className="sp-chart-legend__item" key={series.key}>
            <span className="sp-chart-legend__dot" style={{ background: series.color }} />
            {series.label}
          </div>
        ))}
      </div>
    </div>
  );
}

