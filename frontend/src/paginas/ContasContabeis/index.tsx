import { useCallback, useEffect, useMemo, useState } from "react";
import { Search } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import AppLayout from "../../componentes/layout/AppLayout";
import Button from "../../componentes/ui/Button";
import Input from "../../componentes/ui/Input";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import { extrairMensagemDeErro } from "../../services/api";
import { alterarStatusContaContabil, listarContasContabeis } from "../../services/contasContabeisService";
import { listarEmpresas } from "../../services/empresasService";
import type { ContaContabilTotvs } from "../../interfaces/ContaContabil";
import type { Empresa } from "../../interfaces/Empresa";

/** CODCOLIGADA 0 no TOTVS é o cadastro global/compartilhado — não é uma empresa de verdade. */
const COLIGADA_GLOBAL = 0;

function ordenarPorConta(contas: ContaContabilTotvs[]): ContaContabilTotvs[] {
  return [...contas].sort(
    (a, b) => a.codColigada - b.codColigada || a.codConta.localeCompare(b.codConta, "pt-BR")
  );
}

export default function ContasContabeis() {
  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erroDaLista, setErroDaLista] = useState<string | null>(null);
  const [busca, setBusca] = useState("");

  const [empresas, setEmpresas] = useState<Empresa[]>([]);

  useEffect(() => {
    listarEmpresas()
      .then(setEmpresas)
      .catch(() => setEmpresas([]));
  }, []);

  // Uma coligada pode ter várias filiais — junta todas as filiais (e o nome,
  // que é o mesmo pra todas elas) por coligada, pra exibir na tabela.
  const empresasPorColigada = useMemo(() => {
    const mapa = new Map<number, Empresa[]>();
    for (const empresa of empresas) {
      const lista = mapa.get(empresa.codColigada) ?? [];
      lista.push(empresa);
      mapa.set(empresa.codColigada, lista);
    }
    return mapa;
  }, [empresas]);

  const [alvoDaConfirmacao, setAlvoDaConfirmacao] = useState<ContaContabilTotvs | null>(null);
  const [erroDaConfirmacao, setErroDaConfirmacao] = useState<string | null>(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  const carregarLista = useCallback(async () => {
    setCarregando(true);
    setErroDaLista(null);

    try {
      const lista = await listarContasContabeis();
      setContas(ordenarPorConta(lista));
    } catch (erro) {
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar as contas contábeis."));
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregarLista();
  }, [carregarLista]);

  const listaFiltrada = useMemo(() => {
    const termo = busca.trim().toLocaleLowerCase("pt-BR");
    if (!termo) return contas;

    return contas.filter(
      (conta) =>
        conta.codConta.toLocaleLowerCase("pt-BR").includes(termo) ||
        conta.reduzido.toLocaleLowerCase("pt-BR").includes(termo) ||
        conta.descricao.toLocaleLowerCase("pt-BR").includes(termo)
    );
  }, [contas, busca]);

  function guardarNaLista(salva: ContaContabilTotvs) {
    setContas((atuais) => ordenarPorConta(atuais.map((conta) => (conta.id === salva.id ? salva : conta))));
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
    {
      key: "empresa",
      header: "Empresa",
      render: (row) => {
        if (row.codColigada === COLIGADA_GLOBAL) return "Global (todas as empresas)";
        const filiais = empresasPorColigada.get(row.codColigada);
        return filiais?.[0]?.nome ?? "—";
      },
    },
    {
      key: "filial",
      header: "Filial",
      render: (row) => {
        if (row.codColigada === COLIGADA_GLOBAL) return "—";
        const filiais = empresasPorColigada.get(row.codColigada);
        if (!filiais || filiais.length === 0) return "—";
        return filiais.map((filial) => `${filial.codFilial} - ${filial.nomeFantasia}`).join(", ");
      },
    },
    { key: "reduzido", header: "Reduzido", render: (row) => row.reduzido },
    { key: "descricao", header: "Descrição", render: (row) => row.descricao },
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
        subtitle="Consulte o plano de contas e ative ou desative contas para a conciliação"
      />

      <main className="sp-management">
        <div className="sp-management__toolbar">
          <div className="sp-management__search">
            <Input
              aria-label="Buscar contas contábeis"
              placeholder="Buscar por código, reduzido ou descrição"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              leadingIcon={<Search size={18} />}
            />
          </div>
          <span className="sp-management__count">
            {listaFiltrada.length} {listaFiltrada.length === 1 ? "conta" : "contas"}
          </span>
        </div>

        {erroDaLista && (
          <p className="sp-usuarios__erro" role="alert">
            {erroDaLista}
          </p>
        )}

        <section className="sp-management__table" aria-label="Lista de contas contábeis">
          {carregando ? (
            <div className="sp-usuarios__carregando">
              <LoadingSpinner size={28} />
            </div>
          ) : (
            <Table
              columns={colunas}
              data={listaFiltrada}
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
