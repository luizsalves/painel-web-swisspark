import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileSpreadsheet, FileText, RefreshCw } from "lucide-react";
import DemonstrativoBalanco from "../../componentes/balanco/DemonstrativoBalanco";
import RelatorioBalancoImpressao from "../../componentes/balanco/RelatorioBalancoImpressao";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { carregarBalanco, exportarBalancoExcel } from "../../services/balancoService";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { periodoDosFiltros } from "../../utils/filtrosPainel";
import { CAMPOS_FILTRO_BALANCO } from "./camposFiltro";

/** Aba "Balanço": o balanço patrimonial no fim do período, na tela, em PDF e em Excel. */
export default function BalancoPatrimonial() {
  const { filtros } = useFiltros();
  // Os filtros são os mesmos da DFC; o balanço é a posição no fim do período.
  const { fim } = periodoDosFiltros(filtros);
  const [exportandoExcel, setExportandoExcel] = useState(false);
  const [erroExportacao, setErroExportacao] = useState<string | null>(null);

  const parametros = filtros.empresa
    ? { mes: fim, codColigada: coligadaDaEmpresa(filtros.empresa), codFilial: filtros.filial ? Number(filtros.filial) : undefined }
    : null;
  const { data: balanco, error, isFetching, refetch } = useQuery({
    queryKey: ["balanco", parametros],
    queryFn: () => carregarBalanco(parametros!),
    enabled: parametros !== null,
  });
  const erro = error ? extrairMensagemDeErro(error, "Não foi possível montar o balanço.") : null;

  async function gerarExcel() {
    if (!balanco || !parametros || exportandoExcel) return;
    setExportandoExcel(true);
    setErroExportacao(null);
    try {
      const arquivo = await exportarBalancoExcel({
        empresa: filtros.empresaNome,
        codColigada: parametros.codColigada,
        filial: filtros.filial || undefined,
        mes: parametros.mes,
        balanco,
      });
      const url = URL.createObjectURL(arquivo);
      const link = document.createElement("a");
      link.href = url;
      link.download = `balanco-${parametros.mes}.xls`;
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
      <ResumoFiltros campos={CAMPOS_FILTRO_BALANCO} obrigatorios={["empresa"]} acoes={parametros && (
        <>
          <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>
            Atualizar
          </Button>
          <Button variant="secondary" icon={<FileText size={16} />} disabled={!balanco} onClick={() => window.print()}>
            Gerar PDF
          </Button>
          <Button variant="secondary" icon={<FileSpreadsheet size={16} />} disabled={!balanco} isLoading={exportandoExcel}
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
          <p>O balanço mostra os saldos no último dia do período dos filtros e o resultado do exercício de janeiro até ele.</p>
        </section>
      ) : !balanco ? (
        isFetching && (
          <section className="sp-indicators__empty">
            <LoadingSpinner size={28} />
            <h2>Montando o balanço…</h2>
            <p>Lendo os saldos de {filtros.empresaNome} nas bases do TOTVS. Pode levar alguns segundos.</p>
          </section>
        )
      ) : (
        <>
          <DemonstrativoBalanco key={JSON.stringify(parametros)} balanco={balanco}
            empresaNome={filtros.empresaNome} codColigada={parametros.codColigada} mes={parametros.mes} />
          <RelatorioBalancoImpressao balanco={balanco} empresaNome={filtros.empresaNome} codColigada={parametros.codColigada}
            filial={filtros.filial} mes={parametros.mes} />
        </>
      )}
    </>
  );
}
