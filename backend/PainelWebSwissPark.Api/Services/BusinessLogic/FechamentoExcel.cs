using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using PainelWebSwissPark.Api.DTOs.Requests;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Planilha do fechamento no formato que a contabilidade já usa: cabeçalho verde-claro
/// com a identificação da conta e os responsáveis, Saldo Razão / Composição / Diferença,
/// a tabela DATA | HISTORICO | OBS. | CHAMADO | VALOR e, no fim, as assinaturas do
/// sócio e do contador.
/// </summary>
public static class FechamentoExcel
{
    // A = DATA; B..G = HISTORICO (no cabeçalho, B traz os valores e C..G a filial,
    // a coligada e a pendência); H = OBS.; I = CHAMADO (e os totais em R$); J = VALOR.
    private const int ColA = 0, ColB = 1, ColC = 2, ColD = 3, ColE = 4, ColF = 5, ColG = 6, ColH = 7, ColI = 8, ColJ = 9;

    /// <summary>Linhas em branco, com grade, depois dos lançamentos — como no modelo, para anotações.</summary>
    private const int LinhasEmBranco = 5;

    private const short Menta = 40, Verde = 41, Grade = 42, Rosa = 43, Vermelho = 44;

    public static byte[] Gerar(ExportarFechamentoRequest dados)
    {
        using var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Fechamento");
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
        int[] larguras = [12, 24, 7, 4, 9, 5, 18, 12, 20, 13];
        for (var col = 0; col < larguras.Length; col++) sheet.SetColumnWidth(col, larguras[col] * 256);

        var palette = workbook.GetCustomPalette();
        palette.SetColorAtIndex(Menta, 221, 245, 242);
        palette.SetColorAtIndex(Verde, 26, 168, 146);
        palette.SetColorAtIndex(Grade, 191, 191, 191);
        palette.SetColorAtIndex(Rosa, 255, 199, 206);
        palette.SetColorAtIndex(Vermelho, 156, 0, 6);

        var formatos = workbook.CreateDataFormat();
        ICellStyle Estilo(
            short? fundo = null, bool negrito = false, short tamanho = 11, short? corFonte = null, string? formato = null,
            bool grade = false, HorizontalAlignment alinhamento = HorizontalAlignment.General, bool quebra = false,
            bool linhaEmBaixo = false)
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
            if (linhaEmBaixo) style.BorderBottom = BorderStyle.Thin;
            return style;
        }

        // R$ à esquerda e o número à direita, negativo entre parênteses — como no modelo.
        const string Contabil = "\"R$\"* #,##0.00_);\"R$\"* (#,##0.00);\"R$\"* \"-\"??_);@_)";
        var fundoMenta = Estilo(Menta);
        var rotulo = Estilo(Menta, negrito: true);
        var valor = Estilo(Menta, alinhamento: HorizontalAlignment.Left);
        var valorData = Estilo(Menta, formato: "dd/mm/yyyy", alinhamento: HorizontalAlignment.Left);
        var valorGrande = Estilo(Menta, tamanho: 14, alinhamento: HorizontalAlignment.Left);
        var rotuloComPontos = Estilo(Menta, negrito: true, formato: "@*.");
        var rotuloComTracos = Estilo(Menta, negrito: true, formato: "@*-");
        var totalEmReais = Estilo(Menta, formato: Contabil);
        var cabecalhoTabela = Estilo(Verde, negrito: true, corFonte: IndexedColors.White.Index, grade: true);
        var celulaTabela = Estilo(grade: true, quebra: true);
        var dataTabela = Estilo(grade: true, formato: "dd/mm/yyyy", alinhamento: HorizontalAlignment.Left);
        var valorTabela = Estilo(grade: true, formato: "#,##0.00");
        var linhaDeAssinatura = Estilo(linhaEmBaixo: true);
        var rotuloDeAssinatura = Estilo(negrito: true, alinhamento: HorizontalAlignment.Center);
        var dataDeAssinatura = Estilo(tamanho: 10, alinhamento: HorizontalAlignment.Center);

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

