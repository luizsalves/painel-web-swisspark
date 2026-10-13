import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { lerFiltrosSalvos, salvarFiltros } from "../utils/filtrosPainel";
import type { CampoFiltro, FiltrosPainel } from "../utils/filtrosPainel";

interface FiltrosContextValue {
  /** Filtros aplicados — só mudam quando a pessoa confirma na gaveta. */
  filtros: FiltrosPainel;
  aplicar: (filtros: FiltrosPainel) => void;
  aberto: boolean;
  /** Filtros que a tela aberta usa; a gaveta avisa sobre os demais. */
  camposDaTela: CampoFiltro[];
  abrir: (campos: CampoFiltro[]) => void;
  fechar: () => void;
}

const FiltrosContext = createContext<FiltrosContextValue | undefined>(undefined);

export function FiltrosProvider({ children }: { children: ReactNode }) {
  const [filtros, setFiltros] = useState(lerFiltrosSalvos);
  const [aberto, setAberto] = useState(false);
  const [camposDaTela, setCamposDaTela] = useState<CampoFiltro[]>([]);

  const aplicar = useCallback((novos: FiltrosPainel) => {
    setFiltros(novos);
    salvarFiltros(novos);
  }, []);
  const abrir = useCallback((campos: CampoFiltro[]) => {
    setCamposDaTela(campos);
    setAberto(true);
  }, []);
  const fechar = useCallback(() => setAberto(false), []);

  const valor = useMemo(
    () => ({ filtros, aplicar, aberto, camposDaTela, abrir, fechar }),
    [filtros, aplicar, aberto, camposDaTela, abrir, fechar]
  );

  return <FiltrosContext.Provider value={valor}>{children}</FiltrosContext.Provider>;
}

export function useFiltros(): FiltrosContextValue {
  const contexto = useContext(FiltrosContext);
  if (!contexto) {
    throw new Error("useFiltros deve ser usado dentro de um FiltrosProvider");
  }
  return contexto;
}
