/**
 * Espaço para o sócio e o contador assinarem no fim dos relatórios do "Gerar
 * PDF" (balanço e DFC). Só aparece na impressão, dentro de sp-dfc-print.
 */
export default function AssinaturasImpressao() {
  return (
    <section className="sp-dfc-print__assinaturas" aria-label="Assinaturas">
      {["Sócio", "Contador"].map((papel) => (
        <div key={papel}>
          <div className="sp-dfc-print__assinatura-linha" />
          <strong>Assinatura do {papel}</strong>
          <span>Data: ____/____/______</span>
        </div>
      ))}
    </section>
  );
}