        // Datas chegam como o navegador mostra ("29/10/2024, 00:33"); o modelo só tem o dia.
        void Data(int linha, string? texto)
        {
            var cell = Celula(linha, ColB, ColB, valorData);
            var dia = texto?.Split(',')[0].Trim();
            if (DateTime.TryParseExact(dia, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
                cell.SetCellValue(data);
        }

        // ── Cabeçalho (fundo verde-claro de ponta a ponta) ──
        const int LinhaSaldoRazao = 13, LinhaComposicao = 14, LinhaDiferenca = 15, LinhaCabecalhoTabela = 16;
        for (var linha = 0; linha <= LinhaDiferenca; linha++) Pintar(linha, ColA, ColJ, fundoMenta);

        Celula(0, ColA, ColA, rotulo, "Empresa");
        Celula(0, ColB, ColH, valor, dados.Empresa);

        Celula(1, ColA, ColA, rotulo, "Conta nº:");
        Celula(1, ColB, ColB, valor, dados.CodConta);
        Celula(1, ColC, ColC, rotulo, "Filial:");
        Celula(1, ColD, ColD, valorGrande).SetCellValue(dados.CodFilial);
        Celula(1, ColE, ColE, rotulo, "Coligada:");
        Celula(1, ColF, ColF, valorGrande).SetCellValue(dados.CodColigada);
        Celula(1, ColG, ColG, rotulo, "Pendência");
        Celula(1, ColH, ColH, valor, dados.Pendencia ? "Sim" : "Não");
        Linha(1, 19);

        Celula(2, ColA, ColA, rotulo, "Reduzido:");
        Celula(2, ColB, ColB, valor, dados.Reduzido);
        Celula(3, ColA, ColA, rotulo, "Descrição:");
        Celula(3, ColB, ColH, valor, dados.DescricaoConta);
        Celula(4, ColA, ColA, rotulo, "Período:");
        Celula(4, ColB, ColB, valor, dados.Periodo);

        // No painel, quem prepara é quem faz o fechamento da conta; depois vem a
        // conferência e, por último, a finalização (etapa que o modelo antigo não tinha).
        Celula(6, ColA, ColA, rotulo, "Resp. Fech");
        Celula(6, ColB, ColB, valor, dados.PreparadoPor ?? "");
        Celula(7, ColA, ColA, rotulo, "Data Fech");
        Data(7, dados.PreparadoEm);
        Celula(8, ColA, ColA, rotulo, "Conferido");
        Celula(8, ColB, ColB, valor, dados.ConferidoPor ?? "");
        Celula(9, ColA, ColA, rotulo, "Data Conf.");
        Data(9, dados.ConferidoEm);
        Celula(10, ColA, ColA, rotulo, "Finalizado");
        Celula(10, ColB, ColB, valor, dados.FinalizadoPor ?? "");
        Celula(11, ColA, ColA, rotulo, "Data Final.");
        Data(11, dados.FinalizadoEm);

        Celula(LinhaSaldoRazao, ColA, ColH, rotuloComPontos, "Saldo Razão");
        Celula(LinhaSaldoRazao, ColI, ColI, totalEmReais).SetCellValue((double)dados.SaldoRazao);
        Celula(LinhaComposicao, ColA, ColH, rotuloComPontos, "Composição do saldo");
        Celula(LinhaComposicao, ColI, ColI, totalEmReais).SetCellValue((double)dados.ComposicaoSaldo);
        Celula(LinhaDiferenca, ColA, ColH, rotuloComTracos, "Diferença ");
        // Fórmula: se alguém ajustar a composição na planilha, a diferença acompanha.
        Celula(LinhaDiferenca, ColI, ColI, totalEmReais)
            .SetCellFormula($"I{LinhaSaldoRazao + 1}-I{LinhaComposicao + 1}");

        // Diferença diferente de zero fica rosa com o valor em vermelho, como no modelo.
        var formatacao = sheet.SheetConditionalFormatting;
        var regra = formatacao.CreateConditionalFormattingRule(ComparisonOperator.NotEqual, "0", null);
        var preenchimento = regra.CreatePatternFormatting();
        preenchimento.FillBackgroundColor = Rosa;
        preenchimento.FillForegroundColor = Rosa;
        preenchimento.FillPattern = FillPattern.SolidForeground;
        regra.CreateFontFormatting().FontColorIndex = Vermelho;
        formatacao.AddConditionalFormatting(
            [new CellRangeAddress(LinhaDiferenca, LinhaDiferenca, ColI, ColI)], regra);

        // ── Tabela de lançamentos ──
        Linha(LinhaCabecalhoTabela, 18);
        Celula(LinhaCabecalhoTabela, ColA, ColA, cabecalhoTabela, "DATA");
        Celula(LinhaCabecalhoTabela, ColB, ColG, cabecalhoTabela, "HISTORICO");
        Celula(LinhaCabecalhoTabela, ColH, ColH, cabecalhoTabela, "OBS.");
        Celula(LinhaCabecalhoTabela, ColI, ColI, cabecalhoTabela, "CHAMADO");
        Celula(LinhaCabecalhoTabela, ColJ, ColJ, cabecalhoTabela, "VALOR");
        sheet.RepeatingRows = new CellRangeAddress(LinhaCabecalhoTabela, LinhaCabecalhoTabela, -1, -1);

        var rowIndex = LinhaCabecalhoTabela + 1;
        foreach (var item in dados.Lancamentos)
        {
            var linhasDeTexto = item.Historico.Split('\n').Sum(l => Math.Max(1, (int)Math.Ceiling(l.Length / 70d)));
            Linha(rowIndex, Math.Min(409, Math.Max(15, linhasDeTexto * 15)));
            var data = Celula(rowIndex, ColA, ColA, dataTabela, item.Data);
            if (DateTime.TryParseExact(item.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
                data.SetCellValue(dia);
            Celula(rowIndex, ColB, ColG, celulaTabela, item.Historico);
            Celula(rowIndex, ColH, ColH, celulaTabela);
            Celula(rowIndex, ColI, ColI, celulaTabela);
            // Mesmo sinal da tela: positivo/negativo conforme a natureza da conta.
            Celula(rowIndex, ColJ, ColJ, valorTabela).SetCellValue((double)item.Valor);
            rowIndex++;
        }
        for (var i = 0; i < LinhasEmBranco; i++, rowIndex++)
        {
            Celula(rowIndex, ColA, ColA, celulaTabela);
            Celula(rowIndex, ColB, ColG, celulaTabela);
            Celula(rowIndex, ColH, ColH, celulaTabela);
            Celula(rowIndex, ColI, ColI, celulaTabela);
            Celula(rowIndex, ColJ, ColJ, celulaTabela);
        }

        // ── Assinaturas ──
        rowIndex += 2;
        Linha(rowIndex, 42); // Espaço para assinar acima da linha.
        Celula(rowIndex, ColA, ColD, linhaDeAssinatura);
        Celula(rowIndex, ColH, ColJ, linhaDeAssinatura);
        rowIndex++;
        Celula(rowIndex, ColA, ColD, rotuloDeAssinatura, "Assinatura do Sócio");
        Celula(rowIndex, ColH, ColJ, rotuloDeAssinatura, "Assinatura do Contador");
        rowIndex++;
        Linha(rowIndex, 20);
        Celula(rowIndex, ColA, ColD, dataDeAssinatura, "Data: ____/____/______");
        Celula(rowIndex, ColH, ColJ, dataDeAssinatura, "Data: ____/____/______");

        HSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
        workbook.SetPrintArea(0, ColA, ColJ, 0, rowIndex);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }
}
