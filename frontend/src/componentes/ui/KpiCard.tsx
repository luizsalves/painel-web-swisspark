import type { ReactNode } from "react";

type KpiTone = "neutral" | "warning" | "green" | "red";

interface KpiCardProps {
  label: string;
  value: number;
  unit?: string;
  icon: ReactNode;
  tone?: KpiTone;
  isLoading?: boolean;
}

export default function KpiCard({
  label,
  value,
  unit = "Contas",
  icon,
  tone = "neutral",
  isLoading = false,
}: KpiCardProps) {
  return (
    <div className={`sp-kpi sp-kpi--${tone}`}>
      <div className="sp-kpi__icon">{icon}</div>
      <div className="sp-kpi__content">
        <span className="sp-kpi__value">{isLoading ? "—" : value}</span>
        <span className="sp-kpi__label">{label}</span>
        <span className="sp-kpi__unit">{unit}</span>
      </div>
    </div>
  );
}

