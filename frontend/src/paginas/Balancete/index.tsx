import { useSearchParams } from "react-router-dom";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import BalanceteResumo from "./BalanceteResumo";
import BalancoDfc from "./BalancoDfc";
import BalancoPatrimonial from "./BalancoPatrimonial";
import { CAMPOS_FILTRO_BALANCO, CAMPOS_FILTRO_DFC, CAMPOS_FILTRO_RESUMO } from "./camposFiltro";
import type { CampoFiltro } from "../../utils/filtrosPainel";

type Aba = "resumo" | "balanco" | "dfc";

const ABAS: { id: Aba; rotulo: string; subtitulo: string; campos: CampoFiltro[] }[] = [
  { id: "resumo", rotulo: "Balancete Resumo", subtitulo: "Consulte saldos e movimentos contábeis por empresa, filial, conta e período.", campos: CAMPOS_FILTRO_RESUMO },
  { id: "balanco", rotulo: "Balanço", subtitulo: "Balanço patrimonial no fim do período, com exportação em PDF e Excel.", campos: CAMPOS_FILTRO_BALANCO },
  { id: "dfc", rotulo: "Balanço DFC", subtitulo: "Demonstração do fluxo de caixa pelo método indireto, com exportação em PDF e Excel.", campos: CAMPOS_FILTRO_DFC },
];

export default function Balancete() {
  // A aba fica na URL (?aba=dfc): dá para abrir direto nela ou compartilhar o link.
  const [parametros, setParametros] = useSearchParams();
  const atual = ABAS.find((item) => item.id === parametros.get("aba")) ?? ABAS[0];
  const aba = atual.id;

  return (
    // Fora do resumo, o "Gerar PDF" imprime só o relatório da aba.
    <AppLayout className={aba === "resumo" ? "" : "sp-dfc-print-layout"}>
      <AppHeader title="Balancete" subtitle={atual.subtitulo} camposFiltro={atual.campos} />
      <main className="sp-balance">
        <div className="sp-abas" role="tablist" aria-label="Visões do balancete">
          {ABAS.map((item) => (
            <button key={item.id} type="button" role="tab" aria-selected={aba === item.id}
              className={aba === item.id ? "sp-abas__botao sp-abas__botao--ativa" : "sp-abas__botao"}
              onClick={() => setParametros(item.id === "resumo" ? {} : { aba: item.id }, { replace: true })}>
              {item.rotulo}
            </button>
          ))}
        </div>
        {aba === "balanco" ? <BalancoPatrimonial />
          : aba === "dfc" ? <BalancoDfc />
          : <BalanceteResumo />}
      </main>
    </AppLayout>
  );
}
