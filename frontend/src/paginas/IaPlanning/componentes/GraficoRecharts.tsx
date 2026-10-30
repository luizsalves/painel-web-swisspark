import { useId } from "react";
import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  LabelList,
  Legend,
  Line,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatarValorGrafico } from "../formatarGrafico";
import { rotuloDoMes, type GraficoTatiDados } from "../respostaTati";

// As cores do card da Tati: o azul das barras de antes, o amarelo da Tati e um verde para a 3ª série.
const CORES = ["#1f3fbf", "#e0a100", "#0f9d77"];
const COR_TEXTO = "#25325f";
const COR_EIXO = "#617bc5";
const COR_GRADE = "#e3eaf9";
// Cabe na área de gráficos da tela larga (340 px) com o topo e a fonte do card.
const ALTURA = 220;

/** O rótulo do eixo: meses viram "out/2025"; nomes longos ganham reticências. */
function rotuloDoEixo(valor: string, maximo: number): string {
  const rotulo = rotuloDoMes(valor);
  return rotulo.length > maximo ? `${rotulo.slice(0, maximo - 1)}…` : rotulo;
}

/**
 * O desenho do gráfico da Tati com o Recharts: barras horizontais (ranking), colunas (categorias
 * ou períodos lado a lado) e linha (evolução). Fica num arquivo próprio para o Recharts só ser
 * baixado quando a tela da Tati mostrar um gráfico.
 */
