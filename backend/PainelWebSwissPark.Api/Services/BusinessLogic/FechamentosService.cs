using System.Globalization;
using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.ViewModels;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

public class FechamentosService(IFechamentoRepository fechamentoRepository) : IFechamentosService
{
    public async Task<FechamentoViewModel?> ObterFechamentoAsync(
        int empresaId, int contaContabilId, DateOnly periodo, CancellationToken cancellationToken)
    {
        var fechamento = await fechamentoRepository.ObterPorFiltroAsync(
            empresaId, contaContabilId, periodo, cancellationToken);

        if (fechamento is null)
        {
            return null;
        }

        return new FechamentoViewModel
        {
            EmpresaId = fechamento.EmpresaId,
            Empresa = fechamento.Empresa?.RazaoSocial ?? string.Empty,
            ContaContabilId = fechamento.ContaContabilId,
            ContaContabilCodigo = fechamento.ContaContabil?.Codigo ?? string.Empty,
            ContaContabilDescricao = fechamento.ContaContabil?.Descricao ?? string.Empty,
            Periodo = fechamento.Periodo.ToString("MM/yyyy", CultureInfo.InvariantCulture),

            SaldoRazao = fechamento.SaldoRazao,
            ComposicaoSaldo = fechamento.ComposicaoSaldo,
            Diferenca = fechamento.SaldoRazao - fechamento.ComposicaoSaldo,
            PossuiPendencia = fechamento.PossuiPendencia,

            Status = fechamento.Status,
            Prioridade = fechamento.Prioridade,
            Responsavel = fechamento.Responsavel?.Nome ?? string.Empty,
            AtualizadoEm = fechamento.AtualizadoEm.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),

            Lancamentos = fechamento.Lancamentos
                .OrderBy(l => l.Data)
                .Select(l => new LancamentoViewModel
                {
                    Data = l.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    Historico = l.Historico,
                    Documento = l.Documento,
                    Numero = l.Numero,
                    Valor = l.Valor,
                    Tipo = l.Tipo,
                })
                .ToList(),

            Pendencias = fechamento.Pendencias
                .Where(p => !p.Resolvida)
                .Select(p => new PendenciaViewModel
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Descricao = p.Descricao,
                    Prioridade = p.Prioridade,
                })
                .ToList(),
        };
    }
}
