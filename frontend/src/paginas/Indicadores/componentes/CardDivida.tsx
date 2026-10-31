import BotaoAnalisarTati from "../../../componentes/tati/BotaoAnalisarTati";
import { perguntasAnalisar } from "../../../componentes/tati/perguntasAnalisar";
import type { BaseCalculoIndicadores } from "../../../interfaces/Indicadores";
import { escalaDosValores, formatarMoedaAbreviada } from "../regras";
import type { ResultadoIndicador } from "../regras";
import StatusPill from "./StatusPill";

const percentual = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const anos = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** Barra empilhada de 14 px: o que vence em até 12 meses e o que vence depois. */
function BarraVencimento({ curtoPrazo, longoPrazo }: { curtoPrazo: number; longoPrazo: number }) {
  const total = curtoPrazo + longoPrazo;
  if (total <= 0) return <span className="sp-ind-divida__sem-passivo">Sem passivo registrado no fim do período.</span>;
  const fatiaCurta = (curtoPrazo / total) * 100;
  const escala = escalaDosValores([curtoPrazo, longoPrazo]);
  return (
    <div className="sp-ind-vencimento">
      <span className="sp-ind-vencimento__titulo">Quando o passivo total vence</span>
      <div className="sp-ind-vencimento__barra" role="img"
        aria-label={`${percentual.format(fatiaCurta)}% do passivo vence em até 12 meses`}>
        <div className="sp-ind-vencimento__curto" style={{ width: `${fatiaCurta}%` }} />
        <div className="sp-ind-vencimento__longo" style={{ width: `${100 - fatiaCurta}%` }} />
      </div>
      <div className="sp-ind-vencimento__rotulos">
        <span>Até 12 meses · {formatarMoedaAbreviada(curtoPrazo, { escala })} · {percentual.format(fatiaCurta)}%</span>
        <span>Depois · {formatarMoedaAbreviada(longoPrazo, { escala })} · {percentual.format(100 - fatiaCurta)}%</span>
      </div>
    </div>
  );
}

interface CardDividaProps {
  base: BaseCalculoIndicadores;
  multiplo: ResultadoIndicador;
  dataBalanco: string;
}

/** "Quanto a empresa deve": dívida bruta, caixa, dívida líquida, em anos de geração de caixa, e quando vence. */
export default function CardDivida({ base, multiplo, dataBalanco }: CardDividaProps) {
  // As três linhas são uma conta (bruta − caixa = líquida): todas na mesma unidade.
  const escala = escalaDosValores([base.dividaBruta, base.disponivel, base.dividaLiquida]);
  const leitura = base.dividaLiquida <= 0
    ? <span>A empresa tem <strong>{formatarMoedaAbreviada(-base.dividaLiquida)}</strong> a mais em caixa do que em dívidas</span>
    : multiplo.valor === null
      ? <span>A operação não está gerando caixa para pagar a dívida</span>
      : <span>Equivale a <strong>{anos.format(multiplo.valor)} anos</strong> de geração de caixa da operação</span>;

  return (
    <section className="sp-ind-card sp-ind-bloco" aria-label="Dívida">
      <div className="sp-ind-bloco__cabecalho">
        <h2>Quanto a empresa deve<BotaoAnalisarTati assunto="a dívida" pergunta={perguntasAnalisar.divida()} /></h2>
        <span>Posição em {dataBalanco}</span>
      </div>
      <div className="sp-ind-lista">
        <div><span>Empréstimos e financiamentos</span><span>{formatarMoedaAbreviada(base.dividaBruta, { escala })}</span></div>
        <div><span>(−) Caixa</span><span>{formatarMoedaAbreviada(base.disponivel, { escala })}</span></div>
        <div className="sp-ind-lista__total"><span>Dívida líquida</span><span>{formatarMoedaAbreviada(base.dividaLiquida, { escala })}</span></div>
      </div>
      <div className="sp-ind-divida__leitura">
        {leitura}
        <StatusPill status={multiplo.status} />
      </div>
      <BarraVencimento curtoPrazo={base.passivoCirculante} longoPrazo={base.passivoNaoCirculante} />
    </section>
  );
}
