import { useEffect, useMemo, useState } from "react";
import { Archive, ClipboardList, CheckCircle2, AlertTriangle, CheckCheck, RefreshCw } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import KpiCard from "../../componentes/ui/KpiCard";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Button from "../../componentes/ui/Button";
import MetricDonut from "../../componentes/charts/MetricDonut";
import { extrairMensagemDeErro } from "../../services/api";
import { obterResumoEmpresas, obterUltimoPeriodoComDados } from "../../services/dashboardService";
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
  const [porEmpresa, setPorEmpresa] = useState<Record<string, DashboardResumo>>({});
  const [atualizadoEm, setAtualizadoEm] = useState<Date | null>(null);
  const [recarregar, setRecarregar] = useState(0);
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
    const controller = new AbortController();
    setIsLoading(true);
    setErro(null);
    setPorEmpresa({});
    setAtualizadoEm(null);
    if (!anoMes) { setIsLoading(false); return; }
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
    .filter((nome) => !empresa || nome === empresa)
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
  const meta = empresasComContas.length;
  const preparadas = meta - restaPreparar.length;
  const conferidas = meta - aConferir.length;

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

        <section className="sp-dashboard__companies" aria-label="Andamento por empresa" aria-busy={isLoading}>
          <div className="sp-dashboard__companies-heading">
            <div><h2>Conciliação por empresa</h2><p>Todas as contas e filiais ativas devem concluir a etapa. Empresas sem contas ficam fora da meta.</p></div>
            <span>Atualizado em: {atualizadoEm ? atualizadoEm.toLocaleString("pt-BR") : "—"}</span>
            <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isLoading} disabled={!anoMes} onClick={() => setRecarregar((valor) => valor + 1)}>Atualizar</Button>
          </div>
          {isLoading ? <div className="sp-card__loading"><LoadingSpinner size={26} /></div> : erro ?
            <p role="status">Não foi possível carregar a visão por empresa.</p> : (
            <div className="sp-dashboard__company-grid">
              <aside className="sp-dashboard__company-metrics">
                <div className="sp-dashboard__company-goal"><span>Meta de empresas</span><strong>{meta}</strong></div>
                <div className="sp-card"><h3>{preparadas} preparadas</h3><MetricDonut label="Preparadas" value={preparadas} total={meta} /></div>
                <div className="sp-card"><h3>{conferidas} conferidas</h3><MetricDonut label="Conferidas" value={conferidas} total={meta} /></div>
              </aside>
              {[
                { titulo: "Resta preparar", lista: restaPreparar },
                { titulo: "A conferir", lista: aConferir },
              ].map(({ titulo, lista }) => (
                <section className="sp-dashboard__company-list" key={titulo} aria-label={titulo}>
                  <header><h3>{titulo}</h3><strong>{lista.length}</strong></header>
                  <div className="sp-dashboard__company-table">
                    <table><thead><tr><th>Empresa</th></tr></thead><tbody>
                      {lista.map((item) => <tr key={item.nome}><td>{item.nome}</td></tr>)}
                      {lista.length === 0 && <tr><td>{meta === 0 ? "Nenhuma empresa com contas neste filtro." : "Nenhuma empresa pendente nesta etapa."}</td></tr>}
                    </tbody></table>
                  </div>
                </section>
              ))}
            </div>
          )}
        </section>
      </main>
    </AppLayout>
  );
}
