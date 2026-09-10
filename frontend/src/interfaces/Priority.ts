export type PriorityLevel = "Alta" | "Média" | "Regular" | "Baixa";

export interface PriorityBreakdown {
  alta: number;
  media: number;
  regular: number;
  baixa: number;
}

export interface StatusPriorityDatum {
  status: string;
  breakdown: PriorityBreakdown;
}

export interface RealizedPriorityDatum {
  priority: PriorityLevel;
  percentage: number;
}
