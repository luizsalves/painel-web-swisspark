/**
 * A tela aberta, para o ícone da Tati: o nome do menu, a aba e os filtros que a tela usa. A aba
 * vem do endereço (?aba= no Balancete, ?visao= nos Indicadores), então as telas não mudam.
 */
import type { PerguntarCfoTatiNaTelaPayload } from "../../interfaces/CfoTati";
import { CAMPOS_FILTRO_BALANCO, CAMPOS_FILTRO_DFC, CAMPOS_FILTRO_RESUMO } from "../../paginas/Balancete/camposFiltro";
import { VISOES, lerVisao } from "../../paginas/Indicadores/visoes";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { descreverMes, descreverPeriodo, descreverStatus, periodoDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro, FiltrosPainel } from "../../utils/filtrosPainel";

export interface TelaDaTati {
  tela: string;
  aba?: string;
  /** Os filtros da gaveta da tela: a Tati recebe só o que a tela mostra. */
  campos: CampoFiltro[];
}

// Os rótulos das abas e os filtros são os de cada tela (os da Produtividade, os mesmos de
// useResumoProdutividade, repetidos aqui para este arquivo não carregar a API).
const TELAS: Record<string, (busca: URLSearchParams) => TelaDaTati> = {
  "/dashboard": () => ({ tela: "Produtividade", campos: ["empresa", "filial", "conta", "periodo", "status"] }),
  "/fechamentos": () => ({ tela: "Fechamentos", campos: ["empresa", "filial", "conta", "mes", "status"] }),
  "/balancete": (busca) => {
    const aba = busca.get("aba");
    if (aba === "balanco") return { tela: "Balancete", aba: "Balanço", campos: CAMPOS_FILTRO_BALANCO };
    if (aba === "dfc") return { tela: "Balancete", aba: "Balanço DFC", campos: CAMPOS_FILTRO_DFC };
    return { tela: "Balancete", aba: "Balancete Resumo", campos: CAMPOS_FILTRO_RESUMO };
  },
  "/indicadores": (busca) => {
    const visao = lerVisao(busca.get("visao"));
    return { tela: "Indicadores", aba: VISOES.find((item) => item.visao === visao)?.rotulo, campos: ["empresa", "filial", "periodo"] };
  },
  "/empresas": () => ({ tela: "Empresas", campos: ["empresa"] }),
  "/contas": () => ({ tela: "Contas Contábeis", campos: ["empresa", "filial", "conta"] }),
  "/usuarios": () => ({ tela: "Usuários", campos: [] }),
};

/** A tela do endereço; null onde o ícone não aparece (login, troca de senha, a própria tela da Tati). */
export function telaDaTati(caminho: string, busca: string): TelaDaTati | null {
  const tela = TELAS[caminho.replace(/\/+$/, "")];
  return tela ? tela(new URLSearchParams(busca)) : null;
}

/**
 * Os filtros da tela no formato do chat, como a tela da Tati manda: período (ou o mês, nas telas
 * de um mês só), empresa e coligada, e filial, conta e status só onde a tela usa. Nas empresas
 * de filiais consolidadas a filial não recorta nada.
 */
export function pedidoDaTela(
  tela: TelaDaTati, filtros: FiltrosPainel, consolidada: boolean,
): Omit<PerguntarCfoTatiNaTelaPayload, "pergunta" | "historico"> {
  const usa = (campo: CampoFiltro) => tela.campos.includes(campo);
  const { inicio, fim } = periodoDosFiltros(filtros);
  const empresa = usa("empresa") ? filtros.empresa : null;
  return {
    tela: tela.tela,
    aba: tela.aba,
    anoMes: fim,
    anoMesInicial: usa("periodo") && inicio !== fim ? inicio : undefined,
    empresa: empresa ? filtros.empresaNome.trim() || undefined : undefined,
    codColigada: empresa ? coligadaDaEmpresa(empresa) : undefined,
    codFilial: empresa && usa("filial") && filtros.filial && !consolidada ? Number(filtros.filial) : undefined,
    codConta: empresa && usa("conta") ? filtros.conta || undefined : undefined,
    status: usa("status") ? filtros.status || undefined : undefined,
  };
}

/** "Balancete · Balanço · EMPRESA · Jan a Set de 2026": o que a Tati está vendo, no topo do painel. */
export function descreverContexto(tela: TelaDaTati, filtros: FiltrosPainel, consolidada: boolean): string {
  const usa = (campo: CampoFiltro) => tela.campos.includes(campo);
  const comEmpresa = usa("empresa") && Boolean(filtros.empresa);
  return [
    tela.tela,
    tela.aba,
    usa("empresa") && (comEmpresa ? filtros.empresaNome.trim() : "Todas as empresas"),
    comEmpresa && usa("filial") && filtros.filial && !consolidada && `filial ${filtros.filial}`,
    comEmpresa && usa("conta") && filtros.conta && `conta ${filtros.conta}`,
    usa("periodo") ? descreverPeriodo(filtros) : usa("mes") ? descreverMes(filtros) : null,
    usa("status") && filtros.status && descreverStatus(filtros),
  ].filter(Boolean).join(" · ");
}
