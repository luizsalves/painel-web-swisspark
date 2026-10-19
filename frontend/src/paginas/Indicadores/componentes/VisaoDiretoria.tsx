import type { Detalhe } from "../ancoras";
import { ultimoDiaDoMes, vesperaDoMes } from "../../../utils/formatacaoContabil";
import {
  descreverPeriodo, descreverPeriodoExtenso, mesmoPeriodoDoAnoAnterior, nomeDoMes, palavraPeriodo,
} from "../periodo";
import {
  formatarMil, formatarMoedaAbreviada, gerarAlertas, gerarVeredito, lerCascata, montarCascata, montarPilares,
} from "../regras";
import type { DadosPainel } from "../regras";
import BarrasHorizontais from "./BarrasHorizontais";
import type { LinhaBarra } from "./BarrasHorizontais";
import BlocoSemDados from "./BlocoSemDados";
import CardDivida from "./CardDivida";
import CardVeredito from "./CardVeredito";
import CascataResultado from "./CascataResultado";
import ListaAlertas from "./ListaAlertas";

interface VisaoDiretoriaProps {
  dados: DadosPainel;
  inicio: string;
  fim: string;
  onVerDetalhe: (detalhe: Detalhe) => void;
}

/** "A empresa está bem, para onde está indo e o que exige atenção" — sem fórmula. */
export default function VisaoDiretoria({ dados, inicio, fim, onVerDetalhe }: VisaoDiretoriaProps) {
  const { base, dfc, meses } = dados;
  const periodo = palavraPeriodo(meses);
  const descricao = descreverPeriodo(inicio, fim);
  const extenso = descreverPeriodoExtenso(inicio, fim);
  const pilares = montarPilares(dados);
  const veredito = gerarVeredito(pilares, { meses });
  const anoAnterior = mesmoPeriodoDoAnoAnterior(inicio, fim);

  const tom = (valor: number): LinhaBarra["tom"] => (valor < 0 ? "negativo" : "positivo");
  const linhasCaixa: LinhaBarra[] = [
    { rotulo: "Gerado pela operação", valor: dfc.operacionais, texto: formatarMil(dfc.operacionais, { sinal: true }), tom: tom(dfc.operacionais) },
    { rotulo: "Empréstimos e sócios", valor: dfc.financiamento, texto: formatarMil(dfc.financiamento, { sinal: true }), tom: tom(dfc.financiamento) },
    { rotulo: "Investimentos", valor: dfc.investimentos, texto: formatarMil(dfc.investimentos, { sinal: true }), tom: tom(dfc.investimentos) },
    ...(dfc.naoClassificadas !== 0
      ? [{ rotulo: "Ainda sem classificação", valor: dfc.naoClassificadas, texto: formatarMil(dfc.naoClassificadas, { sinal: true }), tom: "neutro" as const }]
      : []),
  ];
  const variacaoCaixa = dfc.saldoFinal - dfc.saldoInicial;

  return (
    <>
      <CardVeredito status={veredito.status} frase={veredito.frase} descricaoPeriodo={descricao}
        pilares={pilares} onVerDetalhe={onVerDetalhe} />

      <ListaAlertas alertas={gerarAlertas(dados)} />

      <div className="sp-ind-grade">
        <section className="sp-ind-card sp-ind-bloco" aria-label="Da receita ao lucro">
          <div className="sp-ind-bloco__cabecalho">
            <h2>Da receita ao lucro</h2>
            <span>{extenso} · R$ mil</span>
          </div>
          <CascataResultado degraus={montarCascata(base)} />
          <p className="sp-ind-bloco__leitura">{lerCascata(base, { meses, nomeDoMes: nomeDoMes(fim) })}</p>
        </section>

        <section className="sp-ind-card sp-ind-bloco" aria-label="De onde veio o caixa">
          <div className="sp-ind-bloco__cabecalho">
            <h2>De onde veio o caixa</h2>
            <span>{extenso}</span>
          </div>
          <div className="sp-ind-caixa__saldo">
            <strong>{formatarMoedaAbreviada(dfc.saldoFinal)}</strong>
            <span className={variacaoCaixa < 0 ? "sp-ind-negativo" : "sp-ind-positivo"}>
              {formatarMil(variacaoCaixa, { sinal: true })} no {periodo}
            </span>
          </div>
          <span className="sp-ind-caixa__inicio">Saldo em {vesperaDoMes(inicio)}: {formatarMoedaAbreviada(dfc.saldoInicial)}</span>
          <BarrasHorizontais linhas={linhasCaixa} />
        </section>

        <section className="sp-ind-card sp-ind-bloco" aria-label="Tendência de 12 meses">
          <div className="sp-ind-bloco__cabecalho">
            <h2>Últimos 12 meses</h2>
            <span>Comparado com o mesmo mês do ano anterior</span>
          </div>
          <BlocoSemDados>Receita, EBITDA e caixa mês a mês aparecem quando o histórico estiver disponível.</BlocoSemDados>
        </section>

        <CardDivida base={base} multiplo={dados.indicadores["divida-liquida-ebitda"]} dataBalanco={ultimoDiaDoMes(fim)} />

        <section className="sp-ind-card sp-ind-bloco" aria-label="Por que o resultado mudou">
          <div className="sp-ind-bloco__cabecalho">
            <h2>Por que o resultado mudou</h2>
            <span>{descreverPeriodoExtenso(anoAnterior.inicio, anoAnterior.fim)} → {extenso.charAt(0).toLowerCase() + extenso.slice(1)} · R$ mil</span>
          </div>
          <BlocoSemDados>{`A comparação com ${descreverPeriodo(anoAnterior.inicio, anoAnterior.fim)} aparece quando o histórico estiver disponível.`}</BlocoSemDados>
        </section>

        <section className="sp-ind-card sp-ind-bloco sp-ind-bloco--preparacao" aria-label="Investidas e sócios">
          <div className="sp-ind-bloco__cabecalho">
            <h2>Investidas e sócios</h2>
            <span>Em preparação</span>
          </div>
          <div className="sp-ind-lista sp-ind-lista--suave">
            <div><span>Resultado por investida</span><span>[aguardando de-para]</span></div>
            <div><span>Dividendos recebidos das investidas</span><span>[aguardando de-para]</span></div>
            <div><span>Lucros distribuídos aos sócios</span><span>[aguardando de-para]</span></div>
          </div>
          <p className="sp-ind-bloco__leitura">
            Entra quando as contas de equivalência patrimonial e de dividendos estiverem mapeadas no plano de contas.
          </p>
        </section>
      </div>

      <p className="sp-ind-rodape">
        Faixas e regras dos alertas definidas pela contabilidade em Configurações. Números do fechamento de {descricao}.
      </p>
    </>
  );
}
