import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BookText, Building2, CalendarDays, CalendarRange, ListChecks, X } from "lucide-react";
import Button from "../ui/Button";
import SearchableSelect from "../ui/SearchableSelect";
import type { SearchableSelectOption } from "../ui/SearchableSelect";
import { useFiltros } from "../../contexts/FiltrosContext";
import { useEmpresasConsolidadas } from "../../hooks/useEmpresasConsolidadas";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../../interfaces/ContaContabil";
import { extrairMensagemDeErro } from "../../services/api";
import { listarContasBalancete, listarEmpresasBalancete, listarFiliaisBalancete } from "../../services/balanceteService";
import { obterContasDaEmpresa } from "../../services/dashboardService";
import { chaveEmpresaContabil, coligadaDaEmpresa } from "../../utils/empresaContabil";
import { MESES, STATUS_FILTRO, descreverMes, descreverPeriodo, filtrosPadrao, mesDosFiltros } from "../../utils/filtrosPainel";
import type { CampoFiltro, FiltrosPainel } from "../../utils/filtrosPainel";

const ATALHOS_PERIODO: { rotulo: string; inicial: number; final: number }[] = [
  { rotulo: "Ano inteiro", inicial: 1, final: 12 },
  { rotulo: "1º semestre", inicial: 1, final: 6 },
  { rotulo: "2º semestre", inicial: 7, final: 12 },
];

/**
 * Gaveta de filtros do painel. Fica montada uma vez no App e abre pelo botão
 * "Filtros" do cabeçalho; o que se edita aqui é um rascunho até "Aplicar".
 */
export default function PainelFiltros() {
  const { aberto, filtros, camposDaTela, aplicar, fechar } = useFiltros();
  const [empresas, setEmpresas] = useState<EmpresaContabil[] | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  // A lista de empresas só é buscada na primeira abertura e fica guardada.
  useEffect(() => {
    if (!aberto || empresas !== null) return;
    let cancelado = false;
    listarEmpresasBalancete()
      .then((lista) => {
        if (cancelado) return;
        setEmpresas(lista.filter((item) => item.codColigada !== 0));
        setErro(null);
      })
      .catch((falha) => { if (!cancelado) setErro(extrairMensagemDeErro(falha, "Não foi possível carregar as empresas.")); });
    return () => { cancelado = true; };
  }, [aberto, empresas]);

  if (!aberto) return null;

  return (
    <ConteudoFiltros
      inicial={filtros}
      camposDaTela={camposDaTela}
      empresas={empresas}
      erro={erro}
      onFechar={fechar}
      onAplicar={(novos) => { aplicar(novos); fechar(); }}
    />
  );
}

interface ConteudoFiltrosProps {
  inicial: FiltrosPainel;
  camposDaTela: CampoFiltro[];
  empresas: EmpresaContabil[] | null;
  erro: string | null;
  onFechar: () => void;
  onAplicar: (filtros: FiltrosPainel) => void;
}

function AvisoNaoUsado({ campo, campos }: { campo: CampoFiltro; campos: CampoFiltro[] }) {
  if (campos.includes(campo)) return null;
  return <small className="sp-filtros__unused">Esta tela não usa este filtro; ele vale para as outras telas.</small>;
}