export default function GraficoRecharts({ grafico }: { grafico: GraficoTatiDados }) {
  const idGradiente = useId().replace(/:/g, "");
  const { tipo, unidade, categorias, series } = grafico;
  const chaves = series.map((_, indice) => `s${indice}`);
  const linhas = categorias.map((categoria, indice) =>
    Object.fromEntries([["categoria", categoria], ...series.map((serie, s) => [chaves[s], serie.valores[indice]])]));
  const temNegativo = series.some((serie) => serie.valores.some((valor) => valor !== null && valor < 0));
  const umaSerie = series.length === 1;
  const formatar = (valor: unknown) => formatarValorGrafico(typeof valor === "number" ? valor : null, unidade);
  const formatarCurto = (valor: unknown) => formatarValorGrafico(typeof valor === "number" ? valor : null, unidade, true);

  const tooltip = (
    <Tooltip
      formatter={(valor, nome) => [formatar(valor), nome]}
      labelFormatter={(rotulo) => rotuloDoMes(String(rotulo))}
      contentStyle={{ borderRadius: 10, borderColor: "#dbe4fb", fontSize: 12, color: COR_TEXTO }}
      cursor={{ fill: "rgba(31, 63, 191, 0.06)" }}
    />
  );
  const legenda = umaSerie ? null : <Legend wrapperStyle={{ fontSize: 12, color: COR_TEXTO }} iconType="circle" />;
  const zero = temNegativo ? <ReferenceLine y={0} stroke={COR_EIXO} strokeDasharray="3 3" /> : null;

  if (tipo === "barras") {
    // Ranking: uma barra por categoria, do jeito que a Tati mandou (do maior para o menor).
    const altura = Math.max(ALTURA, categorias.length * 28 + 30);
    return (
      <ResponsiveContainer width="100%" height={altura}>
        <ComposedChart data={linhas} layout="vertical" margin={{ top: 4, right: 72, bottom: 4, left: 4 }}>
          <CartesianGrid horizontal={false} stroke={COR_GRADE} />
          <XAxis type="number" tickFormatter={formatarCurto} tick={{ fontSize: 11, fill: COR_EIXO }} axisLine={false} tickLine={false} />
          <YAxis type="category" dataKey="categoria" width={150} tickFormatter={(valor) => rotuloDoEixo(String(valor), 22)} tick={{ fontSize: 12, fill: COR_TEXTO }} axisLine={false} tickLine={false} />
          {tooltip}
          {legenda}
          {temNegativo ? <ReferenceLine x={0} stroke={COR_EIXO} strokeDasharray="3 3" /> : null}
          {series.map((serie, s) => (
            <Bar key={chaves[s]} dataKey={chaves[s]} name={serie.nome} fill={CORES[s % CORES.length]} radius={[0, 6, 6, 0]} barSize={18} isAnimationActive>
              {umaSerie && <LabelList dataKey={chaves[s]} position="right" formatter={formatar} style={{ fontSize: 11, fill: COR_TEXTO, fontWeight: 600 }} />}
            </Bar>
          ))}
        </ComposedChart>
      </ResponsiveContainer>
    );
  }

  if (tipo === "colunas") {
    // Valor em cima de cada coluna enquanto couber; com muitas colunas, só no tooltip.
    const mostrarValores = categorias.length * series.length <= 12;
    return (
      <ResponsiveContainer width="100%" height={ALTURA}>
        <ComposedChart data={linhas} margin={{ top: 24, right: 12, bottom: 4, left: 4 }}>
          <CartesianGrid vertical={false} stroke={COR_GRADE} />
          <XAxis dataKey="categoria" tickFormatter={(valor) => rotuloDoEixo(String(valor), 16)} tick={{ fontSize: 11, fill: COR_TEXTO }} interval={0} axisLine={{ stroke: COR_GRADE }} tickLine={false} />
          <YAxis tickFormatter={formatarCurto} tick={{ fontSize: 11, fill: COR_EIXO }} width={78} axisLine={false} tickLine={false} />
          {tooltip}
          {legenda}
          {zero}
          {series.map((serie, s) => (
            <Bar key={chaves[s]} dataKey={chaves[s]} name={serie.nome} fill={CORES[s % CORES.length]} radius={[6, 6, 0, 0]} maxBarSize={48} isAnimationActive>
              {mostrarValores && <LabelList dataKey={chaves[s]} position="top" formatter={formatarCurto} style={{ fontSize: 10, fill: COR_TEXTO, fontWeight: 600 }} />}
            </Bar>
          ))}
        </ComposedChart>
      </ResponsiveContainer>
    );
  }

  // Linha: com uma série, a área em degradê embaixo; null é buraco, nunca zero.
  return (
    <ResponsiveContainer width="100%" height={ALTURA}>
      <ComposedChart data={linhas} margin={{ top: 24, right: 16, bottom: 4, left: 4 }}>
        <defs>
          <linearGradient id={idGradiente} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={CORES[0]} stopOpacity={0.28} />
            <stop offset="100%" stopColor={CORES[0]} stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} stroke={COR_GRADE} />
        <XAxis dataKey="categoria" tickFormatter={(valor) => rotuloDoEixo(String(valor), 10)} tick={{ fontSize: 11, fill: COR_TEXTO }} axisLine={{ stroke: COR_GRADE }} tickLine={false} />
        <YAxis tickFormatter={formatarCurto} tick={{ fontSize: 11, fill: COR_EIXO }} width={78} axisLine={false} tickLine={false} domain={["auto", "auto"]} />
        {tooltip}
        {legenda}
        {zero}
        {umaSerie ? (
          <Area type="monotone" dataKey={chaves[0]} name={series[0].nome} stroke={CORES[0]} strokeWidth={2.5} fill={`url(#${idGradiente})`} connectNulls={false} dot={{ r: 3.5, fill: "#fff", strokeWidth: 2 }} activeDot={{ r: 5 }} isAnimationActive>
            <LabelList dataKey={chaves[0]} position="top" formatter={formatarCurto} style={{ fontSize: 10, fill: COR_TEXTO, fontWeight: 600 }} />
          </Area>
        ) : (
          series.map((serie, s) => (
            <Line key={chaves[s]} type="monotone" dataKey={chaves[s]} name={serie.nome} stroke={CORES[s % CORES.length]} strokeWidth={2.5} connectNulls={false} dot={{ r: 3.5, fill: "#fff", strokeWidth: 2 }} activeDot={{ r: 5 }} isAnimationActive />
          ))
        )}
      </ComposedChart>
    </ResponsiveContainer>
  );
}
