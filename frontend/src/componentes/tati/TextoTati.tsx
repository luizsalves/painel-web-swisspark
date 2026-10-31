import { Fragment, useMemo } from "react";
import { destinoDoCard } from "./cardsDaTati";
import { lerBlocos, lerTrechos } from "./lerTextoTati";
import type { Bloco, Trecho } from "./lerTextoTati";

interface TextoTatiProps {
  texto: string;
  /** O clique num número com link para o card; sem ele (na tela da Tati), o número aparece como texto. */
  onCard?: (alvo: string) => void;
}

/** O texto de uma resposta da Tati: parágrafos, títulos, listas e tabelas, com os números clicáveis. */
export default function TextoTati({ texto, onCard }: TextoTatiProps) {
  const blocos = useMemo(() => lerBlocos(texto), [texto]);
  return (
    <div className="ia-texto">
      {blocos.map((bloco, indice) => <BlocoDoTexto key={indice} bloco={bloco} onCard={onCard} />)}
    </div>
  );
}

function BlocoDoTexto({ bloco, onCard }: { bloco: Bloco; onCard?: (alvo: string) => void }) {
  switch (bloco.tipo) {
    case "titulo":
      return <p className="ia-texto__titulo"><Linha texto={bloco.texto} onCard={onCard} /></p>;
    case "lista": {
      const Lista = bloco.ordenada ? "ol" : "ul";
      return (
        <Lista className="ia-texto__lista">
          {bloco.itens.map((item, indice) => <li key={indice}><Linha texto={item} onCard={onCard} /></li>)}
        </Lista>
      );
    }
    case "tabela":
      return (
        <div className="ia-texto__tabela">
          <table>
            <thead>
              <tr>{bloco.cabecalho.map((celula, indice) => <th key={indice}><Linha texto={celula} onCard={onCard} /></th>)}</tr>
            </thead>
            <tbody>
              {bloco.linhas.map((linha, indice) => (
                <tr key={indice}>{linha.map((celula, coluna) => <td key={coluna}><Linha texto={celula} onCard={onCard} /></td>)}</tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    default:
      return (
        <p>
          {bloco.linhas.map((linha, indice) => (
            <Fragment key={indice}>{indice > 0 && "\n"}<Linha texto={linha} onCard={onCard} /></Fragment>
          ))}
        </p>
      );
  }
}

function Linha({ texto, onCard }: { texto: string; onCard?: (alvo: string) => void }) {
  return <Trechos trechos={lerTrechos(texto)} onCard={onCard} />;
}

function Trechos({ trechos, onCard }: { trechos: Trecho[]; onCard?: (alvo: string) => void }) {
  return (
    <>
      {trechos.map((trecho, indice) => {
        if (trecho.tipo === "negrito") return <strong key={indice}><Trechos trechos={trecho.trechos} onCard={onCard} /></strong>;
        if (trecho.tipo === "card" && onCard && destinoDoCard(trecho.alvo)) {
          return (
            <button key={indice} type="button" className="tati-link-card" title="Ver o card de onde veio este número"
              onClick={() => onCard(trecho.alvo)}>
              {trecho.texto}
            </button>
          );
        }
        return <Fragment key={indice}>{trecho.texto}</Fragment>;
      })}
    </>
  );
}
