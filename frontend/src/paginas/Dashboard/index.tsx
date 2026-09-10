import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Archive,
  ClipboardList,
  Eye,
  CheckCircle2,
  AlertTriangle,
  CheckCheck,
  ChevronRight,
} from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import KpiCard from "../../componentes/ui/KpiCard";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import StackedBarChart from "../../componentes/charts/StackedBarChart";
import DonutChart from "../../componentes/charts/DonutChart";
import { getDashboardSnapshot, PERIOD_OPTIONS, COMPANY_OPTIONS } from "../../services/mockApi";
import type { TableColumn } from "../../componentes/ui/Table";
import type { DashboardSnapshot, RecentClosing } from "../../interfaces/Dashboard";

const FILTER_DELAY_MIN = 300;
const FILTER_DELAY_MAX = 500;

function simulateNetworkDelay() {
  const range = FILTER_DELAY_MAX - FILTER_DELAY_MIN;
  const ms = FILTER_DELAY_MIN + Math.random() * range;
  return new Promise((resolve) => setTimeout(resolve, ms));
}

const closingColumns: TableColumn<RecentClosing>[] = [
  { key: "company", header: "Empresa", render: (row) => row.company },
  { key: "accountCode", header: "Conta Contábil", render: (row) => row.accountCode },
  { key: "description", header: "Descrição", render: (row) => row.description },
  { key: "period", header: "Período", render: (row) => row.period },
  {
    key: "status",
    header: "Status",
    render: (row) => <StatusBadge label={row.status} />,
  },
  { key: "responsible", header: "Responsável", render: (row) => row.responsible },
  { key: "closedAt", header: "Data Fechamento", render: (row) => row.closedAt },
];

export default function Dashboard() {
  const [period, setPeriod] = useState("01/2026");
  const [company, setCompany] = useState("todas");
  const [snapshot, setSnapshot] = useState<DashboardSnapshot>(() =>
    getDashboardSnapshot("01/2026", "todas")
  );
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    let isActive = true;
    setIsLoading(true);

    simulateNetworkDelay().then(() => {
      if (!isActive) return;
      setSnapshot(getDashboardSnapshot(period, company));
      setIsLoading(false);
    });

    return () => {
      isActive = false;
    };
  }, [period, company]);

  const { metrics, statusByPriority, realizedByPriority, recentPendings, recentClosings } =
    snapshot;

  return (
    <AppLayout>
      <AppHeader
        title="Painel de Conciliação"
        subtitle="Visão geral das conciliações contábeis"
        period={period}
        company={company}
        periodOptions={PERIOD_OPTIONS}
        companyOptions={COMPANY_OPTIONS}
        onPeriodChange={setPeriod}
        onCompanyChange={setCompany}
      />

      <main className="sp-dashboard">
        <section className="sp-dashboard__kpis" aria-label="Indicadores gerais">
          <KpiCard label="Backlog Total" value={metrics.backlogTotal} icon={<Archive size={20} />} isLoading={isLoading} />
          <KpiCard label="Preparados" value={metrics.preparados} icon={<ClipboardList size={20} />} isLoading={isLoading} />
          <KpiCard label="Pré-conferidos" value={metrics.preConferidos} icon={<Eye size={20} />} tone="warning" isLoading={isLoading} />
          <KpiCard label="Conferidos" value={metrics.conferidos} icon={<CheckCircle2 size={20} />} tone="green" isLoading={isLoading} />
          <KpiCard label="Pendências" value={metrics.pendencias} icon={<AlertTriangle size={20} />} tone="red" isLoading={isLoading} />
          <KpiCard label="Fechados" value={metrics.fechados} icon={<CheckCheck size={20} />} tone="green" isLoading={isLoading} />
        </section>

        <section className="sp-dashboard__charts">
          <div className="sp-card sp-card--chart">
            <h2 className="sp-card__title">Status por Prioridade</h2>
            {isLoading ? (
              <div className="sp-card__loading">
                <LoadingSpinner size={26} color="var(--color-green)" />
              </div>
            ) : (
              <StackedBarChart data={statusByPriority} />
            )}
          </div>

          <div className="sp-card sp-card--chart">
            <h2 className="sp-card__title">% Realizado por Prioridade</h2>
            {isLoading ? (
              <div className="sp-card__loading">
                <LoadingSpinner size={26} color="var(--color-green)" />
              </div>
            ) : (
              <DonutChart data={realizedByPriority} />
            )}
          </div>
        </section>

        <section className="sp-card sp-dashboard__pendings">
          <div className="sp-card__header">
            <h2 className="sp-card__title">Pendências Recentes</h2>
            <a className="sp-card__link" href="#">
              Ver todas pendências
              <ChevronRight size={15} />
            </a>
          </div>

          <ul className="sp-pending-list">
            {recentPendings.map((item) => (
              <li key={item.id} className="sp-pending-list__item">
                <div className="sp-pending-list__info">
                  <span className="sp-pending-list__code">{item.accountCode}</span>
                  <span className="sp-pending-list__description">{item.description}</span>
                </div>
                <StatusBadge label={item.priority} />
              </li>
            ))}
          </ul>
        </section>

        <section className="sp-card sp-dashboard__closings">
          <div className="sp-card__header">
            <h2 className="sp-card__title">Últimos Fechamentos</h2>
            <Link className="sp-card__link" to="/fechamentos">
              Ver todos os fechamentos
              <ChevronRight size={15} />
            </Link>
          </div>

          <Table columns={closingColumns} data={recentClosings} getRowId={(row) => row.id} />
        </section>
      </main>
    </AppLayout>
  );
}
