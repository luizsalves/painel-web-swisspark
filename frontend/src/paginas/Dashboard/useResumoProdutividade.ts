import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useEmpresasConsolidadas } from "../../hooks/useEmpresasConsolidadas";
import { extrairMensagemDeErro } from "../../services/api";
import { obterContasDaEmpresa, obterResumoEmpresas } from "../../services/dashboardService";
import type { DashboardConta, DashboardResumo, StatusFechamentoConta } from "../../interfaces/Dashboard";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { periodoDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro, FiltrosPainel } from "../../utils/filtrosPainel";
import { contaPassaNoFiltro } from "./filtroContas";
import type { FiltroContas } from "./filtroContas";

// Os mesmos filtros do Fechamento, mais o status — mas por período (ano inteiro,
// semestre ou meses escolhidos), não por um mês só.
export const CAMPOS_FILTRO: CampoFiltro[] = ["empresa", "filial", "conta", "periodo", "status"];
// Nas empresas de filiais consolidadas as contas vêm na filial 0: a filial não recorta nada.
export const CAMPOS_FILTRO_CONSOLIDADO = CAMPOS_FILTRO.filter((campo) => campo !== "filial");

export const RESUMO_VAZIO: DashboardResumo = {
  backlogTotal: 0,
  preparados: 0,
  conferidos: 0,
  fechados: 0,
  pendentes: 0,
};

const CAMPO_DO_STATUS: Record<StatusFechamentoConta, Exclude<keyof DashboardResumo, "backlogTotal">> = {
  Pendente: "pendentes",
  Preparado: "preparados",
  Conferido: "conferidos",
  Fechado: "fechados",
};

/** Resumo só com as contas do status: as demais etapas zeram e o backlog vira o total dele. */
function somenteStatus(resumo: DashboardResumo, status?: StatusFechamentoConta): DashboardResumo {
  if (!status) return resumo;
  const campo = CAMPO_DO_STATUS[status];
  return { ...RESUMO_VAZIO, backlogTotal: resumo[campo], [campo]: resumo[campo] };
}

function contarPorStatus(contas: DashboardConta[]): DashboardResumo {
  const resumo = { ...RESUMO_VAZIO };
  for (const conta of contas) {
    resumo.backlogTotal++;
    resumo[CAMPO_DO_STATUS[conta.status]]++;
  }
  return resumo;
}

/**
 * O backlog do fechamento pelos filtros da gaveta (empresa, filial, conta, período e
 * status), por empresa e no total. A Produtividade e a CFO Tati contam por aqui, para
 * os números das duas telas baterem. `recarregar` muda para buscar tudo de novo.
 */
export function useResumoProdutividade(filtros: FiltrosPainel, recarregar = 0) {
  const { inicio, fim } = periodoDosFiltros(filtros);
  // O resumo vem agrupado pelo nome da empresa (GFILIAL.NOME), o mesmo da gaveta.
  const empresa = filtros.empresa ? filtros.empresaNome.trim() : "";
  // Filial e conta só existem dentro de uma empresa (a gaveta pede a empresa antes).
  const consolidaFiliais = useEmpresasConsolidadas();
  const consolidada = Boolean(consolidaFiliais(filtros.empresa));
  const comFilial = Boolean(filtros.empresa && filtros.filial && !consolidada);
  const filtroContas = useMemo<FiltroContas>(() => ({
    codColigada: comFilial ? coligadaDaEmpresa(filtros.empresa ?? "") : undefined,
    codFilial: comFilial ? Number(filtros.filial) : undefined,
    codConta: filtros.empresa ? filtros.conta || undefined : undefined,
    status: filtros.status || undefined,
  }), [comFilial, filtros.empresa, filtros.filial, filtros.conta, filtros.status]);
  // O resumo por empresa não abre filial nem conta: com esses filtros, a empresa
  // escolhida é contada a partir das contas dela.
  const recorteDeContas = filtroContas.codFilial !== undefined || Boolean(filtroContas.codConta);

  const resumoEmpresas = useQuery({
    queryKey: ["dashboard-empresas", inicio, fim, recarregar],
    queryFn: ({ signal }) => obterResumoEmpresas(inicio, fim, signal),
  });
  const contasDaEmpresa = useQuery({
    queryKey: ["dashboard-contas", inicio, fim, empresa, recarregar],
    queryFn: ({ signal }) => obterContasDaEmpresa(inicio, fim, empresa, signal),
    enabled: recorteDeContas,
  });
  const carregando = resumoEmpresas.isPending || (recorteDeContas && contasDaEmpresa.isPending);
  const erro = resumoEmpresas.error
    ? extrairMensagemDeErro(resumoEmpresas.error, "Falha ao carregar o dashboard.")
    : recorteDeContas && contasDaEmpresa.error
      ? extrairMensagemDeErro(contasDaEmpresa.error, "Falha ao carregar as contas da empresa.") : null;

  const porEmpresa = resumoEmpresas.data;
  const selecionadas = useMemo(() => Object.keys(porEmpresa ?? {})
    .sort((a, b) => a.localeCompare(b, "pt-BR"))
    .filter((nome) => !empresa || nome.trim() === empresa)
    .map((nome) => ({
      nome,
      ...(recorteDeContas
        ? contarPorStatus((contasDaEmpresa.data ?? []).filter((conta) => contaPassaNoFiltro(conta, filtroContas)))
        : somenteStatus(porEmpresa![nome], filtroContas.status)),
    })), [empresa, porEmpresa, recorteDeContas, contasDaEmpresa.data, filtroContas]);
  const resumo = useMemo(() => selecionadas.reduce((total, item) => ({
    backlogTotal: total.backlogTotal + item.backlogTotal,
    preparados: total.preparados + item.preparados,
    conferidos: total.conferidos + item.conferidos,
    fechados: total.fechados + item.fechados,
    pendentes: total.pendentes + item.pendentes,
  }), RESUMO_VAZIO), [selecionadas]);

  return {
    inicio,
    fim,
    empresa,
    consolidada,
    filtroContas,
    selecionadas,
    resumo,
    carregando,
    erro,
    atualizadoEm: resumoEmpresas.data ? new Date(resumoEmpresas.dataUpdatedAt) : null,
  };
}
