import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import Button from "../../componentes/ui/Button";
import { useFiltros } from "../../contexts/FiltrosContext";
import type { BalanceteConta } from "../../interfaces/Balancete";
import { extrairMensagemDeErro } from "../../services/api";
import { carregarBalancete, listarContasBalancete } from "../../services/balanceteService";
import { mesDosFiltros } from "../../utils/filtrosPainel";
import { CAMPOS_FILTRO_RESUMO } from "./camposFiltro";

const numero = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const tamanhoPagina = 100;
const chaveConta = (item: BalanceteConta) => `${item.codColigada}:${item.codConta}`;

function periodoPorExtenso(valor: string) {
  const [ano, mes] = valor.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" }).format(new Date(ano, mes - 1, 1));
}

/** Aba "Balancete Resumo": saldos e movimentos do mês por conta. */
export default function BalanceteResumo() {
  const { filtros } = useFiltros();
  const parametros = {
    anoMes: mesDosFiltros(filtros),
    codColigada: filtros.empresa ? coligadaDaEmpresa(filtros.empresa) : undefined,
    codFilial: filtros.filial ? Number(filtros.filial) : undefined,
    codConta: filtros.conta || undefined,
  };
  const chaveConsulta = JSON.stringify(parametros);

  // Sem empresa a consulta varre as quatro bases inteiras — só roda quando a
  // pessoa confirma. A confirmação vale apenas para a combinação de filtros atual.
  const [todasConfirmadas, setTodasConfirmadas] = useState<string | null>(null);
  const podeCarregar = parametros.codColigada !== undefined || todasConfirmadas === chaveConsulta;

  const { data: linhas = [], error, isFetching, isSuccess, refetch } = useQuery({
    queryKey: ["balancete", parametros],
    queryFn: () => carregarBalancete(parametros),
    enabled: podeCarregar,
  });
  const erro = error ? extrairMensagemDeErro(error, "Não foi possível carregar o balancete.") : null;

  // O reduzido é da conta escolhida nos filtros; a lista vem do cache do backend.
  const { data: contas = [] } = useQuery({
    queryKey: ["balancete-contas", parametros.codColigada, parametros.codFilial],
    queryFn: () => listarContasBalancete({ codColigada: parametros.codColigada, codFilial: parametros.codFilial }),
    enabled: Boolean(filtros.conta),
  });
  const codigoReduzido = useMemo(() => {
    if (!filtros.conta) return null;
    const codigos = [...new Set(contas.filter((item) => item.codConta === filtros.conta)
      .map((item) => item.reduzido.trim()).filter(Boolean))];
    return codigos.length > 1 ? "Vários" : codigos[0] ?? null;
  }, [filtros.conta, contas]);

  // Expansão e página recomeçam quando os filtros mudam.
  const [chaveTabela, setChaveTabela] = useState(chaveConsulta);
  const [expandidas, setExpandidas] = useState<Set<string>>(() => new Set());
  const [pagina, setPagina] = useState(1);
  if (chaveTabela !== chaveConsulta) {
    setChaveTabela(chaveConsulta);
    setExpandidas(new Set());
    setPagina(1);
  }

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
  const carregado = podeCarregar && isSuccess;

  function alternarExpansao(chave: string) {
    setExpandidas((atuais) => {
      const proximas = new Set(atuais);
      if (proximas.has(chave)) proximas.delete(chave);
      else proximas.add(chave);
      return proximas;
    });
  }

  return (
    <>
      <ResumoFiltros campos={CAMPOS_FILTRO_RESUMO} acoes={podeCarregar ? (
        <Button variant="secondary" icon={<RefreshCw size={16} />} isLoading={isFetching} onClick={() => void refetch()}>Atualizar</Button>
      ) : (
        <Button icon={<RefreshCw size={16} />} onClick={() => setTodasConfirmadas(chaveConsulta)}>Carregar todas as empresas</Button>
      )} />

      {erro && <p className="sp-balance__error" role="alert">{erro}</p>}

      <section className="sp-balance__report" aria-label="Resultado do balancete">
        <div className="sp-balance__report-head">
          <div>
            <h2>Balancete de {periodoPorExtenso(parametros.anoMes)}</h2>
            <p>
              {carregado
                ? `${linhas.length} contas e grupos encontrados · ${linhasVisiveis.length} visíveis`
                : isFetching
                  ? "Carregando o balancete..."
                  : !podeCarregar
                    ? "Sem empresa escolhida, o balancete reúne todas as empresas e demora mais. Escolha uma empresa nos filtros ou clique em \"Carregar todas as empresas\"."
                    : "Não foi possível carregar o balancete."}
              {codigoReduzido && ` · Cód. reduzido da conta: ${codigoReduzido}`}
            </p>
          </div>
        </div>
        {carregado && (
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
                                onClick={() => alternarExpansao(chaveConta(item))}>
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
    </>
  );
}
