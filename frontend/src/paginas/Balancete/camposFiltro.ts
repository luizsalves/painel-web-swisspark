import type { CampoFiltro } from "../../utils/filtrosPainel";

export const CAMPOS_FILTRO_RESUMO: CampoFiltro[] = ["empresa", "filial", "conta", "mes"];

// A DFC precisa de todas as contas, então o filtro de conta não se aplica a ela.
export const CAMPOS_FILTRO_DFC: CampoFiltro[] = ["empresa", "filial", "periodo"];

// Os mesmos filtros da DFC: o balanço é a posição no fim do período.
export const CAMPOS_FILTRO_BALANCO: CampoFiltro[] = CAMPOS_FILTRO_DFC;
