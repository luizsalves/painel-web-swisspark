import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileSpreadsheet, FileText, RefreshCw } from "lucide-react";
import DemonstracaoFluxoCaixa from "../../componentes/dfc/DemonstracaoFluxoCaixa";
import RelatorioDfcImpressao from "../../componentes/dfc/RelatorioDfcImpressao";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { carregarIndicadores, exportarDfcExcel } from "../../services/indicadoresService";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { periodoDosFiltros } from "../../utils/filtrosPainel";
import { CAMPOS_FILTRO_DFC } from "./camposFiltro";

/** Aba "Balanço DFC": a DFC do período, na tela, em PDF e em Excel. */
export default function BalancoDfc() {
  const { filtros } = useFiltros();
  const { inicio, fim } = periodoDosFiltros(filtros);
  const [exportandoExcel, setExportandoExcel] = useState(false);
  const [erroExportacao, setErroExportacao] = useState<string | null>(null);

  // Mesma consulta (e mesmo cache) da tela de Indicadores.
  const parametros = filtros.empresa
    ? { inicio, fim, codColigada: coligadaDaEmpresa(filtros.empresa), codFilial: filtros.filial ? Number(filtros.filial) : undefined }
    : null;
  const { data: resultado, error, isFetching, refetch } = useQuery({
    queryKey: ["indicadores", parametros],
    queryFn: () => carregarIndicadores(parametros!),
    enabled: parametros !== null,
  });
  const erro = error ? extrairMensagemDeErro(error, "Não foi possível calcular a DFC.") : null;

  async function gerarExcel() {
    if (!resultado || !parametros || exportandoExcel) return;
    const resultadoParametros = parametros;
    setExportandoExcel(true);
    setErroExportacao(null);
    try {
      const arquivo = await exportarDfcExcel({
        empresa: filtros.empresaNome,
        codColigada: resultadoParametros.codColigada,
        filial: filtros.filial || undefined,
        inicio,
        fim,
        dfc: resultado.dfc,
      });
      const url = URL.createObjectURL(arquivo);
      const link = document.createElement("a");
      link.href = url;
      link.download = `dfc-${inicio}-a-${fim}.xls`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (falha) {
      setErroExportacao(extrairMensagemDeErro(falha, "Não foi possível gerar o Excel."));
    } finally {
      setExportandoExcel(false);
    }
  }

  return (
    <>
      <ResumoFiltros campos={CAMPOS_FILTRO_DFC} obrigatorios={["empresa"]} acoes={parametros && (
        <>
          <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>
            Atualizar
          </Button>
          <Button variant="secondary" icon={<FileText size={16} />} disabled={!resultado} onClick={() => window.print()}>
            Gerar PDF
          </Button>
          <Button variant="secondary" icon={<FileSpreadsheet size={16} />} disabled={!resultado} isLoading={exportandoExcel}
            onClick={() => void gerarExcel()}>
            Gerar Excel
          </Button>
        </>
      )} />

      {erro && <p className="sp-balance__error" role="alert">{erro}</p>}
      {erroExportacao && <p className="sp-balance__error" role="alert">{erroExportacao}</p>}

      {!parametros ? (
        <section className="sp-indicators__empty">
          <h2>Escolha a empresa nos filtros</h2>
          <p>A DFC usa o período dos filtros: saldo de caixa no início, variações no período e saldo no fim.</p>
        </section>
      ) : !resultado ? (
        isFetching && (
          <section className="sp-indicators__empty">
            <LoadingSpinner size={28} />
            <h2>Calculando a DFC…</h2>
            <p>Lendo os saldos de {filtros.empresaNome} nas bases do TOTVS. Pode levar alguns segundos.</p>
          </section>
        )
      ) : (
        <>
          <DemonstracaoFluxoCaixa key={JSON.stringify(parametros)} dfc={resultado.dfc}
            empresaNome={filtros.empresaNome} codColigada={parametros.codColigada} inicio={inicio} fim={fim} />
          <RelatorioDfcImpressao dfc={resultado.dfc} empresaNome={filtros.empresaNome} codColigada={parametros.codColigada}
            filial={filtros.filial} inicio={inicio} fim={fim} />
        </>
      )}
    </>
  );
}
