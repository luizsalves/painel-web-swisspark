import type { AccountingAccount, CompanyRecord } from "../interfaces/Company";
import type { CompanyOption, PeriodOption } from "../interfaces/Filtros";
import type { DashboardSnapshot, RecentClosing } from "../interfaces/Dashboard";

// ==========================================================================
// Filtros
// ==========================================================================

export const PERIOD_OPTIONS: PeriodOption[] = [
  { value: "01/2026", label: "01/2026" },
  { value: "12/2025", label: "12/2025" },
  { value: "11/2025", label: "11/2025" },
];

export const COMPANY_OPTIONS: CompanyOption[] = [
  { value: "todas", label: "Todas" },
  { value: "caieiras", label: "SWISS PARK CAIEIRAS SPE LTDA" },
  { value: "campinas", label: "SWISS PARK CAMPINAS SPE LTDA" },
];

export const COMPANY_RECORDS: CompanyRecord[] = [
  {
    id: "cmp-001",
    value: "caieiras",
    legalName: "SWISS PARK CAIEIRAS SPE LTDA",
    document: "12.345.678/0001-10",
    location: "Caieiras/SP",
    accountCount: 128,
    status: "Ativa",
  },
  {
    id: "cmp-002",
    value: "campinas",
    legalName: "SWISS PARK CAMPINAS SPE LTDA",
    document: "23.456.789/0001-21",
    location: "Campinas/SP",
    accountCount: 96,
    status: "Ativa",
  },
];

export const ACCOUNTING_ACCOUNTS: AccountingAccount[] = [
  {
    id: "acc-001",
    code: "2.1.1.01.01.0001",
    description: "Fornecedores de Materiais",
    companyValue: "caieiras",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    priority: "Alta",
    status: "Fechado",
    responsible: "Karime",
  },
  {
    id: "acc-002",
    code: "1.1.01.01.0002",
    description: "Caixa Matriz",
    companyValue: "caieiras",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    priority: "Média",
    status: "Conferido",
    responsible: "Maria Santos",
  },
  {
    id: "acc-003",
    code: "5.4.01.01.0001",
    description: "Impostos a Recuperar",
    companyValue: "caieiras",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    priority: "Alta",
    status: "Pré-conferido",
    responsible: "João Silva",
  },
  {
    id: "acc-004",
    code: "1.1.02.01.0003",
    description: "Banco Itaú",
    companyValue: "campinas",
    company: "SWISS PARK CAMPINAS SPE LTDA",
    priority: "Regular",
    status: "Preparado",
    responsible: "Ana Costa",
  },
  {
    id: "acc-005",
    code: "2.3.02.01.0005",
    description: "Empréstimos e Financiamentos",
    companyValue: "campinas",
    company: "SWISS PARK CAMPINAS SPE LTDA",
    priority: "Média",
    status: "Pendência",
    responsible: "Carlos Lima",
  },
];

export const CLOSING_RECORDS: RecentClosing[] = [
  {
    id: "clo-001",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    accountCode: "2.1.1.01.01.0001",
    description: "Fornecedores de Materiais",
    period: "01/2026",
    status: "Fechado",
    responsible: "Karime",
    closedAt: "05/02/2026 11:04",
  },
  {
    id: "clo-002",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    accountCode: "1.1.01.01.0002",
    description: "Caixa Matriz",
    period: "01/2026",
    status: "Conferido",
    responsible: "Maria Santos",
    closedAt: "05/02/2026 10:32",
  },
  {
    id: "clo-003",
    company: "SWISS PARK CAIEIRAS SPE LTDA",
    accountCode: "5.4.01.01.0001",
    description: "Impostos a Recuperar",
    period: "01/2026",
    status: "Pré-conferido",
    responsible: "João Silva",
    closedAt: "05/02/2026 09:15",
  },
  {
    id: "clo-004",
    company: "SWISS PARK CAMPINAS SPE LTDA",
    accountCode: "1.1.02.01.0003",
    description: "Banco Itaú",
    period: "01/2026",
    status: "Fechado",
    responsible: "Ana Costa",
    closedAt: "04/02/2026 17:48",
  },
  {
    id: "clo-005",
    company: "SWISS PARK CAMPINAS SPE LTDA",
    accountCode: "2.3.02.01.0005",
    description: "Empréstimos e Financiamentos",
    period: "12/2025",
    status: "Conferido",
    responsible: "Carlos Lima",
    closedAt: "15/01/2026 14:20",
  },
];

