import { useState } from "react";
import type { BaseCalculoIndicadores } from "../../../interfaces/Indicadores";
import { numeroContabil } from "../../../utils/formatacaoContabil";
import { custosZeradosComEstoque } from "../regras";

interface Linha {
  rotulo: string;
  valor: number;
  observacao?: { texto: string; tom: "aviso" | "neutro" };
  destaque?: boolean;
}

/** Valor completo em R$, negativo entre parênteses e zero como "—", como nos demonstrativos. */
function contabil(valor: number) {
  if (valor === 0) return "—";
  return valor < 0 ? `(${numeroContabil.format(-valor)})` : numeroContabil.format(valor);
}

function Coluna({ titulo, linhas }: { titulo: string; linhas: Linha[] }) {
  return (
    <div className="sp-ind-base__coluna">
      <span className="sp-ind-base__titulo">{titulo}</span>
      {linhas.map((linha) => (
        <div key={linha.rotulo} className={linha.destaque ? "sp-ind-base__linha sp-ind-base__linha--destaque" : "sp-ind-base__linha"}>
          <span>
            {linha.rotulo}
            {linha.observacao && <small className={`sp-ind-base__obs sp-ind-base__obs--${linha.observacao.tom}`}>{linha.observacao.texto}</small>}
          </span>
          <span className={linha.valor < 0 && linha.destaque ? "sp-ind-negativo" : undefined}>{contabil(linha.valor)}</span>
        </div>
      ))}
    </div>
  );
}

interface BaseCalculoProps {
  base: BaseCalculoIndicadores;
  dataBalanco: string;
  descricaoPeriodo: string;
}

/** Os valores das fórmulas, recolhidos por padrão. */
export default function BaseCalculo({ base, dataBalanco, descricaoPeriodo }: BaseCalculoProps) {
  const [aberta, setAberta] = useState(false);
  const porDiferenca = { texto: "por diferença", tom: "neutro" } as const;
  // EBIT + financeiro − IR não fecha no lucro quando há equivalência patrimonial (fora do EBIT).
  const outros = base.lucroLiquido + base.impostoRendaContribuicao - (base.ebit + base.resultadoFinanceiro);

  const balanco: Linha[] = [
    { rotulo: "Ativo total", valor: base.ativoTotal },
    { rotulo: "Ativo circulante", valor: base.ativoCirculante },
    { rotulo: "Disponível (caixa e equivalentes)", valor: base.disponivel },
    { rotulo: "Estoques", valor: base.estoques },
    { rotulo: "Realizável a longo prazo", valor: base.realizavelLongoPrazo },
    { rotulo: "Passivo circulante", valor: base.passivoCirculante },
    { rotulo: "Passivo não circulante", valor: base.passivoNaoCirculante },
    { rotulo: "Patrimônio líquido", valor: base.patrimonioLiquido },
    { rotulo: "Dívida bruta (empréstimos e financiamentos)", valor: base.dividaBruta },
    { rotulo: "Dívida líquida", valor: base.dividaLiquida },
  ];
  const resultado: Linha[] = [
    { rotulo: "Receita líquida", valor: base.receitaLiquida },
    {
      rotulo: "(−) Custos", valor: -base.custos,
      observacao: custosZeradosComEstoque(base) ? { texto: "verificar", tom: "aviso" } : undefined,
    },
    { rotulo: "Lucro bruto", valor: base.lucroBruto },
    { rotulo: "(−) Despesas operacionais, exceto D&A", valor: base.ebitda - base.lucroBruto, observacao: porDiferenca },
    { rotulo: "EBITDA", valor: base.ebitda },
    { rotulo: "(−) Depreciação e amortização", valor: -base.depreciacaoAmortizacao },
    { rotulo: "EBIT (resultado operacional)", valor: base.ebit },
    { rotulo: "(+/−) Resultado financeiro", valor: base.resultadoFinanceiro },
    ...(Math.abs(outros) >= 0.005 ? [{ rotulo: "(+/−) Equivalência e outros", valor: outros, observacao: porDiferenca }] : []),
    { rotulo: "(−) IR e CS", valor: -base.impostoRendaContribuicao },
    { rotulo: "Lucro líquido", valor: base.lucroLiquido, destaque: true },
  ];

  return (
    <section className="sp-ind-card sp-ind-base" aria-label="Base de cálculo">
      <button type="button" className="sp-ind-base__alternar" aria-expanded={aberta} onClick={() => setAberta((atual) => !atual)}>
        <span>
          <strong>Base de cálculo</strong>
          <small>Valores usados nas fórmulas, em R$</small>
        </span>
        <span className="sp-ind-base__acao">{aberta ? "Ocultar" : "Mostrar"}</span>
      </button>
      {aberta && (
        <div className="sp-ind-base__colunas">
          <Coluna titulo={`Balanço em ${dataBalanco}`} linhas={balanco} />
          <Coluna titulo={`Resultado de ${descricaoPeriodo}`} linhas={resultado} />
        </div>
      )}
    </section>
  );
}
