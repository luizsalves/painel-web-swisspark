import { useMemo } from "react";
import DemonstracaoFluxoCaixa from "../../../componentes/dfc/DemonstracaoFluxoCaixa";
import type { BaseCalculoIndicadores, FormatoIndice, Indicadores as ResultadoIndicadores } from "../../../interfaces/Indicadores";
import { classeNumero, formatarValor, mesAno, numeroContabil, ultimoDiaDoMes } from "../../../utils/formatacaoContabil";

const CONTAS_BALANCO: [keyof BaseCalculoIndicadores, string][] = [
  ["ativoTotal", "Ativo total"],
  ["ativoCirculante", "Ativo circulante"],
  ["disponivel", "Disponível (caixa e equivalentes)"],
  ["estoques", "Estoques"],
  ["realizavelLongoPrazo", "Realizável a longo prazo"],
  ["passivoCirculante", "Passivo circulante"],
  ["passivoNaoCirculante", "Passivo não circulante"],
  ["patrimonioLiquido", "Patrimônio líquido"],
  ["dividaBruta", "Dívida bruta (empréstimos e financiamentos)"],
  ["dividaLiquida", "Dívida líquida"],
];

const CONTAS_RESULTADO: [keyof BaseCalculoIndicadores, string][] = [
  ["receitaLiquida", "Receita líquida"],
  ["custos", "(−) Custos"],
  ["lucroBruto", "Lucro bruto"],
  ["ebit", "EBIT (resultado operacional)"],
  ["depreciacaoAmortizacao", "(+) Depreciação e amortização"],
  ["ebitda", "EBITDA"],
  ["resultadoFinanceiro", "Resultado financeiro"],
  ["impostoRendaContribuicao", "(−) IR e CS"],
  ["lucroLiquido", "Lucro líquido"],
];

function formatarIndice(valor: number | null, formato: FormatoIndice) {
  if (valor === null) return "—";
  switch (formato) {
    case "percentual": return `${numeroContabil.format(valor * 100)}%`;
    case "vezes": return `${numeroContabil.format(valor)}x`;
    case "moeda": return `R$ ${numeroContabil.format(valor)}`;
    default: return numeroContabil.format(valor);
  }
}

interface IndicesFinanceirosProps {
  resultado: ResultadoIndicadores;
  empresaNome: string;
  codColigada: number;
  inicio: string;
  fim: string;
  /** Chave dos filtros aplicados: troca de filtro remonta a DFC (que guarda o que está aberto). */
  chave: string;
}

/**
 * O painel de índices que já existia, sem mudança: os índices como o backend
 * calcula (em cards, por categoria), a base de cálculo e a DFC.
 */
export default function IndicesFinanceiros({ resultado, empresaNome, codColigada, inicio, fim, chave }: IndicesFinanceirosProps) {
  const categorias = useMemo(() => {
    const grupos = new Map<string, ResultadoIndicadores["indices"]>();
    for (const indice of resultado.indices) {
      grupos.set(indice.categoria, [...(grupos.get(indice.categoria) ?? []), indice]);
    }
    return [...grupos.entries()];
  }, [resultado]);

  return (
    <>
      <section className="sp-indicators__section" aria-label="Índices financeiros">
        <div className="sp-indicators__section-head">
          <h2>Índices financeiros</h2>
          <p>{empresaNome} · Coligada {codColigada} · balanço em {ultimoDiaDoMes(fim)} · resultado de {mesAno(inicio)} a {mesAno(fim)}</p>
        </div>
        {categorias.map(([categoria, indices]) => (
          <div key={categoria} className="sp-indicators__category">
            <h3>{categoria}</h3>
            <div className="sp-indicators__cards">
              {indices.map((indice) => (
                <article key={indice.codigo} className="sp-indicators__card">
                  <span className="sp-indicators__card-name">{indice.nome}</span>
                  <strong className={indice.valor !== null && indice.valor < 0 ? "sp-indicators__card-value sp-indicators__number--negative" : "sp-indicators__card-value"}
                    title={indice.valor === null ? "Sem valor: o denominador da fórmula é zero." : undefined}>
                    {formatarIndice(indice.valor, indice.formato)}
                  </strong>
                  <span className="sp-indicators__card-formula">{indice.formula}</span>
                  <p className="sp-indicators__card-hint">{indice.interpretacao}</p>
                </article>
              ))}
            </div>
          </div>
        ))}
      </section>

      <section className="sp-indicators__section" aria-label="Base de cálculo">
        <div className="sp-indicators__section-head">
          <h2>Base de cálculo</h2>
          <p>Valores usados nas fórmulas acima, em R$.</p>
        </div>
        <div className="sp-indicators__base">
          <table className="sp-indicators__table">
            <thead><tr><th>Balanço em {ultimoDiaDoMes(fim)}</th><th className="sp-indicators__number">Valor (R$)</th></tr></thead>
            <tbody>
              {CONTAS_BALANCO.map(([campo, rotulo]) => (
                <tr key={campo}><td>{rotulo}</td><td className={classeNumero(resultado.baseCalculo[campo])}>{formatarValor(resultado.baseCalculo[campo])}</td></tr>
              ))}
            </tbody>
          </table>
          <table className="sp-indicators__table">
            <thead><tr><th>Resultado de {mesAno(inicio)} a {mesAno(fim)}</th><th className="sp-indicators__number">Valor (R$)</th></tr></thead>
            <tbody>
              {CONTAS_RESULTADO.map(([campo, rotulo]) => (
                <tr key={campo}><td>{rotulo}</td><td className={classeNumero(resultado.baseCalculo[campo])}>{formatarValor(resultado.baseCalculo[campo])}</td></tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <DemonstracaoFluxoCaixa key={chave} dfc={resultado.dfc}
        empresaNome={empresaNome} codColigada={codColigada} inicio={inicio} fim={fim} />
    </>
  );
}
