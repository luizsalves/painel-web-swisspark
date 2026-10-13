import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import DemonstracaoFluxoCaixa from "../../componentes/dfc/DemonstracaoFluxoCaixa";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useFiltros } from "../../contexts/FiltrosContext";
import type { BaseCalculoIndicadores, FormatoIndice, Indicadores as ResultadoIndicadores } from "../../interfaces/Indicadores";
import { extrairMensagemDeErro } from "../../services/api";
import { carregarIndicadores } from "../../services/indicadoresService";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { periodoDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro } from "../../utils/filtrosPainel";
import { classeNumero, formatarValor, mesAno, numeroContabil, ultimoDiaDoMes } from "../../utils/formatacaoContabil";

// Índices e DFC precisam de todas as contas, então o filtro de conta não se aplica aqui.
const CAMPOS_FILTRO: CampoFiltro[] = ["empresa", "filial", "periodo"];

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

export default function Indicadores() {
  const { filtros } = useFiltros();
  const { inicio, fim } = periodoDosFiltros(filtros);

  // Os filtros vêm da gaveta global; cada combinação aplicada vira uma consulta
  // (e fica em cache por um minuto se a pessoa voltar a ela).
  const parametros = filtros.empresa
    ? { inicio, fim, codColigada: coligadaDaEmpresa(filtros.empresa), codFilial: filtros.filial ? Number(filtros.filial) : undefined }
    : null;
  const { data: resultado, error, isFetching, refetch } = useQuery({
    queryKey: ["indicadores", parametros],
    queryFn: () => carregarIndicadores(parametros!),
    enabled: parametros !== null,
  });
  const erro = error ? extrairMensagemDeErro(error, "Não foi possível calcular os indicadores.") : null;

  const categorias = useMemo(() => {
    const grupos = new Map<string, ResultadoIndicadores["indices"]>();
    for (const indice of resultado?.indices ?? []) {
      grupos.set(indice.categoria, [...(grupos.get(indice.categoria) ?? []), indice]);
    }
    return [...grupos.entries()];
  }, [resultado]);

  return (
    <AppLayout>
      <AppHeader title="Indicadores" subtitle="Índices financeiros e fluxo de caixa calculados a partir dos saldos contábeis do TOTVS." camposFiltro={CAMPOS_FILTRO} />
      <main className="sp-indicators">
        <ResumoFiltros campos={CAMPOS_FILTRO} obrigatorios={["empresa"]} acoes={parametros && (
          <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>
            Atualizar
          </Button>
        )} />

        {erro && <p className="sp-balance__error" role="alert">{erro}</p>}

        {!parametros ? (
          <section className="sp-indicators__empty">
            <h2>Escolha a empresa nos filtros</h2>
            <p>O balanço considera o último dia do mês final; o resultado e o fluxo de caixa, o período todo.</p>
          </section>
        ) : !resultado ? (
          isFetching && (
            <section className="sp-indicators__empty">
              <LoadingSpinner size={28} />
              <h2>Calculando indicadores…</h2>
              <p>Lendo os saldos de {filtros.empresaNome} nas bases do TOTVS. Pode levar alguns segundos.</p>
            </section>
          )
        ) : (
          <>
            <section className="sp-indicators__section" aria-label="Índices financeiros">
              <div className="sp-indicators__section-head">
                <h2>Índices financeiros</h2>
                <p>{filtros.empresaNome} · Coligada {parametros.codColigada} · balanço em {ultimoDiaDoMes(fim)} · resultado de {mesAno(inicio)} a {mesAno(fim)}</p>
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

            <DemonstracaoFluxoCaixa key={JSON.stringify(parametros)} dfc={resultado.dfc}
              empresaNome={filtros.empresaNome} codColigada={parametros.codColigada} inicio={inicio} fim={fim} />
          </>
        )}
      </main>
    </AppLayout>
  );
}
