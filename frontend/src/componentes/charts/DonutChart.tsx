import type { RealizedPriorityDatum } from "../../interfaces/Priority";

const COLOR_BY_PRIORITY: Record<string, string> = {
  Alta: "var(--color-red)",
  Média: "var(--color-warning)",
  Regular: "var(--color-green)",
  Baixa: "var(--color-green-dark)",
};

interface DonutChartProps {
  data: RealizedPriorityDatum[];
}

const RADIUS = 60;
const STROKE = 16;

export default function DonutChart({ data }: DonutChartProps) {
  return (
    <div className="sp-donut-chart">
      <div className="sp-donut-chart__rings">
        <svg viewBox="0 0 160 160" width="180" height="180">
          <g transform="translate(80,80) rotate(-90)">
            {data.map((item, index) => {
              const radius = RADIUS - index * (STROKE + 4);
              return (
                <circle
                  key={item.priority}
                  r={radius}
                  cx={0}
                  cy={0}
                  fill="none"
                  stroke="var(--color-border)"
                  strokeWidth={STROKE}
                />
              );
            })}
            {data.map((item, index) => {
              const radius = RADIUS - index * (STROKE + 4);
              const circumference = 2 * Math.PI * radius;
              const dash = (item.percentage / 100) * circumference;
              return (
                <circle
                  key={`${item.priority}-value`}
                  r={radius}
                  cx={0}
                  cy={0}
                  fill="none"
                  stroke={COLOR_BY_PRIORITY[item.priority]}
                  strokeWidth={STROKE}
                  strokeDasharray={`${dash} ${circumference - dash}`}
                  strokeLinecap="round"
                />
              );
            })}
          </g>
        </svg>
      </div>

      <ul className="sp-donut-chart__legend">
        {data.map((item) => (
          <li key={item.priority}>
            <span
              className="sp-chart-legend__dot"
              style={{ background: COLOR_BY_PRIORITY[item.priority] }}
            />
            <span className="sp-donut-chart__legend-label">{item.priority}</span>
            <span className="sp-donut-chart__legend-value">{item.percentage}%</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

