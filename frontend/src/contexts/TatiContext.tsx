import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";

/** Uma pergunta pronta que um botão "Analisar" pediu e o ícone da Tati ainda não mandou. */
export interface PedidoDeAnalise {
  id: number;
  pergunta: string;
}

interface TatiContextValue {
  pedido: PedidoDeAnalise | null;
  /** Abre a conversa do ícone da Tati e manda a pergunta, com a tela e os filtros abertos. */
  analisar: (pergunta: string) => void;
  /** O ícone mandou a pergunta: o pedido sai da fila. */
  atender: (id: number) => void;
}

const TatiContext = createContext<TatiContextValue | undefined>(undefined);

/**
 * Liga os botões "Analisar" dos cards ao ícone da Tati, que fica fora das rotas. Só guarda o
 * último pedido: um clique novo antes de a Tati responder troca a pergunta que estava na fila.
 */
export function TatiProvider({ children }: { children: ReactNode }) {
  const [pedido, setPedido] = useState<PedidoDeAnalise | null>(null);
  const proximoId = useRef(1);

  const analisar = useCallback((pergunta: string) => {
    setPedido({ id: proximoId.current++, pergunta });
  }, []);
  const atender = useCallback((id: number) => {
    setPedido((atual) => (atual?.id === id ? null : atual));
  }, []);

  const valor = useMemo(() => ({ pedido, analisar, atender }), [pedido, analisar, atender]);

  return <TatiContext.Provider value={valor}>{children}</TatiContext.Provider>;
}

export function useTati(): TatiContextValue {
  const contexto = useContext(TatiContext);
  if (!contexto) {
    throw new Error("useTati deve ser usado dentro de um TatiProvider");
  }
  return contexto;
}
