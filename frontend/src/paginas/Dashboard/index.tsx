import { useCallback, useEffect, useMemo, useState } from "react";
import { Archive, ClipboardList, CheckCircle2, AlertTriangle, CheckCheck } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import KpiCard from "../../componentes/ui/KpiCard";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import WaterfallChart from "../../componentes/charts/WaterfallChart";
import MetricDonut from "../../componentes/charts/MetricDonut";
import { extrairMensagemDeErro } from "../../services/api";
import { obterResumoDashboard, obterUltimoPeriodoComDados } from "../../services/dashboardService";
import { listarEmpresas } from "../../services/empresasService";
import type { DashboardResumo } from "../../interfaces/Dashboard";

const RESUMO_VAZIO: DashboardResumo = {
  backlogTotal: 0,
  preparados: 0,
  conferidos: 0,
  fechados: 0,
  pendentes: 0,
};

function mesAtual(): string {
  return new Date().toISOString().slice(0, 7);
}

export default function Dashboard() {
  const [anoMes, setAnoMes] = useState(mesAtual());
  const [empresa, setEmpresa] = useState("");
  const [nomesDeEmpresas, setNomesDeEmpresas] = useState<string[]>([]);
  const [resumo, setResumo] = useState<DashboardResumo>(RESUMO_VAZIO);
  const [isLoading, setIsLoading] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    // Abre no mês mais recente que já tem fechamento registrado — o mês
    // calendário atual normalmente ainda está vazio.
    obterUltimoPeriodoComDados()
      .then((ultimoPeriodo) => {
        if (ultimoPeriodo) setAnoMes(ultimoPeriodo);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    listarEmpresas()
      .then((lista) => {
        const nomes = [...new Set(lista.filter((e) => e.ativo).map((e) => e.nome))].sort((a, b) =>
          a.localeCompare(b, "pt-BR")
        );
        setNomesDeEmpresas(nomes);
      })
      .catch(() => setNomesDeEmpresas([]));
  }, []);

  const carregarResumo = useCallback(async () => {
    setIsLoading(true);
    setErro(null);

    try {
      setResumo(await obterResumoDashboard(anoMes, empresa));
    } catch (erroCapturado) {
      setErro(extrairMensagemDeErro(erroCapturado, "Não foi possível carregar o resumo do dashboard."));
      setResumo(RESUMO_VAZIO);
    } finally {
      setIsLoading(false);
    }
  }, [anoMes, empresa]);

  useEffect(() => {
    void carregarResumo();
  }, [carregarResumo]);

  const barras = useMemo(
    () =>
      [
        { label: "Backlog", value: resumo.backlogTotal, tipo: "total" },
        { label: "Finalizado", value: resumo.fechados, tipo: "diminuicao" },
        { label: "Conferido", value: resumo.conferidos, tipo: "diminuicao" },
        { label: "Preparado", value: resumo.preparados, tipo: "diminuicao" },
        { label: "Pendente", value: resumo.pendentes, tipo: "total" },
      ] as const,
    [resumo]
  );

  return (
    <AppLayout>
      <AppHeader
        title="Produtividade e Eficiência"
        subtitle="Backlog de fechamentos por período, cruzando empresas e contas contábeis ativas"
      />

      <main className="sp-dashboard">
        <section className="sp-dashboard__filters" aria-label="Filtros do dashboard">
          <label className="sp-dashboard__field">
            <span>Período (Mês/Ano)</span>
            <input type="month" value={anoMes} onChange={(event) => setAnoMes(event.target.value)} />
          </label>

          <label className="sp-dashboard__field">
            <span>Empresa</span>
            <select value={empresa} onChange={(event) => setEmpresa(event.target.value)}>
              <option value="">Todas</option>
              {nomesDeEmpresas.map((nome) => (
                <option key={nome} value={nome}>
                  {nome}
                </option>
              ))}
            </select>
          </label>
        </section>

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

        <section className="sp-dashboard__charts">
          <div className="sp-card sp-card--chart">
            <h2 className="sp-card__title">Backlog de Atividades: Total vs. Realizado</h2>
            {isLoading ? (
              <div className="sp-card__loading">
                <LoadingSpinner size={26} color="var(--color-green)" />
              </div>
            ) : (
              <WaterfallChart data={barras} />
            )}
          </div>

          <div className="sp-dashboard__donuts">
            <div className="sp-card sp-card--chart">
              <h2 className="sp-card__title">% Finalizado</h2>
              {isLoading ? (
                <div className="sp-card__loading">
                  <LoadingSpinner size={26} color="var(--color-green)" />
                </div>
              ) : (
                <MetricDonut label="Finalizado" value={resumo.fechados} total={resumo.backlogTotal} />
              )}
            </div>

            <div className="sp-card sp-card--chart">
              <h2 className="sp-card__title">% Conferido</h2>
              {isLoading ? (
                <div className="sp-card__loading">
                  <LoadingSpinner size={26} color="var(--color-green)" />
                </div>
              ) : (
                <MetricDonut
                  label="Conferido"
                  value={resumo.conferidos}
                  total={resumo.backlogTotal}
                  color="var(--color-green)"
                />
              )}
            </div>

            <div className="sp-card sp-card--chart">
              <h2 className="sp-card__title">% Preparado</h2>
              {isLoading ? (
                <div className="sp-card__loading">
                  <LoadingSpinner size={26} color="var(--color-green)" />
                </div>
              ) : (
                <MetricDonut
                  label="Preparado"
                  value={resumo.preparados}
                  total={resumo.backlogTotal}
                  color="var(--color-warning)"
                />
              )}
            </div>
          </div>
        </section>
      </main>
    </AppLayout>
  );
}
