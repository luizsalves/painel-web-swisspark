import { useEffect, useMemo, useState } from "react";
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
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { obterResumoEmpresas } from "../../services/dashboardService";
import type { DashboardResumo, StatusFechamentoConta } from "../../interfaces/Dashboard";
import { mesDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro } from "../../utils/filtrosPainel";

const CAMPOS_FILTRO: CampoFiltro[] = ["empresa", "mes"];

// O "+" de cada lista abre só as contas que ainda faltam naquela etapa.
const ETAPAS_A_PREPARAR: StatusFechamentoConta[] = ["Pendente"];
const ETAPAS_A_CONFERIR: StatusFechamentoConta[] = ["Pendente", "Preparado"];

const COLUNAS_A_PREPARAR: ColunaEmpresa[] = [{ titulo: "Faltam", valor: (item) => item.pendentes }];
const COLUNAS_A_CONFERIR: ColunaEmpresa[] = [{ titulo: "Faltam", valor: (item) => item.pendentes + item.preparados }];

const RESUMO_VAZIO: DashboardResumo = {
  backlogTotal: 0,
  preparados: 0,
  conferidos: 0,
  fechados: 0,
  pendentes: 0,
};

export default function Dashboard() {
  const { filtros } = useFiltros();
  const anoMes = mesDosFiltros(filtros);
  // O resumo vem agrupado pelo nome da empresa (GFILIAL.NOME), o mesmo da gaveta.
  const empresa = filtros.empresa ? filtros.empresaNome.trim() : "";
  const [porEmpresa, setPorEmpresa] = useState<Record<string, DashboardResumo>>({});
  const [atualizadoEm, setAtualizadoEm] = useState<Date | null>(null);
  const [recarregar, setRecarregar] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setIsLoading(true);
    setErro(null);
    setPorEmpresa({});
    setAtualizadoEm(null);
    obterResumoEmpresas(anoMes, controller.signal)
      .then((dados) => {
        if (controller.signal.aborted) return;
        setPorEmpresa(dados);
        setAtualizadoEm(new Date());
      })
      .catch((falha) => {
        if (!controller.signal.aborted) setErro(extrairMensagemDeErro(falha, "Falha ao carregar o dashboard."));
      })
      .finally(() => { if (!controller.signal.aborted) setIsLoading(false); });
    return () => controller.abort();
  }, [anoMes, recarregar]);

  const nomesDeEmpresas = useMemo(() => Object.keys(porEmpresa).sort((a, b) => a.localeCompare(b, "pt-BR")), [porEmpresa]);
  const selecionadas = useMemo(() => nomesDeEmpresas
    .filter((nome) => !empresa || nome.trim() === empresa)
    .map((nome) => ({ nome, ...porEmpresa[nome] })), [nomesDeEmpresas, empresa, porEmpresa]);
  const resumo = useMemo(() => selecionadas.reduce((total, item) => ({
    backlogTotal: total.backlogTotal + item.backlogTotal,
    preparados: total.preparados + item.preparados,
    conferidos: total.conferidos + item.conferidos,
    fechados: total.fechados + item.fechados,
    pendentes: total.pendentes + item.pendentes,
  }), RESUMO_VAZIO), [selecionadas]);
  const empresasComContas = selecionadas.filter((item) => item.backlogTotal > 0);
  const restaPreparar = empresasComContas.filter((item) => item.pendentes > 0);
  const aConferir = empresasComContas.filter((item) => item.pendentes + item.preparados > 0);
  // Com uma empresa no filtro, contar empresas não diz nada (é sempre 1): a meta
  // e o andamento passam a ser em contas. Conferida e finalizada já passaram pela preparação.
  const porConta = Boolean(empresa);
  const unidade = porConta ? "contas" : "empresas";
  const meta = porConta ? resumo.backlogTotal : empresasComContas.length;
  const preparadas = porConta ? resumo.preparados + resumo.conferidos + resumo.fechados : meta - restaPreparar.length;
  const conferidas = porConta ? resumo.conferidos + resumo.fechados : meta - aConferir.length;
  const faltamPreparar = porConta ? resumo.pendentes : restaPreparar.length;
  const faltamConferir = porConta ? resumo.pendentes + resumo.preparados : aConferir.length;

  return (
    <AppLayout>
      <AppHeader
        title="Produtividade e Eficiência"
        subtitle="Backlog de fechamentos por período, cruzando empresas e contas contábeis ativas"
        camposFiltro={CAMPOS_FILTRO}
      />

      <main className="sp-dashboard">
        <ResumoFiltros campos={CAMPOS_FILTRO} />

        {erro && (
          <span className="sp-closing__feedback sp-closing__feedback--error" role="alert">
            {erro}
          </span>
        )}

        <section className="sp-dashboard__kpis" aria-label="Indicadores gerais">
          <KpiCard label="Backlog Total" value={resumo.backlogTotal} icon={<Archive size={20} />} isLoading={isLoading} />
          <KpiCard label="Preparados" value={resumo.preparados} icon={<ClipboardList size={20} />} isLoading={isLoading} />
          <KpiCard label="Conferidos" value={resumo.conferidos} icon={<CheckCircle2 size={20} />} tone="green" isLoading={isLoading} />
          <KpiCard label="Pendentes" value={resumo.pendentes} icon={<AlertTriangle size={20} />} tone="red" isLoading={isLoading} />
          <KpiCard label="Finalizados" value={resumo.fechados} icon={<CheckCheck size={20} />} tone="green" isLoading={isLoading} />
        </section>

        <section className="sp-dashboard__companies" aria-label="Andamento por empresa" aria-busy={isLoading}>
          <div className="sp-dashboard__companies-heading">
            <div><h2>Conciliação por empresa</h2><p>Todas as contas e filiais ativas devem concluir a etapa. Empresas sem contas ficam fora da meta.</p></div>
            <span>Atualizado em: {atualizadoEm ? atualizadoEm.toLocaleString("pt-BR") : "—"}</span>
            <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isLoading} onClick={() => setRecarregar((valor) => valor + 1)}>Atualizar</Button>
          </div>
          {isLoading ? <div className="sp-card__loading"><LoadingSpinner size={26} /></div> : erro ?
            <p role="status">Não foi possível carregar a visão por empresa.</p> : (
            <div className="sp-dashboard__company-grid">
              <aside className="sp-dashboard__company-metrics">
                <div className="sp-dashboard__company-goal"><span>Meta de {unidade}</span><strong>{meta}</strong></div>
                <div className="sp-card"><h3>{preparadas} preparadas</h3><MetricDonut label="Preparadas" value={preparadas} total={meta} /></div>
                <div className="sp-card"><h3>{conferidas} conferidas</h3><MetricDonut label="Conferidas" value={conferidas} total={meta} /></div>
              </aside>
              {[
                { titulo: "Resta preparar", lista: restaPreparar, faltam: faltamPreparar, colunas: COLUNAS_A_PREPARAR, etapas: ETAPAS_A_PREPARAR },
                { titulo: "A conferir", lista: aConferir, faltam: faltamConferir, colunas: COLUNAS_A_CONFERIR, etapas: ETAPAS_A_CONFERIR },
              ].map(({ titulo, lista, faltam, colunas, etapas }) => (
                <section className="sp-dashboard__company-list" key={titulo} aria-label={titulo}>
                  <header><h3>{titulo}</h3><strong>{faltam}</strong></header>
                  <TabelaEmpresas empresas={lista} colunas={colunas} etapas={etapas} anoMes={anoMes} versao={recarregar}
                    vazio={meta === 0 ? "Nenhuma empresa com contas neste filtro." : "Nenhuma empresa pendente nesta etapa."} />
                </section>
              ))}
            </div>
          )}
        </section>
      </main>
    </AppLayout>
  );
}
