
type BadgeTone = "red" | "orange" | "green" | "green-light" | "neutral";

const TONE_BY_LABEL: Record<string, BadgeTone> = {
  Alta: "red",
  Média: "orange",
  Regular: "green",
  Baixa: "green-light",
  Fechado: "green",
  Conferido: "green-light",
  "Pré-conferido": "orange",
  Preparado: "neutral",
  Backlog: "neutral",
  Pendência: "red",
};

interface StatusBadgeProps {
  label: string;
  tone?: BadgeTone;
}

export default function StatusBadge({ label, tone }: StatusBadgeProps) {
  const resolvedTone = tone ?? TONE_BY_LABEL[label] ?? "neutral";
  return <span className={`sp-badge sp-badge--${resolvedTone}`}>{label}</span>;
}

