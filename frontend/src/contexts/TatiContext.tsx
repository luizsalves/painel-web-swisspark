import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";

/** Uma pergunta pronta que um botão pediu e o ícone da Tati ainda não mandou. */
export interface PedidoDeAnalise {
  id: number;
  pergunta: string;
  /** O resumo do mês do botão do Dashboard: vai pela rota do resumo, que escolhe as ferramentas. */
  resumo?: boolean;
}

interface TatiContextValue {
  pedido: PedidoDeAnalise | null;
  /** Abre a conversa do ícone da Tati e manda a pergunta, com a tela e os filtros abertos. */
  analisar: (pergunta: string) => void;
  /** Abre a conversa do ícone da Tati e pede o resumo ("Resumo do mês" ou "do período") dos filtros da tela. */
  resumirMes: (pergunta: string) => void;
  /** O ícone mandou a pergunta: o pedido sai da fila. */
  atender: (id: number) => void;
}

const TatiContext = createContext<TatiContextValue | undefined>(undefined);

/**
 * Liga os botões "Analisar" dos cards e o "Resumo do mês" do Dashboard ao ícone da Tati, que fica
 * fora das rotas. Só guarda o último pedido: um clique novo antes de a Tati responder troca a
 * pergunta que estava na fila.
 */
export function TatiProvider({ children }: { children: ReactNode }) {
  const [pedido, setPedido] = useState<PedidoDeAnalise | null>(null);
  const proximoId = useRef(1);

  const analisar = useCallback((pergunta: string) => {
    setPedido({ id: proximoId.current++, pergunta });
  }, []);
  const resumirMes = useCallback((pergunta: string) => {
    setPedido({ id: proximoId.current++, pergunta, resumo: true });
  }, []);
  const atender = useCallback((id: number) => {
    setPedido((atual) => (atual?.id === id ? null : atual));
  }, []);

  const valor = useMemo(() => ({ pedido, analisar, resumirMes, atender }), [pedido, analisar, resumirMes, atender]);

  return <TatiContext.Provider value={valor}>{children}</TatiContext.Provider>;
}

export function useTati(): TatiContextValue {
  const contexto = useContext(TatiContext);
  if (!contexto) {
    throw new Error("useTati deve ser usado dentro de um TatiProvider");
  }
  return contexto;
}
