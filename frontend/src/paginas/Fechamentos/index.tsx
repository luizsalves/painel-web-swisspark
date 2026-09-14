import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  CircleCheck,
  ClipboardCheck,
  FileSpreadsheet,
  FileText,
  History,
  ListChecks,
  LockKeyhole,
  MessageSquareText,
  MoreVertical,
  RefreshCw,
  Undo2,
} from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import Button from "../../componentes/ui/Button";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import api, { extrairMensagemDeErro } from "../../services/api";
import { ACCOUNTING_ACCOUNTS } from "../../services/mockApi";
import { useAuth } from "../../contexts/AuthContext";
import { MODULO_CONCILIACAO } from "../../interfaces/Auth";
import type { Empresa } from "../../interfaces/Empresa";
import type { ContaContabilTotvs } from "../../interfaces/ContaContabil";
import type { LancamentoTotvs } from "../../interfaces/LancamentoTotvs";
import type { FechamentoValidacaoStatus } from "../../interfaces/FechamentoValidacao";

/** A primeira parte de um `id` no formato codColigada-resto. */
function prefixoColigada(id: string): string {
  return id.split("-")[0];
}

type ClosingTab = "entries" | "pendings" | "notes" | "history";

interface LedgerEntry {
  id: string;
  date: string;
  history: string;
  document: string;
  number: string;
  value: number;
  type: "Débito" | "Crédito";
}

interface HistoryEntry {
  id: string;
  label: string;
  date: string;
}

const PENDING_ITEMS = [
  {
    id: "pending-1",
    title: "Documento fiscal não anexado",
    description: "Lançamento 12346 precisa do comprovante para conferência.",
    priority: "Alta",
  },
  {
    id: "pending-2",
    title: "Diferença na composição do saldo",
    description: "Validar a diferença de R$ 9.215,65 com o responsável.",
    priority: "Média",
  },
];

const TAB_ITEMS: Array<{ id: ClosingTab; label: string; icon: typeof ListChecks }> = [
  { id: "entries", label: "Lançamentos", icon: ListChecks },
  { id: "pendings", label: "Pendências (2)", icon: AlertTriangle },
  { id: "notes", label: "Observações", icon: MessageSquareText },
  { id: "history", label: "Histórico", icon: History },
];

const currencyFormatter = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatCurrency(value: number) {
  return `R$ ${currencyFormatter.format(value)}`;
}

