import type { PriorityLevel, RealizedPriorityDatum, StatusPriorityDatum } from "./Priority";

export type ReconciliationStatus =
  | "Backlog"
  | "Preparado"
  | "Pré-conferido"
  | "Conferido"
  | "Pendência"
  | "Fechado";

export interface DashboardMetrics {
  backlogTotal: number;
  preparados: number;
  preConferidos: number;
  conferidos: number;
  pendencias: number;
  fechados: number;
}

export interface RecentPending {
  id: string;
  accountCode: string;
  description: string;
  priority: PriorityLevel;
}

export type ClosingStatus = "Fechado" | "Conferido" | "Pré-conferido";

export interface RecentClosing {
  id: string;
  company: string;
  accountCode: string;
  description: string;
  period: string;
  status: ClosingStatus;
  responsible: string;
  closedAt: string;
}

export interface DashboardSnapshot {
  metrics: DashboardMetrics;
  statusByPriority: StatusPriorityDatum[];
  realizedByPriority: RealizedPriorityDatum[];
  recentPendings: RecentPending[];
  recentClosings: RecentClosing[];
}
