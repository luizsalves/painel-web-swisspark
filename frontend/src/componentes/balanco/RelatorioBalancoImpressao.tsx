import type { Balanco, BalancoLinha } from "../../interfaces/Balanco";
import { formatarValor, numeroContabil, ultimoDiaDoMes, ultimoDiaPorExtenso } from "../../utils/formatacaoContabil";

interface RelatorioBalancoImpressaoProps {
  balanco: Balanco;
  empresaNome: string;
  codColigada: number;
  filial: string;
  /** Mês do balanço, AAAA-MM. */
  mes: string;
}

/**
 * Versão do balanço para o "Gerar PDF": fica oculta na tela e é a única coisa
 * impressa (ver sp-dfc-print-layout em indicators.css). Ativo e passivo lado a
 * lado, sem a composição das linhas — o detalhe por conta vai no Excel.
 */
export default function RelatorioBalancoImpressao({ balanco, empresaNome, codColigada, filial, mes }: RelatorioBalancoImpressaoProps) {
  const diferenca = Math.round((balanco.totalAtivo - balanco.totalPassivo) * 100) / 100;

  return (
    <article className="sp-dfc-print sp-balanco-print" aria-label="Balanço patrimonial para impressão">
      <header className="sp-dfc-print__header">
        <img src="/branding/logo-swiss-park.png" alt="Swiss Park" />
        <div>
          <span>Demonstrativo contábil</span>
          <h1>Balanço Patrimonial</h1>
          <p>{ultimoDiaPorExtenso(mes)} · Em R$</p>
        </div>
      </header>

      <div className="sp-dfc-print__identification">
        <div><span>Empresa</span><strong>{empresaNome}</strong></div>
        <div><span>Coligada / Filial</span><strong>{codColigada} / {filial || "Todas"}</strong></div>
      </div>

      <div className="sp-balanco-print__lados">
        <LadoImpresso titulo="ATIVO" linhas={balanco.ativo} mes={mes} />
        <LadoImpresso titulo="PASSIVO" linhas={balanco.passivo} mes={mes} />
      </div>

      {diferenca !== 0 && (
        <p className="sp-dfc-print__note">
          Ativo e passivo diferem em R$ {numeroContabil.format(diferenca)}. Há lançamentos desbalanceados ou contas fora do ativo e do passivo.
        </p>
      )}
      <footer className="sp-dfc-print__footer">
        Emitido em {new Date().toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })} pelo Painel SwissPark.
      </footer>
    </article>
  );
}

function LadoImpresso({ titulo, linhas, mes }: { titulo: string; linhas: BalancoLinha[]; mes: string }) {
  return (
    <table className="sp-dfc-print__table sp-balanco-print__tabela">
      <thead><tr><th>{titulo}</th><th>{ultimoDiaDoMes(mes)}</th></tr></thead>
      <tbody>
        {linhas.map((linha, indice) => (
          <tr key={indice} className={`sp-balanco-print__${linha.tipo}`}>
            <td>{linha.descricao}</td>
            <td>{formatarValor(linha.valor)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
