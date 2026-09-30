import { useEffect, useMemo, useState } from "react";
import { AlertTriangle } from "lucide-react";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import type { Diferencas } from "../../interfaces/Diferencas";

const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const moedaCompacta = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", notation: "compact", maximumFractionDigits: 1 });

interface PainelDiferencasProps {
  diferencas: Diferencas | null;
  carregando: boolean;
  erro: string | null;
}

export default function PainelDiferencas({ diferencas, carregando, erro }: PainelDiferencasProps) {
  const [empresaSelecionada, setEmpresaSelecionada] = useState<string | null>(null);

  const empresas = useMemo(() => diferencas?.empresas ?? [], [diferencas]);

  useEffect(() => {
    if (empresas.length === 0) {
      setEmpresaSelecionada(null);
    } else if (!empresaSelecionada || !empresas.some((e) => e.empresa === empresaSelecionada)) {
      setEmpresaSelecionada(empresas[0].empresa);
    }
  }, [empresas, empresaSelecionada]);

  const contasDaEmpresa = useMemo(
    () => (diferencas?.contas ?? []).filter((c) => c.empresa === empresaSelecionada),
    [diferencas, empresaSelecionada]
  );

  const maiorEmpresa = Math.max(...empresas.map((e) => e.valorAbsoluto), 1);
  const maiorConta = Math.max(...contasDaEmpresa.map((c) => Math.abs(c.diferenca)), 1);

  if (carregando || erro || !diferencas || empresas.length === 0) {
    return (
      <div className="sp-card ia-dif__estado">
        {carregando ? (
          <>
            <LoadingSpinner size={26} color="var(--color-green)" />
            <p>Calculando as diferenças da conciliação… na primeira vez do mês pode levar alguns minutos.</p>
          </>
        ) : erro ? (
          <p className="ia-dif__erro">{erro}</p>
        ) : (
          <p>Nenhuma diferença entre o saldo do razão e a composição do saldo neste período.</p>
        )}
      </div>
    );
  }

  return (
    <div className="ia-dif">
      <div className="sp-card ia-dif__card">
        <div className="ia-dif__topo">
          <h2 className="sp-card__title">Empresas com diferença</h2>
          <span className="ia-dif__selo">
            <AlertTriangle size={13} /> {diferencas.totalEmpresas} empresas · {diferencas.totalContas} contas
          </span>
        </div>
        <small className="ia-dif__legenda">Soma das diferenças (razão − composição), em valor absoluto. Clique para ver as contas.</small>
        <ol className="ia-dif__ranking">
          {empresas.map((empresa) => (
            <li key={empresa.empresa}>
              <button
                type="button"
                className={empresa.empresa === empresaSelecionada ? "ia-dif__empresa ia-dif__empresa--ativa" : "ia-dif__empresa"}
                onClick={() => setEmpresaSelecionada(empresa.empresa)}
                title={`${empresa.empresa} — ${moeda.format(empresa.valorAbsoluto)}`}
              >
                <span className="ia-dif__nome">{empresa.empresa}</span>
                <span className="ia-dif__trilho">
                  <span className="ia-dif__barra" style={{ width: `${(empresa.valorAbsoluto / maiorEmpresa) * 100}%` }} />
                </span>
                <b>{moedaCompacta.format(empresa.valorAbsoluto)}</b>
                <em>{empresa.contas} {empresa.contas === 1 ? "conta" : "contas"}</em>
              </button>
            </li>
          ))}
        </ol>
      </div>

      <div className="sp-card ia-dif__card">
        <div className="ia-dif__topo">
          <h2 className="sp-card__title" title={empresaSelecionada ?? ""}>Contas com diferença — {empresaSelecionada}</h2>
        </div>
        <small className="ia-dif__legenda">
          <i className="ia-dif__ponto ia-dif__ponto--pos" /> razão maior que a composição
          <i className="ia-dif__ponto ia-dif__ponto--neg" /> razão menor que a composição
        </small>
        <ul className="ia-dif__contas">
          {contasDaEmpresa.map((conta) => (
            <li key={`${conta.codColigada}-${conta.codFilial}-${conta.codConta}`}>
              <div className="ia-dif__conta-topo">
                <strong>{conta.codConta}</strong>
                <span>{conta.descricao}</span>
                <em>Filial {conta.nomeFilial}</em>
              </div>
              <div className="ia-dif__conta-barra">
                <span
                  className={conta.diferenca > 0 ? "ia-dif__barra ia-dif__barra--pos" : "ia-dif__barra ia-dif__barra--neg"}
                  style={{ width: `${(Math.abs(conta.diferenca) / maiorConta) * 100}%` }}
                />
              </div>
              <dl className="ia-dif__valores">
                <div><dt>Razão</dt><dd>{moeda.format(conta.saldoRazao)}</dd></div>
                <div><dt>Composição</dt><dd>{moeda.format(conta.composicao)}</dd></div>
                <div className={conta.diferenca > 0 ? "ia-dif__valor--pos" : "ia-dif__valor--neg"}>
                  <dt>Diferença</dt><dd>{moeda.format(conta.diferenca)}</dd>
                </div>
              </dl>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
