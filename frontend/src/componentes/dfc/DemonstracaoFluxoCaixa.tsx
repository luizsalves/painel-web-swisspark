import { Fragment, useState } from "react";
import { AlertTriangle, ChevronsDownUp, ChevronsUpDown } from "lucide-react";
import type { Dfc } from "../../interfaces/Indicadores";
import {
  classeNumero, formatarValor, mesAno, numeroContabil, ultimoDiaDoMes, vesperaDoMes,
} from "../../utils/formatacaoContabil";

interface DemonstracaoFluxoCaixaProps {
  dfc: Dfc;
  empresaNome: string;
  codColigada: number;
  /** Período no formato AAAA-MM. */
  inicio: string;
  fim: string;
}

/**
 * DFC pelo método indireto. Os grupos começam recolhidos, mostrando só o total;
 * aberto, cada grupo lista suas linhas, e cada linha abre as contas que a compõem.
 */
export default function DemonstracaoFluxoCaixa({ dfc, empresaNome, codColigada, inicio, fim }: DemonstracaoFluxoCaixaProps) {
  const [expandidas, setExpandidas] = useState<Set<string>>(() => new Set());
  const [gruposAbertos, setGruposAbertos] = useState<Set<string>>(() => new Set());
  const todosAbertos = dfc.grupos.every((grupo) => gruposAbertos.has(grupo.nome));

  const somaAtividades = dfc.grupos.reduce((total, grupo) => total + grupo.total, 0);
  const diferencaCaixa = Math.round((dfc.variacaoCaixa - somaAtividades) * 100) / 100;

  function alternar(chave: string, atualizar: (mudar: (atuais: Set<string>) => Set<string>) => void) {
    atualizar((atuais) => {
      const proximas = new Set(atuais);
      if (proximas.has(chave)) proximas.delete(chave);
      else proximas.add(chave);
      return proximas;
    });
  }

  return (
    <section className="sp-indicators__section" aria-label="Demonstração do fluxo de caixa">
      <div className="sp-indicators__section-head sp-indicators__section-head--actions">
        <div>
          <h2>Demonstração do Fluxo de Caixa — método indireto</h2>
          <p>{empresaNome} · Coligada {codColigada} · {mesAno(inicio)} a {mesAno(fim)} · clique em + para abrir os grupos e as contas de cada linha.</p>
        </div>
        <button type="button" className="sp-indicators__toggle-all"
          onClick={() => setGruposAbertos(todosAbertos ? new Set() : new Set(dfc.grupos.map((grupo) => grupo.nome)))}>
          {todosAbertos ? <ChevronsDownUp size={15} /> : <ChevronsUpDown size={15} />}
          {todosAbertos ? "Recolher tudo" : "Expandir tudo"}
        </button>
      </div>
      {diferencaCaixa !== 0 && (
        <p className="sp-indicators__warning" role="alert">
          <AlertTriangle size={16} />
          A soma das atividades difere da variação do caixa em R$ {numeroContabil.format(diferencaCaixa)}. Há lançamentos desbalanceados no período.
        </p>
      )}
      <div className="sp-balance__table-wrap">
        <table className="sp-indicators__table sp-indicators__dfc">
          <thead><tr><th>Descrição</th><th className="sp-indicators__number">Valor (R$)</th></tr></thead>
          <tbody>
            {dfc.grupos.map((grupo) => {
              const grupoAberto = gruposAbertos.has(grupo.nome);
              return (
                <Fragment key={grupo.nome}>
                  <tr className="sp-indicators__dfc-group">
                    <td>
                      <div className="sp-indicators__dfc-label">
                        <button type="button" className="sp-balance__toggle" aria-expanded={grupoAberto}
                          aria-label={`${grupoAberto ? "Recolher" : "Expandir"} ${grupo.nome}`}
                          onClick={() => alternar(grupo.nome, setGruposAbertos)}>
                          {grupoAberto ? "−" : "+"}
                        </button>
                        <span>{grupo.nome}</span>
                      </div>
                    </td>
                    {/* Recolhido, o próprio título mostra o total do grupo. */}
                    <td className={classeNumero(grupo.total)}>{grupoAberto ? "" : formatarValor(grupo.total)}</td>
                  </tr>
                  {grupoAberto && grupo.linhas.map((linha) => {
                    const chave = `${grupo.nome}:${linha.descricao}`;
                    const aberta = expandidas.has(chave);
                    return (
                      <Fragment key={chave}>
                        <tr className={linha.naoClassificada ? "sp-indicators__dfc-line--warning" : undefined}>
                          <td>
                            <div className="sp-indicators__dfc-label sp-indicators__dfc-label--line">
                              {linha.composicao.length > 0 ? (
                                <button type="button" className="sp-balance__toggle" aria-expanded={aberta}
                                  aria-label={`${aberta ? "Recolher" : "Expandir"} contas de ${linha.descricao}`}
                                  onClick={() => alternar(chave, setExpandidas)}>
                                  {aberta ? "−" : "+"}
                                </button>
                              ) : <span className="sp-balance__toggle-placeholder" aria-hidden="true" />}
                              <span>{linha.descricao}</span>
                            </div>
                          </td>
                          <td className={classeNumero(linha.valor)}>{formatarValor(linha.valor)}</td>
                        </tr>
                        {aberta && linha.composicao.map((item) => (
                          <tr key={`${chave}:${item.codConta}:${item.medida}`} className="sp-indicators__dfc-detail">
                            <td>
                              <span className="sp-indicators__dfc-account">{item.codConta}</span>
                              {item.descricao || "—"}
                              <span className="sp-indicators__dfc-measure">{item.medida}</span>
                            </td>
                            <td className={classeNumero(item.valor)}>{formatarValor(item.valor)}</td>
                          </tr>
                        ))}
                      </Fragment>
                    );
                  })}
                  {grupoAberto && (
                    <tr className="sp-indicators__dfc-total">
                      <td>Total — {grupo.nome}</td>
                      <td className={classeNumero(grupo.total)}>{formatarValor(grupo.total)}</td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
            <tr className="sp-indicators__dfc-total sp-indicators__dfc-total--strong">
              <td>{somaAtividades < 0 ? "Decréscimo" : "Acréscimo"} de caixa do período</td>
              <td className={classeNumero(somaAtividades)}>{formatarValor(somaAtividades)}</td>
            </tr>
            <tr>
              <td>Saldo de caixa e equivalentes em {vesperaDoMes(inicio)}</td>
              <td className={classeNumero(dfc.saldoInicialCaixa)}>{formatarValor(dfc.saldoInicialCaixa)}</td>
            </tr>
            <tr>
              <td>Saldo de caixa e equivalentes em {ultimoDiaDoMes(fim)}</td>
              <td className={classeNumero(dfc.saldoFinalCaixa)}>{formatarValor(dfc.saldoFinalCaixa)}</td>
            </tr>
            <tr className="sp-indicators__dfc-total sp-indicators__dfc-total--strong">
              <td>Variação de caixa</td>
              <td className={classeNumero(dfc.variacaoCaixa)}>{formatarValor(dfc.variacaoCaixa)}</td>
            </tr>
          </tbody>
        </table>
      </div>
      {dfc.grupos.some((grupo) => grupo.linhas.some((linha) => linha.naoClassificada)) && (
        <p className="sp-indicators__note">
          <AlertTriangle size={15} />
          &quot;Variações não classificadas&quot; reúne contas que movimentaram no período e ainda não têm linha definida na DFC. Elas mantêm o fechamento com o caixa, mas precisam ser classificadas com a contabilidade.
        </p>
      )}
    </section>
  );
}
