import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import Input from "../../componentes/ui/Input";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import StatusBadge from "../../componentes/ui/StatusBadge";
import { extrairMensagemDeErro } from "../../services/api";
import { obterContasDaEmpresa } from "../../services/dashboardService";
import type { StatusFechamentoConta } from "../../interfaces/Dashboard";
import { MESES } from "../../utils/filtrosPainel";
import { SEM_FILTRO, contaPassaNoFiltro } from "./filtroContas";
import type { FiltroContas } from "./filtroContas";

export const ETAPAS: { id: StatusFechamentoConta; rotulo: string; plural: string; tom: "red" | "orange" | "green-light" | "green" }[] = [
  { id: "Pendente", rotulo: "Pendente", plural: "Pendentes", tom: "red" },
  { id: "Preparado", rotulo: "Preparada", plural: "Preparadas", tom: "orange" },
  { id: "Conferido", rotulo: "Conferida", plural: "Conferidas", tom: "green-light" },
  { id: "Fechado", rotulo: "Finalizada", plural: "Finalizadas", tom: "green" },
];

// Uma empresa grande no ano inteiro passa de dezenas de milhares de linhas;
// a tabela mostra as primeiras e a busca e os filtros chegam nas demais.
const LIMITE_DE_LINHAS = 500;

function normalizar(texto: string) {
  return texto.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
}

/** "2025-03" → "Mar/2025". */
function descreverAnoMes(anoMes: string) {
  const [ano, mes] = anoMes.split("-").map(Number);
  return `${MESES[mes - 1] ?? mes}/${ano}`;
}

interface ContasDaEmpresaProps {
  empresa: string;
  /** Período (AAAA-MM, inclusive); com mais de um mês, cada conta aparece uma vez por mês. */
  inicio: string;
  fim: string;
  versao: number;
  /** Só as contas nessas etapas; sem informar, todas. */
  etapas?: StatusFechamentoConta[];
  /** Filtros da gaveta (filial, conta, status). */
  filtro?: FiltroContas;
}

/** Contas da empresa no período com a situação de cada uma — o que abre no "+" da empresa. */
export default function ContasDaEmpresa({ empresa, inicio, fim, versao, etapas, filtro = SEM_FILTRO }: ContasDaEmpresaProps) {
  const [filtroEtapa, setFiltroEtapa] = useState<StatusFechamentoConta | null>(null);
  const [busca, setBusca] = useState("");
  // Mesma chave nas três tabelas: abrir a empresa em outra lista não busca de novo.
  const { data: todas = [], error, isLoading } = useQuery({
    queryKey: ["dashboard-contas", inicio, fim, empresa, versao],
    queryFn: ({ signal }) => obterContasDaEmpresa(inicio, fim, empresa, signal),
  });

  const opcoes = ETAPAS.filter((etapa) => !etapas || etapas.includes(etapa.id));
  const contas = useMemo(() => todas.filter((conta) => (!etapas || etapas.includes(conta.status))
    && contaPassaNoFiltro(conta, filtro)), [todas, etapas, filtro]);
  const contagem = useMemo(() => {
    const total: Record<string, number> = {};
    for (const conta of contas) total[conta.status] = (total[conta.status] ?? 0) + 1;
    return total;
  }, [contas]);
  const variasFiliais = useMemo(() => new Set(contas.map((conta) => conta.codFilial)).size > 1, [contas]);
  // Conta pendente não tem ação registrada; a coluna só aparece se houver outras etapas.
  const mostrarSituacao = opcoes.length > 1;
  const mostrarUltimaAcao = opcoes.some((etapa) => etapa.id !== "Pendente");
  const variosMeses = inicio !== fim;
  const colunas = 2 + Number(variosMeses) + Number(variasFiliais) + Number(mostrarSituacao) + Number(mostrarUltimaAcao);

  const termo = normalizar(busca.trim());
  const visiveis = contas.filter((conta) => (!filtroEtapa || conta.status === filtroEtapa)
    && (!termo || normalizar(`${conta.codConta} ${conta.reduzido} ${conta.descricao}`).includes(termo)));

  if (isLoading) return <div className="sp-dashboard__accounts-message"><LoadingSpinner size={20} /></div>;
  if (error) {
    return <p className="sp-dashboard__accounts-message sp-dashboard__accounts-message--erro" role="alert">
      {extrairMensagemDeErro(error, "Não foi possível carregar as contas.")}
    </p>;
  }

  return (
    <div className="sp-dashboard__accounts">
      <div className="sp-dashboard__accounts-toolbar">
        {mostrarSituacao && (
          <div className="sp-abas" role="tablist" aria-label="Filtrar contas por etapa">
            {[{ id: null, plural: "Todas", total: contas.length }, ...opcoes.map((etapa) => ({ ...etapa, total: contagem[etapa.id] ?? 0 }))]
              .map((opcao) => (
                <button key={opcao.plural} type="button" role="tab" aria-selected={filtroEtapa === opcao.id}
                  className={filtroEtapa === opcao.id ? "sp-abas__botao sp-abas__botao--ativa" : "sp-abas__botao"}
                  onClick={() => setFiltroEtapa(opcao.id)}>
                  {opcao.plural} ({opcao.total})
                </button>
              ))}
          </div>
        )}
        <Input aria-label={`Buscar conta de ${empresa}`} placeholder="Buscar por código ou descrição" value={busca}
          leadingIcon={<Search size={16} />} onChange={(evento) => setBusca(evento.target.value)} />
      </div>

      <div className="sp-dashboard__accounts-table">
        <table>
          <thead>
            <tr>
              <th>Conta</th>
              <th>Descrição</th>
              {variosMeses && <th>Mês</th>}
              {variasFiliais && <th>Filial</th>}
              {mostrarSituacao && <th>Situação</th>}
              {mostrarUltimaAcao && <th>Última ação</th>}
            </tr>
          </thead>
          <tbody>
            {visiveis.slice(0, LIMITE_DE_LINHAS).map((conta) => {
              const etapa = ETAPAS.find((item) => item.id === conta.status) ?? ETAPAS[0];
              return (
                <tr key={`${conta.anoMes}:${conta.codFilial}:${conta.codConta}`}>
                  <td className="sp-dashboard__account-code">{conta.codConta}</td>
                  <td>{conta.descricao || "—"}</td>
                  {variosMeses && <td>{descreverAnoMes(conta.anoMes)}</td>}
                  {variasFiliais && <td>{conta.filial}</td>}
                  {mostrarSituacao && <td><StatusBadge label={etapa.rotulo} tone={etapa.tom} /></td>}
                  {mostrarUltimaAcao && (
                    <td className="sp-dashboard__muted">
                      {conta.ultimaAcaoEm
                        ? `${conta.ultimaAcaoPor ?? "—"} · ${new Date(conta.ultimaAcaoEm).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })}`
                        : "—"}
                    </td>
                  )}
                </tr>
              );
            })}
            {visiveis.length === 0 && (
              <tr><td colSpan={colunas}>Nenhuma conta {filtroEtapa || termo ? "com esse filtro" : "nesta etapa"}.</td></tr>
            )}
            {visiveis.length > LIMITE_DE_LINHAS && (
              <tr><td colSpan={colunas} className="sp-dashboard__muted">
                Mostrando {LIMITE_DE_LINHAS} de {visiveis.length} linhas. Use a busca ou os filtros para encontrar as demais.
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
