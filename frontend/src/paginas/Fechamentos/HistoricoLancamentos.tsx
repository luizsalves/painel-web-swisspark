import { useState } from "react";
import { useQueries } from "@tanstack/react-query";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import api, { extrairMensagemDeErro } from "../../services/api";
import type { LancamentoTotvs } from "../../interfaces/LancamentoTotvs";
import { MESES } from "../../utils/filtrosPainel";

/** Quantos meses, contando o do fechamento, o histórico alcança: cada mês é uma consulta ao TOTVS. */
const MAXIMO_DE_MESES = 12;
/** Um mês muito movimentado não trava a aba; o mês inteiro está na aba Lançamentos daquele mês. */
const LIMITE_DE_LINHAS_POR_MES = 300;

const formatoMoeda = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatoMesLongo = new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" });

function somarMeses(anoMes: string, meses: number) {
  const [ano, mes] = anoMes.split("-").map(Number);
  const total = ano * 12 + (mes - 1) + meses;
  return `${Math.floor(total / 12)}-${String((total % 12) + 1).padStart(2, "0")}`;
}

function mesesDoPeriodo(inicio: string, fim: string) {
  const meses: string[] = [];
  for (let anoMes = inicio; anoMes <= fim; anoMes = somarMeses(anoMes, 1)) meses.push(anoMes);
  return meses;
}

