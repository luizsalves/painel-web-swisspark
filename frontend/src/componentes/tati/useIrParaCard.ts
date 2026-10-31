import { useCallback, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { destinoDoCard } from "./cardsDaTati";

// A tela de destino pode levar alguns segundos para mostrar o card: os indicadores são calculados na hora.
const ESPERA_MAXIMA_MS = 20_000;

// Por dentro do card, para o destaque não ser cortado por uma área com rolagem.
const DESTAQUE: Keyframe[] = [
  { boxShadow: "inset 0 0 0 3px rgba(255, 210, 63, 0)" },
  { boxShadow: "inset 0 0 0 3px rgba(255, 210, 63, 0.95)" },
  { boxShadow: "inset 0 0 0 3px rgba(255, 210, 63, 0)" },
];

/**
 * O clique num número do resumo da Tati: vai para a tela (e a aba) do card e, quando ele aparece,
 * rola até ele e pisca o contorno. O card é achado pelo data-card que cada tela põe nele; os
 * filtros são os da gaveta, então a tela mostra o mesmo recorte do resumo.
 */
export function useIrParaCard() {
  const navigate = useNavigate();
  const { pathname, search } = useLocation();
  const [procurado, setProcurado] = useState<{ ancora: string; vez: number } | null>(null);

  const irParaCard = useCallback((alvo: string) => {
    const destino = destinoDoCard(alvo);
    if (!destino) return;
    const atuais = new URLSearchParams(search);
    const mesmaTela = pathname === destino.caminho
      && Object.entries(destino.busca).every(([chave, valor]) => atuais.get(chave) === valor);
    if (!mesmaTela) navigate({ pathname: destino.caminho, search: new URLSearchParams(destino.busca).toString() });
    // A "vez" faz o mesmo card ser procurado de novo num segundo clique.
    setProcurado((anterior) => ({ ancora: destino.ancora, vez: (anterior?.vez ?? 0) + 1 }));
  }, [navigate, pathname, search]);

  useEffect(() => {
    if (!procurado) return;
    const seletor = `[data-card="${procurado.ancora}"]`;
    const destacar = () => {
      const card = document.querySelector<HTMLElement>(seletor);
      if (!card) return false;
      const semMovimento = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
      card.scrollIntoView({ behavior: semMovimento ? "auto" : "smooth", block: "center" });
      card.animate?.(DESTAQUE, { duration: 1100, iterations: 3, easing: "ease-in-out" });
      return true;
    };
    if (destacar()) return;

    const observador = new MutationObserver(() => {
      if (destacar()) parar();
    });
    const limite = window.setTimeout(() => parar(), ESPERA_MAXIMA_MS);
    function parar() {
      observador.disconnect();
      window.clearTimeout(limite);
    }
    observador.observe(document.body, { childList: true, subtree: true });
    return parar;
  }, [procurado]);

  return irParaCard;
}
