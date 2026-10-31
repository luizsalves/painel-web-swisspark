/**
 * O texto da resposta da Tati em blocos, para as duas conversas (a tela da Tati e o ícone): títulos
 * (##), listas (- e 1.), tabelas (| a | b |) e parágrafos; dentro de cada linha, o negrito e os links
 * dos números para os cards ([R$ 1,2 mi](card:resultado/receitaLiquida)). O resto do markdown
 * passa como texto.
 */

export type Trecho =
  | { tipo: "texto"; texto: string }
  | { tipo: "negrito"; trechos: Trecho[] }
  | { tipo: "card"; texto: string; alvo: string };

export type Bloco =
  | { tipo: "paragrafo"; linhas: string[] }
  | { tipo: "titulo"; texto: string }
  | { tipo: "lista"; ordenada: boolean; itens: string[] }
  | { tipo: "tabela"; cabecalho: string[]; linhas: string[][] };

const TITULO = /^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$/;
const ITEM = /^\s{0,3}[-*•]\s+(.+)$/;
const ITEM_NUMERADO = /^\s{0,3}\d{1,2}[.)]\s+(.+)$/;
const LINHA_DE_TABELA = /^\s*\|.*\|\s*$/;
const SEPARADOR_DE_TABELA = /^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?\s*$/;
const LINHA_HORIZONTAL = /^\s{0,3}([-*_])(\s*\1){2,}\s*$/;

// O link primeiro: o rótulo pode vir em negrito ("[**R$ 1,2 mi**](card:...)").
const TRECHO_ESPECIAL = /(\[[^[\]\n]{1,120}\]\(card:[A-Za-z0-9_/-]{1,80}\)|\*\*[^*]+?\*\*)/g;
const LINK = /^\[([^[\]\n]{1,120})\]\(card:([A-Za-z0-9_/-]{1,80})\)$/;

function comecaTabela(linhas: string[], indice: number) {
  return LINHA_DE_TABELA.test(linhas[indice]) && indice + 1 < linhas.length && SEPARADOR_DE_TABELA.test(linhas[indice + 1]);
}

function celulas(linha: string): string[] {
  return linha.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((celula) => celula.trim());
}

export function lerBlocos(texto: string): Bloco[] {
  const linhas = texto.replace(/\r\n?/g, "\n").split("\n");
  const blocos: Bloco[] = [];
  let i = 0;

  while (i < linhas.length) {
    const linha = linhas[i];
    if (!linha.trim() || LINHA_HORIZONTAL.test(linha)) {
      i++;
      continue;
    }

    const titulo = TITULO.exec(linha);
    if (titulo) {
      blocos.push({ tipo: "titulo", texto: titulo[1] });
      i++;
      continue;
    }

    if (comecaTabela(linhas, i)) {
      const cabecalho = celulas(linha);
      const corpo: string[][] = [];
      for (i += 2; i < linhas.length && LINHA_DE_TABELA.test(linhas[i]); i++) corpo.push(celulas(linhas[i]));
      blocos.push({ tipo: "tabela", cabecalho, linhas: corpo });
      continue;
    }

    const ordenada = ITEM_NUMERADO.test(linha);
    if (ordenada || ITEM.test(linha)) {
      const padrao = ordenada ? ITEM_NUMERADO : ITEM;
      const itens: string[] = [];
      for (let item = padrao.exec(linhas[i]); item; item = i < linhas.length ? padrao.exec(linhas[i]) : null) {
        itens.push(item[1]);
        i++;
      }
      blocos.push({ tipo: "lista", ordenada, itens });
      continue;
    }

    // As linhas seguidas formam um parágrafo, com as quebras de linha (a tela usa pre-wrap).
    const paragrafo: string[] = [];
    while (
      i < linhas.length && linhas[i].trim() && !TITULO.test(linhas[i]) && !ITEM.test(linhas[i])
      && !ITEM_NUMERADO.test(linhas[i]) && !LINHA_HORIZONTAL.test(linhas[i]) && !comecaTabela(linhas, i)
    ) {
      paragrafo.push(linhas[i]);
      i++;
    }
    blocos.push({ tipo: "paragrafo", linhas: paragrafo });
  }
  return blocos;
}

export function lerTrechos(linha: string): Trecho[] {
  const trechos: Trecho[] = [];
  for (const parte of linha.split(TRECHO_ESPECIAL)) {
    if (!parte) continue;
    const link = LINK.exec(parte);
    if (link) {
      trechos.push({ tipo: "card", texto: link[1].replace(/\*\*/g, ""), alvo: link[2] });
    } else if (parte.length > 4 && parte.startsWith("**") && parte.endsWith("**")) {
      trechos.push({ tipo: "negrito", trechos: lerTrechos(parte.slice(2, -2)) });
    } else {
      trechos.push({ tipo: "texto", texto: parte });
    }
  }
  return trechos;
}
