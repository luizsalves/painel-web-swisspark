import { useEffect, useMemo, useRef, useState } from "react";
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
  const [carregando, setCarregando] = useState(false);
  const [opcoesContas, setOpcoesContas] = useState<ContaContabilTotvs[]>([]);
  const [carregandoOpcoes, setCarregandoOpcoes] = useState(false);
  const [consultou, setConsultou] = useState(false);
  const [erroDaLista, setErroDaLista] = useState<string | null>(null);

  const [empresas, setEmpresas] = useState<Empresa[]>([]);

  const [empresaSelecionada, setEmpresaSelecionada] = useState("");
  const [contaSelecionada, setContaSelecionada] = useState("");
  const [somenteBsb, setSomenteBsb] = useState(true);
  const [todasEmpresas, setTodasEmpresas] = useState(false);
  const buscandoEmpresas = useRef(false);

  useEffect(() => {
    let cancelado = false;
    listarEmpresas(true).then((lista) => {
      if (cancelado) return;
      setEmpresas(lista);
      const padrao = lista.find((empresa) =>
        empresa.nome.toLocaleUpperCase("pt-BR").replace(/[^A-Z0-9]/g, "") === "AEPLAGROPECUARIALTDA"
      );
      if (!padrao) throw new Error("AEPL AGROPECUARIA LTDA não encontrada no banco BSB Brasília.");
      setEmpresaSelecionada(String(padrao.codColigada));
    }).catch((erro) => {
      if (cancelado) return;
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar a empresa padrão."));
      setCarregando(false);
    });
    return () => { cancelado = true; };
  }, []);

  async function carregarTodasEmpresas() {
    if (todasEmpresas || buscandoEmpresas.current) return;
    buscandoEmpresas.current = true;
    try {
      const lista = await listarEmpresas();
      setEmpresas(lista);
      setTodasEmpresas(true);
    } catch (erro) {
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar as empresas."));
    } finally {
      buscandoEmpresas.current = false;
    }
  }

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

  // Prepara apenas as opcoes do combo; a tabela depende do botao Carregar.
  useEffect(() => {
    if (!empresaSelecionada) return;
    let cancelado = false;
    setCarregandoOpcoes(true);
    setOpcoesContas([]);
    listarContasContabeis({ somenteBsb, codColigada: Number(empresaSelecionada) })
      .then((lista) => { if (!cancelado) setOpcoesContas(ordenarPorConta(lista)); })
      .catch((erro) => {
        if (!cancelado) setErroDaLista(extrairMensagemDeErro(erro, "Erro ao carregar as opcoes de contas."));
      })
      .finally(() => { if (!cancelado) setCarregandoOpcoes(false); });
    return () => { cancelado = true; };
  }, [empresaSelecionada, somenteBsb]);

  async function carregarContas() {
    if (!empresaSelecionada || carregando) return;
    setCarregando(true);
    setConsultou(true);
    setErroDaLista(null);
    setContas([]);
    try {
      const lista = await listarContasContabeis({ somenteBsb, codColigada: Number(empresaSelecionada) });
      setContas(ordenarPorConta(contaSelecionada
        ? lista.filter((conta) => conta.codConta === contaSelecionada)
        : lista));
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
        <div className="sp-contas__filtros">
          <Select
            id="filtro-empresa" label="Empresa" value={empresaSelecionada}
            disabled={carregando}
            onFocus={() => void carregarTodasEmpresas()}
            onPointerDown={() => void carregarTodasEmpresas()}
            onChange={(event) => {
              setEmpresaSelecionada(event.target.value);
              setContaSelecionada("");
              setContas([]);
              setConsultou(false);
              setSomenteBsb(false);
            }}
            options={[
              ...(!empresaSelecionada ? [{ value: "", label: "Selecione uma empresa" }] : []),
              ...Array.from(empresasPorColigada, ([codigo, filiais]) => ({
                value: String(codigo), label: `${filiais[0].nome} (${codigo})`,
              })),
            ]}
          />
          <Select
            id="filtro-conta" label="Conta contábil" value={contaSelecionada}
            disabled={carregando || carregandoOpcoes || !empresaSelecionada}
            onChange={(event) => {
              setContaSelecionada(event.target.value);
              setContas([]);
              setConsultou(false);
            }}
            options={[
              { value: "", label: "Todas as contas da empresa" },
              ...opcoesContas.map((conta) => ({ value: conta.codConta, label: `${conta.codConta} - ${conta.descricao}` })),
            ]}
          />
          <Button
            type="button" icon={<RefreshCw size={18} />}
            isLoading={carregando}
            disabled={!empresaSelecionada || carregandoOpcoes}
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
