import { useState } from "react";
import { Archive, ClipboardList, CheckCircle2, AlertTriangle, CheckCheck, RefreshCw } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import KpiCard from "../../componentes/ui/KpiCard";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Button from "../../componentes/ui/Button";
import MetricDonut from "../../componentes/charts/MetricDonut";
import TabelaEmpresas from "./TabelaEmpresas";
import type { ColunaEmpresa } from "./TabelaEmpresas";
import { ETAPAS } from "./ContasDaEmpresa";
import { CAMPOS_FILTRO, CAMPOS_FILTRO_CONSOLIDADO, useResumoProdutividade } from "./useResumoProdutividade";
import { useFiltros } from "../../contexts/FiltrosContext";
import type { StatusFechamentoConta } from "../../interfaces/Dashboard";

// O "+" de cada lista abre só as contas que ainda faltam naquela etapa.
const ETAPAS_A_PREPARAR: StatusFechamentoConta[] = ["Pendente"];
const ETAPAS_A_CONFERIR: StatusFechamentoConta[] = ["Pendente", "Preparado"];

const COLUNAS_A_PREPARAR: ColunaEmpresa[] = [{ titulo: "Faltam", valor: (item) => item.pendentes }];
const COLUNAS_A_CONFERIR: ColunaEmpresa[] = [{ titulo: "Faltam", valor: (item) => item.pendentes + item.preparados }];
const COLUNAS_DO_STATUS: ColunaEmpresa[] = [{ titulo: "Contas", valor: (item) => item.backlogTotal }];

