import type { PriorityLevel } from "./Priority";
import type { ReconciliationStatus } from "./Dashboard";

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
