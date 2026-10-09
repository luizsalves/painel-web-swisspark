import { chaveEmpresaContabil, coligadaDaEmpresa } from "../../utils/empresaContabil";
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
import SearchableSelect from "../../componentes/ui/SearchableSelect";
import type { SearchableSelectOption } from "../../componentes/ui/SearchableSelect";
import api, { extrairMensagemDeErro } from "../../services/api";
import { listarEmpresasBalancete, listarFiliaisBalancete, listarContasBalancete } from "../../services/balanceteService";
import { useAuth } from "../../contexts/AuthContext";
import { MODULO_CONCILIACAO } from "../../interfaces/Auth";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../../interfaces/ContaContabil";
import type { LancamentoTotvs } from "../../interfaces/LancamentoTotvs";
import type { FechamentoValidacaoStatus } from "../../interfaces/FechamentoValidacao";

type ClosingTab = "entries" | "pendings" | "notes" | "history";

interface LedgerEntry {
  id: string;
  date: string;
  idPartida: string;
  history: string;
  value: number;
  type: "Débito" | "Crédito";
}

interface HistoryEntry {
  id: string;
  label: string;
  date: string;
}

interface LoadedSelection {
  empresa: string;
  codColigada: number;
  codFilial: number;
  codConta: string;
  descricaoConta: string;
  period: string;
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

function formatPeriod(value: string) {
  const [year, month] = value.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" }).format(new Date(year, month - 1, 1));
}

/** Uma linha por código de conta — o mesmo CODCONTA pode se repetir entre coligadas. */
function dedupeContasPorCodigo(lista: ContaContabilTotvs[]): ContaContabilTotvs[] {
  const porCodigo = new Map<string, ContaContabilTotvs>();
  for (const conta of lista) {
    if (!porCodigo.has(conta.codConta) || (!porCodigo.get(conta.codConta)?.descricao && conta.descricao))
      porCodigo.set(conta.codConta, conta);
  }
  return [...porCodigo.values()].sort((a, b) => a.codConta.localeCompare(b.codConta));
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

  const [company, setCompany] = useState("");
  const [empresaEscolhida, setEmpresaEscolhida] = useState(false);
  const [filialId, setFilialId] = useState("");
  const [filialEscolhida, setFilialEscolhida] = useState(false);
  const [accountId, setAccountId] = useState("");
  const [contaEscolhida, setContaEscolhida] = useState(false);
  const [period, setPeriod] = useState("");
  const [loadedSelection, setLoadedSelection] = useState<LoadedSelection | null>(null);
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

  const [empresas, setEmpresas] = useState<EmpresaContabil[]>([]);
  const [isLoadingEmpresas, setIsLoadingEmpresas] = useState(true);
  const [erroEmpresas, setErroEmpresas] = useState<string | null>(null);

  useEffect(() => {
    listarEmpresasBalancete()
      .then((lista) => setEmpresas(lista.filter((empresa) => empresa.codColigada !== 0)))
      .catch(() => setErroEmpresas("Não foi possível carregar a lista de empresas."))
      .finally(() => setIsLoadingEmpresas(false));
  }, []);

  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [isLoadingContas, setIsLoadingContas] = useState(false);
  const [contasCarregadas, setContasCarregadas] = useState(false);
  const [erroContas, setErroContas] = useState<string | null>(null);
  const [filiaisMovimentadas, setFiliaisMovimentadas] = useState<ColigadaFilialContabil[]>([]);
  const [isLoadingFiliais, setIsLoadingFiliais] = useState(false);
  const [filiaisCarregadas, setFiliaisCarregadas] = useState(false);

  useEffect(() => {
    if (!empresaEscolhida) return;
    let cancelado = false;
    const codColigada = company ? coligadaDaEmpresa(company) : undefined;
    setIsLoadingFiliais(true);
    setFiliaisCarregadas(false);
    listarFiliaisBalancete(codColigada)
      .then((lista) => {
        if (!cancelado) {
          setFiliaisMovimentadas(lista);
          setFiliaisCarregadas(true);
        }
      })
      .catch(() => { if (!cancelado) setErroEmpresas("Não foi possível carregar as filiais."); })
      .finally(() => { if (!cancelado) setIsLoadingFiliais(false); });
    return () => { cancelado = true; };
  }, [empresaEscolhida, company]);

  // Os combos usam a mesma origem e os mesmos códigos do Balancete.
  const opcoesEmpresa = useMemo<SearchableSelectOption[]>(
    () => [{ value: "", label: "Todas" }, ...empresas.map((empresa) => ({ value: chaveEmpresaContabil(empresa), label: empresa.nome }))],
    [empresas]
  );

  const opcoesFilial = useMemo<SearchableSelectOption[]>(
    () => [
      { value: "", label: "Todas" },
      ...[...new Set(filiaisMovimentadas.map((filial) => filial.codFilial))].sort((a, b) => a - b)
        .map((codigo) => ({ value: String(codigo), label: String(codigo) })),
    ],
    [filiaisMovimentadas]
  );

  // O fechamento exige uma empresa e uma filial específicas para formar sua chave.
  const filialSelecionada = useMemo(() => {
    if (!company || !filialId) return undefined;
    const empresa = empresas.find((item) => chaveEmpresaContabil(item) === company);
    const filial = filiaisMovimentadas.find((item) =>
      item.codColigada === coligadaDaEmpresa(company) && item.codFilial === Number(filialId));
    return empresa && filial ? { ...filial, nome: empresa.nome } : undefined;
  }, [empresas, company, filialId, filiaisMovimentadas]);

  // A conta é consultada apenas após a escolha explícita de empresa e filial.
  useEffect(() => {
    if (!empresaEscolhida || !filialEscolhida || !filiaisCarregadas) return;
    let cancelado = false;
    setIsLoadingContas(true);
    setContasCarregadas(false);
    listarContasBalancete({
      codColigada: company ? coligadaDaEmpresa(company) : undefined,
      codFilial: filialId ? Number(filialId) : undefined,
    })
      .then((lista) => {
        if (!cancelado) {
          setContas(lista);
          setContasCarregadas(true);
        }
      })
      .catch(() => { if (!cancelado) setErroContas("Não foi possível carregar o plano de contas."); })
      .finally(() => { if (!cancelado) setIsLoadingContas(false); });
    return () => { cancelado = true; };
  }, [empresaEscolhida, filialEscolhida, filiaisCarregadas, company, filialId]);

  const contasDaColigada = useMemo(() => dedupeContasPorCodigo(contas), [contas]);

  const opcoesConta = useMemo<SearchableSelectOption[]>(
    () => [
      { value: "", label: "Todas" },
      ...contasDaColigada.map((conta) => ({ value: conta.codConta, label: conta.descricao ? `${conta.codConta} - ${conta.descricao}` : conta.codConta })),
    ],
    [contasDaColigada]
  );

  const [lancamentos, setLancamentos] = useState<LancamentoTotvs[]>([]);
  const [erroLancamentos, setErroLancamentos] = useState<string | null>(null);

  // A linha-resumo vem com data nula e traz o SALDO_RAZAO já calculado pelo
  // backend; as demais são os lançamentos de verdade do período.
  const linhasDetalhe = useMemo(() => lancamentos.filter((l) => l.data !== null), [lancamentos]);
  const linhaResumo = useMemo(() => lancamentos.find((l) => l.data === null), [lancamentos]);
  const codigoReduzido = useMemo(
    () => {
      const daConta = contasDaColigada.find((conta) => conta.codConta === accountId)?.reduzido.trim();
      if (daConta && daConta !== "-") return daConta;
      return lancamentos.map((linha) => linha.reduzido.trim()).find((valor) => valor && valor !== "-") ?? "—";
    },
    [accountId, contasDaColigada, lancamentos]
  );

  const ledgerEntries = useMemo<LedgerEntry[]>(
    () =>
      linhasDetalhe.map((l, index) => ({
        id: `${l.idLancamento ?? "s"}-${l.idPartida ?? "s"}-${index}`,
        date: l.data ? new Date(l.data).toLocaleDateString("pt-BR") : "",
        idPartida: l.idPartida != null ? String(l.idPartida) : "",
        history: l.complemento || l.reduzido || "",
        value: l.debitos !== 0 ? l.debitos : l.creditos,
        type: l.debitos !== 0 ? "Débito" : "Crédito",
      })),
    [linhasDetalhe]
  );

  const totaisRelatorio = useMemo(() => ledgerEntries.reduce(
    (totais, entry) => {
      if (entry.type === "Débito") totais.debito += entry.value;
      else totais.credito += entry.value;
      return totais;
    },
    { debito: 0, credito: 0 }
  ), [ledgerEntries]);

  const [filtroData, setFiltroData] = useState("");
  const [filtroIdPartida, setFiltroIdPartida] = useState("");
  const [filtroHistorico, setFiltroHistorico] = useState("");
  const [filtroValor, setFiltroValor] = useState("");

  const filtrosAtivos = Boolean(
    filtroData.trim() || filtroIdPartida.trim() || filtroHistorico.trim() || filtroValor.trim()
  );

  const filteredEntries = useMemo(
    () =>
      ledgerEntries.filter((entry) => {
        if (filtroData.trim() && !entry.date.includes(filtroData.trim())) return false;
        if (filtroIdPartida.trim() && !entry.idPartida.includes(filtroIdPartida.trim())) return false;
        if (
          filtroHistorico.trim() &&
          !entry.history.toLowerCase().includes(filtroHistorico.trim().toLowerCase())
        )
          return false;
        if (filtroValor.trim() && !currencyFormatter.format(entry.value).includes(filtroValor.trim())) return false;
        return true;
      }),
    [ledgerEntries, filtroData, filtroIdPartida, filtroHistorico, filtroValor]
  );

  const handleLimparFiltros = () => {
    setFiltroData("");
    setFiltroIdPartida("");
    setFiltroHistorico("");
    setFiltroValor("");
  };

  useEffect(() => {
    setPage(1);
  }, [filtroData, filtroIdPartida, filtroHistorico, filtroValor]);

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

  // Balanço "oficial" do backend — é o valor usado nas ações de Preparar/Fechar,
  // que sempre precisam considerar o período inteiro, nunca um subconjunto filtrado.
  const ledgerBalance = linhaResumo?.saldoRazao ?? 0;
  const previousBalance = linhaResumo?.saldoAnterior ?? 0;

  // Soma débito/crédito só dos lançamentos que passam pelos filtros da tabela.
  const somaFiltrada = useMemo(
    () =>
      filteredEntries.reduce((acc, entry) => acc + (entry.type === "Débito" ? entry.value : -entry.value), 0),
    [filteredEntries]
  );

  // Com filtro ativo, Saldo Razão e Composição do Saldo exibidos passam a
  // refletir apenas os lançamentos filtrados (saldo anterior + soma filtrada).
  const saldoRazaoExibido = filtrosAtivos ? previousBalance + somaFiltrada : ledgerBalance;

  // Composição do saldo ainda não tem cálculo real no backend (ver roadmap,
  // Fase 05) — até existir, mostra zerado em vez de um valor inventado.
  const difference = 0;
  const compositionBalance = saldoRazaoExibido - difference;
  const pageCount = Math.ceil(filteredEntries.length / pageSize);
  const visibleEntries = filteredEntries.slice((page - 1) * pageSize, page * pageSize);
  const startEntry = (page - 1) * pageSize + 1;
  const endEntry = Math.min(page * pageSize, filteredEntries.length);

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
    setLoadedSelection(null);
    setCompany(value);
    setEmpresaEscolhida(true);
    setFilialId("");
    setFilialEscolhida(false);
    setFiliaisMovimentadas([]);
    setFiliaisCarregadas(false);
    setAccountId("");
    setContaEscolhida(false);
    setContas([]);
    setContasCarregadas(false);
    setIsLoadingContas(false);
    setErroEmpresas(null);
    setErroContas(null);
    setPeriod("");
    setLancamentos([]);
    setValidacao(null);
    setErroLancamentos(null);
    setErroValidacao(null);
  };