function ConteudoFiltros({ inicial, camposDaTela, empresas, erro, onFechar, onAplicar }: ConteudoFiltrosProps) {
  const [rascunho, setRascunho] = useState(inicial);
  // "Período específico": os meses são escolhidos um a um na grade. Já começa
  // ligado quando o período aberto não é um dos atalhos.
  const [especifico, setEspecifico] = useState(() => !ATALHOS_PERIODO.some(
    (atalho) => atalho.inicial === inicial.mesInicial && atalho.final === inicial.mesFinal));
  // Logo depois de clicar em "Período específico" a grade fica vazia, esperando os meses.
  const [semMeses, setSemMeses] = useState(false);
  const [filiais, setFiliais] = useState<{ chave: string; lista: ColigadaFilialContabil[] } | null>(null);
  const [contas, setContas] = useState<{ chave: string; lista: ContaContabilTotvs[] } | null>(null);
  const botaoFecharRef = useRef<HTMLButtonElement>(null);

  useEffect(() => { botaoFecharRef.current?.focus(); }, []);

  useEffect(() => {
    function aoTeclar(evento: KeyboardEvent) {
      if (evento.key === "Escape") onFechar();
    }
    document.addEventListener("keydown", aoTeclar);
    return () => document.removeEventListener("keydown", aoTeclar);
  }, [onFechar]);

  // As listas dependem da empresa (e as contas também da filial); a chave diz
  // a qual escolha a lista carregada pertence, para não mostrar a de outra.
  const chaveFiliais = rascunho.empresa ?? "";
  const chaveContas = `${rascunho.empresa ?? ""}|${rascunho.filial}`;

  useEffect(() => {
    const empresa = rascunho.empresa;
    if (!empresa) return;
    let cancelado = false;
    listarFiliaisBalancete(coligadaDaEmpresa(empresa))
      .then((lista) => { if (!cancelado) setFiliais({ chave: empresa, lista }); })
      .catch(() => { if (!cancelado) setFiliais({ chave: empresa, lista: [] }); });
    return () => { cancelado = true; };
  }, [rascunho.empresa]);

  useEffect(() => {
    const empresa = rascunho.empresa;
    if (!empresa) return;
    const chave = `${empresa}|${rascunho.filial}`;
    let cancelado = false;
    listarContasBalancete({
      codColigada: coligadaDaEmpresa(empresa),
      codFilial: rascunho.filial ? Number(rascunho.filial) : undefined,
    })
      .then((lista) => { if (!cancelado) setContas({ chave, lista }); })
      .catch(() => { if (!cancelado) setContas({ chave, lista: [] }); });
    return () => { cancelado = true; };
  }, [rascunho.empresa, rascunho.filial]);

  // Tela de um único mês: a grade escolhe o mês final; o inicial só recua se
  // ficar depois dele. Assim o período das outras telas não se perde.
  const mesUnico = camposDaTela.includes("mes") && !camposDaTela.includes("periodo");

  // Numa tela de um mês só (Fechamento), com um status escolhido, a lista de contas
  // fica só com as que estão nele no mês — o status vem do dashboard, que cruza o
  // cadastro com o histórico do fechamento. Nas telas de período o status muda de
  // um mês para outro, e o filtro vale para cada mês na própria tela.
  const empresaDoStatus = mesUnico && rascunho.status && rascunho.empresa ? rascunho.empresaNome.trim() : "";
  // Nas empresas de filiais consolidadas as contas do dashboard vêm todas na filial 0.
  const consolidaFiliais = useEmpresasConsolidadas();
  const empresaConsolidada = Boolean(consolidaFiliais(rascunho.empresa));
  const anoMesRascunho = mesDosFiltros(rascunho);
  const contasDoStatus = useQuery({
    queryKey: ["dashboard-contas", anoMesRascunho, anoMesRascunho, empresaDoStatus, 0],
    queryFn: ({ signal }) => obterContasDaEmpresa(anoMesRascunho, anoMesRascunho, empresaDoStatus, signal),
    enabled: Boolean(empresaDoStatus),
    // Sempre relê: quem acabou de preparar uma conta quer a próxima da lista, não ela.
    staleTime: 0,
  });
  const codigosDoStatus = useMemo(() => {
    if (!empresaDoStatus || contasDoStatus.isFetching || !contasDoStatus.data) return null;
    const coligada = coligadaDaEmpresa(rascunho.empresa ?? "");
    return new Set(contasDoStatus.data
      .filter((conta) => conta.status === rascunho.status
        && (!rascunho.filial || empresaConsolidada
          || (conta.codColigada === coligada && String(conta.codFilial) === rascunho.filial)))
      .map((conta) => conta.codConta));
  }, [empresaDoStatus, contasDoStatus.isFetching, contasDoStatus.data, rascunho.empresa, rascunho.filial, rascunho.status,
    empresaConsolidada]);

  // Uma conta fora do status escolhido sai do filtro, como acontece ao trocar a filial.
  if (codigosDoStatus && rascunho.conta && !codigosDoStatus.has(rascunho.conta)) {
    setRascunho((atual) => ({ ...atual, conta: "", contaDescricao: "" }));
  }

  const carregandoFiliais = rascunho.empresa !== null && filiais?.chave !== chaveFiliais;
  const carregandoContas = rascunho.empresa !== null
    && (contas?.chave !== chaveContas || (Boolean(empresaDoStatus) && contasDoStatus.isFetching));

  // O código aparece porque o mesmo nome se repete (SCPs são filiais da mesma coligada).
  // "Todas" (vazio) deixa a empresa nula: as telas que exigem uma empresa pedem a escolha.
  const opcoesEmpresa = useMemo<SearchableSelectOption[]>(() => [
    { value: "", label: "Todas" },
    ...(empresas ?? []).map((item) => ({ value: chaveEmpresaContabil(item), label: `${item.nome} - ${item.codColigada}` })),
  ], [empresas]);
  const opcoesFilial = useMemo<SearchableSelectOption[]>(() => {
    const lista = filiais?.chave === chaveFiliais ? filiais.lista : [];
    return [
      { value: "", label: "Todas" },
      ...[...new Set(lista.map((item) => item.codFilial))].sort((a, b) => a - b)
        .map((codigo) => ({ value: String(codigo), label: String(codigo) })),
    ];
  }, [filiais, chaveFiliais]);
  const contasUnicas = useMemo(() => {
    const unicas = new Map<string, ContaContabilTotvs>();
    for (const item of contas?.chave === chaveContas ? contas.lista : []) {
      if (!unicas.has(item.codConta) || (!unicas.get(item.codConta)?.descricao && item.descricao))
        unicas.set(item.codConta, item);
    }
    return unicas;
  }, [contas, chaveContas]);
  const opcoesConta = useMemo<SearchableSelectOption[]>(() => [
    { value: "", label: "Todas" },
    ...[...contasUnicas.values()]
      .filter((item) => !codigosDoStatus || codigosDoStatus.has(item.codConta))
      .sort((a, b) => a.codConta.localeCompare(b.codConta))
      .map((item) => ({ value: item.codConta, label: item.descricao ? `${item.codConta} - ${item.descricao}` : item.codConta })),
  ], [contasUnicas, codigosDoStatus]);

  const anos = useMemo(() => {
    const atual = new Date().getFullYear();
    return [...new Set([atual - 3, atual - 2, atual - 1, atual, rascunho.ano])].sort((a, b) => a - b);
  }, [rascunho.ano]);

  // O período é sempre contínuo (a DFC precisa de saldo inicial e final): um mês
  // de fora estica o intervalo até ele, um mês da ponta sai, e um mês do meio
  // passa a ser o último.
  function clicarMes(mes: number) {
    if (!especifico || semMeses) {
      setRascunho((atual) => ({ ...atual, mesInicial: mes, mesFinal: mes }));
      setEspecifico(true);
      setSemMeses(false);
      return;
    }
    const { mesInicial, mesFinal } = rascunho;
    if (mes < mesInicial || mes > mesFinal) {
      setRascunho((atual) => ({ ...atual, mesInicial: Math.min(atual.mesInicial, mes), mesFinal: Math.max(atual.mesFinal, mes) }));
    } else if (mesInicial === mesFinal) {
      setSemMeses(true);
    } else if (mes === mesInicial) {
      setRascunho((atual) => ({ ...atual, mesInicial: mes + 1 }));
    } else {
      setRascunho((atual) => ({ ...atual, mesFinal: mes === mesFinal ? mes - 1 : mes }));
    }
  }

  const periodoVazio = !mesUnico && semMeses;

  return (
    <div className="sp-filtros">
      <div className="sp-filtros__backdrop" onClick={onFechar} aria-hidden="true" />
      <aside className="sp-filtros__drawer" role="dialog" aria-modal="true" aria-labelledby="sp-filtros-titulo">
        <header className="sp-filtros__head">
          <div>
            <h2 id="sp-filtros-titulo">Filtros</h2>
            <p>Valem para todas as telas do painel.</p>
          </div>
          <button ref={botaoFecharRef} type="button" className="sp-filtros__close" onClick={onFechar} aria-label="Fechar filtros">
            <X size={18} />
          </button>
        </header>

        <div className="sp-filtros__body">
          {erro && <p className="sp-balance__error" role="alert">{erro}</p>}

          <section className="sp-filtros__section">
            <h3><Building2 size={16} /> Empresa e Coligada</h3>
            <label className="sp-filtros__field">
              <span>Empresa</span>
              <SearchableSelect value={rascunho.empresa ?? ""} options={opcoesEmpresa}
                onChange={(valor) => setRascunho((atual) => ({
                  ...atual,
                  empresa: valor || null,
                  empresaNome: (empresas ?? []).find((item) => chaveEmpresaContabil(item) === valor)?.nome ?? "",
                  filial: "",
                  conta: "",
                  contaDescricao: "",
                }))}
                disabled={empresas === null} placeholder={empresas === null ? "Carregando empresas..." : "Selecione uma empresa"}
                ariaLabel="Empresa" />
              <AvisoNaoUsado campo="empresa" campos={camposDaTela} />
            </label>
            <label className="sp-filtros__field">
              <span>Filial</span>
              <SearchableSelect value={rascunho.filial} options={opcoesFilial}
                onChange={(valor) => setRascunho((atual) => ({ ...atual, filial: valor, conta: "", contaDescricao: "" }))}
                disabled={rascunho.empresa === null || carregandoFiliais}
                placeholder={carregandoFiliais ? "Carregando filiais..." : "Todas"} ariaLabel="Filial" />
              {empresaConsolidada && (
                <small className="sp-filtros__unused">
                  No Fechamento e na Produtividade esta empresa fecha com todas as filiais juntas; a filial vale para as outras telas.
                </small>
              )}
              <AvisoNaoUsado campo="filial" campos={camposDaTela} />
            </label>
          </section>

          <section className="sp-filtros__section">
            <h3><BookText size={16} /> Conta contábil</h3>
            <label className="sp-filtros__field">
              <span>Conta</span>
              <SearchableSelect value={rascunho.conta} options={opcoesConta}
                onChange={(valor) => setRascunho((atual) => ({
                  ...atual,
                  conta: valor,
                  contaDescricao: contasUnicas.get(valor)?.descricao ?? "",
                }))}
                disabled={rascunho.empresa === null || carregandoContas}
                placeholder={rascunho.empresa === null ? "Escolha a empresa primeiro" : carregandoContas ? "Carregando contas..." : "Todas"}
                ariaLabel="Conta contábil" />
              {contasDoStatus.isError && empresaDoStatus && (
                <small className="sp-filtros__unused">Não foi possível filtrar as contas pelo status; a lista mostra todas.</small>
              )}
              <AvisoNaoUsado campo="conta" campos={camposDaTela} />
            </label>
          </section>

          <section className="sp-filtros__section">
            <h3><ListChecks size={16} /> Status do fechamento</h3>
            <div className="sp-filtros__shortcuts" role="radiogroup" aria-label="Status do fechamento">
              {[{ valor: "" as const, rotulo: "Todos" }, ...STATUS_FILTRO].map((opcao) => (
                <button key={opcao.rotulo} type="button" role="radio" aria-checked={rascunho.status === opcao.valor}
                  className={rascunho.status === opcao.valor ? "sp-filtros__chip sp-filtros__chip--active" : "sp-filtros__chip"}
                  onClick={() => setRascunho((atual) => ({ ...atual, status: opcao.valor }))}>
                  {opcao.rotulo}
                </button>
              ))}
            </div>
            <p className="sp-filtros__hint">
              {mesUnico
                ? "Situação da conta no mês escolhido. Com uma empresa, a lista de contas acima mostra só as desse status."
                : "Situação da conta em cada mês do período."}
            </p>
            <AvisoNaoUsado campo="status" campos={camposDaTela} />
          </section>

          <section className="sp-filtros__section">
            <h3><CalendarDays size={16} /> Ano</h3>
            <div className="sp-filtros__segmented" role="radiogroup" aria-label="Ano">
              {anos.map((ano) => (
                <button key={ano} type="button" role="radio" aria-checked={rascunho.ano === ano}
                  className={rascunho.ano === ano ? "sp-filtros__segment sp-filtros__segment--active" : "sp-filtros__segment"}
                  onClick={() => setRascunho((atual) => ({ ...atual, ano }))}>
                  {ano}
                </button>
              ))}
            </div>
          </section>

          <section className="sp-filtros__section">
            <h3><CalendarRange size={16} /> {mesUnico ? "Mês" : "Período"}</h3>
            {!mesUnico && (
              <div className="sp-filtros__shortcuts">
                {ATALHOS_PERIODO.map((atalho) => {
                  const ativo = !especifico && rascunho.mesInicial === atalho.inicial && rascunho.mesFinal === atalho.final;
                  return (
                    <button key={atalho.rotulo} type="button"
                      className={ativo ? "sp-filtros__chip sp-filtros__chip--active" : "sp-filtros__chip"}
                      onClick={() => {
                        setRascunho((atual) => ({ ...atual, mesInicial: atalho.inicial, mesFinal: atalho.final }));
                        setEspecifico(false);
                        setSemMeses(false);
                      }}>
                      {atalho.rotulo}
                    </button>
                  );
                })}
                <button type="button"
                  className={especifico ? "sp-filtros__chip sp-filtros__chip--active" : "sp-filtros__chip"}
                  onClick={() => { setEspecifico(true); setSemMeses(true); }}>
                  Período específico
                </button>
              </div>
            )}
            <div className="sp-filtros__months">
              {MESES.map((rotulo, indice) => {
                const mes = indice + 1;
                const noIntervalo = mesUnico
                  ? mes === rascunho.mesFinal
                  : !periodoVazio && mes >= rascunho.mesInicial && mes <= rascunho.mesFinal;
                // Todo mês do período tem o mesmo destaque: todos foram escolhidos.
                const ponta = noIntervalo;
                return (
                  <button key={rotulo} type="button" aria-pressed={noIntervalo}
                    className={["sp-filtros__month", noIntervalo ? "sp-filtros__month--range" : "", ponta ? "sp-filtros__month--edge" : ""]
                      .filter(Boolean).join(" ")}
                    onClick={() => {
                      if (mesUnico) setRascunho((atual) => ({ ...atual, mesInicial: Math.min(atual.mesInicial, mes), mesFinal: mes }));
                      else clicarMes(mes);
                    }}>
                    {rotulo}
                  </button>
                );
              })}
            </div>
            <p className="sp-filtros__hint">
              {mesUnico
                ? `Esta tela usa um único mês. Nas telas de período (DFC, Indicadores), ele é o mês final: ${descreverPeriodo(rascunho)}.`
                : !especifico
                  ? "Ou clique nos meses para montar um período específico."
                  : periodoVazio
                    ? "Clique nos meses que entram no período."
                    : "Clique em outros meses para aumentar o período; clicar num mês da ponta tira ele. Os meses do meio entram juntos."}
            </p>
            <p className="sp-filtros__summary">
              {mesUnico ? descreverMes(rascunho) : periodoVazio ? "Nenhum mês escolhido" : descreverPeriodo(rascunho)}
            </p>
            {!camposDaTela.includes("mes") && <AvisoNaoUsado campo="periodo" campos={camposDaTela} />}
          </section>
        </div>

        <footer className="sp-filtros__foot">
          <Button variant="ghost" onClick={() => { setRascunho(filtrosPadrao()); setEspecifico(false); setSemMeses(false); }}>Limpar</Button>
          <Button onClick={() => onAplicar(rascunho)} disabled={periodoVazio}
            title={periodoVazio ? "Escolha pelo menos um mês do período." : undefined}>
            Aplicar filtros
          </Button>
        </footer>
      </aside>
    </div>
  );
}
