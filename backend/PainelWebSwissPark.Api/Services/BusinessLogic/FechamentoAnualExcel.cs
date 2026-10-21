using NPOI.HSSF.UserModel;
using PainelWebSwissPark.Api.DTOs.Requests;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// O Excel do fechamento com o ano até o mês fechado: a aba RAZAO, com o analítico mês a
/// mês (a mesma planilha da aba Histórico), e uma aba por mês no formato do fechamento.
/// Abre na aba do mês do fechamento.
/// </summary>
public static class FechamentoAnualExcel
{
    public static byte[] Gerar(ExportarFechamentoAnualRequest dados)
    {
        using var workbook = new HSSFWorkbook();

        FechamentoHistoricoExcel.PreencherAba(workbook, workbook.CreateSheet("RAZAO"), new ExportarHistoricoFechamentoRequest
        {
            Empresa = dados.Empresa,
            CodColigada = dados.CodColigada,
            CodFilial = dados.CodFilial,
            CodConta = dados.CodConta,
            DescricaoConta = dados.DescricaoConta,
            Reduzido = dados.Reduzido,
            Meses = dados.Meses.Cast<MesHistoricoFechamento>().ToList(),
        });

        foreach (var mes in dados.Meses)
        {
            FechamentoExcel.PreencherAba(workbook, workbook.CreateSheet(mes.AnoMes), new ExportarFechamentoRequest
            {
                Empresa = dados.Empresa,
                Periodo = mes.AnoMes,
                CodColigada = dados.CodColigada,
                CodFilial = dados.CodFilial,
                CodConta = dados.CodConta,
                DescricaoConta = dados.DescricaoConta,
                Reduzido = dados.Reduzido,
                PreparadoPor = mes.PreparadoPor,
                ConferidoPor = mes.ConferidoPor,
                FinalizadoPor = mes.FinalizadoPor,
                PreparadoEm = mes.PreparadoEm,
                ConferidoEm = mes.ConferidoEm,
                FinalizadoEm = mes.FinalizadoEm,
                SaldoAnterior = mes.SaldoAnterior,
                SaldoRazao = mes.SaldoMes,
                ComposicaoSaldo = mes.ComposicaoSaldo,
                Diferenca = mes.SaldoMes - mes.ComposicaoSaldo,
                Pendencia = mes.Pendencia,
                Lancamentos = mes.Lancamentos,
            });
        }

        var abaDoFechamento = workbook.NumberOfSheets - 1;
        workbook.SetActiveSheet(abaDoFechamento);
        workbook.SetSelectedTab(abaDoFechamento);

        HSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }
}
