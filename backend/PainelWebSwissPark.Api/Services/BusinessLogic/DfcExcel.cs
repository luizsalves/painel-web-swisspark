using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using PainelWebSwissPark.Api.DTOs.Requests;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Planilha da DFC no mesmo visual do Excel de Fechamentos. As contas de cada
/// linha vão em grupos recolhidos, que abrem pelo "+" da margem do Excel.
/// </summary>
public static class DfcExcel
{
    private const int UltimaColuna = 6;

    public static byte[] Gerar(ExportarDfcRequest dados)
    {
        using var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("DFC");
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
        // O botão do grupo fica na linha da DFC, acima das contas que ela soma.
        sheet.RowSumsBelow = false;
        int[] larguras = [12, 14, 14, 14, 14, 18, 20];
        for (var col = 0; col < larguras.Length; col++) sheet.SetColumnWidth(col, larguras[col] * 256);
        var palette = workbook.GetCustomPalette();
        palette.SetColorAtIndex(40, 0, 88, 68);
        palette.SetColorAtIndex(41, 20, 38, 58);
        palette.SetColorAtIndex(42, 82, 101, 121);
        palette.SetColorAtIndex(43, 245, 249, 248);
        palette.SetColorAtIndex(44, 220, 229, 232);
        palette.SetColorAtIndex(45, 254, 242, 227);

        var formatoValor = workbook.CreateDataFormat().GetFormat("#,##0.00;[Red]-#,##0.00;\"-\"");

        ICellStyle Estilo(bool negrito = false, short cor = 41, short tamanho = 10, short? fundo = null,
            bool valor = false, bool borda = false, short recuo = 0)
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
            style.Indention = recuo;
            if (fundo is not null) { style.FillForegroundColor = fundo.Value; style.FillPattern = FillPattern.SolidForeground; }
            if (valor) { style.DataFormat = formatoValor; style.Alignment = HorizontalAlignment.Right; }
            if (borda) { style.BorderBottom = BorderStyle.Thin; style.BottomBorderColor = 44; }
            return style;
        }

        var label = Estilo(cor: 42, tamanho: 9);
        var destaque = Estilo(negrito: true);
        var titulo = Estilo(negrito: true, tamanho: 22);
        var secao = Estilo(negrito: true, cor: 40, tamanho: 12);
        var cabecalho = Estilo(negrito: true, cor: 40, fundo: 43, borda: true);
        var cabecalhoValor = Estilo(negrito: true, cor: 40, fundo: 43, borda: true);
        cabecalhoValor.Alignment = HorizontalAlignment.Right;
        var grupo = Estilo(negrito: true, cor: 40, fundo: 43);
        var linhaTexto = Estilo(borda: true, recuo: 1);
        var linhaValor = Estilo(valor: true, borda: true);
        var alertaTexto = Estilo(borda: true, recuo: 1, fundo: 45);
        var alertaValor = Estilo(valor: true, borda: true, fundo: 45);
        var detalheTexto = Estilo(cor: 42, tamanho: 9, recuo: 3);
        var detalheValor = Estilo(cor: 42, tamanho: 9, valor: true);
        var totalTexto = Estilo(negrito: true, cor: 40, fundo: 43);
        var totalValor = Estilo(negrito: true, cor: 40, fundo: 43, valor: true);

        ICell Celula(int row, int start, int end, string texto, ICellStyle estilo, float altura = 20)
        {
            var linha = sheet.GetRow(row) ?? sheet.CreateRow(row);
            linha.HeightInPoints = Math.Max(linha.HeightInPoints, altura);
            for (var c = start; c <= end; c++) (linha.GetCell(c) ?? linha.CreateCell(c)).CellStyle = estilo;
            var cell = linha.GetCell(start);
            cell.SetCellValue(texto); // Texto literal: nomes e descrições nunca viram fórmulas.
            if (end > start) sheet.AddMergedRegion(new CellRangeAddress(row, row, start, end));
            return cell;
        }
        void Linha(int row, string texto, decimal valor, ICellStyle estiloTexto, ICellStyle estiloValor, float altura = 20)
        {
            Celula(row, 0, UltimaColuna - 1, texto, estiloTexto, altura);
            Celula(row, UltimaColuna, UltimaColuna, "", estiloValor, altura).SetCellValue((double)valor);
        }

