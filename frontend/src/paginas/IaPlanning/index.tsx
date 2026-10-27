import { useEffect, useRef, useState } from "react";
import { Cpu, Sparkles, BarChart3, X, ChevronDown, ArrowUp, SquarePen, Users, ListChecks, FileSearch, TrendingUp } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import UserMenu from "../../componentes/layout/UserMenu";
import BotaoFiltros from "../../componentes/filtros/BotaoFiltros";
import ResumoFiltros from "../../componentes/filtros/ResumoFiltros";
import { useFiltros } from "../../contexts/FiltrosContext";

import LoadingSpinner from "../../componentes/ui/LoadingSpinner";

import { extrairMensagemDeErro } from "../../services/api";
import { perguntarCfoTati } from "../../services/cfoTatiService";
import type { MensagemCfoTati } from "../../interfaces/CfoTati";
import { coligadaDaEmpresa } from "../../utils/empresaContabil";
import { descreverStatus } from "../../utils/filtrosPainel";
// Os mesmos filtros (e a mesma conta) da Produtividade: as duas telas mostram os mesmos números.
import { CAMPOS_FILTRO, CAMPOS_FILTRO_CONSOLIDADO, useResumoProdutividade } from "../Dashboard/useResumoProdutividade";

const SUGESTOES_RAPIDAS = [
  "Quem mais fechou contas este mês?",
  "Como está a conciliação do período?",
  "Crie um gráfico por empresa",
];

// Os 4 assuntos da Tati, com o mesmo texto das opções da saudação dela.
const ASSUNTOS_TATI = [
  { rotulo: "Produtividade do time", pergunta: "Ver a produtividade do time", Icone: Users },
  { rotulo: "Acompanhamento da conciliação", pergunta: "Acompanhar a conciliação do período", Icone: ListChecks },
  { rotulo: "Fechamento de uma conta", pergunta: "Verificar o fechamento de uma conta", Icone: FileSearch },
  { rotulo: "Saúde financeira", pergunta: "Ver a saúde financeira da empresa", Icone: TrendingUp },
];

function horarioAtual(): string {
  return new Date().toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

function formatarMes(anoMes: string): string {
  const data = new Date(`${anoMes}-01T12:00:00`);
  return Number.isNaN(data.getTime())
    ? anoMes
    : data.toLocaleDateString("pt-BR", { month: "long", year: "numeric" });
}

function formatarPeriodo(inicio: string, fim: string): string {
  return inicio === fim ? formatarMes(fim) : `${formatarMes(inicio)} a ${formatarMes(fim)}`;
}

function mensagemInicial(): MensagemCfoTati {
  return {
    autor: "tati",
    texto: "Olá! Sou a Tati. Posso analisar suas contas e criar insights para você.",
    horario: horarioAtual(),
  };
}

interface GraficoTati {
  id: number;
  titulo: string;
  itens: { label: string; valor: number }[];
  horario: string;
}

// O fechamento ``` é opcional: se a resposta vier cortada, o bloco ainda some do texto.
const BLOCO_OPCOES = /```opcoes\s*([\s\S]*?)(?:```|$)/;
const BLOCO_GRAFICO = /```grafico\s*([\s\S]*?)(?:```|$)/;

function lerJson(bruto: string): unknown {
  try {
    return JSON.parse(bruto);
  } catch {
    return null;
  }
}

// Resposta cortada no meio do JSON: aproveita os itens que chegaram inteiros.
function recuperarGraficoCortado(bruto: string): { titulo?: string; itens: { label: string; valor: number }[] } | null {
  const titulo = bruto.match(/"titulo"\s*:\s*"([^"]*)"/)?.[1];
  const itens = [...bruto.matchAll(/\{\s*"label"\s*:\s*"([^"]+)"\s*,\s*"valor"\s*:\s*(-?[\d.]+)\s*\}/g)].map((m) => ({
    label: m[1],
    valor: Number(m[2]),
  }));
  return itens.length > 0 ? { titulo, itens } : null;
}