export default function Dashboard() {
  const { filtros } = useFiltros();
  const [recarregar, setRecarregar] = useState(0);
  const {
    inicio, fim, empresa, consolidada, filtroContas, selecionadas, resumo, carregando, erro: erroDaTela, atualizadoEm,
  } = useResumoProdutividade(filtros, recarregar);
  // Com mais de um mês, cada conta entra uma vez por mês: a unidade é o fechamento.
  const variosMeses = inicio !== fim;
  const empresasComContas = selecionadas.filter((item) => item.backlogTotal > 0);
  const restaPreparar = empresasComContas.filter((item) => item.pendentes > 0);
  const aConferir = empresasComContas.filter((item) => item.pendentes + item.preparados > 0);
  // Com uma empresa no filtro, contar empresas não diz nada (é sempre 1): a meta
  // e o andamento passam a ser em contas. Conferida e finalizada já passaram pela preparação.
  const porConta = Boolean(empresa);
  const unidade = porConta ? (variosMeses ? "fechamentos" : "contas") : "empresas";
  const unidadeKpi = variosMeses ? "Fechamentos (conta × mês)" : "Contas";
  const meta = porConta ? resumo.backlogTotal : empresasComContas.length;
  const preparadas = porConta ? resumo.preparados + resumo.conferidos + resumo.fechados : meta - restaPreparar.length;
  const conferidas = porConta ? resumo.conferidos + resumo.fechados : meta - aConferir.length;
  const faltamPreparar = porConta ? resumo.pendentes : restaPreparar.length;
  const faltamConferir = porConta ? resumo.pendentes + resumo.preparados : aConferir.length;
  // Com um status no filtro, as duas listas de etapa dão lugar a uma só, com as
  // empresas que têm contas nele — senão contas conferidas e finalizadas não apareceriam.
  const etapaFiltrada = ETAPAS.find((etapa) => etapa.id === filtroContas.status);
  const listas = etapaFiltrada
    ? [{ titulo: `Contas ${etapaFiltrada.plural.toLowerCase()}`, lista: empresasComContas, faltam: resumo.backlogTotal,
        colunas: COLUNAS_DO_STATUS, etapas: [etapaFiltrada.id], vazio: "Nenhuma conta com este status no filtro." }]
    : [
        { titulo: "Resta preparar", lista: restaPreparar, faltam: faltamPreparar, colunas: COLUNAS_A_PREPARAR, etapas: ETAPAS_A_PREPARAR,
          vazio: meta === 0 ? "Nenhuma empresa com contas neste filtro." : "Nenhuma empresa pendente nesta etapa." },
        { titulo: "A conferir", lista: aConferir, faltam: faltamConferir, colunas: COLUNAS_A_CONFERIR, etapas: ETAPAS_A_CONFERIR,
          vazio: meta === 0 ? "Nenhuma empresa com contas neste filtro." : "Nenhuma empresa pendente nesta etapa." },
      ];

  return (
    <AppLayout>
      <AppHeader
        title="Produtividade e Eficiência"
        subtitle="Backlog de fechamentos por período, cruzando empresas e contas contábeis ativas"
        camposFiltro={CAMPOS_FILTRO}
      />

      <main className="sp-dashboard">
        <ResumoFiltros campos={consolidada ? CAMPOS_FILTRO_CONSOLIDADO : CAMPOS_FILTRO} />

        {erroDaTela && (
          <span className="sp-closing__feedback sp-closing__feedback--error" role="alert">
            {erroDaTela}
          </span>
        )}

        <section className="sp-dashboard__kpis" aria-label="Indicadores gerais">
          <KpiCard label="Backlog Total" unit={unidadeKpi} value={resumo.backlogTotal} icon={<Archive size={20} />} isLoading={carregando} />
          <KpiCard label="Preparados" unit={unidadeKpi} value={resumo.preparados} icon={<ClipboardList size={20} />} isLoading={carregando} />
          <KpiCard label="Conferidos" unit={unidadeKpi} value={resumo.conferidos} icon={<CheckCircle2 size={20} />} tone="green" isLoading={carregando} />
          <KpiCard label="Pendentes" unit={unidadeKpi} value={resumo.pendentes} icon={<AlertTriangle size={20} />} tone="red" isLoading={carregando} />
          <KpiCard label="Finalizados" unit={unidadeKpi} value={resumo.fechados} icon={<CheckCheck size={20} />} tone="green" isLoading={carregando} />
        </section>

        <section className="sp-dashboard__companies" aria-label="Andamento por empresa" aria-busy={carregando}>
          <div className="sp-dashboard__companies-heading">
            <div><h2>Conciliação por empresa</h2><p>Todas as contas e filiais ativas devem concluir a etapa. Empresas sem contas ficam fora da meta.</p></div>
            <span>Atualizado em: {atualizadoEm ? atualizadoEm.toLocaleString("pt-BR") : "—"}</span>
            <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={carregando} onClick={() => setRecarregar((valor) => valor + 1)}>Atualizar</Button>
          </div>
          {carregando ? <div className="sp-card__loading"><LoadingSpinner size={26} /></div> : erroDaTela ?
            <p role="status">Não foi possível carregar a visão por empresa.</p> : (
            <div className="sp-dashboard__company-grid">
              <aside className="sp-dashboard__company-metrics">
                <div className="sp-dashboard__company-goal"><span>Meta de {unidade}</span><strong>{meta}</strong></div>
                <div className="sp-card"><h3>{preparadas} preparadas</h3><MetricDonut label="Preparadas" value={preparadas} total={meta} /></div>
                <div className="sp-card"><h3>{conferidas} conferidas</h3><MetricDonut label="Conferidas" value={conferidas} total={meta} /></div>
              </aside>
              {listas.map(({ titulo, lista, faltam, colunas, etapas, vazio }) => (
                <section className={etapaFiltrada ? "sp-dashboard__company-list sp-dashboard__company-list--unica" : "sp-dashboard__company-list"}
                  key={titulo} aria-label={titulo}>
                  <header><h3>{titulo}</h3><strong>{faltam}</strong></header>
                  <TabelaEmpresas empresas={lista} colunas={colunas} etapas={etapas} filtro={filtroContas} inicio={inicio} fim={fim} versao={recarregar}
                    vazio={vazio} />
                </section>
              ))}
            </div>
          )}
        </section>
      </main>
    </AppLayout>
  );
}
