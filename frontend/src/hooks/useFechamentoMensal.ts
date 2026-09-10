import { useQuery } from "@tanstack/react-query";
import { getFechamento, type FechamentoFiltro } from "../services/fechamentosService";

/**
 * Busca o fechamento mensal (saldo, composição, lançamentos e pendências)
 * para a empresa/conta/período selecionados na tela de Fechamento Mensal.
 * Fica pausado (enabled: false) até o usuário ter os três filtros preenchidos.
 */
export function useFechamentoMensal(filtro: FechamentoFiltro | null) {
  return useQuery({
    queryKey: ["fechamento", filtro?.empresaId, filtro?.contaContabilId, filtro?.periodo],
    queryFn: () => getFechamento(filtro!),
    enabled: filtro !== null,
  });
}
