using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using PainelWebSwissPark.Api.DTOs.Requests;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public static class FechamentoExcel
{
    public static byte[] Gerar(ExportarFechamentoRequest dados)
    {
        using var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Fechamento mensal");
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
        int[] larguras = [14, 14, 16, 16, 16, 16, 20, 14];
        for (var col = 0; col < larguras.Length; col++) sheet.SetColumnWidth(col, larguras[col] * 256);
        var palette = workbook.GetCustomPalette();
        palette.SetColorAtIndex(40, 0, 88, 68);
        palette.SetColorAtIndex(41, 20, 38, 58);
        palette.SetColorAtIndex(42, 82, 101, 121);
        palette.SetColorAtIndex(43, 245, 249, 248);
        palette.SetColorAtIndex(44, 220, 229, 232);

        ICellStyle Estilo(bool negrito = false, short cor = 41, short tamanho = 10, bool fundo = false, bool moeda = false, bool borda = false)
        {
            var font = workbook.CreateFont();
            font.FontName = "Arial";
            font.FontHeightInPoints = tamanho;
            font.IsBold = negrito;
            font.Color = cor;
            var style = workbook.CreateCellStyle();
            style.SetFont(font);
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Center;
            if (fundo) { style.FillForegroundColor = 43; style.FillPattern = FillPattern.SolidForeground; }
            if (moeda) { style.DataFormat = workbook.CreateDataFormat().GetFormat("\"R$\" #,##0.00;\"R$\" -#,##0.00"); style.Alignment = HorizontalAlignment.Right; }
            if (borda) { style.BorderBottom = BorderStyle.Thin; style.BottomBorderColor = 44; }
            return style;
        }

        var normal = Estilo(borda: true);
        var label = Estilo(cor: 42, tamanho: 9);
        var destaque = Estilo(negrito: true);
        var titulo = Estilo(negrito: true, tamanho: 22);
        var secao = Estilo(negrito: true, cor: 40, tamanho: 12);
        var cabecalho = Estilo(negrito: true, cor: 40, fundo: true, borda: true);
        var moeda = Estilo(moeda: true, borda: true);
        var total = Estilo(negrito: true, cor: 40, fundo: true, moeda: true);
        var totalLabel = Estilo(negrito: true, cor: 40, fundo: true);
        var dataStyle = Estilo(borda: true);
        dataStyle.DataFormat = workbook.CreateDataFormat().GetFormat("dd/mm/yyyy");

        ICell Celula(int row, int start, int end, string texto, ICellStyle estilo, float altura = 22)
        {
            var linha = sheet.GetRow(row) ?? sheet.CreateRow(row);
            linha.HeightInPoints = Math.Max(linha.HeightInPoints, altura);
            for (var c = start; c <= end; c++) (linha.GetCell(c) ?? linha.CreateCell(c)).CellStyle = estilo;
            var cell = linha.GetCell(start);
            cell.SetCellValue(texto); // Texto literal: nomes e históricos nunca viram fórmulas.
            if (end > start) sheet.AddMergedRegion(new CellRangeAddress(row, row, start, end));
            return cell;
        }
        void Valor(int row, int start, int end, decimal valor) => Celula(row, start, end, "", total, 26).SetCellValue((double)valor);
        string Nome(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;

        using (var logo = typeof(FechamentoExcel).Assembly.GetManifestResourceStream("PainelWebSwissPark.Api.Assets.logo-swiss-park.png"))
        {
            if (logo is not null)
            {
                using var image = new MemoryStream();
                logo.CopyTo(image);
                var id = workbook.AddPicture(image.ToArray(), PictureType.PNG);
                var anchor = new HSSFClientAnchor(0, 0, 900, 240, 0, 0, 1, 3);
                sheet.CreateDrawingPatriarch().CreatePicture(anchor, id);
            }
        }
        Celula(0, 2, 7, "RELATÓRIO CONTÁBIL", secao);
        Celula(1, 2, 7, "Fechamento mensal", titulo, 34);
        var periodo = DateTime.ParseExact(dados.Periodo, "yyyy-MM", CultureInfo.InvariantCulture);
        Celula(2, 2, 7, "Referência: " + periodo.ToString("MMMM 'de' yyyy", CultureInfo.GetCultureInfo("pt-BR")), destaque);
        Celula(4, 0, 5, "Empresa", label);
        Celula(4, 6, 7, "Coligada / Filial", label);
        Celula(5, 0, 5, dados.Empresa, destaque, 30);
        Celula(5, 6, 7, $"{dados.CodColigada} / {dados.CodFilial}", destaque);
        Celula(6, 6, 7, "Reduzido: " + dados.Reduzido, label);
        Celula(7, 0, 7, "Conta contábil", label);
        Celula(8, 0, 7, dados.CodConta + " — " + dados.DescricaoConta, destaque, 30);
        string[] rotulos = ["Preparado por", "Conferido por", "Finalizado por"];
        string[] nomes = [Nome(dados.PreparadoPor), Nome(dados.ConferidoPor), Nome(dados.FinalizadoPor)];
        string[] datas = [Nome(dados.PreparadoEm), Nome(dados.ConferidoEm), Nome(dados.FinalizadoEm)];
        int[] inicios = [0, 3, 6]; int[] finais = [2, 5, 7];
        for (var i = 0; i < 3; i++)
        {
            Celula(10, inicios[i], finais[i], rotulos[i], label);
            Celula(11, inicios[i], finais[i], nomes[i], destaque, 32);
            Celula(12, inicios[i], finais[i], "Em: " + datas[i], label);
        }
        Celula(13, 0, 7, "Resumo do período", secao);
        string[] metricas = ["Saldo anterior", "Saldo razão", "Composição do saldo", "Diferença"];
        decimal[] valores = [dados.SaldoAnterior, dados.SaldoRazao, dados.ComposicaoSaldo, dados.Diferenca];
        for (var i = 0; i < 4; i++) { Celula(14, i * 2, i * 2 + 1, metricas[i], label); Valor(15, i * 2, i * 2 + 1, valores[i]); }
        Celula(17, 0, 1, "Pendência: " + (dados.Pendencia ? "Sim" : "Não"), label);
        Celula(17, 2, 3, "Status: " + dados.Status, label);
        Celula(17, 4, 7, "Responsável: " + Nome(dados.Responsavel), label, 30);
        Celula(19, 0, 5, "Lançamentos contábeis", secao);
        Celula(19, 6, 7, $"{dados.Lancamentos.Count} lançamento(s)", label);
        Celula(20, 0, 0, "Data", cabecalho); Celula(20, 1, 1, "IdPartida", cabecalho);
        Celula(20, 2, 5, "Histórico", cabecalho); Celula(20, 6, 6, "Valor (R$)", cabecalho); Celula(20, 7, 7, "Tipo", cabecalho);
        sheet.RepeatingRows = new CellRangeAddress(20, 20, -1, -1);
        sheet.CreateFreezePane(0, 21);
        var rowIndex = 21;
        foreach (var item in dados.Lancamentos)
        {
            var cell = Celula(rowIndex, 0, 0, item.Data, dataStyle);
            if (DateTime.TryParseExact(item.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)) cell.SetCellValue(data);
            Celula(rowIndex, 1, 1, item.IdPartida, normal);
            var linhas = item.Historico.Split('\n').Sum(l => Math.Max(1, (int)Math.Ceiling(l.Length / 60d)));
            Celula(rowIndex, 2, 5, item.Historico, normal, Math.Min(409, Math.Max(24, linhas * 14 + 8)));
            Celula(rowIndex, 6, 6, "", moeda).SetCellValue((double)item.Valor);
            Celula(rowIndex, 7, 7, item.Tipo, normal);
            rowIndex++;
        }
        if (dados.Lancamentos.Count == 0) Celula(rowIndex++, 0, 7, "Nenhum lançamento encontrado neste período.", label);
        rowIndex++;
        foreach (var tipo in new[] { "Débito", "Crédito" })
        {
            Celula(rowIndex, 0, 5, tipo == "Débito" ? "Total de débitos" : "Total de créditos", totalLabel, 28);
            var cell = Celula(rowIndex++, 6, 7, "", total);
            if (dados.Lancamentos.Count > 0)
                cell.SetCellFormula($"SUMIF(H22:H{21 + dados.Lancamentos.Count},\"{tipo}\",G22:G{21 + dados.Lancamentos.Count})");
            else cell.SetCellValue(0);
        }
        HSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
        workbook.SetPrintArea(0, 0, 7, 0, rowIndex - 1);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }

}
