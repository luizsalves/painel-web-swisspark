import { useAuth } from "../../contexts/AuthContext";
import { useTati } from "../../contexts/TatiContext";

interface BotaoAnalisarTatiProps {
  /** O que o card mostra, para o leitor de tela e a dica: "Analisar <assunto> com a Tati". */
  assunto: string;
  /** A pergunta pronta, de perguntasAnalisar. */
  pergunta: string;
}

/**
 * O rostinho da Tati ao lado do título de um card: abre a conversa do ícone com a pergunta pronta.
 * Só para o superadmin, como a Tati.
 */
export default function BotaoAnalisarTati({ assunto, pergunta }: BotaoAnalisarTatiProps) {
  const { user } = useAuth();
  const { analisar } = useTati();
  if (!user?.isSuperAdmin) return null;

  return (
    <button
      type="button"
      className="tati-analisar"
      aria-label={`Analisar ${assunto} com a Tati`}
      title={`Analisar ${assunto} com a Tati`}
      // O card pode ter clique próprio (abrir linhas, ir para o detalhe).
      onClick={(event) => { event.stopPropagation(); analisar(pergunta); }}
    >
      <span className="ia-tati-avatar" aria-hidden="true" />
    </button>
  );
}
