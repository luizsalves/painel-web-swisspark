import { useCallback, useEffect, useMemo, useState } from "react";
import { Search } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import AppLayout from "../../componentes/layout/AppLayout";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import Input from "../../componentes/ui/Input";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Modal from "../../componentes/ui/Modal";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import { useFiltros } from "../../contexts/FiltrosContext";
import { extrairMensagemDeErro } from "../../services/api";
import { alterarStatusEmpresa, listarEmpresas } from "../../services/empresasService";
import { listarContasContabeis } from "../../services/contasContabeisService";
import type { Empresa } from "../../interfaces/Empresa";
import type { ContaContabilTotvs } from "../../interfaces/ContaContabil";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import type { CampoFiltro } from "../../utils/filtrosPainel";

const CAMPOS_FILTRO: CampoFiltro[] = ["empresa"];

function ordenarPorNome(empresas: Empresa[]): Empresa[] {
  return [...empresas].sort(
    (a, b) =>
      a.nome.localeCompare(b.nome, "pt-BR") || a.codFilial - b.codFilial
  );
}

export default function Empresas() {
  const { filtros } = useFiltros();
  const coligadaFiltrada = filtros.empresa ? coligadaDaEmpresa(filtros.empresa) : null;
  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erroDaLista, setErroDaLista] = useState<string | null>(null);
  const [busca, setBusca] = useState("");

  const [alvoDaConfirmacao, setAlvoDaConfirmacao] = useState<Empresa | null>(null);
  const [erroDaConfirmacao, setErroDaConfirmacao] = useState<string | null>(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  const carregarLista = useCallback(async () => {
    setCarregando(true);
    setErroDaLista(null);

    try {
      const [lista, planoDeContas] = await Promise.all([
        listarEmpresas(),
        listarContasContabeis(),
      ]);

      setEmpresas(ordenarPorNome(lista));
      setContas(planoDeContas);
    } catch (erro) {
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar as empresas."));
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregarLista();
  }, [carregarLista]);

  // Plano de contas é por coligada (não por filial) — conta quantas contas
  // cada coligada tem, pra mostrar na coluna "Contas Contábeis".
  const contasPorColigada = useMemo(() => {
    const mapa = new Map<number, number>();
    for (const conta of contas) {
      mapa.set(conta.codColigada, (mapa.get(conta.codColigada) ?? 0) + 1);
    }
    return mapa;
  }, [contas]);

  const listaFiltrada = useMemo(() => {
    // A empresa da gaveta restringe à coligada; a busca refina por texto.
    const daColigada = coligadaFiltrada === null
      ? empresas
      : empresas.filter((empresa) => empresa.codColigada === coligadaFiltrada);
    const termo = busca.trim().toLocaleLowerCase("pt-BR");
    if (!termo) return daColigada;

    return daColigada.filter(
      (empresa) =>
        empresa.nome.toLocaleLowerCase("pt-BR").includes(termo) ||
        empresa.nomeFantasia.toLocaleLowerCase("pt-BR").includes(termo) ||
        empresa.cgc.includes(termo) ||
        empresa.cidade.toLocaleLowerCase("pt-BR").includes(termo)
    );
  }, [empresas, busca, coligadaFiltrada]);

  function guardarNaLista(salva: Empresa) {
    setEmpresas((atuais) =>
      ordenarPorNome(atuais.map((empresa) => (empresa.id === salva.id ? salva : empresa)))
    );
  }

  async function confirmarAlteracaoDeStatus() {
    if (!alvoDaConfirmacao) return;

    setAlterandoStatus(true);
    setErroDaConfirmacao(null);

    try {
      const salva = await alterarStatusEmpresa(
        alvoDaConfirmacao.codColigada,
        alvoDaConfirmacao.codFilial,
        !alvoDaConfirmacao.ativo
      );
      guardarNaLista(salva);
      setAlvoDaConfirmacao(null);
    } catch (erro) {
      setErroDaConfirmacao(extrairMensagemDeErro(erro, "Não foi possível alterar o status da empresa."));
    } finally {
      setAlterandoStatus(false);
    }
  }

  const colunas: TableColumn<Empresa>[] = [
    { key: "coligadaFilial", header: "Coligada/Filial", render: (row) => `${row.codColigada} - ${row.codFilial}` },
    { key: "legalName", header: "Empresa", render: (row) => row.nomeFantasia || row.nome },
    { key: "document", header: "CNPJ", render: (row) => row.cgc },
    { key: "location", header: "Cidade/UF", render: (row) => `${row.cidade}/${row.estado}` },
    {
      key: "accountCount",
      header: "Contas Contábeis",
      render: (row) => contasPorColigada.get(row.codColigada) ?? 0,
      align: "center",
    },
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
        title="Empresas"
        subtitle="Consulte as empresas cadastradas no painel de conciliação"
        camposFiltro={CAMPOS_FILTRO}
      />

      <main className="sp-management">
        <ResumoFiltros campos={CAMPOS_FILTRO} />

        <div className="sp-management__toolbar">
          <div className="sp-management__search">
            <Input
              aria-label="Buscar empresas"
              placeholder="Buscar por empresa, CNPJ ou cidade"
              value={busca}
              onChange={(event) => setBusca(event.target.value)}
              leadingIcon={<Search size={18} />}
            />
          </div>
          <span className="sp-management__count">
            {listaFiltrada.length} {listaFiltrada.length === 1 ? "empresa" : "empresas"}
          </span>
        </div>

        {erroDaLista && (
          <p className="sp-usuarios__erro" role="alert">
            {erroDaLista}
          </p>
        )}

        <section className="sp-management__table" aria-label="Lista de empresas">
          {carregando ? (
            <div className="sp-usuarios__carregando">
              <LoadingSpinner size={28} />
            </div>
          ) : (
            <Table
              columns={colunas}
              data={listaFiltrada}
              getRowId={(row) => row.id}
              emptyMessage="Nenhuma empresa encontrada."
            />
          )}
        </section>
      </main>

      <Modal
        isOpen={alvoDaConfirmacao !== null}
        onClose={() => setAlvoDaConfirmacao(null)}
        title={alvoDaConfirmacao?.ativo ? "Desativar empresa" : "Reativar empresa"}
        description={
          alvoDaConfirmacao?.ativo
            ? `${alvoDaConfirmacao?.nomeFantasia || alvoDaConfirmacao?.nome} deixa de aparecer nos comboboxes da tela de Fechamentos.`
            : `${alvoDaConfirmacao?.nomeFantasia || alvoDaConfirmacao?.nome} volta a aparecer nos comboboxes da tela de Fechamentos.`
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