        using (var logo = typeof(DfcExcel).Assembly.GetManifestResourceStream("PainelWebSwissPark.Api.Assets.logo-swiss-park.png"))
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

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var inicio = DateTime.ParseExact(dados.Inicio, "yyyy-MM", CultureInfo.InvariantCulture);
        var fim = DateTime.ParseExact(dados.Fim, "yyyy-MM", CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1);
        var vespera = inicio.AddDays(-1);

        Celula(0, 2, UltimaColuna, "DEMONSTRATIVO CONTÁBIL", secao);
        Celula(1, 2, UltimaColuna, "Demonstração do Fluxo de Caixa", titulo, 34);
        Celula(2, 2, UltimaColuna, $"Método indireto · {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}", destaque);
        Celula(4, 0, 4, "Empresa", label);
        Celula(4, 5, UltimaColuna, "Coligada / Filial", label);
        Celula(5, 0, 4, dados.Empresa, destaque, 28);
        Celula(5, 5, UltimaColuna, $"{dados.CodColigada} / {(string.IsNullOrWhiteSpace(dados.Filial) ? "Todas" : dados.Filial)}", destaque);

        const int linhaCabecalho = 7;
        Celula(linhaCabecalho, 0, UltimaColuna - 1, "Descrição", cabecalho);
        Celula(linhaCabecalho, UltimaColuna, UltimaColuna, "Valor (R$)", cabecalhoValor);
        sheet.RepeatingRows = new CellRangeAddress(linhaCabecalho, linhaCabecalho, -1, -1);
        sheet.CreateFreezePane(0, linhaCabecalho + 1);

        var rowIndex = linhaCabecalho + 1;
        foreach (var bloco in dados.Dfc.Grupos)
        {
            Celula(rowIndex++, 0, UltimaColuna, bloco.Nome.ToUpper(cultura), grupo, 24);
            foreach (var linha in bloco.Linhas)
            {
                Linha(rowIndex++, linha.Descricao, linha.Valor,
                    linha.NaoClassificada ? alertaTexto : linhaTexto, linha.NaoClassificada ? alertaValor : linhaValor);
                if (linha.Composicao.Count == 0) continue;

                var primeira = rowIndex;
                foreach (var item in linha.Composicao)
                {
                    var descricao = string.IsNullOrWhiteSpace(item.Descricao) ? "" : $" {item.Descricao}";
                    Linha(rowIndex++, $"{item.CodConta}{descricao} · {item.Medida}", item.Valor, detalheTexto, detalheValor, 16);
                }
                sheet.GroupRow(primeira, rowIndex - 1);
                sheet.SetRowGroupCollapsed(primeira, true);
            }
            Linha(rowIndex++, $"Total — {bloco.Nome}", bloco.Total, totalTexto, totalValor, 24);
            rowIndex++;
        }

        var somaAtividades = dados.Dfc.Grupos.Sum(g => g.Total);
        Linha(rowIndex++, somaAtividades < 0 ? "Decréscimo de caixa do período" : "Acréscimo de caixa do período",
            somaAtividades, totalTexto, totalValor, 24);
        Linha(rowIndex++, $"Saldo de caixa e equivalentes em {vespera:dd/MM/yyyy}", dados.Dfc.SaldoInicialCaixa, linhaTexto, linhaValor);
        Linha(rowIndex++, $"Saldo de caixa e equivalentes em {fim:dd/MM/yyyy}", dados.Dfc.SaldoFinalCaixa, linhaTexto, linhaValor);
        Linha(rowIndex++, "Variação de caixa", dados.Dfc.VariacaoCaixa, totalTexto, totalValor, 24);

        if (dados.Dfc.Grupos.Any(g => g.Linhas.Any(l => l.NaoClassificada)))
        {
            rowIndex++;
            Celula(rowIndex++, 0, UltimaColuna,
                "\"Variações não classificadas\" reúne contas que movimentaram no período e ainda não têm linha definida na DFC. " +
                "Elas mantêm o fechamento com o caixa, mas precisam ser classificadas com a contabilidade.", label, 30);
        }
        rowIndex++;
        Celula(rowIndex, 0, UltimaColuna, $"Emitido em {DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura)} pelo Painel SwissPark.", label);

        workbook.SetPrintArea(0, 0, UltimaColuna, 0, rowIndex);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }
}
