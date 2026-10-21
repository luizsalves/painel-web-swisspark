import { useCallback } from "react";
import { useQuery } from "@tanstack/react-query";
import api from "../services/api";
import type { EmpresaContabil } from "../interfaces/ContaContabil";
import { chaveEmpresaContabil } from "../utils/empresaContabil";

/**
 * Empresas que fecham com todas as filiais juntas (a lista vem do backend). Devolve,
 * para a empresa dos filtros, se ela consolida as filiais — `undefined` enquanto a
 * lista não chegou. Se ela não vier, cada empresa segue por filial.
 */
export function useEmpresasConsolidadas(): (empresa: string | null) => boolean | undefined {
  const { data, isError } = useQuery({
    queryKey: ["empresas-consolidadas"],
    queryFn: async ({ signal }) => {
      const { data } = await api.get<EmpresaContabil[]>("/empresas-consolidadas", { signal });
      return new Set(data.map(chaveEmpresaContabil));
    },
    staleTime: Infinity,
  });

  return useCallback((empresa: string | null) => {
    if (!empresa || isError) return false;
    return data ? data.has(empresa) : undefined;
  }, [data, isError]);
}
