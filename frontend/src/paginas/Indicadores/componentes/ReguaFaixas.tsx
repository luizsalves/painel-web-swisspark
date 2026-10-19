import { formatarValor, ROTULO_STATUS } from "../regras";
import type { ResultadoIndicador } from "../regras";

/**
 * Trilho de 150 × 16 px com as três faixas na escala do indicador e um marcador
 * no valor. Em "maior é melhor" a ordem é risco, atenção, bom; em "menor é
 * melhor", bom, atenção, risco.
 */
export default function ReguaFaixas({ indicador }: { indicador: ResultadoIndicador }) {
  const { config, valorFaixa, status, semFaixa } = indicador;
  const escala = config.escala;
  if (semFaixa || !escala || valorFaixa === null || status === "verificar") {
    return <span className="sp-ind-regua-nota">{semFaixa ?? "Sem faixa definida"}</span>;
  }

  const largura = escala.max - escala.min;
  const fatia = (de: number, ate: number) => Math.max(0, ((ate - de) / largura) * 100);
  const [inferior, superior] = config.direcao === "maior_melhor" ? [config.risco, config.bom] : [config.bom, config.risco];
  const faixas = config.direcao === "maior_melhor" ? ["risco", "atencao", "bom"] : ["bom", "atencao", "risco"];
  const larguras = [fatia(escala.min, inferior), fatia(inferior, superior), fatia(superior, escala.max)];
  // Entre 1% e 99%, para o marcador não sumir na borda.
  const posicao = Math.min(99, Math.max(1, ((valorFaixa - escala.min) / largura) * 100));

  return (
    <div className="sp-ind-regua" role="img"
      aria-label={`${formatarValor(valorFaixa, config)} na faixa ${ROTULO_STATUS[status]}`}>
      <div className="sp-ind-regua__trilho">
        {faixas.map((faixa, indice) => (
          <div key={faixa} className={`sp-ind-regua__faixa sp-ind-regua__faixa--${faixa}`} style={{ width: `${larguras[indice]}%` }} />
        ))}
      </div>
      <div className="sp-ind-regua__marcador" style={{ left: `${posicao}%` }} />
    </div>
  );
}
