import { ROTULO_STATUS } from "../regras";
import type { Severidade, Status } from "../regras";

type Tom = Status | Severidade;

const ROTULO: Record<Tom, string> = { ...ROTULO_STATUS, info: "Info" };

interface StatusPillProps {
  status: Tom;
  /** "grande" é a do veredito; "fixa" tem largura fixa, para alinhar a lista de alertas. */
  variante?: "normal" | "grande" | "fixa";
}

/** O status sempre com texto, nunca só pela cor. */
export default function StatusPill({ status, variante = "normal" }: StatusPillProps) {
  const tom = status === "info" ? "verificar" : status;
  return (
    <span className={`sp-ind-pill sp-ind-pill--${tom}${variante !== "normal" ? ` sp-ind-pill--${variante}` : ""}`}>
      {ROTULO[status]}
    </span>
  );
}
