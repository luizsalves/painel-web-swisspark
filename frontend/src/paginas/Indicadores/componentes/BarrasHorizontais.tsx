export interface LinhaBarra {
  rotulo: string;
  valor: number;
  texto: string;
  /** Cor da barra e do valor; "neutro" é cinza (ex.: o que está sem classificação). */
  tom: "positivo" | "negativo" | "neutro";
}

/** Nome e valor em cima, barra de 8 px num trilho embaixo; largura = |valor| ÷ maior |valor|. */
export default function BarrasHorizontais({ linhas }: { linhas: LinhaBarra[] }) {
  const maior = Math.max(...linhas.map((linha) => Math.abs(linha.valor)), 1);
  return (
    <div className="sp-ind-barras">
      {linhas.map((linha) => (
        <div key={linha.rotulo} className="sp-ind-barras__linha">
          <div className="sp-ind-barras__topo">
            <span>{linha.rotulo}</span>
            <strong className={`sp-ind-barras__valor--${linha.tom}`}>{linha.texto}</strong>
          </div>
          <div className="sp-ind-trilho">
            <div className={`sp-ind-trilho__barra sp-ind-barras__barra--${linha.tom}`}
              style={{ width: `${(Math.abs(linha.valor) / maior) * 100}%` }} />
          </div>
        </div>
      ))}
    </div>
  );
}
