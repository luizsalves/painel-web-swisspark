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
  Users,
} from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import Button from "../../componentes/ui/Button";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import api from "../../services/api";
import { ACCOUNTING_ACCOUNTS, COMPANY_RECORDS } from "../../services/mockApi";
import type { ReconciliationStatus } from "../../interfaces/Dashboard";
import type { Restaurante } from "../../interfaces/Restaurante";

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

const SUPPLIERS = ["ABC", "XYZ", "LMN", "QRS", "TUV"];
const VALUES = [125000, 240500, 310200, 85700, 98300];

const LEDGER_ENTRIES: LedgerEntry[] = Array.from({ length: 25 }, (_, index) => ({
  id: `entry-${index + 1}`,
  date: `${String(2 + (index % 25)).padStart(2, "0")}/01/2026`,
  history: `Fornecedor ${SUPPLIERS[index % SUPPLIERS.length]} Ltda`,
  document: index % 4 === 3 ? "REC" : "NF",
  number: String(12345 + index),
  value: VALUES[index % VALUES.length] + Math.floor(index / VALUES.length) * 1250,
  type: index % 4 === 3 ? "Crédito" : "Débito",
}));

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

export default function Fechamentos() {
  const [company, setCompany] = useState("caieiras");
  const [accountId, setAccountId] = useState("acc-001");
  const [period, setPeriod] = useState("2026-01");
  const [loadedSelection, setLoadedSelection] = useState({
    company: "caieiras",
    accountId: "acc-001",
    period: "2026-01",
  });
  const [hasPending, setHasPending] = useState(true);
  const [status, setStatus] = useState<ReconciliationStatus>("Pré-conferido");
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

  const [restaurantes, setRestaurantes] = useState<Restaurante[]>([]);
  const [isLoadingRestaurantes, setIsLoadingRestaurantes] = useState(true);
  const [erroRestaurantes, setErroRestaurantes] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<Restaurante[]>("/restaurantes")
      .then((resposta) => setRestaurantes(resposta.data))
      .catch(() => setErroRestaurantes("Não foi possível carregar os restaurantes da API."))
      .finally(() => setIsLoadingRestaurantes(false));
  }, []);

  const availableAccounts = useMemo(
    () => ACCOUNTING_ACCOUNTS.filter((account) => account.companyValue === company),
    [company]
  );

  const loadedAccount =
    ACCOUNTING_ACCOUNTS.find((account) => account.id === loadedSelection.accountId) ??
    ACCOUNTING_ACCOUNTS[0];
  const accountIndex = Math.max(
    0,
    ACCOUNTING_ACCOUNTS.findIndex((account) => account.id === loadedSelection.accountId)
  );
  const ledgerBalance = 1248532.1 + accountIndex * 35420.75;
  const difference = hasPending ? 9215.65 : 0;
  const compositionBalance = ledgerBalance - difference;
  const pageCount = Math.ceil(LEDGER_ENTRIES.length / pageSize);
  const visibleEntries = LEDGER_ENTRIES.slice((page - 1) * pageSize, page * pageSize);
  const startEntry = (page - 1) * pageSize + 1;
  const endEntry = Math.min(page * pageSize, LEDGER_ENTRIES.length);

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
  };

  const handleLoadData = () => {
    setIsLoading(true);
    window.setTimeout(() => {
      setLoadedSelection({ company, accountId, period });
      setPage(1);
      setIsLoading(false);
      showFeedback("Dados do fechamento carregados.");
      addHistory("Dados do razão atualizados");
    }, 450);
  };

  const handleStatusChange = (nextStatus: ReconciliationStatus, actionLabel: string) => {
    setStatus(nextStatus);
    addHistory(`${actionLabel} por Karime`);
    showFeedback(`Status alterado para ${nextStatus}.`);
  };

  const handleConfirmClosing = () => {
    setStatus("Fechado");
    setIsCloseModalOpen(false);
    addHistory("Fechamento realizado por Karime");
    showFeedback("Fechamento realizado com sucesso.");
  };

  const handleExportExcel = () => {
    const header = "Data;Histórico;Documento;Número;Valor;Tipo";
    const rows = LEDGER_ENTRIES.map((entry) =>
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
      <main className="sp-closing">
        <div className="sp-closing__heading">
          <div>
            <h1>Fechamento Mensal</h1>
            <p>Concilie saldos, trate pendências e conclua o período contábil.</p>
          </div>
          {feedback && <span className="sp-closing__feedback" role="status">{feedback}</span>}
        </div>

        <section className="sp-closing__filters" aria-label="Filtros do fechamento">
          <label className="sp-closing__field">
            <span>Empresa</span>
            <select value={company} onChange={(event) => handleCompanyChange(event.target.value)}>
              {COMPANY_RECORDS.map((item) => (
                <option key={item.id} value={item.value}>{item.legalName}</option>
              ))}
            </select>
          </label>

          <label className="sp-closing__field sp-closing__field--account">
            <span>Conta Contábil</span>
            <select value={accountId} onChange={(event) => setAccountId(event.target.value)}>
              {availableAccounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.code} - {account.description}
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
            onClick={handleLoadData}
          >
            Carregar Dados
          </Button>
        </section>

        <section className="sp-closing__summary" aria-label="Resumo da conciliação">
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
            <StatusBadge label={status} />
          </div>
          <div className="sp-closing__metric sp-closing__metric--responsible">
            <span>Responsável</span>
            <strong>{loadedAccount.responsible}</strong>
            <small>05/02/2026 07:00</small>
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
                  <span>Mostrando {startEntry} a {endEntry} de {LEDGER_ENTRIES.length} lançamentos</span>
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
            <Button icon={<ClipboardCheck size={18} />} onClick={() => handleStatusChange("Preparado", "Preparação realizada")}>Preparar</Button>
            <Button icon={<Users size={18} />} onClick={() => handleStatusChange("Pré-conferido", "Pré-conferência realizada")}>Pré-conferir</Button>
            <Button icon={<CircleCheck size={18} />} onClick={() => handleStatusChange("Conferido", "Conferência realizada")}>Conferir</Button>
            <Button variant="danger" icon={<LockKeyhole size={18} />} onClick={() => setIsCloseModalOpen(true)}>Realizar Fechamento</Button>
            <div className="sp-closing__actions-divider" />
            <Button variant="secondary" icon={<FileSpreadsheet size={19} />} onClick={handleExportExcel}>Gerar Excel</Button>
            <Button variant="secondary" icon={<FileText size={19} />} onClick={() => window.print()}>Gerar PDF</Button>
          </aside>
        </div>

        <section className="sp-card" aria-label="Teste: restaurantes da API do ERP">
          <div className="sp-card__header">
            <h2 className="sp-card__title">Teste — Restaurantes (API do ERP)</h2>
          </div>

          {isLoadingRestaurantes && (
            <div className="sp-card__loading">
              <LoadingSpinner size={26} color="var(--color-green)" />
            </div>
          )}

          {erroRestaurantes && <p style={{ color: "var(--color-error)" }}>{erroRestaurantes}</p>}

          {!isLoadingRestaurantes && !erroRestaurantes && (
            <ul className="sp-pending-list">
              {restaurantes.map((restaurante) => (
                <li key={restaurante.id} className="sp-pending-list__item">
                  <div className="sp-pending-list__info">
                    <span className="sp-pending-list__code">{restaurante.nome}</span>
                    <span className="sp-pending-list__description">
                      {restaurante.pratos.length} {restaurante.pratos.length === 1 ? "prato" : "pratos"}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
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
          <Button variant="danger" icon={<LockKeyhole size={17} />} onClick={handleConfirmClosing}>Confirmar fechamento</Button>
        </div>
      </Modal>
    </AppLayout>
  );
}