function descreverMesLongo(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  const texto = formatoMesLongo.format(new Date(ano, mes - 1, 1));
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

function descreverMesCurto(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  return `${MESES[mes - 1]}/${ano}`;
}

interface HistoricoLancamentosProps {
  codColigada: number;
  codFilial: number;
  codConta: string;
  /** Mês do fechamento aberto (AAAA-MM): o histórico termina nele. */
  anoMesFinal: string;
}

/**
 * Partidas da conta mês a mês, de um mês escolhido aqui até o do fechamento.
 * O filtro é só desta aba: não mexe nos filtros da tela nem na aba Lançamentos.
 */
export default function HistoricoLancamentos({ codColigada, codFilial, codConta, anoMesFinal }: HistoricoLancamentosProps) {
  const anoMesMinimo = somarMeses(anoMesFinal, -(MAXIMO_DE_MESES - 1));
  const [inicio, setInicio] = useState(anoMesFinal);
  const [anoInicio, mesInicio] = inicio.split("-").map(Number);
  const anos = [...new Set([Number(anoMesMinimo.slice(0, 4)), Number(anoMesFinal.slice(0, 4))])];

  // Trocar o ano mantém o mês quando ele cabe no intervalo; senão vai para a ponta mais próxima.
  const escolherInicio = (anoMes: string) =>
    setInicio(anoMes < anoMesMinimo ? anoMesMinimo : anoMes > anoMesFinal ? anoMesFinal : anoMes);

  const meses = mesesDoPeriodo(inicio, anoMesFinal);
  // A mesma consulta da aba Lançamentos, um mês por vez — cada mês fica em cache
  // e voltar mais um mês só busca o que falta.
  const consultas = useQueries({
    queries: meses.map((anoMes) => ({
      queryKey: ["lancamentos", codColigada, codFilial, codConta, anoMes],
      queryFn: async ({ signal }: { signal: AbortSignal }) => {
        const { data } = await api.get<LancamentoTotvs[]>("/lancamentos", {
          params: { codColigada, codFilial, anoMes, codConta }, signal,
        });
        return data;
      },
    })),
  });
  const totalDeLancamentos = consultas.reduce(
    (total, consulta) => total + (consulta.data?.filter((linha) => linha.data !== null).length ?? 0), 0);
  const carregando = consultas.some((consulta) => consulta.isPending);

  return (
    <div className="sp-closing__historico">
      <div className="sp-closing__historico-filtros" aria-label="Período do histórico">
        <label className="sp-closing__field">
          <span>Ano</span>
          <select value={anoInicio} onChange={(evento) =>
            escolherInicio(`${evento.target.value}-${String(mesInicio).padStart(2, "0")}`)}>
            {anos.map((ano) => <option key={ano} value={ano}>{ano}</option>)}
          </select>
        </label>
        <label className="sp-closing__field">
          <span>A partir do mês</span>
          <select value={mesInicio} onChange={(evento) =>
            escolherInicio(`${anoInicio}-${String(evento.target.value).padStart(2, "0")}`)}>
            {MESES.map((rotulo, indice) => {
              const anoMes = `${anoInicio}-${String(indice + 1).padStart(2, "0")}`;
              return (
                <option key={rotulo} value={indice + 1} disabled={anoMes < anoMesMinimo || anoMes > anoMesFinal}>
                  {rotulo}
                </option>
              );
            })}
          </select>
        </label>
        <p className="sp-closing__historico-resumo">
          {inicio === anoMesFinal
            ? `Só ${descreverMesCurto(anoMesFinal)}, o mês do fechamento. Escolha um mês anterior para ver mais.`
            : `De ${descreverMesCurto(inicio)} até ${descreverMesCurto(anoMesFinal)} (mês do fechamento)`}
          {!carregando && ` · ${totalDeLancamentos} ${totalDeLancamentos === 1 ? "lançamento" : "lançamentos"}`}
        </p>
      </div>

      {meses.map((anoMes, indice) => {
        const consulta = consultas[indice];
        const linhas = consulta.data ?? [];
        const detalhe = linhas.filter((linha) => linha.data !== null);
        const resumo = linhas.find((linha) => linha.data === null);
        const debitos = detalhe.reduce((total, linha) => total + linha.debitos, 0);
        const creditos = detalhe.reduce((total, linha) => total + linha.creditos, 0);

        return (
          <section key={anoMes} className="sp-closing__historico-mes" aria-label={descreverMesLongo(anoMes)}>
            <header>
              <h3>{descreverMesLongo(anoMes)}{anoMes === anoMesFinal && <small> · mês do fechamento</small>}</h3>
              {consulta.data && (
                <dl>
                  <div><dt>Saldo anterior</dt><dd>{formatoMoeda.format(resumo?.saldoAnterior ?? 0)}</dd></div>
                  <div><dt>Débitos</dt><dd>{formatoMoeda.format(debitos)}</dd></div>
                  <div><dt>Créditos</dt><dd>{formatoMoeda.format(creditos)}</dd></div>
                  <div><dt>Saldo do mês</dt><dd>{formatoMoeda.format(resumo?.saldoRazao ?? 0)}</dd></div>
                </dl>
              )}
            </header>

            {consulta.isPending ? (
              <div className="sp-closing__historico-mensagem"><LoadingSpinner size={20} /></div>
            ) : consulta.error ? (
              <p className="sp-closing__historico-mensagem sp-closing__historico-mensagem--erro" role="alert">
                {extrairMensagemDeErro(consulta.error, "Não foi possível carregar os lançamentos deste mês.")}
              </p>
            ) : detalhe.length === 0 ? (
              <p className="sp-closing__historico-mensagem">Nenhum lançamento neste mês.</p>
            ) : (
              <div className="sp-closing__table-wrap">
                <table className="sp-closing__table">
                  <thead>
                    <tr><th>Data</th><th>IdPartida</th><th>Histórico</th><th>Valor (R$)</th><th>Tipo</th></tr>
                  </thead>
                  <tbody>
                    {detalhe.slice(0, LIMITE_DE_LINHAS_POR_MES).map((linha, posicao) => (
                      <tr key={`${linha.idLancamento ?? "s"}-${linha.idPartida ?? "s"}-${posicao}`}>
                        <td>{linha.data ? new Date(linha.data).toLocaleDateString("pt-BR") : ""}</td>
                        <td>{linha.idPartida ?? ""}</td>
                        <td>{linha.complemento || linha.reduzido || ""}</td>
                        <td>{formatoMoeda.format(linha.debitos !== 0 ? linha.debitos : linha.creditos)}</td>
                        <td>{linha.debitos !== 0 ? "Débito" : "Crédito"}</td>
                      </tr>
                    ))}
                    {detalhe.length > LIMITE_DE_LINHAS_POR_MES && (
                      <tr><td colSpan={5} className="sp-closing__historico-limite">
                        Mostrando {LIMITE_DE_LINHAS_POR_MES} de {detalhe.length} lançamentos deste mês. O mês completo
                        aparece na aba Lançamentos ao escolher {descreverMesCurto(anoMes)} nos filtros.
                      </td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        );
      })}
    </div>
  );
}
