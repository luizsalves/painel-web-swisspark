import { Fragment, useState } from "react";
import { AlertTriangle, ChevronsDownUp, ChevronsUpDown } from "lucide-react";
import BotaoAnalisarTati from "../tati/BotaoAnalisarTati";
import { perguntasAnalisar } from "../tati/perguntasAnalisar";
import type { Balanco, BalancoLinha } from "../../interfaces/Balanco";
import { classeNumero, formatarValor, numeroContabil, ultimoDiaDoMes, ultimoDiaPorExtenso } from "../../utils/formatacaoContabil";

interface DemonstrativoBalancoProps {
  balanco: Balanco;
  empresaNome: string;
  codColigada: number;
  /** Mês do balanço, AAAA-MM. */
  mes: string;
}

/**
 * Balanço patrimonial no layout da contabilidade: ativo e passivo lado a lado.
 * Cada linha abre as contas que a compõem.
 */
export default function DemonstrativoBalanco({ balanco, empresaNome, codColigada, mes }: DemonstrativoBalancoProps) {
  const [abertas, setAbertas] = useState<Set<string>>(() => new Set());
  const chavesComContas = [
    ...balanco.ativo.flatMap((linha, indice) => (linha.composicao.length > 0 ? [`ativo:${indice}`] : [])),
    ...balanco.passivo.flatMap((linha, indice) => (linha.composicao.length > 0 ? [`passivo:${indice}`] : [])),
  ];
  const todasAbertas = chavesComContas.length > 0 && chavesComContas.every((chave) => abertas.has(chave));
  const diferenca = Math.round((balanco.totalAtivo - balanco.totalPassivo) * 100) / 100;

  function alternar(chave: string) {
    setAbertas((atuais) => {
      const proximas = new Set(atuais);
      if (proximas.has(chave)) proximas.delete(chave);
      else proximas.add(chave);
      return proximas;
    });
  }

  return (
    <section className="sp-indicators__section" aria-label="Balanço patrimonial">
      <div className="sp-indicators__section-head sp-indicators__section-head--actions">
        <div>
          <h2>Balanço Patrimonial — {ultimoDiaPorExtenso(mes)}<BotaoAnalisarTati assunto="o balanço" pergunta={perguntasAnalisar.balanco()} /></h2>
          <p>{empresaNome} · Coligada {codColigada} · Em R$ · clique em + para ver as contas de cada linha.</p>
        </div>
        <button type="button" className="sp-indicators__toggle-all" disabled={chavesComContas.length === 0}
          onClick={() => setAbertas(todasAbertas ? new Set() : new Set(chavesComContas))}>
          {todasAbertas ? <ChevronsDownUp size={15} /> : <ChevronsUpDown size={15} />}
          {todasAbertas ? "Recolher tudo" : "Expandir tudo"}
        </button>
      </div>
      {diferenca !== 0 && (
        <p className="sp-indicators__warning" role="alert">
          <AlertTriangle size={16} />
          Ativo e passivo diferem em R$ {numeroContabil.format(diferenca)}. Há lançamentos desbalanceados ou contas fora do ativo e do passivo.
        </p>
      )}
      <div className="sp-balanco">
        <LadoBalanco nome="ativo" titulo="ATIVO" linhas={balanco.ativo} mes={mes} abertas={abertas} onAlternar={alternar} />
        <LadoBalanco nome="passivo" titulo="PASSIVO" linhas={balanco.passivo} mes={mes} abertas={abertas} onAlternar={alternar} />
      </div>
    </section>
  );
}

interface LadoBalancoProps {
  nome: string;
  titulo: string;
  linhas: BalancoLinha[];
  mes: string;
  abertas: Set<string>;
  onAlternar: (chave: string) => void;
}

function LadoBalanco({ nome, titulo, linhas, mes, abertas, onAlternar }: LadoBalancoProps) {
  return (
    <div className="sp-balance__table-wrap">
      <table className="sp-indicators__table sp-balanco__tabela">
        <thead><tr><th>{titulo}<BotaoAnalisarTati assunto={`o ${nome}`} pergunta={perguntasAnalisar.ladoDoBalanco(nome)} /></th><th className="sp-indicators__number">{ultimoDiaDoMes(mes)}</th></tr></thead>
        <tbody>
          {linhas.map((linha, indice) => {
            const chave = `${nome}:${indice}`;
            const aberta = abertas.has(chave);
            return (
              <Fragment key={chave}>
                <tr className={`sp-balanco__${linha.tipo}`}>
                  <td>
                    {linha.tipo === "linha" ? (
                      <div className="sp-indicators__dfc-label">
                        {linha.composicao.length > 0 ? (
                          <button type="button" className="sp-balance__toggle" aria-expanded={aberta}
                            aria-label={`${aberta ? "Recolher" : "Expandir"} contas de ${linha.descricao}`}
                            onClick={() => onAlternar(chave)}>
                            {aberta ? "−" : "+"}
                          </button>
                        ) : <span className="sp-balance__toggle-placeholder" aria-hidden="true" />}
                        <span>{linha.descricao}</span>
                      </div>
                    ) : linha.descricao}
                  </td>
                  <td className={classeNumero(linha.valor)}>{formatarValor(linha.valor)}</td>
                </tr>
                {aberta && linha.composicao.map((item, posicao) => (
                  <tr key={`${chave}:${posicao}`} className="sp-indicators__dfc-detail sp-balanco__conta">
                    <td>
                      {item.codConta && <span className="sp-indicators__dfc-account">{item.codConta}</span>}
                      {item.descricao || "—"}
                    </td>
                    <td className={classeNumero(item.valor)}>{formatarValor(item.valor)}</td>
                  </tr>
                ))}
              </Fragment>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
