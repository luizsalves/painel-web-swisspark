import { useCallback, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { RefreshCw } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { carregarIndicadores } from "../../services/indicadoresService";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { periodoDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro } from "../../utils/filtrosPainel";
import { ultimoDiaDoMes } from "../../utils/formatacaoContabil";
import { lerDetalhe } from "./ancoras";
import type { Detalhe } from "./ancoras";
import AbasIndicadores from "./componentes/AbasIndicadores";
import IndicesFinanceiros from "./componentes/IndicesFinanceiros";
import VisaoContador from "./componentes/VisaoContador";
import VisaoDiretoria from "./componentes/VisaoDiretoria";
import { descreverPeriodo, mesesDoPeriodo, mesmoPeriodoDoAnoAnterior, periodoAnterior, rotuloPeriodo } from "./periodo";
import { calcularIndicadores, totaisDaDfc } from "./regras";
import type { DadosPainel } from "./regras";
import { lerVisao } from "./visoes";
import type { Visao } from "./visoes";

// Índices e DFC precisam de todas as contas, então o filtro de conta não se aplica aqui.
const CAMPOS_FILTRO: CampoFiltro[] = ["empresa", "filial", "periodo"];

const TITULO: Record<Visao, string> = {
  indices: "Indicadores",
  contador: "Indicadores financeiros",
  diretoria: "Saúde da empresa",
};

const SUBTITULO_PADRAO = "Índices financeiros e fluxo de caixa calculados a partir dos saldos contábeis do TOTVS.";

const OPCOES_COMPARACAO = [
  { valor: "anterior-e-ano-anterior", rotulo: "Período anterior e mesmo período do ano anterior" },
  { valor: "anterior", rotulo: "Só o período anterior" },
  { valor: "acumulado-ano-anterior", rotulo: "Acumulado do ano anterior" },
];

export default function Indicadores() {
  const { filtros } = useFiltros();
  const { inicio, fim } = periodoDosFiltros(filtros);
  const [parametrosUrl, setParametrosUrl] = useSearchParams();
  const visao = lerVisao(parametrosUrl.get("visao"));
  const detalhe = lerDetalhe(parametrosUrl.get("detalhe"));
  // Fase 1: a escolha já existe, mas as comparações só chegam com o histórico.
  const [comparacao, setComparacao] = useState(OPCOES_COMPARACAO[0].valor);

  const trocarVisao = useCallback((proxima: Visao, ancora?: Detalhe) => {
    setParametrosUrl((atuais) => {
      const novos = new URLSearchParams(atuais);
      novos.set("visao", proxima);
      if (ancora) novos.set("detalhe", ancora);
      else novos.delete("detalhe");
      return novos;
    });
  }, [setParametrosUrl]);
  // Depois de rolar até o grupo, o "detalhe" sai da URL para não rolar de novo.
  const detalheVisto = useCallback(() => {
    setParametrosUrl((atuais) => {
      const novos = new URLSearchParams(atuais);
      novos.delete("detalhe");
      return novos;
    }, { replace: true });
  }, [setParametrosUrl]);

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

  const meses = mesesDoPeriodo(inicio, fim);
  const dados: DadosPainel | null = resultado
    ? { base: resultado.baseCalculo, indicadores: calcularIndicadores(resultado.baseCalculo, meses), dfc: totaisDaDfc(resultado.dfc), meses }
    : null;

  const descricao = descreverPeriodo(inicio, fim);
  const anterior = periodoAnterior(inicio, fim);
  const anoAnterior = mesmoPeriodoDoAnoAnterior(inicio, fim);
  const rotulos = {
    atual: rotuloPeriodo(inicio, fim),
    anterior: rotuloPeriodo(anterior.inicio, anterior.fim),
    anoAnterior: rotuloPeriodo(anoAnterior.inicio, anoAnterior.fim),
  };
  const subtitulo = !parametros || visao === "indices"
    ? SUBTITULO_PADRAO
    : visao === "contador"
      ? `${filtros.empresaNome} · Coligada ${parametros.codColigada} · balanço em ${ultimoDiaDoMes(fim)} · resultado de ${descricao}`
      : `${filtros.empresaNome} · fechamento de ${descricao}`;

  return (
    <AppLayout>
      <AppHeader title={TITULO[visao]} subtitle={subtitulo} camposFiltro={CAMPOS_FILTRO} />
      <main className="sp-indicators">
        <ResumoFiltros campos={CAMPOS_FILTRO} obrigatorios={["empresa"]} acoes={parametros && (
          <>
            {visao === "contador" && (
              <label className="sp-ind-comparar">
                <span>Comparar com</span>
                <select value={comparacao} onChange={(evento) => setComparacao(evento.target.value)}>
                  {OPCOES_COMPARACAO.map((opcao) => <option key={opcao.valor} value={opcao.valor}>{opcao.rotulo}</option>)}
                </select>
              </label>
            )}
            <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>
              Atualizar
            </Button>
          </>
        )} />

        <AbasIndicadores ativa={visao} onTrocar={(proxima) => trocarVisao(proxima)} />

        {erro && <p className="sp-balance__error" role="alert">{erro}</p>}

        {!parametros ? (
          <section className="sp-indicators__empty">
            <h2>Escolha a empresa nos filtros</h2>
            <p>O balanço considera o último dia do mês final; o resultado e o fluxo de caixa, o período todo.</p>
          </section>
        ) : !resultado || !dados ? (
          isFetching && (
            <section className="sp-indicators__empty">
              <LoadingSpinner size={28} />
              <h2>Calculando indicadores…</h2>
              <p>Lendo os saldos de {filtros.empresaNome} nas bases do TOTVS. Pode levar alguns segundos.</p>
            </section>
          )
        ) : visao === "indices" ? (
          <IndicesFinanceiros resultado={resultado} empresaNome={filtros.empresaNome} codColigada={parametros.codColigada}
            inicio={inicio} fim={fim} chave={JSON.stringify(parametros)} />
        ) : (
          <>
            <div className="sp-ind-aviso">
              <strong>Dados</strong>
              <span>
                {visao === "contador"
                  ? `Os valores de ${descricao} são os reais do sistema. Comparações com períodos anteriores e minigráficos aparecem quando o histórico estiver disponível.`
                  : `Os números de ${descricao} são reais. Comparações com o ano anterior e a tendência de 12 meses aparecem quando o histórico estiver disponível.`}
              </span>
            </div>
            {visao === "contador" ? (
              <VisaoContador dados={dados} dfc={resultado.dfc} inicio={inicio} fim={fim} empresaNome={filtros.empresaNome}
                codColigada={parametros.codColigada} rotulos={rotulos} detalhe={detalhe} onDetalheVisto={detalheVisto} />
            ) : (
              <VisaoDiretoria dados={dados} inicio={inicio} fim={fim} onVerDetalhe={(ancora) => trocarVisao("contador", ancora)} />
            )}
          </>
        )}
      </main>
    </AppLayout>
  );
}