  const handleFilialChange = (value: string) => {
    setLoadedSelection(null);
    setFilialId(value);
    setFilialEscolhida(true);
    setAccountId("");
    setContaEscolhida(false);
    setContas([]);
    setContasCarregadas(false);
    setIsLoadingContas(false);
    setErroContas(null);
    setPeriod("");
    setLancamentos([]);
    setValidacao(null);
    setErroLancamentos(null);
    setErroValidacao(null);
  };

  const handleAccountChange = (value: string) => {
    setLoadedSelection(null);
    setAccountId(value);
    setContaEscolhida(true);
    setPeriod("");
    setLancamentos([]);
    setValidacao(null);
    setErroLancamentos(null);
    setErroValidacao(null);
  };

  const handleLoadData = async () => {
    if (!empresaEscolhida || !filialEscolhida || !contaEscolhida || !filialSelecionada || !accountId || !period) return;

    const chave = {
      codColigada: filialSelecionada.codColigada,
      codFilial: filialSelecionada.codFilial,
      anoMes: period,
      codConta: accountId,
    };

    setLoadedSelection(null);
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
      setLoadedSelection({
        empresa: filialSelecionada.nome,
        codColigada: filialSelecionada.codColigada,
        codFilial: filialSelecionada.codFilial,
        codConta: accountId,
        descricaoConta: contasDaColigada.find((conta) => conta.codConta === accountId)?.descricao ?? "",
        period,
      });
      setPage(1);

      if (lancamentosResposta.data.length === 0) {
        showFeedback("Nenhum lançamento encontrado para essa combinação.");
      } else {
        showFeedback("Dados do fechamento carregados.");
      }

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

  const [exportandoExcel, setExportandoExcel] = useState(false);

  const handleExportExcel = async () => {
    if (!loadedSelection || exportandoExcel) return;
    setExportandoExcel(true);
    try {
      const resposta = await api.post<Blob>("/fechamentos/exportar-excel", {
        empresa: loadedSelection.empresa,
        periodo: loadedSelection.period,
        codColigada: loadedSelection.codColigada,
        codFilial: loadedSelection.codFilial,
        codConta: loadedSelection.codConta,
        descricaoConta: loadedSelection.descricaoConta,
        reduzido: codigoReduzido,
        preparadoPor: validacao?.preparadoPorNome,
        conferidoPor: validacao?.conferidoPorNome,
        finalizadoPor: validacao?.fechadoPorNome,
        preparadoEm: formatarData(validacao?.preparadoEm ?? null),
        conferidoEm: formatarData(validacao?.conferidoEm ?? null),
        finalizadoEm: formatarData(validacao?.fechadoEm ?? null),
        saldoAnterior: previousBalance,
        saldoRazao: ledgerBalance,
        composicaoSaldo: ledgerBalance - difference,
        diferenca: difference,
        pendencia: hasPending,
        status: statusAtual,
        responsavel: responsavelAtual?.nome,
        lancamentos: ledgerEntries.map((entry) => ({
          data: entry.date, idPartida: entry.idPartida, historico: entry.history,
          valor: entry.value, tipo: entry.type,
        })),
      }, { responseType: "blob" });
      const url = URL.createObjectURL(resposta.data);
      const link = document.createElement("a");
      link.href = url;
      link.download = `fechamento-${loadedSelection.period}.xls`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
      showFeedback("Arquivo Excel gerado.");
    } catch {
      showFeedback("Falha ao gerar o Excel. Tente novamente.");
    } finally {
      setExportandoExcel(false);
    }
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
    <AppLayout className="sp-closing-layout">
      <AppHeader
        title="Fechamento Mensal"
        subtitle="Concilie saldos, trate pendências e conclua o período contábil."
      />
      <main className="sp-closing">
        {loadedSelection && (
          <article className="sp-closing__print-report" aria-label="Relatório de fechamento mensal">
            <header className="sp-closing__print-header">
              <img src="/branding/logo-swiss-park.png" alt="Swiss Park" />
              <div>
                <span>Relatório contábil</span>
                <h1>Fechamento mensal</h1>
                <p>Referência: <strong>{formatPeriod(loadedSelection.period)}</strong></p>
              </div>
            </header>

            <div className="sp-closing__print-identification">
              <div><span>Empresa</span><strong>{loadedSelection.empresa}</strong></div>
              <div><span>Coligada / Filial</span><strong>{loadedSelection.codColigada} / {loadedSelection.codFilial}</strong><small>Reduzido: {codigoReduzido}</small></div>
              <div className="sp-closing__print-account"><span>Conta contábil</span><strong>{loadedSelection.codConta}{loadedSelection.descricaoConta ? ` — ${loadedSelection.descricaoConta}` : ""}</strong></div>
            </div>

            <div className="sp-closing__print-responsaveis" aria-label="Responsáveis pelo fechamento">
              <div><span>Preparado por</span><strong>{validacao?.preparadoPorNome || "—"}</strong><small>Em: {formatarData(validacao?.preparadoEm ?? null) || "—"}</small></div>
              <div><span>Conferido por</span><strong>{validacao?.conferidoPorNome || "—"}</strong><small>Em: {formatarData(validacao?.conferidoEm ?? null) || "—"}</small></div>
              <div><span>Finalizado por</span><strong>{validacao?.fechadoPorNome || "—"}</strong><small>Em: {formatarData(validacao?.fechadoEm ?? null) || "—"}</small></div>
            </div>

            <section className="sp-closing__print-section" aria-label="Resumo do período">
              <h2>Resumo do período</h2>
              <div className="sp-closing__print-metrics">
                <div><span>Saldo anterior</span><strong>{formatCurrency(previousBalance)}</strong></div>
                <div><span>Saldo razão</span><strong>{formatCurrency(ledgerBalance)}</strong></div>
                <div><span>Composição do saldo</span><strong>{formatCurrency(ledgerBalance - difference)}</strong></div>
                <div><span>Diferença</span><strong>{formatCurrency(difference)}</strong></div>
              </div>
              <div className="sp-closing__print-status">
                <span>Pendência: <strong>{hasPending ? "Sim" : "Não"}</strong></span>
                <span>Status: <strong>{statusAtual}</strong></span>
                <span>Responsável: <strong>{responsavelAtual?.nome ?? "—"}</strong></span>
              </div>
            </section>

            <section className="sp-closing__print-section" aria-label="Lançamentos contábeis">
              <div className="sp-closing__print-section-title">
                <h2>Lançamentos contábeis</h2>
                <span>{ledgerEntries.length} {ledgerEntries.length === 1 ? "lançamento" : "lançamentos"}</span>
              </div>
              <table className="sp-closing__print-table">
                <colgroup><col className="sp-closing__print-col-date" /><col className="sp-closing__print-col-id" /><col /><col className="sp-closing__print-col-value" /><col className="sp-closing__print-col-type" /></colgroup>
                <thead><tr><th>Data</th><th>IdPartida</th><th>Histórico</th><th>Valor (R$)</th><th>Tipo</th></tr></thead>
                <tbody>
                  {ledgerEntries.length === 0 ? (
                    <tr><td colSpan={5} className="sp-closing__print-empty">Nenhum lançamento encontrado neste período.</td></tr>
                  ) : ledgerEntries.map((entry) => (
                    <tr key={entry.id}>
                      <td>{entry.date}</td><td>{entry.idPartida}</td><td>{entry.history}</td><td>{currencyFormatter.format(entry.value)}</td><td>{entry.type}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div className="sp-closing__print-totals" aria-label="Totais dos lançamentos">
                <div><span>Total de débitos</span><strong>{formatCurrency(totaisRelatorio.debito)}</strong></div>
                <div><span>Total de créditos</span><strong>{formatCurrency(totaisRelatorio.credito)}</strong></div>
              </div>
            </section>
          </article>
        )}

        {feedback && (
          <div className="sp-closing__heading">
            <span className="sp-closing__feedback" role="status">{feedback}</span>
          </div>
        )}

        <section className="sp-closing__filters" aria-label="Filtros do fechamento">
          <label className="sp-closing__field">
            <span>Empresa</span>
            <SearchableSelect
              value={empresaEscolhida ? company : "__pendente__"}
              options={opcoesEmpresa}
              onChange={handleCompanyChange}
              disabled={isLoadingEmpresas || !!erroEmpresas}
              placeholder={isLoadingEmpresas ? "Carregando empresas..." : erroEmpresas ?? "Selecione uma empresa"}
              ariaLabel="Empresa"
            />
          </label>

          <div className="sp-closing__codes">
            <div className="sp-closing__field sp-closing__field--codigo">
              <span>Cód. Coligada</span>
              <strong className="sp-closing__readonly">{empresaEscolhida ? (company ? coligadaDaEmpresa(company) : "Todas") : "—"}</strong>
            </div>
            <div className="sp-closing__field sp-closing__field--reduzido">
              <span>Cód. Reduzido</span>
              <strong className="sp-closing__readonly">{codigoReduzido}</strong>
            </div>
          </div>

          <label className="sp-closing__field sp-closing__field--filial">
            <span>Filial</span>
            <SearchableSelect
              value={filialEscolhida ? filialId : "__pendente__"}
              options={opcoesFilial}
              onChange={handleFilialChange}
              disabled={!empresaEscolhida || isLoadingFiliais || !filiaisCarregadas || !!erroEmpresas}
              placeholder={isLoadingFiliais ? "Carregando filiais..." : "Selecione uma filial"}
              ariaLabel="Filial"
            />
          </label>

          <label className="sp-closing__field sp-closing__field--account">
            <span>Conta Contábil</span>
            <SearchableSelect
              value={contaEscolhida ? accountId : "__pendente__"}
              options={opcoesConta}
              onChange={handleAccountChange}
              disabled={!filialEscolhida || isLoadingContas || !contasCarregadas || !!erroContas}
              placeholder={isLoadingContas ? "Carregando contas..." : erroContas ?? "Selecione uma conta"}
              ariaLabel="Conta Contábil"
            />
          </label>

          <label className="sp-closing__field">
            <span>Período (Mês/Ano)</span>
            <input type="month" value={period} disabled={!contaEscolhida || !contasCarregadas}
              onChange={(event) => {
                setPeriod(event.target.value);
                setLoadedSelection(null);
                setLancamentos([]);
                setValidacao(null);
              }} />
          </label>

          <Button
            className="sp-closing__load"
            icon={<RefreshCw size={18} />}
            isLoading={isLoading}
            disabled={!filialSelecionada || !accountId || !period}
            title={!filialSelecionada || !accountId || !period ? "Escolha uma empresa, uma filial e uma conta específicas, depois o mês, para carregar." : undefined}
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
            <strong>{formatCurrency(saldoRazaoExibido)}</strong>
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
            <section className="sp-closing__flow" aria-label="Etapas do fechamento">
              <div className="sp-closing__flow-heading">
                <strong>Fluxo do fechamento</strong>
                <span>{!loadedSelection ? "Carregue um fechamento para acompanhar" : erroValidacao ? "Status indisponível" : statusAtual === "Fechado" ? "Finalizado" : statusAtual}</span>
              </div>
              <ol className="sp-closing__flow-steps">
                {[
                  { status: "Pendente", label: "Pendente", icon: FileText, nome: null, data: null },
                  { status: "Preparado", label: "Preparado", icon: ClipboardCheck, nome: validacao?.preparadoPorNome, data: validacao?.preparadoEm },
                  { status: "Conferido", label: "Conferido", icon: CheckCircle2, nome: validacao?.conferidoPorNome, data: validacao?.conferidoEm },
                  { status: "Fechado", label: "Finalizado", icon: LockKeyhole, nome: validacao?.fechadoPorNome, data: validacao?.fechadoEm },
                ].map((etapa, indice) => {
                  const atual = loadedSelection && validacao && !erroValidacao
                    ? ["Pendente", "Preparado", "Conferido", "Fechado"].indexOf(statusAtual) : -1;
                  const atingida = indice <= atual;
                  const Icon = etapa.icon;
                  return (
                    <li key={etapa.status}
                      className={`sp-closing__flow-step${atingida ? " sp-closing__flow-step--done" : ""}${indice === atual ? " sp-closing__flow-step--current" : ""}`}
                      aria-current={indice === atual ? "step" : undefined}>
                      <div className="sp-closing__flow-icon"><Icon size={24} aria-hidden="true" /></div>
                      <strong>{etapa.label}</strong>
                      <span>{atingida && etapa.nome ? etapa.nome : indice === 0 ? (atingida ? "Fechamento iniciado" : "Aguardando consulta") : "Aguardando"}</span>
                      {atingida && etapa.data && <small>{formatarData(etapa.data)}</small>}
                    </li>
                  );
                })}
              </ol>
            </section>
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
                <div className="sp-closing__table-filters" aria-label="Filtros dos lançamentos">
                  <label className="sp-closing__field">
                    <span>Data</span>
                    <input
                      type="text"
                      placeholder="dd/mm/aaaa"
                      value={filtroData}
                      onChange={(event) => setFiltroData(event.target.value)}
                    />
                  </label>
                  <label className="sp-closing__field">
                    <span>IdPartida</span>
                    <input
                      type="text"
                      placeholder="Filtrar por IdPartida"
                      value={filtroIdPartida}
                      onChange={(event) => setFiltroIdPartida(event.target.value)}
                    />
                  </label>
                  <label className="sp-closing__field">
                    <span>Histórico</span>
                    <input
                      type="text"
                      placeholder="Filtrar por histórico"
                      value={filtroHistorico}
                      onChange={(event) => setFiltroHistorico(event.target.value)}
                    />
                  </label>
                  <label className="sp-closing__field">
                    <span>Valor</span>
                    <input
                      type="text"
                      placeholder="Filtrar por valor"
                      value={filtroValor}
                      onChange={(event) => setFiltroValor(event.target.value)}
                    />
                  </label>
                  {filtrosAtivos && (
                    <button type="button" className="sp-closing__clear-filters" onClick={handleLimparFiltros}>
                      Limpar filtros
                    </button>
                  )}
                </div>
                <div className="sp-closing__table-wrap">
                  <table className="sp-closing__table">
                    <thead>
                      <tr>
                        <th>Data</th><th>IdPartida</th><th>Histórico</th><th>Valor (R$)</th><th>Tipo</th><th aria-label="Ações" />
                      </tr>
                    </thead>
                    <tbody>
                      {visibleEntries.map((entry) => (
                        <tr key={entry.id}>
                          <td>{entry.date}</td>
                          <td>{entry.idPartida}</td>
                          <td>{entry.history}</td>
                          <td>{currencyFormatter.format(entry.value)}</td>
                          <td>{entry.type}</td>
                          <td className="sp-closing__row-action">
                            <button type="button" aria-label={`Ações da partida ${entry.idPartida}`} onClick={() => setOpenEntryMenu(openEntryMenu === entry.id ? null : entry.id)}>
                              <MoreVertical size={18} />
                            </button>
                            {openEntryMenu === entry.id && (
                              <div className="sp-closing__row-menu">
                                <button type="button" onClick={() => { setOpenEntryMenu(null); showFeedback(`Partida ${entry.idPartida} selecionada.`); }}>Ver detalhes</button>
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
                  <span>Mostrando {startEntry} a {endEntry} de {filteredEntries.length} lançamentos</span>
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
            <Button variant="secondary" icon={<FileSpreadsheet size={19} />} disabled={!loadedSelection} isLoading={exportandoExcel} onClick={handleExportExcel}>Gerar Excel</Button>
            <Button variant="secondary" icon={<FileText size={19} />} disabled={!loadedSelection} onClick={() => window.print()}>Gerar PDF</Button>
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
