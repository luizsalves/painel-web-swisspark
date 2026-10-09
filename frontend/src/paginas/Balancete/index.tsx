import { chaveEmpresaContabil, coligadaDaEmpresa } from "../../utils/empresaContabil";
import { useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
import Button from "../../componentes/ui/Button";
import SearchableSelect from "../../componentes/ui/SearchableSelect";
import type { SearchableSelectOption } from "../../componentes/ui/SearchableSelect";
import type { BalanceteConta } from "../../interfaces/Balancete";
import type { ColigadaFilialContabil, ContaContabilTotvs, EmpresaContabil } from "../../interfaces/ContaContabil";
import { extrairMensagemDeErro } from "../../services/api";
import {
  carregarBalancete, listarContasBalancete, listarEmpresasBalancete, listarFiliaisBalancete,
} from "../../services/balanceteService";

const numero = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const tamanhoPagina = 100;
const chaveConta = (item: BalanceteConta) => `${item.codColigada}:${item.codConta}`;

function periodoPorExtenso(valor: string) {
  const [ano, mes] = valor.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" }).format(new Date(ano, mes - 1, 1));
}

export default function Balancete() {
  const [empresas, setEmpresas] = useState<EmpresaContabil[]>([]);
  const [filiais, setFiliais] = useState<ColigadaFilialContabil[]>([]);
  const [contas, setContas] = useState<ContaContabilTotvs[]>([]);
  const [empresa, setEmpresa] = useState<string | null>(null);
  const [filial, setFilial] = useState<string | null>(null);
  const [conta, setConta] = useState<string | null>(null);
  const [periodo, setPeriodo] = useState("");
  const [linhas, setLinhas] = useState<BalanceteConta[]>([]);
  const [expandidas, setExpandidas] = useState<Set<string>>(() => new Set());
  const [periodoCarregado, setPeriodoCarregado] = useState("");
  const [pagina, setPagina] = useState(1);
  const [carregandoEmpresas, setCarregandoEmpresas] = useState(true);
  const [carregandoFiliais, setCarregandoFiliais] = useState(false);
  const [carregandoContas, setCarregandoContas] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    listarEmpresasBalancete()
      .then((lista) => { if (!cancelado) setEmpresas(lista.filter((item) => item.codColigada !== 0)); })
      .catch((falha) => { if (!cancelado) setErro(extrairMensagemDeErro(falha, "Não foi possível carregar as empresas.")); })
      .finally(() => { if (!cancelado) setCarregandoEmpresas(false); });
    return () => { cancelado = true; };
  }, []);

  useEffect(() => {
    if (empresa === null) return;
    let cancelado = false;
    setCarregandoFiliais(true);
    listarFiliaisBalancete(empresa ? coligadaDaEmpresa(empresa) : undefined)
      .then((lista) => { if (!cancelado) setFiliais(lista); })
      .catch((falha) => { if (!cancelado) setErro(extrairMensagemDeErro(falha, "Não foi possível carregar as filiais.")); })
      .finally(() => { if (!cancelado) setCarregandoFiliais(false); });
    return () => { cancelado = true; };
  }, [empresa]);

  useEffect(() => {
    if (empresa === null || filial === null) return;
    let cancelado = false;
    setCarregandoContas(true);
    listarContasBalancete({
      codColigada: empresa ? coligadaDaEmpresa(empresa) : undefined,
      codFilial: filial ? Number(filial) : undefined,
    })
      .then((lista) => { if (!cancelado) setContas(lista); })
      .catch((falha) => { if (!cancelado) setErro(extrairMensagemDeErro(falha, "Não foi possível carregar as contas.")); })
      .finally(() => { if (!cancelado) setCarregandoContas(false); });
    return () => { cancelado = true; };
  }, [empresa, filial]);

  const opcoesEmpresa = useMemo<SearchableSelectOption[]>(
    () => [{ value: "", label: "Todas" }, ...empresas.map((item) => ({ value: chaveEmpresaContabil(item), label: item.nome }))],
    [empresas]
  );
  const opcoesFilial = useMemo<SearchableSelectOption[]>(
    () => [{ value: "", label: "Todas" }, ...[...new Set(filiais.map((item) => item.codFilial))].sort((a, b) => a - b)
      .map((codigo) => ({ value: String(codigo), label: String(codigo) }))],
    [filiais]
  );
  const opcoesConta = useMemo<SearchableSelectOption[]>(() => {
    const unicas = new Map<string, ContaContabilTotvs>();
    for (const item of contas) {
      if (!unicas.has(item.codConta) || (!unicas.get(item.codConta)?.descricao && item.descricao))
        unicas.set(item.codConta, item);
    }
    return [
      { value: "", label: "Todas" },
      ...[...unicas.values()].sort((a, b) => a.codConta.localeCompare(b.codConta))
        .map((item) => ({ value: item.codConta, label: item.descricao ? `${item.codConta} - ${item.descricao}` : item.codConta })),
    ];
  }, [contas]);

  const codigoReduzido = useMemo(() => {
    if (conta === null) return "—";
    if (!conta) return "Todas";
    const codigos = [...new Set(contas.filter((item) => item.codConta === conta)
      .map((item) => item.reduzido.trim()).filter(Boolean))];
    return codigos.length > 1 ? "Vários" : codigos[0] ?? "—";
  }, [conta, contas]);

  const { contasComFilhos, linhasVisiveis } = useMemo(() => {
    const chaves = new Set(linhas.map(chaveConta));
    const comFilhos = new Set<string>();
    const visiveis = linhas.filter((item) => {
      let mostrar = true;
      let separador = item.codConta.indexOf(".");
      while (separador !== -1) {
        const chavePai = `${item.codColigada}:${item.codConta.slice(0, separador)}`;
        if (chaves.has(chavePai)) {
          comFilhos.add(chavePai);
          if (!expandidas.has(chavePai)) mostrar = false;
        }
        separador = item.codConta.indexOf(".", separador + 1);
      }
      return mostrar;
    });
    return { contasComFilhos: comFilhos, linhasVisiveis: visiveis };
  }, [linhas, expandidas]);

  const paginas = Math.max(1, Math.ceil(linhasVisiveis.length / tamanhoPagina));
  const linhasDaPagina = linhasVisiveis.slice((pagina - 1) * tamanhoPagina, pagina * tamanhoPagina);

  function limparResultado() {
    setLinhas([]);
    setExpandidas(new Set());
    setPeriodoCarregado("");
    setPagina(1);
    setErro(null);
  }

  async function carregar() {
    if (empresa === null || filial === null || conta === null || !periodo || carregando) return;
    setCarregando(true);
    setErro(null);
    try {
      const resultado = await carregarBalancete({
        anoMes: periodo,
        codColigada: empresa ? coligadaDaEmpresa(empresa) : undefined,
        codFilial: filial ? Number(filial) : undefined,
        codConta: conta || undefined,
      });
      setLinhas(resultado);
      setExpandidas(new Set());
      setPeriodoCarregado(periodo);
      setPagina(1);
    } catch (falha) {
      setLinhas([]);
      setExpandidas(new Set());
      setPeriodoCarregado("");
      setErro(extrairMensagemDeErro(falha, "Não foi possível carregar o balancete."));
    } finally {
      setCarregando(false);
    }
  }

  return (
    <AppLayout>
      <AppHeader title="Balancete" subtitle="Consulte saldos e movimentos contábeis por empresa, filial, conta e período." />
      <main className="sp-balance">
        <section className="sp-balance__filters" aria-label="Filtros do balancete">
          <label className="sp-balance__field sp-balance__field--company">
            <span>Empresa</span>
            <SearchableSelect value={empresa ?? "__pendente__"} options={opcoesEmpresa}
              onChange={(valor) => { setEmpresa(valor); setFilial(null); setConta(null); setPeriodo(""); setFiliais([]); setContas([]); limparResultado(); }}
              disabled={carregandoEmpresas} placeholder={carregandoEmpresas ? "Carregando empresas..." : "Selecione uma empresa"} ariaLabel="Empresa" />
          </label>
          <div className="sp-balance__codes">
            <div className="sp-balance__field sp-balance__field--code">
              <span>Cód. Coligada</span>
              <strong className="sp-balance__readonly">{empresa === null ? "—" : empresa ? coligadaDaEmpresa(empresa) : "Todas"}</strong>
            </div>
            <div className="sp-balance__field sp-balance__field--code">
              <span>Cód. Reduzido</span>
              <strong className="sp-balance__readonly">{codigoReduzido}</strong>
            </div>
          </div>
          <label className="sp-balance__field sp-balance__field--filial">
            <span>Filial</span>
            <SearchableSelect value={filial ?? "__pendente__"} options={opcoesFilial}
              onChange={(valor) => { setFilial(valor); setConta(null); setPeriodo(""); setContas([]); limparResultado(); }}
              disabled={empresa === null || carregandoFiliais} placeholder={carregandoFiliais ? "Carregando filiais..." : "Selecione uma filial"} ariaLabel="Filial" />
          </label>
          <label className="sp-balance__field sp-balance__field--account">
            <span>Conta contábil</span>
            <SearchableSelect value={conta ?? "__pendente__"} options={opcoesConta}
              onChange={(valor) => { setConta(valor); setPeriodo(""); limparResultado(); }}
              disabled={filial === null || carregandoContas} placeholder={carregandoContas ? "Carregando contas..." : "Selecione uma conta"} ariaLabel="Conta contábil" />
          </label>
          <label className="sp-balance__field sp-balance__field--period">
            <span>Período (Mês/Ano)</span>
            <input type="month" value={periodo} disabled={conta === null || carregandoContas}
              onChange={(evento) => { setPeriodo(evento.target.value); limparResultado(); }} />
          </label>
          <Button className="sp-balance__load" icon={<RefreshCw size={18} />} isLoading={carregando}
            disabled={empresa === null || filial === null || conta === null || !periodo}
            onClick={() => void carregar()}>Carregar Dados</Button>
        </section>

        {erro && <p className="sp-balance__error" role="alert">{erro}</p>}

        <section className="sp-balance__report" aria-label="Resultado do balancete">
          <div className="sp-balance__report-head">
            <div>
              <h2>{periodoCarregado ? `Balancete de ${periodoPorExtenso(periodoCarregado)}` : "Balancete"}</h2>
              <p>{periodoCarregado ? `${linhas.length} contas e grupos encontrados · ${linhasVisiveis.length} visíveis` : "Selecione os filtros e carregue os dados para consultar o balancete."}</p>
            </div>
          </div>
          {periodoCarregado && (
            <>
              <div className="sp-balance__table-wrap">
                <table className="sp-balance__table">
                  <thead><tr>
                    <th>Empresa</th><th>Código</th><th>Descrição da conta</th>
                    <th className="sp-balance__number">Saldo anterior (R$)</th>
                    <th className="sp-balance__number">Débito (R$)</th>
                    <th className="sp-balance__number">Crédito (R$)</th>
                    <th className="sp-balance__number">Saldo atual (R$)</th>
                  </tr></thead>
                  <tbody>
                    {linhasDaPagina.length === 0 ? <tr><td colSpan={7} className="sp-balance__empty">Nenhuma conta encontrada para o período.</td></tr> :
                      linhasDaPagina.map((item) => (
                        <tr key={chaveConta(item)} className={item.sintetica ? "sp-balance__group" : undefined}>
                          <td>{item.empresa}</td>
                          <td className="sp-balance__account-code" style={{ paddingLeft: `${14 + Math.min(item.nivel, 5) * 12}px` }}>
                            <div className="sp-balance__account-cell">
                              {contasComFilhos.has(chaveConta(item)) ? (
                                <button type="button" className="sp-balance__toggle"
                                  aria-expanded={expandidas.has(chaveConta(item))}
                                  aria-label={`${expandidas.has(chaveConta(item)) ? "Recolher" : "Expandir"} subcontas de ${item.codConta}`}
                                  onClick={() => setExpandidas((atuais) => {
                                    const proximas = new Set(atuais);
                                    const chave = chaveConta(item);
                                    if (proximas.has(chave)) proximas.delete(chave);
                                    else proximas.add(chave);
                                    return proximas;
                                  })}>
                                  {expandidas.has(chaveConta(item)) ? "−" : "+"}
                                </button>
                              ) : <span className="sp-balance__toggle-placeholder" aria-hidden="true" />}
                              <span>{item.codConta}</span>
                            </div>
                          </td>
                          <td>{item.descricao || "—"}</td>
                          <td className="sp-balance__number">{numero.format(item.saldoAnterior)}</td>
                          <td className="sp-balance__number">{numero.format(item.debito)}</td>
                          <td className="sp-balance__number">{numero.format(item.credito)}</td>
                          <td className="sp-balance__number">{numero.format(item.saldoAtual)}</td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
              {paginas > 1 && (
                <div className="sp-balance__pagination">
                  <span>Mostrando {(pagina - 1) * tamanhoPagina + 1}–{Math.min(pagina * tamanhoPagina, linhasVisiveis.length)} de {linhasVisiveis.length} linhas visíveis</span>
                  <div>
                    <button type="button" disabled={pagina === 1} onClick={() => setPagina((atual) => atual - 1)}>Anterior</button>
                    <span>Página {pagina} de {paginas}</span>
                    <button type="button" disabled={pagina === paginas} onClick={() => setPagina((atual) => atual + 1)}>Próxima</button>
                  </div>
                </div>
              )}
            </>
          )}
        </section>
      </main>
    </AppLayout>
  );
}
