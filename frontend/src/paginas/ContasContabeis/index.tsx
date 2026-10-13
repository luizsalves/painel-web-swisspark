import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import AppLayout from "../../componentes/layout/AppLayout";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { alterarStatusContaContabil, listarContasContabeis, listarEmpresasContabeis } from "../../services/contasContabeisService";
import type { ContaContabilTotvs } from "../../interfaces/ContaContabil";
import type { CampoFiltro } from "../../utils/filtrosPainel";

const CAMPOS_FILTRO: CampoFiltro[] = ["empresa", "filial", "conta"];

function ordenarPorConta(contas: ContaContabilTotvs[]): ContaContabilTotvs[] {
  return [...contas].sort(
    (a, b) => a.codColigada - b.codColigada || a.codConta.localeCompare(b.codConta, "pt-BR")
  );
}

export default function ContasContabeis() {
  const { filtros } = useFiltros();
  const queryClient = useQueryClient();
  const codColigada = filtros.empresa ? coligadaDaEmpresa(filtros.empresa) : undefined;
  const codFilial = filtros.filial ? Number(filtros.filial) : undefined;
  const chaveLista = ["contas-contabeis", codColigada, codFilial];

  const { data: todasAsContas = [], error, isFetching, isPending, refetch } = useQuery({
    queryKey: chaveLista,
    queryFn: async () => ordenarPorConta(await listarContasContabeis({ codColigada, codFilial })),
  });
  // Os nomes identificam as empresas na coluna "Empresa".
  const { data: empresas = [] } = useQuery({
    queryKey: ["contas-contabeis-empresas"],
    queryFn: listarEmpresasContabeis,
  });
  const erroDaLista = error ? extrairMensagemDeErro(error, "Erro ao carregar as contas contábeis.") : null;

  const contas = useMemo(
    () => todasAsContas.filter((conta) => !filtros.conta || conta.codConta === filtros.conta),
    [todasAsContas, filtros.conta]
  );
  const empresasPorColigada = useMemo(() => {
    return new Map([...empresas].reverse().map((empresa) => [empresa.codColigada, empresa.nome]));
  }, [empresas]);

  const [alvoDaConfirmacao, setAlvoDaConfirmacao] = useState<ContaContabilTotvs | null>(null);
  const [erroDaConfirmacao, setErroDaConfirmacao] = useState<string | null>(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  function guardarNaLista(salva: ContaContabilTotvs) {
    queryClient.setQueryData<ContaContabilTotvs[]>(chaveLista, (atuais) =>
      atuais && ordenarPorConta(atuais.map((conta) => (conta.id === salva.id ? salva : conta))));
  }

  async function confirmarAlteracaoDeStatus() {
    if (!alvoDaConfirmacao) return;

    setAlterandoStatus(true);
    setErroDaConfirmacao(null);

    try {
      const salva = await alterarStatusContaContabil(
        alvoDaConfirmacao.codColigada,
        alvoDaConfirmacao.codConta,
        !alvoDaConfirmacao.ativo
      );
      guardarNaLista(salva);
      setAlvoDaConfirmacao(null);
    } catch (erro) {
      setErroDaConfirmacao(extrairMensagemDeErro(erro, "Não foi possível alterar o status da conta contábil."));
    } finally {
      setAlterandoStatus(false);
    }
  }

  const colunas: TableColumn<ContaContabilTotvs>[] = [
    { key: "coligada", header: "Coligada", render: (row) => row.codColigada },
    { key: "codConta", header: "Código", render: (row) => row.codConta },
    { key: "reduzido", header: "Reduzido", render: (row) => row.reduzido || "—" },
    {
      key: "empresa",
      header: "Empresa",
      render: (row) => {
        return empresasPorColigada.get(row.codColigada) ?? "—";
      },
    },
    {
      key: "filial",
      header: "Filial",
      render: (row) => {
        return row.codFiliais
          .filter((codigo) => codFilial === undefined || codigo === codFilial)
          .join(", ") || "—";
      },
    },
    { key: "descricao", header: "Descrição da conta", render: (row) => row.descricao || "—" },
    {
      key: "status",
      header: "Status",
      render: (row) => (
        <StatusBadge label={row.ativo ? "Ativa" : "Inativa"} tone={row.ativo ? "green" : "neutral"} />
      ),
    },
    {
      key: "acoes",
      header: "Ações",
      align: "right",
      render: (row) => (
        <Button
          type="button"
          variant={row.ativo ? "danger" : "secondary"}
          onClick={() => {
            setErroDaConfirmacao(null);
            setAlvoDaConfirmacao(row);
          }}
        >
          {row.ativo ? "Desativar" : "Ativar"}
        </Button>
      ),
    },
  ];

  return (
    <AppLayout>
      <AppHeader
        title="Contas Contábeis"
        subtitle="Consulte as contas com lançamentos e ative ou desative contas para a conciliação"
        camposFiltro={CAMPOS_FILTRO}
      />

      <main className="sp-management">
        <ResumoFiltros campos={CAMPOS_FILTRO} acoes={(
          <>
            {!isPending && <span className="sp-management__count">{contas.length} {contas.length === 1 ? "conta" : "contas"}</span>}
            <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>
              Atualizar
            </Button>
          </>
        )} />

        {erroDaLista && (
          <p className="sp-usuarios__erro" role="alert">
            {erroDaLista}
          </p>
        )}

        <section className="sp-management__table" aria-label="Lista de contas contábeis">
          {isPending ? (
            <div className="sp-usuarios__carregando">
              <LoadingSpinner size={28} />
            </div>
          ) : (
            <Table
              columns={colunas}
              data={contas}
              getRowId={(row) => row.id}
              emptyMessage="Nenhuma conta contábil encontrada."
            />
          )}
        </section>
      </main>

      <Modal
        isOpen={alvoDaConfirmacao !== null}
        onClose={() => setAlvoDaConfirmacao(null)}
        title={alvoDaConfirmacao?.ativo ? "Desativar conta contábil" : "Reativar conta contábil"}
        description={
          alvoDaConfirmacao?.ativo
            ? `A conta ${alvoDaConfirmacao?.codConta} deixa de aparecer no combobox de Conta Contábil da tela de Fechamentos.`
            : `A conta ${alvoDaConfirmacao?.codConta} volta a aparecer no combobox de Conta Contábil da tela de Fechamentos.`
        }
      >
        <div className="sp-usuarios__form">
          {erroDaConfirmacao && (
            <p className="sp-usuarios__erro" role="alert">
              {erroDaConfirmacao}
            </p>
          )}

          <div className="sp-usuarios__form-acoes">
            <Button type="button" variant="secondary" onClick={() => setAlvoDaConfirmacao(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant={alvoDaConfirmacao?.ativo ? "danger" : "primary"}
              isLoading={alterandoStatus}
              onClick={() => void confirmarAlteracaoDeStatus()}
            >
              {alvoDaConfirmacao?.ativo ? "Desativar" : "Ativar"}
            </Button>
          </div>
        </div>
      </Modal>
    </AppLayout>
  );
}
