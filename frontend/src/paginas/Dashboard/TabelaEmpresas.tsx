import { Fragment, useState } from "react";
import type { ReactNode } from "react";
import ContasDaEmpresa from "./ContasDaEmpresa";
import type { DashboardResumo, StatusFechamentoConta } from "../../interfaces/Dashboard";

export interface ResumoDaEmpresa extends DashboardResumo {
  nome: string;
}

export interface ColunaEmpresa {
  titulo: string;
  valor: (empresa: ResumoDaEmpresa) => ReactNode;
}

interface TabelaEmpresasProps {
  empresas: ResumoDaEmpresa[];
  /** Colunas numéricas depois do nome da empresa. */
  colunas: ColunaEmpresa[];
  /** Etapas das contas que o "+" mostra; sem informar, todas. */
  etapas?: StatusFechamentoConta[];
  anoMes: string;
  versao: number;
  vazio: string;
}

/** Empresas com um "+" que abre as contas de cada uma, como no Balancete. */
export default function TabelaEmpresas({ empresas, colunas, etapas, anoMes, versao, vazio }: TabelaEmpresasProps) {
  const [abertas, setAbertas] = useState<Record<string, boolean>>({});

  return (
    <div className="sp-dashboard__company-table">
      <table>
        <thead>
          <tr>
            <th>Empresa</th>
            {colunas.map((coluna) => <th key={coluna.titulo} className="sp-dashboard__num">{coluna.titulo}</th>)}
          </tr>
        </thead>
        <tbody>
          {empresas.map((empresa) => {
            const aberta = abertas[empresa.nome] ?? false;
            return (
              <Fragment key={empresa.nome}>
                <tr className={aberta ? "sp-dashboard__company-row--aberta" : undefined}>
                  <td>
                    <div className="sp-balance__account-cell">
                      <button type="button" className="sp-balance__toggle" aria-expanded={aberta}
                        aria-label={`${aberta ? "Recolher" : "Expandir"} contas de ${empresa.nome}`}
                        onClick={() => setAbertas((atuais) => ({ ...atuais, [empresa.nome]: !aberta }))}>
                        {aberta ? "−" : "+"}
                      </button>
                      <span>{empresa.nome}</span>
                    </div>
                  </td>
                  {colunas.map((coluna) => <td key={coluna.titulo} className="sp-dashboard__num">{coluna.valor(empresa)}</td>)}
                </tr>
                {aberta && (
                  <tr className="sp-dashboard__company-expansion">
                    <td colSpan={colunas.length + 1}>
                      <ContasDaEmpresa empresa={empresa.nome} anoMes={anoMes} versao={versao} etapas={etapas} />
                    </td>
                  </tr>
                )}
              </Fragment>
            );
          })}
          {empresas.length === 0 && <tr><td colSpan={colunas.length + 1}>{vazio}</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
