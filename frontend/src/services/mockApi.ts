import type { AccountingAccount } from "../interfaces/Company";

// ==========================================================================
// Contas contábeis (usado hoje só pelo combobox de Conta Contábil legado da
// tela de Fechamentos — o restante do painel já usa dados reais)
// ==========================================================================

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
