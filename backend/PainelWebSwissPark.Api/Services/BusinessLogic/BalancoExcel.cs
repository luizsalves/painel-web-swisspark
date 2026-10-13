using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Planilha do balanço no mesmo visual da DFC: ativo e passivo lado a lado, como
/// no balanço da contabilidade, e uma segunda aba com as contas de cada linha.
/// </summary>
public static class BalancoExcel
{
    // Ativo nas colunas 0-1, passivo nas 3-4; a 2 só separa os lados.
    private const int UltimaColuna = 4;

    public static byte[] Gerar(ExportarBalancoRequest dados)
    {
        using var workbook = new HSSFWorkbook();
        var palette = workbook.GetCustomPalette();
        palette.SetColorAtIndex(40, 0, 88, 68);
        palette.SetColorAtIndex(41, 20, 38, 58);
        palette.SetColorAtIndex(42, 82, 101, 121);
        palette.SetColorAtIndex(43, 245, 249, 248);
        palette.SetColorAtIndex(44, 220, 229, 232);
        palette.SetColorAtIndex(45, 254, 242, 227);
        var formatoValor = workbook.CreateDataFormat().GetFormat("#,##0.00;[Red]-#,##0.00;\"-\"");

        ICellStyle Estilo(bool negrito = false, short cor = 41, short tamanho = 10, short? fundo = null,
            bool valor = false, bool borda = false, bool bordaSuperior = false)
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
            if (fundo is not null) { style.FillForegroundColor = fundo.Value; style.FillPattern = FillPattern.SolidForeground; }
            if (valor) { style.DataFormat = formatoValor; style.Alignment = HorizontalAlignment.Right; }
            if (borda) { style.BorderBottom = BorderStyle.Thin; style.BottomBorderColor = 44; }
            if (bordaSuperior) { style.BorderTop = BorderStyle.Thin; style.TopBorderColor = 41; }
            return style;
        }

        var label = Estilo(cor: 42, tamanho: 9);
        var destaque = Estilo(negrito: true);
        var titulo = Estilo(negrito: true, tamanho: 22);
        var secao = Estilo(negrito: true, cor: 40, tamanho: 12);
        var cabecalho = Estilo(negrito: true, cor: 40, fundo: 43, borda: true);
        var cabecalhoValor = Estilo(negrito: true, cor: 40, fundo: 43, borda: true);
        cabecalhoValor.Alignment = HorizontalAlignment.Right;
        var estilos = new Dictionary<string, (ICellStyle Texto, ICellStyle Valor)>
        {
            ["linha"] = (Estilo(borda: true), Estilo(valor: true, borda: true)),
            ["grupo"] = (Estilo(negrito: true, borda: true, bordaSuperior: true),
                Estilo(negrito: true, valor: true, borda: true, bordaSuperior: true)),
            ["bloco"] = (Estilo(negrito: true, cor: 40, fundo: 43, borda: true),
                Estilo(negrito: true, cor: 40, fundo: 43, valor: true, borda: true)),
            ["total"] = (Estilo(negrito: true, cor: 40, fundo: 44, bordaSuperior: true),
                Estilo(negrito: true, cor: 40, fundo: 44, valor: true, bordaSuperior: true)),
        };

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var data = DateTime.ParseExact(dados.Mes, "yyyy-MM", CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1);

        var sheet = workbook.CreateSheet("Balanço");
        Configurar(sheet, [44, 17, 2, 44, 17]);

        ICell Celula(ISheet aba, int row, int start, int end, string texto, ICellStyle estilo, float altura = 20)
        {
            var linha = aba.GetRow(row) ?? aba.CreateRow(row);
            linha.HeightInPoints = Math.Max(linha.HeightInPoints, altura);
            for (var c = start; c <= end; c++) (linha.GetCell(c) ?? linha.CreateCell(c)).CellStyle = estilo;
            var cell = linha.GetCell(start);
            cell.SetCellValue(texto); // Texto literal: nomes e descrições nunca viram fórmulas.
            if (end > start) aba.AddMergedRegion(new CellRangeAddress(row, row, start, end));
            return cell;
        }

        using (var logo = typeof(BalancoExcel).Assembly.GetManifestResourceStream("PainelWebSwissPark.Api.Assets.logo-swiss-park.png"))
        {
            if (logo is not null)
            {
                using var image = new MemoryStream();
                logo.CopyTo(image);
                var id = workbook.AddPicture(image.ToArray(), PictureType.PNG);
                var anchor = new HSSFClientAnchor(0, 0, 560, 240, 0, 0, 0, 3);
                sheet.CreateDrawingPatriarch().CreatePicture(anchor, id);
            }
        }

