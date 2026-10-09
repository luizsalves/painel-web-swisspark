import { chaveEmpresaContabil, coligadaDaEmpresa } from "../../utils/empresaContabil";
import { useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import AppLayout from "../../componentes/layout/AppLayout";
import Button from "../../componentes/ui/Button";
import Select from "../../componentes/ui/Select";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import { extrairMensagemDeErro } from "../../services/api";
import { alterarStatusContaContabil, listarColigadasFiliaisContabeis, listarContasContabeis, listarEmpresasContabeis } from "../../services/contasContabeisService";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../../interfaces/ContaContabil";


function ordenarPorConta(contas: ContaContabilTotvs[]): ContaContabilTotvs[] {
  return [...contas].sort(
    (a, b) => a.codColigada - b.codColigada || a.codConta.localeCompare(b.codConta, "pt-BR")
  );
}

export default function ContasContabeis() {
  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [carregando, setCarregando] = useState(false);
  const [opcoesContas, setOpcoesContas] = useState<ContaContabilTotvs[]>([]);
  const [carregandoOpcoes, setCarregandoOpcoes] = useState(false);
  const [opcoesCarregadas, setOpcoesCarregadas] = useState(false);
  const [consultou, setConsultou] = useState(false);
  const [erroDaLista, setErroDaLista] = useState<string | null>(null);

  const [empresas, setEmpresas] = useState<EmpresaContabil[]>([]);
  const [coligadasFiliais, setColigadasFiliais] = useState<ColigadaFilialContabil[]>([]);
  const [carregandoEmpresas, setCarregandoEmpresas] = useState(true);
  const [carregandoFiliais, setCarregandoFiliais] = useState(false);

  // null = ainda não escolhido; "" = opção Todos escolhida explicitamente.
  const [empresaSelecionada, setEmpresaSelecionada] = useState<string | null>(null);
  const [filialSelecionada, setFilialSelecionada] = useState<string | null>(null);
  const [contaSelecionada, setContaSelecionada] = useState("");

  useEffect(() => {
    let cancelado = false;
    // Ao abrir a tela, apenas os nomes das empresas são consultados.
    listarEmpresasContabeis().then((lista) => {
      if (cancelado) return;
      setEmpresas(lista);
    }).catch((erro) => {
      if (cancelado) return;
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar as empresas."));
    }).finally(() => { if (!cancelado) setCarregandoEmpresas(false); });
    return () => { cancelado = true; };
  }, []);

  // Os nomes identificam as empresas; os códigos das filiais vêm da CPARTIDA.
  const empresasPorColigada = useMemo(() => {
    return new Map([...empresas].reverse().map((empresa) => [empresa.codColigada, empresa.nome]));
  }, [empresas]);

  const filiais = useMemo(() =>
    [...new Set(coligadasFiliais.map((linha) => linha.codFilial))].sort((a, b) => a - b),
    [coligadasFiliais]
  );
  const opcoesContasUnicas = useMemo(() =>
    [...new Map(opcoesContas.map((conta) => [conta.codConta, conta])).values()],
    [opcoesContas]
  );

  useEffect(() => {
    if (empresaSelecionada === null) return;
    let cancelado = false;
    setCarregandoFiliais(true);
    listarColigadasFiliaisContabeis(empresaSelecionada === "" ? undefined : coligadaDaEmpresa(empresaSelecionada))
      .then((lista) => { if (!cancelado) setColigadasFiliais(lista); })
      .catch((erro) => { if (!cancelado) setErroDaLista(extrairMensagemDeErro(erro, "Erro ao carregar as filiais.")); })
      .finally(() => { if (!cancelado) setCarregandoFiliais(false); });
    return () => { cancelado = true; };
  }, [empresaSelecionada]);

  const [alvoDaConfirmacao, setAlvoDaConfirmacao] = useState<ContaContabilTotvs | null>(null);
  const [erroDaConfirmacao, setErroDaConfirmacao] = useState<string | null>(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  // Contas só são consultadas depois da escolha da empresa e da filial.
  useEffect(() => {
    if (empresaSelecionada === null || filialSelecionada === null) return;
    let cancelado = false;
    setCarregandoOpcoes(true);
    setOpcoesCarregadas(false);
    listarContasContabeis({
      codColigada: empresaSelecionada === "" ? undefined : coligadaDaEmpresa(empresaSelecionada),
      codFilial: filialSelecionada === "" ? undefined : Number(filialSelecionada),
    })
      .then((lista) => {
        if (!cancelado) {
          setOpcoesContas(ordenarPorConta(lista));
          setOpcoesCarregadas(true);
        }
      })
      .catch((erro) => {
        if (!cancelado) setErroDaLista(extrairMensagemDeErro(erro, "Erro ao carregar as opcoes de contas."));
      })
      .finally(() => { if (!cancelado) setCarregandoOpcoes(false); });
    return () => { cancelado = true; };
  }, [empresaSelecionada, filialSelecionada]);

  async function carregarContas() {
    if (empresaSelecionada === null || filialSelecionada === null || carregando || !opcoesCarregadas) return;
    setCarregando(true);
    setConsultou(true);
    setErroDaLista(null);
    setContas([]);
    try {
      setContas(ordenarPorConta(opcoesContas.filter((conta) =>
        (!contaSelecionada || conta.codConta === contaSelecionada)
      )));
    } catch (erro) {
      setErroDaLista(extrairMensagemDeErro(erro, "Erro ao carregar as contas contabeis."));
    } finally {
      setCarregando(false);
    }
  }

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
          .filter((codigo) => !filialSelecionada || codigo === Number(filialSelecionada))
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
      />

      <main className="sp-management">
        <div className="sp-contas__filtros">
          <Select
            id="filtro-empresa" label="Empresa" value={empresaSelecionada ?? "__pendente__"}
            disabled={carregando || carregandoEmpresas}
            onChange={(event) => {
              setEmpresaSelecionada(event.target.value);
              setFilialSelecionada(null);
              setColigadasFiliais([]);
              setOpcoesContas([]);
              setOpcoesCarregadas(false);
              setContaSelecionada("");
              setContas([]);
              setConsultou(false);
              setErroDaLista(null);
            }}
            options={[
              ...(empresaSelecionada === null ? [{ value: "__pendente__", label: "Selecione uma empresa" }] : []),
              { value: "", label: "Todas as empresas" },
              ...empresas.map((empresa) => ({ value: chaveEmpresaContabil(empresa), label: empresa.nome })),
            ]}
          />
          <p className="sp-contas__coligada" aria-live="polite">
            Coligada: <strong>{empresaSelecionada === null ? "—" : empresaSelecionada || "Todas"}</strong>
          </p>
          <Select
            id="filtro-filial" label="Filial" value={filialSelecionada ?? "__pendente__"}
            disabled={carregando || carregandoFiliais || empresaSelecionada === null}
            onChange={(event) => {
              setFilialSelecionada(event.target.value);
              setOpcoesContas([]);
              setOpcoesCarregadas(false);
              setContaSelecionada("");
              setContas([]);
              setConsultou(false);
              setErroDaLista(null);
            }}
            options={[
              ...(filialSelecionada === null ? [{ value: "__pendente__", label: "Selecione uma filial" }] : []),
              { value: "", label: "Todas as filiais" },
              ...filiais.map((filial) => ({ value: String(filial), label: String(filial) })),
            ]}
          />
          <Select
            id="filtro-conta" label="Conta contábil" value={contaSelecionada}
            disabled={carregando || !opcoesCarregadas}
            onChange={(event) => {
              setContaSelecionada(event.target.value);
              setContas([]);
              setConsultou(false);
            }}
            options={[
              { value: "", label: opcoesCarregadas ? "Todas as contas" : carregandoOpcoes ? "Carregando contas..." : "Selecione empresa e filial" },
              ...opcoesContasUnicas.map((conta) => ({ value: conta.codConta, label: conta.descricao ? `${conta.codConta} - ${conta.descricao}` : conta.codConta })),
            ]}
          />
          <Button
            type="button" icon={<RefreshCw size={18} />}
            isLoading={carregando}
            disabled={empresaSelecionada === null || filialSelecionada === null || !opcoesCarregadas}
            onClick={() => void carregarContas()}
          >
            Carregar
          </Button>
        </div>
        {erroDaLista && (
          <p className="sp-usuarios__erro" role="alert">
            {erroDaLista}
          </p>
        )}

        {consultou && <section className="sp-management__table" aria-label="Lista de contas contábeis">
          {carregando ? (
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
        </section>}
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
