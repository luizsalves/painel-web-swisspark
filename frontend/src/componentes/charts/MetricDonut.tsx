interface MetricDonutProps {
  label: string;
  value: number;
  total: number;
  color?: string;
}

const RADIUS = 60;
const STROKE = 16;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

function arredondar(percentual: number) {
  return Math.round(percentual * 10) / 10;
}

export default function MetricDonut({ label, value, total, color = "var(--color-green-dark)" }: MetricDonutProps) {
  const percentual = total > 0 ? arredondar((value / total) * 100) : 0;
  const restante = arredondar(Math.max(0, 100 - percentual));
  const dash = (percentual / 100) * CIRCUMFERENCE;

  return (
    <div className="sp-metric-donut">
      <svg viewBox="0 0 160 160" width="140" height="140">
        <g transform="translate(80,80) rotate(-90)">
          <circle r={RADIUS} cx={0} cy={0} fill="none" stroke="var(--color-border)" strokeWidth={STROKE} />
          <circle
            r={RADIUS}
            cx={0}
            cy={0}
            fill="none"
            stroke={color}
            strokeWidth={STROKE}
            strokeDasharray={`${dash} ${CIRCUMFERENCE - dash}`}
            strokeLinecap="round"
          />
        </g>
      </svg>

      <ul className="sp-donut-chart__legend">
        <li>
          <span className="sp-chart-legend__dot" style={{ background: color }} />
          <span className="sp-donut-chart__legend-label">{label}</span>
          <span className="sp-donut-chart__legend-value">{percentual}%</span>
        </li>
        <li>
          <span className="sp-chart-legend__dot" style={{ background: "var(--color-border)" }} />
          <span className="sp-donut-chart__legend-label">Backlog</span>
          <span className="sp-donut-chart__legend-value">{restante}%</span>
        </li>
      </ul>
    </div>
  );
}
