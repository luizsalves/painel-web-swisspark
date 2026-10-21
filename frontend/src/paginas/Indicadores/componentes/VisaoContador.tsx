import { useEffect } from "react";
import { Link } from "react-router-dom";
import DemonstracaoFluxoCaixa from "../../../componentes/dfc/DemonstracaoFluxoCaixa";
import type { Dfc } from "../../../interfaces/Indicadores";
import { ultimoDiaDoMes } from "../../../utils/formatacaoContabil";
import { idDoDetalhe } from "../ancoras";
import type { Detalhe } from "../ancoras";
import type { HistoricoIndicadores } from "../historico";
import { descreverPeriodo, palavraPeriodo } from "../periodo";
import { formatarMoedaAbreviada, montarPilares, pontosAVerificar, textoDoValor } from "../regras";
import type { DadosPainel, ResultadoIndicador, Status } from "../regras";
import BaseCalculo from "./BaseCalculo";
import MatrizMeses from "./MatrizMeses";
import StatusPill from "./StatusPill";
import TabelaIndicadores from "./TabelaIndicadores";
import type { RotulosComparacao } from "./TabelaIndicadores";

interface CardResumoProps {
  rotulo: string;
  status: Status;
  valor: string;
  detalhe?: string;
  rotuloComparado: string;
  /** Nulo enquanto o histórico carrega ou quando o período comparado não tem lançamentos. */
  valorComparado: string | null;
}

function CardResumo({ rotulo, status, valor, detalhe, rotuloComparado, valorComparado }: CardResumoProps) {
  return (
    <div className="sp-ind-resumo">
      <div className="sp-ind-resumo__topo">
        <span>{rotulo}</span>
        <StatusPill status={status} />
      </div>
      <strong className="sp-ind-resumo__valor">{valor}</strong>
      {detalhe && <span className="sp-ind-resumo__detalhe">{detalhe}</span>}
      <span className="sp-ind-resumo__comparacao">{rotuloComparado}: {valorComparado ?? "—"}</span>
    </div>
  );
}

function cardDoIndicador(
  indicador: ResultadoIndicador, rotuloComparado: string, comparado: ResultadoIndicador | undefined,
): CardResumoProps {
  return {
    rotulo: indicador.config.nome,
    status: indicador.status,
    valor: textoDoValor(indicador),
    detalhe: indicador.detalheValor,
    rotuloComparado,
    valorComparado: comparado ? textoDoValor(comparado) : null,
  };
}

interface VisaoContadorProps {
  dados: DadosPainel;
  dfc: Dfc;
  inicio: string;
  fim: string;
  empresaNome: string;
  codColigada: number;
  rotulos: RotulosComparacao;
  /** As comparações e os 12 meses; nulo enquanto o histórico carrega ou se ele falhou. */
  historico: HistoricoIndicadores | null;
  /** O que a matriz mostra enquanto não tem o histórico. */
  mensagemHistorico: string;
  /** Grupo para onde rolar ao chegar pelo "Ver no detalhe" da diretoria. */
  detalhe: Detalhe | null;
  onDetalheVisto: () => void;
}

/** "Qual é o índice, como foi calculado e o que mudou": a visão para conferir. */
export default function VisaoContador({
  dados, dfc, inicio, fim, empresaNome, codColigada, rotulos, historico, mensagemHistorico, detalhe, onDetalheVisto,
}: VisaoContadorProps) {
  const { base, indicadores, meses } = dados;
  const periodo = palavraPeriodo(meses);
  const pontos = pontosAVerificar(dados);
  const statusResultado = montarPilares(dados)[0].status;
  const descricao = descreverPeriodo(inicio, fim);
  const comparada = historico?.comparada;

  useEffect(() => {
    if (!detalhe) return;
    document.getElementById(idDoDetalhe(detalhe))?.scrollIntoView({ behavior: "smooth", block: "start" });
    onDetalheVisto();
  }, [detalhe, onDetalheVisto]);

  const nota = meses === 12
    ? "Dív. líquida / EBITDA usa o EBITDA dos 12 meses do período."
    : `Dív. líquida / EBITDA estimado com o EBITDA de ${descricao} × 12${meses > 1 ? ` ÷ ${meses}` : ""}; nas comparações e nos 12 meses, cada período usa o próprio EBITDA do mesmo jeito.`;

  return (
    <>
      {pontos.length > 0 && (
        <section className="sp-ind-pontos" aria-label="Pontos a verificar">
          <strong>{pontos.length} {pontos.length === 1 ? "ponto" : "pontos"} a verificar antes de fechar o {periodo}</strong>
          {pontos.map((ponto) => <span key={ponto}>{ponto}</span>)}
          <Link to="/contas" className="sp-ind-pontos__link">Revisar em Contas contábeis</Link>
        </section>
      )}

      <div className="sp-ind-resumos">
        <CardResumo {...cardDoIndicador(indicadores["liquidez-corrente"], rotulos.comparada, comparada?.indicadores["liquidez-corrente"])} />
        <CardResumo {...cardDoIndicador(indicadores["endividamento-geral"], rotulos.comparada, comparada?.indicadores["endividamento-geral"])} />
        <CardResumo {...cardDoIndicador(indicadores["divida-liquida-ebitda"], rotulos.comparada, comparada?.indicadores["divida-liquida-ebitda"])} />
        <CardResumo rotulo={`Lucro líquido do ${periodo}`} status={statusResultado}
          valor={formatarMoedaAbreviada(base.lucroLiquido, { simbolo: false })} rotuloComparado={rotulos.comparada}
          valorComparado={comparada ? formatarMoedaAbreviada(comparada.base.lucroLiquido, { simbolo: false }) : null} />
      </div>

      <TabelaIndicadores indicadores={indicadores} rotulos={rotulos} historico={historico} periodo={periodo} nota={nota} />

      <BaseCalculo base={base} dataBalanco={ultimoDiaDoMes(fim)} descricaoPeriodo={descricao} />

      <div id={idDoDetalhe("dfc")}>
        <DemonstracaoFluxoCaixa dfc={dfc} empresaNome={empresaNome} codColigada={codColigada} inicio={inicio} fim={fim} />
      </div>

      <MatrizMeses meses={historico?.meses ?? null} mensagem={mensagemHistorico} />
    </>
  );
}
