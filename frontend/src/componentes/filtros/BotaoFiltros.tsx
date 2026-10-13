import { SlidersHorizontal } from "lucide-react";
import { useFiltros } from "../../contexts/FiltrosContext";
import { contarFiltrosAtivos } from "../../utils/filtrosPainel";
import type { CampoFiltro } from "../../utils/filtrosPainel";

/** Botão que abre a gaveta de filtros, com quantos filtros da tela estão ativos. */
export default function BotaoFiltros({ campos }: { campos: CampoFiltro[] }) {
  const { filtros, abrir } = useFiltros();
  const ativos = contarFiltrosAtivos(filtros, campos);

  return (
    <button type="button" className="sp-header__filters-btn" onClick={() => abrir(campos)}
      aria-label={ativos > 0 ? `Filtros (${ativos} ativos)` : "Filtros"}>
      <SlidersHorizontal size={17} />
      <span>Filtros</span>
      {ativos > 0 && <span className="sp-header__filters-count">{ativos}</span>}
    </button>
  );
}