        Celula(sheet, 0, 1, UltimaColuna, "DEMONSTRATIVO CONTÁBIL", secao);
        Celula(sheet, 1, 1, UltimaColuna, "Balanço Patrimonial", titulo, 34);
        Celula(sheet, 2, 1, UltimaColuna, $"{data.ToString("d 'de' MMMM 'de' yyyy", cultura)} · Em R$", destaque);
        Celula(sheet, 4, 0, 1, "Empresa", label);
        Celula(sheet, 4, 3, UltimaColuna, "Coligada / Filial", label);
        Celula(sheet, 5, 0, 1, dados.Empresa, destaque, 28);
        Celula(sheet, 5, 3, UltimaColuna,
            $"{dados.CodColigada} / {(string.IsNullOrWhiteSpace(dados.Filial) ? "Todas" : dados.Filial)}", destaque);

        const int linhaCabecalho = 7;
        var dataCabecalho = data.ToString("dd/MM/yyyy", cultura);
        Celula(sheet, linhaCabecalho, 0, 0, "ATIVO", cabecalho);
        Celula(sheet, linhaCabecalho, 1, 1, dataCabecalho, cabecalhoValor);
        Celula(sheet, linhaCabecalho, 3, 3, "PASSIVO", cabecalho);
        Celula(sheet, linhaCabecalho, 4, 4, dataCabecalho, cabecalhoValor);
        sheet.RepeatingRows = new CellRangeAddress(linhaCabecalho, linhaCabecalho, -1, -1);
        sheet.CreateFreezePane(0, linhaCabecalho + 1);

        void Lado(List<BalancoLinhaViewModel> linhas, int coluna)
        {
            for (var i = 0; i < linhas.Count; i++)
            {
                var item = linhas[i];
                var (texto, valor) = estilos.GetValueOrDefault(item.Tipo, estilos["linha"]);
                var altura = item.Tipo == "total" ? 24 : 20;
                Celula(sheet, linhaCabecalho + 1 + i, coluna, coluna, item.Descricao, texto, altura);
                Celula(sheet, linhaCabecalho + 1 + i, coluna + 1, coluna + 1, "", valor, altura).SetCellValue((double)item.Valor);
            }
        }
        Lado(dados.Balanco.Ativo, 0);
        Lado(dados.Balanco.Passivo, 3);

        var rowIndex = linhaCabecalho + 2 + Math.Max(dados.Balanco.Ativo.Count, dados.Balanco.Passivo.Count);
        var diferenca = dados.Balanco.TotalAtivo - dados.Balanco.TotalPassivo;
        if (diferenca != 0)
            Celula(sheet, rowIndex++, 0, UltimaColuna,
                $"Ativo e passivo diferem em R$ {diferenca.ToString("N2", cultura)}. Há lançamentos desbalanceados ou contas fora do ativo e do passivo.",
                label, 30);
        Celula(sheet, rowIndex, 0, UltimaColuna,
            $"Emitido em {DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura)} pelo Painel SwissPark. As contas de cada linha estão na aba Composição.", label);
        workbook.SetPrintArea(0, 0, UltimaColuna, 0, rowIndex);

        // Aba com as contas de cada linha, em tabela simples para filtrar.
        var composicao = workbook.CreateSheet("Composição");
        Configurar(composicao, [12, 34, 20, 44, 17]);
        string[] colunas = ["Lado", "Linha do balanço", "Conta", "Descrição", "Valor (R$)"];
        for (var c = 0; c < colunas.Length; c++)
            Celula(composicao, 0, c, c, colunas[c], c == colunas.Length - 1 ? cabecalhoValor : cabecalho);
        composicao.CreateFreezePane(0, 1);
        var (detalheTexto, detalheValor) = estilos["linha"];
        var linhaComposicao = 1;
        foreach (var (nomeLado, linhas) in new[] { ("Ativo", dados.Balanco.Ativo), ("Passivo", dados.Balanco.Passivo) })
        foreach (var linha in linhas.Where(l => l.Tipo == "linha"))
        foreach (var item in linha.Composicao)
        {
            Celula(composicao, linhaComposicao, 0, 0, nomeLado, detalheTexto, 16);
            Celula(composicao, linhaComposicao, 1, 1, linha.Descricao, detalheTexto, 16);
            Celula(composicao, linhaComposicao, 2, 2, item.CodConta, detalheTexto, 16);
            Celula(composicao, linhaComposicao, 3, 3, item.Descricao, detalheTexto, 16);
            Celula(composicao, linhaComposicao, 4, 4, "", detalheValor, 16).SetCellValue((double)item.Valor);
            linhaComposicao++;
        }
        composicao.SetAutoFilter(new CellRangeAddress(0, Math.Max(linhaComposicao - 1, 1), 0, colunas.Length - 1));

        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }

    private static void Configurar(ISheet sheet, int[] larguras)
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
        for (var col = 0; col < larguras.Length; col++) sheet.SetColumnWidth(col, larguras[col] * 256);
    }
}