function lerRespostaTati(texto: string): {
  corpo: string;
  opcoes: string[];
  grafico: Omit<GraficoTati, "id" | "horario"> | null;
} {
  let corpo = texto;
  let opcoes: string[] = [];
  let grafico: Omit<GraficoTati, "id" | "horario"> | null = null;

  const blocoOpcoes = corpo.match(BLOCO_OPCOES);
  if (blocoOpcoes) {
    corpo = corpo.replace(BLOCO_OPCOES, "");
    const lista = lerJson(blocoOpcoes[1]);
    if (Array.isArray(lista)) {
      opcoes = lista.filter((item): item is string => typeof item === "string" && item.trim() !== "");
    }
  }

  const blocoGrafico = corpo.match(BLOCO_GRAFICO);
  if (blocoGrafico) {
    corpo = corpo.replace(BLOCO_GRAFICO, "");
    const dados = (lerJson(blocoGrafico[1]) ?? recuperarGraficoCortado(blocoGrafico[1])) as { titulo?: unknown; itens?: unknown } | null;
    if (dados && Array.isArray(dados.itens)) {
      const itens = dados.itens
        .map((item: { label?: unknown; valor?: unknown }) => ({ label: String(item?.label ?? ""), valor: Number(item?.valor) }))
        .filter((item) => item.label && Number.isFinite(item.valor));
      if (itens.length > 0) {
        grafico = { titulo: typeof dados.titulo === "string" ? dados.titulo : "Gráfico da Tati", itens };
      }
    }
  }

  return { corpo: corpo.trim(), opcoes, grafico };
}

function renderizarTexto(texto: string) {
  return texto.split(/(\*\*[^*]+\*\*)/g).map((parte, indice) =>
    parte.startsWith("**") && parte.endsWith("**") ? <strong key={indice}>{parte.slice(2, -2)}</strong> : parte
  );
}

