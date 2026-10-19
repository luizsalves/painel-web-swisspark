/** Lugar de um bloco que ainda não tem dados: borda tracejada e uma frase. */
export default function BlocoSemDados({ children }: { children: string }) {
  return <div className="sp-ind-sem-dados">{children}</div>;
}