// ==========================================================================
// Snapshot padrão (01/2026 · Todas as empresas)
// ==========================================================================

const BASE_SNAPSHOT: DashboardSnapshot = {
  metrics: {
    backlogTotal: 23,
    preparados: 3,
    preConferidos: 5,
    conferidos: 12,
    pendencias: 4,
    fechados: 18,
  },
  statusByPriority: [
    { status: "Backlog", breakdown: { alta: 6, media: 8, regular: 5, baixa: 4 } },
    { status: "Preparados", breakdown: { alta: 1, media: 1, regular: 1, baixa: 0 } },
    { status: "Pré-conferidos", breakdown: { alta: 2, media: 1, regular: 1, baixa: 1 } },
    { status: "Conferidos", breakdown: { alta: 3, media: 4, regular: 3, baixa: 2 } },
    { status: "Fechados", breakdown: { alta: 5, media: 6, regular: 4, baixa: 3 } },
  ],
  realizedByPriority: [
    { priority: "Alta", percentage: 72 },
    { priority: "Média", percentage: 56 },
    { priority: "Regular", percentage: 48 },
    { priority: "Baixa", percentage: 33 },
  ],
  recentPendings: [
    {
      id: "pnd-001",
      accountCode: "2.1.1.01.01.0001",
      description: "Fornecedores de Materiais",
      priority: "Alta",
    },
    {
      id: "pnd-002",
      accountCode: "1.1.01.01.0002",
      description: "Caixa Matriz",
      priority: "Média",
    },
    {
      id: "pnd-003",
      accountCode: "5.4.01.01.0001",
      description: "Impostos a Recuperar",
      priority: "Alta",
    },
    {
      id: "pnd-004",
      accountCode: "2.3.02.01.0005",
      description: "Empréstimos",
      priority: "Média",
    },
    {
      id: "pnd-005",
      accountCode: "1.1.02.01.0003",
      description: "Banco Itaú",
      priority: "Alta",
    },
  ],
  recentClosings: CLOSING_RECORDS.slice(0, 3),
};

/**
 * Gera um snapshot mockado do painel, variando levemente os números
 * conforme o período e a empresa selecionados. Simula o comportamento
 * de uma consulta real sem exigir backend.
 */
export function getDashboardSnapshot(period: string, company: string): DashboardSnapshot {
  const periodSeed = PERIOD_OPTIONS.findIndex((p) => p.value === period);
  const companySeed = COMPANY_OPTIONS.findIndex((c) => c.value === company);
  const factor = 1 + periodSeed * 0.12 - companySeed * 0.08;

  const scale = (value: number) => Math.max(0, Math.round(value * factor));

  return {
    ...BASE_SNAPSHOT,
    metrics: {
      backlogTotal: scale(BASE_SNAPSHOT.metrics.backlogTotal),
      preparados: scale(BASE_SNAPSHOT.metrics.preparados),
      preConferidos: scale(BASE_SNAPSHOT.metrics.preConferidos),
      conferidos: scale(BASE_SNAPSHOT.metrics.conferidos),
      pendencias: scale(BASE_SNAPSHOT.metrics.pendencias),
      fechados: scale(BASE_SNAPSHOT.metrics.fechados),
    },
    statusByPriority: BASE_SNAPSHOT.statusByPriority.map((item) => ({
      status: item.status,
      breakdown: {
        alta: scale(item.breakdown.alta),
        media: scale(item.breakdown.media),
        regular: scale(item.breakdown.regular),
        baixa: scale(item.breakdown.baixa),
      },
    })),
    realizedByPriority: BASE_SNAPSHOT.realizedByPriority.map((item) => ({
      ...item,
      percentage: Math.min(100, Math.max(0, Math.round(item.percentage * factor))),
    })),
  };
}