function formatarData(valor: string | null): string {
  if (!valor) return "";
  return new Date(valor).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function Fechamentos() {
  const { user } = useAuth();
  // isSuperAdmin e Administrador (do módulo) têm acesso total. Gerente também
  // prepara e fecha, além de conferir — só o Analista não confere (não pode
  // aprovar o que ele mesmo preparou). Espelha ExigePerfilAttribute no backend.
  const meuPerfilNaConciliacao = user?.modulos.find((m) => m.moduleKey === MODULO_CONCILIACAO)?.role;
  const podeConferir =
    Boolean(user?.isSuperAdmin) ||
    meuPerfilNaConciliacao === "Gerente" ||
    meuPerfilNaConciliacao === "Administrador";
  const podePrepararOuFechar = podeConferir || meuPerfilNaConciliacao === "Analista";

  const [company, setCompany] = useState("caieiras");
  const [filialId, setFilialId] = useState("");
  const [accountId, setAccountId] = useState("acc-001");
  const [period, setPeriod] = useState("2026-01");
  const [loadedSelection, setLoadedSelection] = useState({
    company: "caieiras",
    accountId: "acc-001",
    period: "2026-01",
  });
  const [hasPending, setHasPending] = useState(true);
  const [activeTab, setActiveTab] = useState<ClosingTab>("entries");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);
  const [isLoading, setIsLoading] = useState(false);
  const [isCloseModalOpen, setIsCloseModalOpen] = useState(false);
  const [feedback, setFeedback] = useState("");
  const [note, setNote] = useState("");
  const [savedNotes, setSavedNotes] = useState<string[]>([]);
  const [openEntryMenu, setOpenEntryMenu] = useState<string | null>(null);
  const [historyEntries, setHistoryEntries] = useState<HistoryEntry[]>([
    { id: "history-1", label: "Conciliação pré-conferida por Karime", date: "05/02/2026 07:00" },
    { id: "history-2", label: "Dados do razão carregados", date: "04/02/2026 16:42" },
  ]);

  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [isLoadingEmpresas, setIsLoadingEmpresas] = useState(true);
  const [erroEmpresas, setErroEmpresas] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<Empresa[]>("/empresas")
      .then((resposta) => {
        setEmpresas(resposta.data);
        const primeira = resposta.data[0];
        if (primeira) {
          setCompany(primeira.nome);
          setFilialId(primeira.id);
        }
      })
      .catch(() => setErroEmpresas("Não foi possível carregar a lista de empresas."))
      .finally(() => setIsLoadingEmpresas(false));
  }, []);

  // Uma "empresa" é identificada pelo NOME (razão social) — se repete em todas
  // as filiais dela, inclusive entre bases diferentes. NOMEFANTASIA já varia
  // por unidade, por isso não serve pra agrupar.
  const empresasDistintas = useMemo(() => {
    const nomes = new Set<string>();
    return empresas.filter((empresa) => {
      if (nomes.has(empresa.nome)) return false;
      nomes.add(empresa.nome);
      return true;
    });
  }, [empresas]);

  const filiaisDaEmpresa = useMemo(
    () => empresas.filter((empresa) => empresa.nome === company),
    [empresas, company]
  );

  const filialSelecionada = useMemo(
    () => empresas.find((empresa) => empresa.id === filialId),
    [empresas, filialId]
  );

  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [isLoadingContas, setIsLoadingContas] = useState(true);
  const [erroContas, setErroContas] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<ContaContabilTotvs[]>("/contascontabeis")
      .then((resposta) => setContas(resposta.data))
      .catch(() => setErroContas("Não foi possível carregar o plano de contas."))
      .finally(() => setIsLoadingContas(false));
  }, []);

  // Plano de contas é por coligada — só mostra as contas da mesma base+coligada
  // da filial selecionada, senão CODCONTA de empresas diferentes se misturam.
  const contasDaColigada = useMemo(() => {
    if (!filialSelecionada) return [];
    const prefixo = prefixoColigada(filialSelecionada.id);
    return contas.filter((conta) => prefixoColigada(conta.id) === prefixo);
  }, [contas, filialSelecionada]);

  useEffect(() => {
    setAccountId((atual) => {
      if (contasDaColigada.length === 0) return "";
      return contasDaColigada.some((conta) => conta.codConta === atual) ? atual : contasDaColigada[0].codConta;
    });
  }, [contasDaColigada]);

  const [lancamentos, setLancamentos] = useState<LancamentoTotvs[]>([]);
  const [erroLancamentos, setErroLancamentos] = useState<string | null>(null);

  // A linha-resumo vem com data nula e traz o SALDO_RAZAO já calculado pelo
  // backend; as demais são os lançamentos de verdade do período.
  const linhasDetalhe = useMemo(() => lancamentos.filter((l) => l.data !== null), [lancamentos]);
  const linhaResumo = useMemo(() => lancamentos.find((l) => l.data === null), [lancamentos]);

  const ledgerEntries = useMemo<LedgerEntry[]>(
    () =>
      linhasDetalhe.map((l, index) => ({
        id: `${l.idLancamento ?? "s"}-${l.idPartida ?? "s"}-${index}`,
        date: l.data ? new Date(l.data).toLocaleDateString("pt-BR") : "",
        history: l.complemento || l.reduzido || "",
        document: l.documento ?? "",
        number: l.idLancamento != null ? String(l.idLancamento) : "",
        value: l.debitos !== 0 ? Math.abs(l.debitos) : Math.abs(l.creditos),
        type: l.debitos !== 0 ? "Débito" : "Crédito",
      })),
    [linhasDetalhe]
  );

  const [validacao, setValidacao] = useState<FechamentoValidacaoStatus | null>(null);
  const [erroValidacao, setErroValidacao] = useState<string | null>(null);
  const [isProcessandoAcao, setIsProcessandoAcao] = useState(false);

  const statusAtual = validacao?.status ?? "Pendente";

  // Quem reverte uma etapa é sempre quem tem o perfil que a executaria — espelha
  // a regra do backend (FechamentoValidacaoService.ReverterAsync).
  const podeReverter =
    (statusAtual === "Preparado" && podePrepararOuFechar) ||
    (statusAtual === "Conferido" && podeConferir) ||
    (statusAtual === "Fechado" && podePrepararOuFechar);

  const responsavelAtual = useMemo(() => {
    if (!validacao) return null;
    if (statusAtual === "Fechado" && validacao.fechadoPorNome) {
      return { nome: validacao.fechadoPorNome, em: formatarData(validacao.fechadoEm) };
    }
    if (statusAtual === "Conferido" && validacao.conferidoPorNome) {
      return { nome: validacao.conferidoPorNome, em: formatarData(validacao.conferidoEm) };
    }
    if (statusAtual === "Preparado" && validacao.preparadoPorNome) {
      return { nome: validacao.preparadoPorNome, em: formatarData(validacao.preparadoEm) };
    }
    return null;
  }, [validacao, statusAtual]);

  const chaveAtual = () =>
    filialSelecionada && {
      codColigada: filialSelecionada.codColigada,
      codFilial: filialSelecionada.codFilial,
      codConta: accountId,
      anoMes: period,
    };

  const ledgerBalance = linhaResumo?.saldoRazao ?? 0;
  const previousBalance = linhaResumo?.saldoAnterior ?? 0;
  const difference = hasPending ? 9215.65 : 0;
  const compositionBalance = ledgerBalance - difference;
  const pageCount = Math.ceil(ledgerEntries.length / pageSize);
  const visibleEntries = ledgerEntries.slice((page - 1) * pageSize, page * pageSize);
  const startEntry = (page - 1) * pageSize + 1;
  const endEntry = Math.min(page * pageSize, ledgerEntries.length);

  const showFeedback = (message: string) => {
    setFeedback(message);
    window.setTimeout(() => setFeedback(""), 2800);
  };

  const addHistory = (label: string) => {
    setHistoryEntries((current) => [
      {
        id: `history-${Date.now()}`,
        label,
        date: new Date().toLocaleString("pt-BR", {
          day: "2-digit",
          month: "2-digit",
          year: "numeric",
          hour: "2-digit",
          minute: "2-digit",
        }),
      },
      ...current,
    ]);
  };

  const handleCompanyChange = (value: string) => {
    const firstAccount = ACCOUNTING_ACCOUNTS.find((account) => account.companyValue === value);
    setCompany(value);
    setAccountId(firstAccount?.id ?? "");

    const primeiraFilial = empresas.find((empresa) => empresa.nome === value);
    setFilialId(primeiraFilial?.id ?? "");
  };

  const handleLoadData = async () => {
    if (!filialSelecionada || !accountId) return;

    const chave = {
      codColigada: filialSelecionada.codColigada,
      codFilial: filialSelecionada.codFilial,
      anoMes: period,
      codConta: accountId,
    };

    setIsLoading(true);
    try {
      const [lancamentosResposta, validacaoResposta] = await Promise.all([
        api.get<LancamentoTotvs[]>("/lancamentos", { params: chave }),
        api.get<FechamentoValidacaoStatus>("/fechamento-validacao", { params: chave }),
      ]);

      setLancamentos(lancamentosResposta.data);
      setErroLancamentos(null);
      setValidacao(validacaoResposta.data);
      setErroValidacao(null);
      setLoadedSelection({ company, accountId, period });
      setPage(1);
      showFeedback("Dados do fechamento carregados.");
      addHistory("Dados do razão atualizados");
    } catch {
      setLancamentos([]);
      setErroLancamentos("Não foi possível carregar os lançamentos.");
    } finally {
      setIsLoading(false);
    }
  };

  const recarregarValidacao = async () => {
    const chave = chaveAtual();
    if (!chave) return;

    const resposta = await api.get<FechamentoValidacaoStatus>("/fechamento-validacao", { params: chave });
    setValidacao(resposta.data);
  };

  const executarAcao = async (
    acao: () => Promise<unknown>,
    mensagemSucesso: string,
    mensagemHistorico: string
  ) => {
    setIsProcessandoAcao(true);
    try {
      await acao();
      await recarregarValidacao();
      setErroValidacao(null);
      showFeedback(mensagemSucesso);
      addHistory(`${mensagemHistorico} por ${user?.name ?? "usuário"}`);
    } catch (erro) {
      setErroValidacao(extrairMensagemDeErro(erro));
    } finally {
      setIsProcessandoAcao(false);
    }
  };

  const handlePreparar = () => {
    const chave = chaveAtual();
    if (!chave) return;

    void executarAcao(
      () =>
        api.post("/fechamento-validacao/preparar", {
          ...chave,
          saldoAnterior: previousBalance,
          saldoAtual: ledgerBalance,
        }),
      "Fechamento preparado.",
      "Preparação realizada"
    );
  };

  const handleConferir = () => {
    const chave = chaveAtual();
    if (!chave) return;

    void executarAcao(
      () => api.post("/fechamento-validacao/conferir", chave),
      "Fechamento conferido.",
      "Conferência realizada"
    );
  };

  const handleRealizarFechamento = () => {
    const chave = chaveAtual();
    if (!chave) return;

    void executarAcao(
      () => api.post("/fechamento-validacao/realizar-fechamento", { ...chave, saldoAtual: ledgerBalance }),
      "Fechamento realizado com sucesso.",
      "Fechamento realizado"
    );
    setIsCloseModalOpen(false);
  };

  const handleReverter = () => {
    const chave = chaveAtual();
    if (!chave) return;

    void executarAcao(
      () => api.post("/fechamento-validacao/reverter", chave),
      "Status revertido.",
      "Status revertido"
    );
  };

  const handleExportExcel = () => {
    const header = "Data;Histórico;Documento;Número;Valor;Tipo";
    const rows = ledgerEntries.map((entry) =>
      [entry.date, entry.history, entry.document, entry.number, currencyFormatter.format(entry.value), entry.type].join(";")
    );
    const blob = new Blob([`﻿${[header, ...rows].join("\n")}`], {
      type: "text/csv;charset=utf-8",
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `fechamento-${loadedSelection.period}.csv`;
    link.click();
    URL.revokeObjectURL(url);
    showFeedback("Arquivo para Excel gerado.");
  };

  const handleSaveNote = () => {
    const trimmedNote = note.trim();
    if (!trimmedNote) return;
    setSavedNotes((current) => [trimmedNote, ...current]);
    setNote("");
    addHistory("Nova observação registrada por Karime");
    showFeedback("Observação salva.");
  };

  return (
    <AppLayout>
      <AppHeader
        title="Fechamento Mensal"
        subtitle="Concilie saldos, trate pendências e conclua o período contábil."
      />
      <main className="sp-closing">
        {feedback && (
          <div className="sp-closing__heading">
            <span className="sp-closing__feedback" role="status">{feedback}</span>
          </div>
        )}

        <section className="sp-closing__filters" aria-label="Filtros do fechamento">
          <label className="sp-closing__field">
            <span>Empresa</span>
            <select
              value={company}
              onChange={(event) => handleCompanyChange(event.target.value)}
              disabled={isLoadingEmpresas || !!erroEmpresas}
            >
              {isLoadingEmpresas && <option>Carregando empresas...</option>}
              {erroEmpresas && <option>{erroEmpresas}</option>}
              {!isLoadingEmpresas && !erroEmpresas && empresasDistintas.map((empresa) => (
                <option key={empresa.nome} value={empresa.nome}>{empresa.nome}</option>
              ))}
            </select>
          </label>

          <div className="sp-closing__field sp-closing__field--codigo">
            <span>Cód. Coligada</span>
            <strong className="sp-closing__readonly">{filialSelecionada?.codColigada ?? "—"}</strong>
          </div>

          <label className="sp-closing__field sp-closing__field--filial">
            <span>Filial</span>
            <select
              value={filialId}
              onChange={(event) => setFilialId(event.target.value)}
              disabled={isLoadingEmpresas || !!erroEmpresas}
            >
              {filiaisDaEmpresa.map((empresa) => (
                <option key={empresa.id} value={empresa.id}>
                  {empresa.nomeFilial}
                </option>
              ))}
            </select>
          </label>

          <label className="sp-closing__field sp-closing__field--account">
            <span>Conta Contábil</span>
            <select
              value={accountId}
              onChange={(event) => setAccountId(event.target.value)}
              disabled={isLoadingContas || !!erroContas}
            >
              {isLoadingContas && <option>Carregando contas...</option>}
              {erroContas && <option>{erroContas}</option>}
              {!isLoadingContas && !erroContas && contasDaColigada.map((conta) => (
                <option key={conta.id} value={conta.codConta}>
                  {conta.codConta} - {conta.descricao}
                </option>
              ))}
            </select>
          </label>

          <label className="sp-closing__field">
            <span>Período (Mês/Ano)</span>
            <input type="month" value={period} onChange={(event) => setPeriod(event.target.value)} />
          </label>

          <Button
            className="sp-closing__load"
            icon={<RefreshCw size={18} />}
            isLoading={isLoading}
            disabled={!filialSelecionada || !accountId}
            onClick={handleLoadData}
          >
            Carregar Dados
          </Button>
        </section>

        {erroLancamentos && (
          <span className="sp-closing__feedback sp-closing__feedback--error" role="alert">
            {erroLancamentos}
          </span>
        )}

        <section className="sp-closing__summary" aria-label="Resumo da conciliação">
          <div className="sp-closing__metric">
            <span>Saldo Anterior</span>
            <strong>{formatCurrency(previousBalance)}</strong>
          </div>
          <div className="sp-closing__metric">
            <span>Saldo Razão</span>
            <strong>{formatCurrency(ledgerBalance)}</strong>
          </div>
          <div className="sp-closing__metric">
            <span>Composição do Saldo</span>
            <strong>{formatCurrency(compositionBalance)}</strong>
          </div>
          <div className="sp-closing__metric sp-closing__metric--difference">
            <span>Diferença</span>
            <strong>{formatCurrency(difference)}</strong>
          </div>
          <label className="sp-closing__metric sp-closing__metric--select">
            <span>Possui Pendência?</span>
            <select value={hasPending ? "yes" : "no"} onChange={(event) => setHasPending(event.target.value === "yes")}>
              <option value="yes">SIM</option>
              <option value="no">NÃO</option>
            </select>
          </label>
          <div className="sp-closing__metric">
            <span>Status Atual</span>
            <StatusBadge label={statusAtual} />
          </div>
          <div className="sp-closing__metric sp-closing__metric--responsible">
            <span>Responsável</span>
            <strong>{responsavelAtual?.nome ?? "—"}</strong>
            <small>{responsavelAtual?.em ?? ""}</small>
          </div>
        </section>

        <div className="sp-closing__workspace">
          <section className="sp-closing__content">
            <div className="sp-closing__tabs" role="tablist" aria-label="Detalhes do fechamento">
              {TAB_ITEMS.map((tab) => {
                const Icon = tab.icon;
                return (
                  <button
                    key={tab.id}
                    type="button"
                    role="tab"
                    aria-selected={activeTab === tab.id}
                    className={activeTab === tab.id ? "sp-closing__tab sp-closing__tab--active" : "sp-closing__tab"}
                    onClick={() => setActiveTab(tab.id)}
                  >
                    <Icon size={17} />
                    {tab.label}
                  </button>
                );
              })}
            </div>

            {activeTab === "entries" && (
              <div className="sp-closing__table-card">
                <div className="sp-closing__table-wrap">
                  <table className="sp-closing__table">
                    <thead>
                      <tr>
                        <th>Data</th><th>Histórico</th><th>Documento</th><th>Número</th><th>Valor (R$)</th><th>Tipo</th><th aria-label="Ações" />
                      </tr>
                    </thead>
                    <tbody>
                      {visibleEntries.map((entry) => (
                        <tr key={entry.id}>
                          <td>{entry.date}</td>
                          <td>{entry.history}</td>
                          <td>{entry.document}</td>
                          <td>{entry.number}</td>
                          <td>{currencyFormatter.format(entry.value)}</td>
                          <td>{entry.type}</td>
                          <td className="sp-closing__row-action">
                            <button type="button" aria-label={`Ações do lançamento ${entry.number}`} onClick={() => setOpenEntryMenu(openEntryMenu === entry.id ? null : entry.id)}>
                              <MoreVertical size={18} />
                            </button>
                            {openEntryMenu === entry.id && (
                              <div className="sp-closing__row-menu">
                                <button type="button" onClick={() => { setOpenEntryMenu(null); showFeedback(`Lançamento ${entry.number} selecionado.`); }}>Ver detalhes</button>
                                <button type="button" onClick={() => { setOpenEntryMenu(null); setHasPending(true); showFeedback("Lançamento marcado com pendência."); }}>Marcar pendência</button>
                              </div>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="sp-closing__pagination">
                  <span>Mostrando {startEntry} a {endEntry} de {ledgerEntries.length} lançamentos</span>
                  <div className="sp-closing__page-controls">
                    <button type="button" aria-label="Primeira página" disabled={page === 1} onClick={() => setPage(1)}><ChevronsLeft size={17} /></button>
                    <button type="button" aria-label="Página anterior" disabled={page === 1} onClick={() => setPage((current) => current - 1)}><ChevronLeft size={17} /></button>
                    <strong>{page}</strong>
                    <span>de {pageCount}</span>
                    <button type="button" aria-label="Próxima página" disabled={page === pageCount} onClick={() => setPage((current) => current + 1)}><ChevronRight size={17} /></button>
                    <button type="button" aria-label="Última página" disabled={page === pageCount} onClick={() => setPage(pageCount)}><ChevronsRight size={17} /></button>
                  </div>
                  <select aria-label="Itens por página" value={pageSize} onChange={(event) => { setPageSize(Number(event.target.value)); setPage(1); }}>
                    <option value={5}>5 por página</option>
                    <option value={10}>10 por página</option>
                    <option value={25}>25 por página</option>
                  </select>
                </div>
              </div>
            )}

            {activeTab === "pendings" && (
              <div className="sp-closing__panel-list">
                {PENDING_ITEMS.map((item) => (
                  <article key={item.id}>
                    <AlertTriangle size={20} />
                    <div><strong>{item.title}</strong><p>{item.description}</p></div>
                    <StatusBadge label={item.priority} />
                  </article>
                ))}
              </div>
            )}

            {activeTab === "notes" && (
              <div className="sp-closing__notes">
                <label htmlFor="closing-note">Nova observação</label>
                <textarea id="closing-note" value={note} onChange={(event) => setNote(event.target.value)} placeholder="Registre informações relevantes para esta conciliação" />
                <Button onClick={handleSaveNote} disabled={!note.trim()}>Salvar observação</Button>
                {savedNotes.map((savedNote, index) => <p key={`${savedNote}-${index}`}>{savedNote}</p>)}
              </div>
            )}

            {activeTab === "history" && (
              <div className="sp-closing__history">
                {historyEntries.map((entry) => (
                  <div key={entry.id}><CheckCircle2 size={18} /><div><strong>{entry.label}</strong><span>{entry.date}</span></div></div>
                ))}
              </div>
            )}
          </section>

          <aside className="sp-closing__actions" aria-label="Ações do fechamento">
            <h2>Ações</h2>

            {erroValidacao && (
              <span className="sp-closing__feedback sp-closing__feedback--error" role="alert">
                {erroValidacao}
              </span>
            )}

            <Button
              icon={<ClipboardCheck size={18} />}
              isLoading={isProcessandoAcao}
              disabled={!podePrepararOuFechar || statusAtual !== "Pendente" || !linhaResumo}
              title={!podePrepararOuFechar ? "Seu perfil não pode preparar." : undefined}
              onClick={handlePreparar}
            >
              Preparar
            </Button>
            <Button
              icon={<CircleCheck size={18} />}
              isLoading={isProcessandoAcao}
              disabled={!podeConferir || statusAtual !== "Preparado"}
              title={!podeConferir ? "Só Gerente ou Administrador podem conferir." : undefined}
              onClick={handleConferir}
            >
              Conferir
            </Button>
            <Button
              variant="danger"
              icon={<LockKeyhole size={18} />}
              disabled={!podePrepararOuFechar || statusAtual !== "Conferido"}
              title={!podePrepararOuFechar ? "Seu perfil não pode realizar o fechamento." : undefined}
              onClick={() => setIsCloseModalOpen(true)}
            >
              Realizar Fechamento
            </Button>
            <Button
              variant="secondary"
              icon={<Undo2 size={18} />}
              isLoading={isProcessandoAcao}
              disabled={!podeReverter}
              title={!podeReverter ? "Nada para reverter, ou você não tem o perfil pra isso." : undefined}
              onClick={handleReverter}
            >
              Reverter
            </Button>
            <div className="sp-closing__actions-divider" />
            <Button variant="secondary" icon={<FileSpreadsheet size={19} />} onClick={handleExportExcel}>Gerar Excel</Button>
            <Button variant="secondary" icon={<FileText size={19} />} onClick={() => window.print()}>Gerar PDF</Button>
          </aside>
        </div>
      </main>

      <Modal
        isOpen={isCloseModalOpen}
        onClose={() => setIsCloseModalOpen(false)}
        title="Realizar fechamento"
        description="Confirme o encerramento desta conta para o período selecionado."
      >
        <p className="sp-closing__modal-copy">
          Após confirmar, o status será alterado para Fechado e a ação ficará registrada no histórico.
        </p>
        <div className="sp-closing__modal-actions">
          <Button variant="secondary" onClick={() => setIsCloseModalOpen(false)}>Cancelar</Button>
          <Button
            variant="danger"
            icon={<LockKeyhole size={17} />}
            isLoading={isProcessandoAcao}
            onClick={handleRealizarFechamento}
          >
            Confirmar fechamento
          </Button>
        </div>
      </Modal>
    </AppLayout>
  );
}
