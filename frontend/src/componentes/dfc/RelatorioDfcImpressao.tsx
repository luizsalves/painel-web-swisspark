import { Fragment } from "react";
import type { Dfc } from "../../interfaces/Indicadores";
import { formatarValor, mesAno, ultimoDiaDoMes, vesperaDoMes } from "../../utils/formatacaoContabil";
import AssinaturasImpressao from "../ui/AssinaturasImpressao";

interface RelatorioDfcImpressaoProps {
  dfc: Dfc;
  empresaNome: string;
  codColigada: number;
  filial: string;
  /** Período no formato AAAA-MM. */
  inicio: string;
  fim: string;
}

/**
 * Versão da DFC para o "Gerar PDF": fica oculta na tela e é a única coisa
 * impressa (ver sp-dfc-print-layout em indicators.css). Sem a composição das
 * linhas — o detalhe por conta vai no Excel.
 */
export default function RelatorioDfcImpressao({ dfc, empresaNome, codColigada, filial, inicio, fim }: RelatorioDfcImpressaoProps) {
  const somaAtividades = dfc.grupos.reduce((total, grupo) => total + grupo.total, 0);
  const temNaoClassificadas = dfc.grupos.some((grupo) => grupo.linhas.some((linha) => linha.naoClassificada));

  return (
    <article className="sp-dfc-print" aria-label="Relatório da DFC para impressão">
      <header className="sp-dfc-print__header">
        <img src="/branding/logo-swiss-park.png" alt="Swiss Park" />
        <div>
          <span>Demonstrativo contábil</span>
          <h1>Demonstração do Fluxo de Caixa</h1>
          <p>Método indireto · 01/{mesAno(inicio)} a {ultimoDiaDoMes(fim)}</p>
        </div>
      </header>

      <div className="sp-dfc-print__identification">
        <div><span>Empresa</span><strong>{empresaNome}</strong></div>
        <div><span>Coligada / Filial</span><strong>{codColigada} / {filial || "Todas"}</strong></div>
      </div>

      <table className="sp-dfc-print__table">
        <thead><tr><th>Descrição</th><th>Valor (R$)</th></tr></thead>
        <tbody>
          {dfc.grupos.map((grupo) => (
            <Fragment key={grupo.nome}>
              <tr className="sp-dfc-print__group"><td colSpan={2}>{grupo.nome}</td></tr>
              {grupo.linhas.map((linha) => (
                <tr key={linha.descricao}>
                  <td>{linha.descricao}</td>
                  <td>{formatarValor(linha.valor)}</td>
                </tr>
              ))}
              <tr className="sp-dfc-print__total">
                <td>Total — {grupo.nome}</td>
                <td>{formatarValor(grupo.total)}</td>
              </tr>
            </Fragment>
          ))}
          <tr className="sp-dfc-print__total sp-dfc-print__total--strong">
            <td>{somaAtividades < 0 ? "Decréscimo" : "Acréscimo"} de caixa do período</td>
            <td>{formatarValor(somaAtividades)}</td>
          </tr>
          <tr>
            <td>Saldo de caixa e equivalentes em {vesperaDoMes(inicio)}</td>
            <td>{formatarValor(dfc.saldoInicialCaixa)}</td>
          </tr>
          <tr>
            <td>Saldo de caixa e equivalentes em {ultimoDiaDoMes(fim)}</td>
            <td>{formatarValor(dfc.saldoFinalCaixa)}</td>
          </tr>
          <tr className="sp-dfc-print__total sp-dfc-print__total--strong">
            <td>Variação de caixa</td>
            <td>{formatarValor(dfc.variacaoCaixa)}</td>
          </tr>
        </tbody>
      </table>

      {temNaoClassificadas && (
        <p className="sp-dfc-print__note">
          &quot;Variações não classificadas&quot; reúne contas que movimentaram no período e ainda não têm linha definida na DFC.
          Elas mantêm o fechamento com o caixa, mas precisam ser classificadas com a contabilidade.
        </p>
      )}
      <AssinaturasImpressao />
      <footer className="sp-dfc-print__footer">
        Emitido em {new Date().toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })} pelo Painel SwissPark.
      </footer>
    </article>
  );
}
