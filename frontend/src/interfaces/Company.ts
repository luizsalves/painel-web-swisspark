import type { PriorityLevel } from "./Priority";
import type { ReconciliationStatus } from "./Dashboard";

export interface CompanyRecord {
  id: string;
  value: string;
  legalName: string;
  document: string;
  location: string;
  accountCount: number;
  status: "Ativa" | "Inativa";
}

export interface AccountingAccount {
  id: string;
  code: string;
  description: string;
  companyValue: string;
  company: string;
  priority: PriorityLevel;
  status: ReconciliationStatus;
  responsible: string;
}
