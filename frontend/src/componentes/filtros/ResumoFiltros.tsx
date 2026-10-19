import type { ReactNode } from "react";
import { BookText, Building2, CalendarRange, ListChecks, MapPin } from "lucide-react";
import { useFiltros } from "../../contexts/FiltrosContext";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { descreverMes, descreverPeriodo, descreverStatus } from "../../utils/filtrosPainel";
import type { CampoFiltro } from "../../utils/filtrosPainel";

interface ResumoFiltrosProps {
  /** Filtros que a tela usa — só eles aparecem na faixa. */
  campos: CampoFiltro[];
  /** Filtros que a tela exige com um valor específico ("Todas" não serve). */
  obrigatorios?: CampoFiltro[];
  /** Ações próprias da tela, à direita (ex.: atualizar). */
  acoes?: ReactNode;
}

/** Faixa com os filtros aplicados; clicar em qualquer um abre a gaveta. */
export default function ResumoFiltros({ campos, obrigatorios = [], acoes }: ResumoFiltrosProps) {
  const { filtros, abrir } = useFiltros();
  const abrirGaveta = () => abrir(campos);
  // Vazio = "Todas"; quando a tela exige o filtro, o vazio vira um pedido de escolha.
  const valor = (campo: CampoFiltro, preenchido: string) => {
    if (preenchido) return <strong>{preenchido}</strong>;
    return obrigatorios.includes(campo)
      ? <strong className="sp-filtros-resumo__pending">Selecione</strong>
      : <strong>Todas</strong>;
  };

  return (
    <section className="sp-filtros-resumo" aria-label="Filtros aplicados">
      <div className="sp-filtros-resumo__chips">
        {campos.includes("empresa") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}>
            <Building2 size={14} />
            <span>Empresa</span>
            {valor("empresa", filtros.empresa ? filtros.empresaNome : "")}
            {filtros.empresa && <span className="sp-filtros-resumo__codigo" title="Coligada">{coligadaDaEmpresa(filtros.empresa)}</span>}
          </button>
        )}
        {campos.includes("filial") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}>
            <MapPin size={14} />
            <span>Filial</span>
            {valor("filial", filtros.filial)}
          </button>
        )}
        {campos.includes("conta") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}
            title={filtros.conta && filtros.contaDescricao ? `${filtros.conta} - ${filtros.contaDescricao}` : undefined}>
            <BookText size={14} />
            <span>Conta</span>
            {valor("conta", filtros.conta)}
          </button>
        )}
        {campos.includes("mes") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}>
            <CalendarRange size={14} />
            <span>Mês</span>
            <strong>{descreverMes(filtros)}</strong>
          </button>
        )}
        {campos.includes("periodo") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}>
            <CalendarRange size={14} />
            <span>Período</span>
            <strong>{descreverPeriodo(filtros)}</strong>
          </button>
        )}
        {campos.includes("status") && (
          <button type="button" className="sp-filtros-resumo__chip" onClick={abrirGaveta}>
            <ListChecks size={14} />
            <span>Status</span>
            <strong>{descreverStatus(filtros)}</strong>
          </button>
        )}
      </div>
      {acoes && <div className="sp-filtros-resumo__actions">{acoes}</div>}
    </section>
  );
}