function GraficoTatiCard({ grafico, onRemover }: { grafico: GraficoTati; onRemover: () => void }) {
  const maior = Math.max(...grafico.itens.map((item) => Math.abs(item.valor)), 1);

  return (
    <div className="sp-card ia-grafico-tati" id={`grafico-tati-${grafico.id}`}>
      <div className="ia-grafico-tati__topo">
        <div>
          <h2 className="sp-card__title">{grafico.titulo}</h2>
          <small>Criado pela Tati às {grafico.horario}</small>
        </div>
        <button type="button" onClick={onRemover} aria-label={`Remover o gráfico ${grafico.titulo}`}>
          <X size={16} />
        </button>
      </div>
      <ul className="ia-grafico-tati__lista">
        {grafico.itens.map((item) => (
          <li key={item.label}>
            <span className="ia-grafico-tati__label" title={item.label}>{item.label}</span>
            <span className="ia-grafico-tati__trilho">
              <span className="ia-grafico-tati__barra" style={{ width: `${(Math.abs(item.valor) / maior) * 100}%` }} />
            </span>
            <b>{item.valor.toLocaleString("pt-BR")}</b>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default function IaPlanning() {
  const { filtros } = useFiltros();
  // O resumo e a Tati identificam a empresa pelo nome (GFILIAL.NOME), o mesmo da gaveta.
  const { inicio, fim, empresa, consolidada, filtroContas, erro } = useResumoProdutividade(filtros);
  const camposFiltro = consolidada ? CAMPOS_FILTRO_CONSOLIDADO : CAMPOS_FILTRO;
  const variosMeses = inicio !== fim;

  const [mensagens, setMensagens] = useState<MensagemCfoTati[]>(() => [mensagemInicial()]);
  const [pergunta, setPergunta] = useState("");
  const [isEnviando, setIsEnviando] = useState(false);
  const [chatRecolhido, setChatRecolhido] = useState(false);
  const [graficosTati, setGraficosTati] = useState<GraficoTati[]>([]);
  const proximoIdGrafico = useRef(1);
  const listaMensagensRef = useRef<HTMLDivElement>(null);
  const areaGraficosRef = useRef<HTMLDivElement>(null);
  const mostrarGraficos = () => areaGraficosRef.current?.scrollIntoView({ behavior: "smooth", block: "nearest" });
  const mostrarGrafico = (id: number) =>
    document.getElementById(`grafico-tati-${id}`)?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });

  // O que a Tati recebe dos filtros da tela, mostrado na barra lateral.
  const contextoAtual = [
    { rotulo: "Empresa", valor: empresa || "Todas as empresas" },
    { rotulo: "Período", valor: formatarPeriodo(inicio, fim) },
    {
      rotulo: "Filial",
      valor: consolidada ? "Todas (consolidadas)" : filtroContas.codFilial !== undefined ? `Filial ${filtroContas.codFilial}` : "Todas",
    },
    ...(filtroContas.codConta ? [{ rotulo: "Conta", valor: filtroContas.codConta }] : []),
    ...(filtroContas.status ? [{ rotulo: "Status", valor: descreverStatus(filtros) }] : []),
  ];

  useEffect(() => {
    const elemento = listaMensagensRef.current;
    if (elemento) elemento.scrollTop = elemento.scrollHeight;
  }, [mensagens, isEnviando]);

  const ultimaMensagem = mensagens[mensagens.length - 1];
  const opcoesAtuais = ultimaMensagem?.autor === "tati" ? lerRespostaTati(ultimaMensagem.texto).opcoes : [];

  const enviarPergunta = async (texto: string) => {
    let perguntaLimpa = texto.trim();
    if (!perguntaLimpa || isEnviando) return;

    // Digitar só o número escolhe a opção correspondente da última resposta.
    const numero = /^\d+$/.test(perguntaLimpa) ? Number(perguntaLimpa) : 0;
    if (numero >= 1 && numero <= opcoesAtuais.length) perguntaLimpa = opcoesAtuais[numero - 1];

    const historico = mensagens.map((m) => ({ autor: m.autor, texto: m.texto }));

    setMensagens((atual) => [...atual, { autor: "usuario", texto: perguntaLimpa, horario: horarioAtual() }]);
    setPergunta("");
    setIsEnviando(true);

    try {
      const resposta = await perguntarCfoTati({
        pergunta: perguntaLimpa,
        anoMes: fim,
        anoMesInicial: variosMeses ? inicio : undefined,
        empresa: empresa || undefined,
        codColigada: filtros.empresa ? coligadaDaEmpresa(filtros.empresa) : undefined,
        codFilial: filtroContas.codFilial,
        codConta: filtroContas.codConta,
        status: filtroContas.status,
        historico,
      });
      const horario = horarioAtual();
      const contexto = [
        formatarPeriodo(inicio, fim),
        empresa || "Todas as empresas",
        filtroContas.codFilial !== undefined && `filial ${filtroContas.codFilial}`,
        filtroContas.codConta && `conta ${filtroContas.codConta}`,
        filtroContas.status && descreverStatus(filtros),
      ].filter(Boolean).join(" · ");
      setMensagens((atual) => [...atual, { autor: "tati", texto: resposta, horario, contexto }]);

      const { grafico } = lerRespostaTati(resposta);
      if (grafico) {
        const id = proximoIdGrafico.current++;
        setGraficosTati((atual) => [{ ...grafico, id, horario }, ...atual]);
        mostrarGraficos();
      }
    } catch (erroCapturado) {
      const mensagemDeErro = extrairMensagemDeErro(erroCapturado, "Não consegui falar com a IA agora.");
      setMensagens((atual) => [...atual, { autor: "tati", texto: mensagemDeErro, horario: horarioAtual() }]);
    } finally {
      setIsEnviando(false);
    }
  };

  // Nova conversa começa do zero, inclusive sem os gráficos da conversa anterior.
  const limparConversa = () => {
    setMensagens([mensagemInicial()]);
    setPergunta("");
    setGraficosTati([]);
  };
  const conversaVazia = mensagens.length <= 1 && graficosTati.length === 0;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    void enviarPergunta(pergunta);
  };

  return (
    <AppLayout className="sp-planning-shell">
      <header className="ia-topbar">
        <BotaoFiltros campos={CAMPOS_FILTRO} />
        <UserMenu />
        <div className="ia-brand"><span className="ia-chip"><Cpu size={42} /><b>AI</b></span><div><strong>IA Planning</strong><small>Inteligência financeira</small></div></div>
      </header>
      <main className="sp-dashboard ia-page">
        <div className="ia-intro">
          <div className="ia-heading"><h1>Visão financeira <span>Swiss Park</span></h1><h2>Conciliação contábil com inteligência artificial</h2><p>Acompanhe o status das conciliações, identifique pendências e obtenha insights com a CFO Tati.</p></div>
        <ResumoFiltros campos={camposFiltro} />

        </div>
        {erro && (
          <span className="sp-closing__feedback sp-closing__feedback--error" role="alert">
            {erro}
          </span>
        )}

        <section className="sp-ia-planning__workspace">
          {/* Em cima, os gráficos que a Tati cria (o mais novo primeiro); embaixo, a conversa;
              à direita, a barra lateral. */}
          <div className="ia-principal">
          <div className="sp-ia-planning__charts" ref={areaGraficosRef}>
            <div
              className={`ia-graficos-tati${graficosTati.length === 0 ? " ia-graficos-tati--vazio" : ""}`}
              aria-label="Gráficos criados pela Tati"
            >
              {graficosTati.length === 0 ? (
                <div className="sp-card ia-graficos-tati__vazio">
                  <Sparkles size={22} />
                  <p>Os gráficos que a Tati criar aparecem aqui. Peça um no chat — por exemplo, <button type="button" disabled={isEnviando} onClick={() => void enviarPergunta("Crie um gráfico por empresa")}>“Crie um gráfico por empresa”</button>.</p>
                </div>
              ) : (
                graficosTati.map((grafico) => (
                  <GraficoTatiCard
                    key={grafico.id}
                    grafico={grafico}
                    onRemover={() => setGraficosTati((atual) => atual.filter((g) => g.id !== grafico.id))}
                  />
                ))
              )}
            </div>
          </div>

          <aside className={`ia-ask${chatRecolhido ? " ia-ask--recolhido" : ""}`} aria-label="Pergunte à CFO Tati">
            <div className="ia-ask__topo">
              <span className="ia-ask__selo">
                <Sparkles size={14} aria-hidden="true" /> Pergunte à Tati
              </span>
              <button
                type="button"
                className="ia-ask__limpar"
                onClick={limparConversa}
                disabled={isEnviando || conversaVazia}
              >
                Limpar conversa
              </button>
              <button
                type="button"
                className="ia-ask__recolher"
                onClick={() => setChatRecolhido((atual) => !atual)}
                aria-expanded={!chatRecolhido}
                aria-label={chatRecolhido ? "Abrir a conversa" : "Recolher a conversa"}
              >
                <ChevronDown size={18} />
              </button>
            </div>

            {!chatRecolhido && (
              <>
                <div className="ia-ask__mensagens" ref={listaMensagensRef} role="log" aria-live="polite" aria-label="Mensagens da conversa">
                  {mensagens.map((mensagem, index) => {
                    if (mensagem.autor === "usuario") {
                      return (
                        <div key={`${mensagem.horario}-${index}`} className="ia-ask__pergunta">
                          {mensagem.texto}
                          <span className="ia-ask__hora">{mensagem.horario}</span>
                        </div>
                      );
                    }

                    const { corpo, opcoes, grafico } = lerRespostaTati(mensagem.texto);
                    const ehUltima = index === mensagens.length - 1;

                    return (
                      <div key={`${mensagem.horario}-${index}`} className="ia-ask__resposta">
                        <span className="ia-ask__rotulo">
                          <span className="ia-tati-avatar ia-ask__avatar" aria-hidden="true" />
                          Tati
                        </span>
                        <div className="ia-ask__cartao">
                          <p>{renderizarTexto(corpo)}</p>
                          {grafico && (
                            <button type="button" className="ia-ask__ver-grafico" onClick={mostrarGraficos}>
                              <BarChart3 size={14} /> Ver gráfico: {grafico.titulo}
                            </button>
                          )}
                          {mensagem.contexto && (
                            <span className="ia-ask__base">
                              Com base em: <b>{mensagem.contexto}</b>
                            </span>
                          )}
                          <span className="ia-ask__hora">{mensagem.horario}</span>
                        </div>
                        {opcoes.length > 0 && (
                          <ol className="ia-ask__opcoes">
                            {opcoes.map((opcao, indiceOpcao) => (
                              <li key={opcao}>
                                <button
                                  type="button"
                                  disabled={!ehUltima || isEnviando}
                                  onClick={() => void enviarPergunta(opcao)}
                                >
                                  <b>{indiceOpcao + 1}</b>
                                  <span>{opcao}</span>
                                </button>
                              </li>
                            ))}
                          </ol>
                        )}
                      </div>
                    );
                  })}
                  {isEnviando && (
                    <div className="ia-ask__digitando">
                      <LoadingSpinner size={14} color="#ffd23f" />
                      <span>Tati está analisando...</span>
                    </div>
                  )}
                </div>

                {/* Como no ChatGPT, as sugestões aparecem só antes da primeira pergunta; depois,
                    os próximos passos vêm nas opções numeradas de cada resposta. */}
                {mensagens.length <= 1 && !isEnviando && (
                  <div className="ia-ask__sugestoes">
                    {SUGESTOES_RAPIDAS.map((sugestao) => (
                      <button key={sugestao} type="button" onClick={() => void enviarPergunta(sugestao)}>
                        {sugestao}
                      </button>
                    ))}
                  </div>
                )}

                <form className="ia-ask__campo" onSubmit={handleSubmit}>
                  <input
                    type="text"
                    value={pergunta}
                    onChange={(event) => setPergunta(event.target.value)}
                    aria-label="Sua pergunta para a CFO Tati"
                    placeholder="Pergunte à Tati sobre a conciliação..."
                    disabled={isEnviando}
                  />
                  <button type="submit" aria-label="Enviar pergunta" disabled={isEnviando || !pergunta.trim()}>
                    <ArrowUp size={18} />
                  </button>
                </form>
              </>
            )}
          </aside>
          </div>

          {/* Barra lateral, como a do ChatGPT. Some nas telas estreitas, onde as sugestões e o
              "Limpar conversa" do chat fazem esse papel. */}
          <nav className="ia-lateral" aria-label="Atalhos da CFO Tati">
            <button type="button" className="ia-lateral__nova" onClick={limparConversa} disabled={isEnviando || conversaVazia}>
              <SquarePen size={17} /> Nova conversa
            </button>

            <div className="ia-lateral__secao">
              <h3>Assuntos</h3>
              <ul>
                {ASSUNTOS_TATI.map(({ rotulo, pergunta: perguntaDoAssunto, Icone }) => (
                  <li key={rotulo}>
                    <button type="button" className="ia-lateral__item" disabled={isEnviando} onClick={() => void enviarPergunta(perguntaDoAssunto)}>
                      <Icone size={16} /> {rotulo}
                    </button>
                  </li>
                ))}
              </ul>
            </div>

            <div className="ia-lateral__secao">
              <h3>Usando agora</h3>
              <dl className="ia-lateral__contexto">
                {contextoAtual.map(({ rotulo, valor }) => (
                  <div key={rotulo}>
                    <dt>{rotulo}</dt>
                    <dd title={valor}>{valor}</dd>
                  </div>
                ))}
              </dl>
            </div>

            <div className="ia-lateral__secao">
              <h3>Gráficos da conversa</h3>
              {graficosTati.length === 0 ? (
                <p className="ia-lateral__vazio">Nenhum gráfico ainda.</p>
              ) : (
                <ul>
                  {graficosTati.map((grafico) => (
                    <li key={grafico.id}>
                      <button type="button" className="ia-lateral__item" onClick={() => mostrarGrafico(grafico.id)}>
                        <BarChart3 size={16} />
                        <span>{grafico.titulo}</span>
                        <small>{grafico.horario}</small>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </nav>
        </section>
      </main>
    </AppLayout>
  );
}
