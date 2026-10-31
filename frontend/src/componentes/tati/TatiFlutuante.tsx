import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { ArrowUp, Minus, SquarePen } from "lucide-react";
import { useAuth } from "../../contexts/AuthContext";
import { useFiltros } from "../../contexts/FiltrosContext";
import { useTati } from "../../contexts/TatiContext";
import { useEmpresasConsolidadas } from "../../hooks/useEmpresasConsolidadas";
import LoadingSpinner from "../ui/LoadingSpinner";
import { extrairMensagemDeErro } from "../../services/api";
import { perguntarCfoTatiNaTela } from "../../services/cfoTatiService";
import type { MensagemCfoTati } from "../../interfaces/CfoTati";
import { lerRespostaTati } from "../../paginas/IaPlanning/respostaTati";
import GraficoTatiCard from "../../paginas/IaPlanning/componentes/GraficoTatiCard";
import { descreverContexto, pedidoDaTela, telaDaTati } from "./telaDaTati";

const SUGESTOES = ["Como está esse mês?", "O que chama atenção aqui?"];

function horarioAtual(): string {
  return new Date().toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

function mensagemInicial(): MensagemCfoTati {
  return {
    autor: "tati",
    texto: "Olá! Sou a Tati. Pergunte sobre a tela que você está vendo — por exemplo, “como está esse mês?”.",
    horario: horarioAtual(),
  };
}

function renderizarTexto(texto: string) {
  return texto.split(/(\*\*[^*]+\*\*)/g).map((parte, indice) =>
    parte.startsWith("**") && parte.endsWith("**") ? <strong key={indice}>{parte.slice(2, -2)}</strong> : parte
  );
}

/**
 * A Tati nas outras telas: fechada, é o rostinho dela no canto; clicou, abre a conversa por cima
 * da tela. Fica fora das rotas, então a conversa continua quando o usuário troca de tela. Cada
 * pergunta vai com a tela (e a aba) aberta e os filtros que ela usa; os botões "Analisar" dos
 * cards mandam a pergunta pronta por aqui (TatiContext). Só para o superadmin, como a tela da
 * Tati; outro usuário que entra começa uma conversa nova.
 */
export default function TatiFlutuante() {
  const { user } = useAuth();
  return user?.isSuperAdmin ? <ConversaFlutuante key={user.id} /> : null;
}

function ConversaFlutuante() {
  const { filtros } = useFiltros();
  const { pedido, atender } = useTati();
  const { pathname, search } = useLocation();
  const consolidaFiliais = useEmpresasConsolidadas();
  const consolidada = Boolean(consolidaFiliais(filtros.empresa));
  const tela = telaDaTati(pathname, search);

  const [aberta, setAberta] = useState(false);
  const [mensagens, setMensagens] = useState<MensagemCfoTati[]>(() => [mensagemInicial()]);
  const [pergunta, setPergunta] = useState("");
  const [isEnviando, setIsEnviando] = useState(false);
  // Os gráficos que o usuário fechou, pelo índice da mensagem.
  const [graficosFechados, setGraficosFechados] = useState<Set<number>>(() => new Set());
  const listaMensagensRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLInputElement>(null);

  const limparConversa = () => {
    setMensagens([mensagemInicial()]);
    setPergunta("");
    setGraficosFechados(new Set());
  };

  useEffect(() => {
    const elemento = listaMensagensRef.current;
    if (elemento) elemento.scrollTop = elemento.scrollHeight;
  }, [mensagens, isEnviando, aberta]);

  useEffect(() => {
    if (aberta) campoRef.current?.focus();
  }, [aberta]);

  const ultimaMensagem = mensagens[mensagens.length - 1];
  const opcoesAtuais = ultimaMensagem?.autor === "tati" ? lerRespostaTati(ultimaMensagem.texto).opcoes : [];
  const contexto = tela ? descreverContexto(tela, filtros, consolidada) : "";

  const enviarPergunta = async (texto: string) => {
    let perguntaLimpa = texto.trim();
    if (!perguntaLimpa || isEnviando || !tela) return;

    // Digitar só o número escolhe a opção correspondente da última resposta.
    const numero = /^\d+$/.test(perguntaLimpa) ? Number(perguntaLimpa) : 0;
    if (numero >= 1 && numero <= opcoesAtuais.length) perguntaLimpa = opcoesAtuais[numero - 1];

    const historico = mensagens.map((m) => ({ autor: m.autor, texto: m.texto }));

    setMensagens((atual) => [...atual, { autor: "usuario", texto: perguntaLimpa, horario: horarioAtual() }]);
    setPergunta("");
    setIsEnviando(true);

    try {
      const resposta = await perguntarCfoTatiNaTela({ pergunta: perguntaLimpa, historico, ...pedidoDaTela(tela, filtros, consolidada) });
      setMensagens((atual) => [...atual, { autor: "tati", texto: resposta, horario: horarioAtual(), contexto }]);
    } catch (erroCapturado) {
      const mensagemDeErro = extrairMensagemDeErro(erroCapturado, "Não consegui falar com a IA agora.");
      setMensagens((atual) => [...atual, { autor: "tati", texto: mensagemDeErro, horario: horarioAtual() }]);
    } finally {
      setIsEnviando(false);
    }
  };

  // Um botão "Analisar" de um card pediu uma análise: abre a conversa e manda a pergunta pronta,
  // depois da resposta em andamento, se houver uma.
  useEffect(() => {
    if (!pedido || isEnviando || !tela) return;
    atender(pedido.id);
    setAberta(true);
    void enviarPergunta(pedido.pergunta);
  }, [pedido, isEnviando, tela, atender, enviarPergunta]);

  // Na tela da Tati, no login e na troca de senha o ícone some, mas a conversa fica guardada.
  if (!tela) return null;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    void enviarPergunta(pergunta);
  };

  if (!aberta) {
    return (
      <div className="tati-flutuante">
        <button type="button" className="tati-flutuante__icone" onClick={() => setAberta(true)}
          aria-label="Abrir a conversa com a Tati" title="Pergunte à Tati">
          <span className="ia-tati-avatar" aria-hidden="true" />
        </button>
      </div>
    );
  }

  return (
    <div className="tati-flutuante">
      <aside className="ia-ask tati-flutuante__painel" aria-label="Conversa com a CFO Tati"
        onKeyDown={(event) => { if (event.key === "Escape") setAberta(false); }}>
        <div className="tati-flutuante__topo">
          <span className="ia-tati-avatar" aria-hidden="true" />
          <div className="tati-flutuante__titulo">
            <strong>Tati</strong>
            <small title={contexto}>{contexto}</small>
          </div>
          <button type="button" className="ia-ask__recolher" onClick={limparConversa}
            disabled={isEnviando || mensagens.length <= 1} aria-label="Nova conversa" title="Nova conversa">
            <SquarePen size={15} />
          </button>
          <button type="button" className="ia-ask__recolher" onClick={() => setAberta(false)}
            aria-label="Fechar a conversa" title="Fechar (a conversa continua)">
            <Minus size={18} />
          </button>
        </div>

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
                  {mensagem.contexto && (
                    <span className="ia-ask__base">
                      Com base em: <b>{mensagem.contexto}</b>
                    </span>
                  )}
                  <span className="ia-ask__hora">{mensagem.horario}</span>
                </div>
                {grafico && !graficosFechados.has(index) && (
                  <GraficoTatiCard
                    grafico={{ ...grafico, id: index, horario: mensagem.horario }}
                    onRemover={() => setGraficosFechados((atual) => new Set(atual).add(index))}
                  />
                )}
                {opcoes.length > 0 && (
                  <ol className="ia-ask__opcoes">
                    {opcoes.map((opcao, indiceOpcao) => (
                      <li key={opcao}>
                        <button type="button" disabled={!ehUltima || isEnviando} onClick={() => void enviarPergunta(opcao)}>
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

        {mensagens.length <= 1 && !isEnviando && (
          <div className="ia-ask__sugestoes">
            {SUGESTOES.map((sugestao) => (
              <button key={sugestao} type="button" onClick={() => void enviarPergunta(sugestao)}>
                {sugestao}
              </button>
            ))}
          </div>
        )}

        <form className="ia-ask__campo" onSubmit={handleSubmit}>
          <input
            ref={campoRef}
            type="text"
            value={pergunta}
            onChange={(event) => setPergunta(event.target.value)}
            aria-label="Sua pergunta para a CFO Tati"
            placeholder={`Pergunte sobre ${tela.aba ?? tela.tela}...`}
            disabled={isEnviando}
          />
          <button type="submit" aria-label="Enviar pergunta" disabled={isEnviando || !pergunta.trim()}>
            <ArrowUp size={18} />
          </button>
        </form>
      </aside>
    </div>
  );
}
