import { useAuth } from "../../contexts/AuthContext";
import { useFiltros } from "../../contexts/FiltrosContext";
import { useTati } from "../../contexts/TatiContext";
import { perguntaDoResumo } from "./perguntasAnalisar";

/**
 * O "Resumo do mês" do Dashboard (ou "do período", com vários meses nos filtros): abre a conversa
 * do ícone da Tati e pede o resumo dos filtros da tela, com cada número ligado ao card de onde
 * veio. Só para o superadmin, como a Tati.
 */
export default function BotaoResumoTati() {
  const { user } = useAuth();
  const { filtros } = useFiltros();
  const { resumirMes } = useTati();
  if (!user?.isSuperAdmin) return null;

  const pergunta = perguntaDoResumo(filtros);
  return (
    <button type="button" className="tati-resumo" onClick={() => resumirMes(pergunta)}
      title="A Tati resume os filtros da tela, e cada número leva ao card de onde veio">
      <span className="ia-tati-avatar" aria-hidden="true" />
      {pergunta}
    </button>
  );
}
