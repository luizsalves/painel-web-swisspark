using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using PainelWebSwissPark.Api.DTOs.Requests;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Planilha da aba Histórico do fechamento: o cabeçalho da conta no mesmo verde-claro
/// do Excel do fechamento e, para cada mês, o resumo (saldo anterior, débitos, créditos
/// e saldo) seguido das partidas. No fim, o resumo do período inteiro.
/// </summary>
public static class FechamentoHistoricoExcel
{
    // A = DATA, B = IDPARTIDA, C = HISTÓRICO, D = VALOR, E = TIPO.
    private const int ColA = 0, ColB = 1, ColC = 2, ColD = 3, ColE = 4;
    // VerdeEscuro fica fora dos índices do FechamentoExcel (43 e 44 lá são rosa e vermelho):
    // no Excel do ano as duas planilhas dividem a mesma paleta.
    private const short Menta = 40, Verde = 41, Grade = 42, VerdeEscuro = 45;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>"Junho de 2026".</summary>
    private static string MesPorExtenso(string anoMes)
    {
        var texto = DateTime.ParseExact(anoMes, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMMM 'de' yyyy", PtBr);
        return char.ToUpper(texto[0], PtBr) + texto[1..];
    }

    /// <summary>"Jun/2026".</summary>
    private static string MesCurto(string anoMes)
    {
        var texto = DateTime.ParseExact(anoMes, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMM'/'yyyy", PtBr);
        return char.ToUpper(texto[0], PtBr) + texto[1..];
    }

    public static byte[] Gerar(ExportarHistoricoFechamentoRequest dados)
    {
        using var workbook = new HSSFWorkbook();
        PreencherAba(workbook, workbook.CreateSheet("Histórico"), dados);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }

    /// <summary>A aba do histórico; no Excel do ano (<see cref="FechamentoAnualExcel"/>) ela é a RAZAO.</summary>
    internal static void PreencherAba(HSSFWorkbook workbook, ISheet sheet, ExportarHistoricoFechamentoRequest dados)
    {
        sheet.DisplayGridlines = false;
        sheet.IsPrintGridlines = false;
        sheet.FitToPage = true;
        sheet.PrintSetup.PaperSize = (short)PaperSize.A4;
        sheet.PrintSetup.FitWidth = 1;
        sheet.PrintSetup.FitHeight = 0;
        sheet.SetMargin(MarginType.LeftMargin, 0.3);
        sheet.SetMargin(MarginType.RightMargin, 0.3);
        sheet.SetMargin(MarginType.TopMargin, 0.4);
        sheet.SetMargin(MarginType.BottomMargin, 0.4);
        sheet.Footer.Right = "Página &P de &N";
        // B também mostra o código da conta no cabeçalho ("1.1.2.02.01.9999").
        int[] larguras = [12, 18, 58, 16, 10];
        for (var col = 0; col < larguras.Length; col++) sheet.SetColumnWidth(col, larguras[col] * 256);

        var palette = workbook.GetCustomPalette();
        palette.SetColorAtIndex(Menta, 221, 245, 242);
        palette.SetColorAtIndex(Verde, 26, 168, 146);
        palette.SetColorAtIndex(Grade, 191, 191, 191);
        palette.SetColorAtIndex(VerdeEscuro, 0, 76, 63);

        var formatos = workbook.CreateDataFormat();
        ICellStyle Estilo(
            short? fundo = null, bool negrito = false, short tamanho = 11, short? corFonte = null, string? formato = null,
            bool grade = false, HorizontalAlignment alinhamento = HorizontalAlignment.General, bool quebra = false)
        {
            var font = workbook.CreateFont();
            font.FontName = "Calibri";
            font.FontHeightInPoints = tamanho;
            font.IsBold = negrito;
            if (corFonte is { } cor) font.Color = cor;
            var style = workbook.CreateCellStyle();
            style.SetFont(font);
            style.VerticalAlignment = VerticalAlignment.Center;
            style.Alignment = alinhamento;
            style.WrapText = quebra;
            if (fundo is { } corDeFundo) { style.FillForegroundColor = corDeFundo; style.FillPattern = FillPattern.SolidForeground; }
            if (formato is not null) style.DataFormat = formatos.GetFormat(formato);
            if (grade)
            {
                style.BorderTop = style.BorderBottom = style.BorderLeft = style.BorderRight = BorderStyle.Thin;
                style.TopBorderColor = style.BottomBorderColor = style.LeftBorderColor = style.RightBorderColor = Grade;
            }
            return style;
        }

        const string Numero = "#,##0.00";
        var fundoMenta = Estilo(Menta);
        var rotulo = Estilo(Menta, negrito: true);
        var valor = Estilo(Menta, alinhamento: HorizontalAlignment.Left);
        var tituloMes = Estilo(Menta, negrito: true, tamanho: 12, corFonte: VerdeEscuro);
        var rotuloResumo = Estilo(grade: true);
        var valorResumo = Estilo(formato: Numero, grade: true);
        var rotuloResumoForte = Estilo(negrito: true, grade: true);
        var valorResumoForte = Estilo(negrito: true, formato: Numero, grade: true);
        var cabecalhoTabela = Estilo(Verde, negrito: true, corFonte: IndexedColors.White.Index, grade: true);
        var celulaTabela = Estilo(grade: true, quebra: true);
        var dataTabela = Estilo(grade: true, formato: "dd/mm/yyyy", alinhamento: HorizontalAlignment.Left);
        var valorTabela = Estilo(grade: true, formato: Numero);
        var semLancamentos = Estilo(grade: true, corFonte: IndexedColors.Grey50Percent.Index);

        IRow Linha(int indice, float altura = 15)
        {
            var row = sheet.GetRow(indice) ?? sheet.CreateRow(indice);
            row.HeightInPoints = Math.Max(row.HeightInPoints, altura);
            return row;
        }

        void Pintar(int linha, int inicio, int fim, ICellStyle estilo)
        {
            var row = Linha(linha);
            for (var c = inicio; c <= fim; c++) (row.GetCell(c) ?? row.CreateCell(c)).CellStyle = estilo;
        }

        ICell Celula(int linha, int inicio, int fim, ICellStyle estilo, string? texto = null)
        {
            Pintar(linha, inicio, fim, estilo);
            var cell = Linha(linha).GetCell(inicio);
            if (texto is not null) cell.SetCellValue(texto); // Texto literal: nomes e históricos nunca viram fórmulas.
            if (fim > inicio) sheet.AddMergedRegion(new CellRangeAddress(linha, linha, inicio, fim));
            return cell;
        }

        void LinhaDeResumo(int linha, string texto, decimal numero, bool forte = false)
        {
            Celula(linha, ColA, ColC, forte ? rotuloResumoForte : rotuloResumo, texto);
            Celula(linha, ColD, ColD, forte ? valorResumoForte : valorResumo).SetCellValue((double)numero);
            Celula(linha, ColE, ColE, rotuloResumo);
        }

        var meses = dados.Meses;
        var totalDeLancamentos = meses.Sum(m => m.Lancamentos.Count);

        // ── Cabeçalho da conta ──
        const int UltimaLinhaCabecalho = 5;
        for (var linha = 0; linha <= UltimaLinhaCabecalho; linha++) Pintar(linha, ColA, ColE, fundoMenta);
        Celula(0, ColA, ColA, rotulo, "Empresa");
        Celula(0, ColB, ColE, valor, dados.Empresa);
        Celula(1, ColA, ColA, rotulo, "Conta nº:");
        Celula(1, ColB, ColB, valor, dados.CodConta);
        Celula(1, ColC, ColE, valor, $"Filial: {dados.CodFilial}   ·   Coligada: {dados.CodColigada}");
        Celula(2, ColA, ColA, rotulo, "Reduzido:");
        Celula(2, ColB, ColE, valor, dados.Reduzido);
        Celula(3, ColA, ColA, rotulo, "Descrição:");
        Celula(3, ColB, ColE, valor, dados.DescricaoConta);
        Celula(4, ColA, ColA, rotulo, "Período:");
        Celula(4, ColB, ColE, valor, meses.Count == 1
            ? MesCurto(meses[0].AnoMes)
            : $"{MesCurto(meses[0].AnoMes)} a {MesCurto(meses[^1].AnoMes)} (mês do fechamento)");
        Celula(5, ColA, ColA, rotulo, "Lançamentos:");
        Celula(5, ColB, ColE, valor).SetCellValue(totalDeLancamentos);

        // ── Um bloco por mês ──
        var rowIndex = UltimaLinhaCabecalho + 2;
        foreach (var mes in meses)
        {
            Linha(rowIndex, 20);
            Celula(rowIndex++, ColA, ColE, tituloMes, MesPorExtenso(mes.AnoMes));
            LinhaDeResumo(rowIndex++, "Saldo anterior", mes.SaldoAnterior);
            LinhaDeResumo(rowIndex++, "Débitos", mes.Debitos);
            LinhaDeResumo(rowIndex++, "Créditos", mes.Creditos);
            LinhaDeResumo(rowIndex++, "Saldo do mês", mes.SaldoMes, forte: true);

            Linha(rowIndex, 18);
            Celula(rowIndex, ColA, ColA, cabecalhoTabela, "DATA");
            Celula(rowIndex, ColB, ColB, cabecalhoTabela, "IDPARTIDA");
            Celula(rowIndex, ColC, ColC, cabecalhoTabela, "HISTÓRICO");
            Celula(rowIndex, ColD, ColD, cabecalhoTabela, "VALOR (R$)");
            Celula(rowIndex, ColE, ColE, cabecalhoTabela, "TIPO");
            rowIndex++;

            if (mes.Lancamentos.Count == 0)
                Celula(rowIndex++, ColA, ColE, semLancamentos, "Nenhum lançamento neste mês.");

            foreach (var item in mes.Lancamentos)
            {
                var linhasDeTexto = item.Historico.Split('\n').Sum(l => Math.Max(1, (int)Math.Ceiling(l.Length / 75d)));
                Linha(rowIndex, Math.Min(409, Math.Max(15, linhasDeTexto * 15)));
                var data = Celula(rowIndex, ColA, ColA, dataTabela, item.Data);
                if (DateTime.TryParseExact(item.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
                    data.SetCellValue(dia);
                Celula(rowIndex, ColB, ColB, celulaTabela, item.IdPartida);
                Celula(rowIndex, ColC, ColC, celulaTabela, item.Historico);
                // Mesmo sinal da tela: positivo/negativo conforme a natureza da conta.
                Celula(rowIndex, ColD, ColD, valorTabela).SetCellValue((double)item.Valor);
                Celula(rowIndex, ColE, ColE, celulaTabela, item.Tipo);
                rowIndex++;
            }
            rowIndex++;
        }

        // ── Resumo do período ──
        if (meses.Count > 1)
        {
            Linha(rowIndex, 20);
            Celula(rowIndex++, ColA, ColE, tituloMes, "Resumo do período");
            LinhaDeResumo(rowIndex++, $"Saldo anterior a {MesCurto(meses[0].AnoMes)}", meses[0].SaldoAnterior);
            LinhaDeResumo(rowIndex++, "Débitos do período", meses.Sum(m => m.Debitos));
            LinhaDeResumo(rowIndex++, "Créditos do período", meses.Sum(m => m.Creditos));
            LinhaDeResumo(rowIndex++, $"Saldo em {MesCurto(meses[^1].AnoMes)}", meses[^1].SaldoMes, forte: true);
        }

        workbook.SetPrintArea(workbook.GetSheetIndex(sheet), ColA, ColE, 0, Math.Max(0, rowIndex - 1));
    }
}
