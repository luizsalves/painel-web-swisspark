using PainelWebSwissPark.Api.Data.Repositories;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Exceptions;
using PainelWebSwissPark.Api.Services.Auth;

namespace PainelWebSwissPark.Api.Services.BusinessLogic;

/// <summary>
/// Preparar/Conferir/Realizar Fechamento/Reverter — a autorização por perfil fixo
/// (Analista, Gerente e Administrador preparam e fecham; só Gerente e
/// Administrador conferem) é resolvida em <see cref="Authorization.ExigePerfilAttribute"/>,
/// no controller. Só o Reverter decide o perfil aqui, porque depende de qual
/// etapa está sendo desfeita.
/// </summary>
public class FechamentoValidacaoService(IFechamentoValidacaoRepository repository) : IFechamentoValidacaoService
{
    private static readonly string[] PerfisPreparaEFecha =
        [PerfisUsuario.Analista, PerfisUsuario.Gerente, PerfisUsuario.Administrador];

    private static readonly string[] PerfisConfere =
        [PerfisUsuario.Gerente, PerfisUsuario.Administrador];

    public Task<List<ProdutividadeUsuarioViewModel>> ObterProdutividadeAsync(
        string status, string? anoMes, CancellationToken cancellationToken)
    {
        var statusNormalizado = NormalizarStatusDeAcao(status)
            ?? throw new RegraDeNegocioException(
                $"Status inválido: \"{status}\". Use Preparado, Conferido ou Fechado.");

        return repository.ContarPorUsuarioAsync(statusNormalizado, anoMes, cancellationToken);
    }

    // "Finalizado" é o rótulo da tela pra Fechado — no banco o valor é sempre "Fechado".
    public static string? NormalizarStatusDeAcao(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "preparado" => StatusFechamento.Preparado,
            "conferido" => StatusFechamento.Conferido,
            "fechado" or "finalizado" => StatusFechamento.Fechado,
            _ => null,
        };

    public async Task<FechamentoValidacaoViewModel> ObterStatusAtualAsync(
        short codColigada, short codFilial, string codConta, string anoMes, CancellationToken cancellationToken)
    {
        var historico = await repository.ListarHistoricoAsync(codColigada, codFilial, codConta, anoMes, cancellationToken);
        return await MontarViewModelAsync(historico, cancellationToken);
    }

    public async Task<FechamentoValidacaoViewModel> PrepararAsync(
        PrepararFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken)
    {
        var historico = await repository.ListarHistoricoAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);

        var statusAtual = historico.Count == 0 ? StatusFechamento.Pendente : historico[^1].Status;
        if (statusAtual != StatusFechamento.Pendente)
        {
            throw new RegraDeNegocioException(
                $"Essa conta já está {statusAtual}. Reverta antes de preparar de novo.");
        }

        var linha = new FechamentoValidacao
        {
            CodColigada = request.CodColigada,
            CodFilial = request.CodFilial,
            CodConta = request.CodConta,
            AnoMes = request.AnoMes,
            Status = StatusFechamento.Preparado,
            Prioridade = request.Prioridade,
            SaldoAnterior = request.SaldoAnterior,
            SaldoAtual = request.SaldoAtual,
            DataFechamento = PrimeiroDiaDoMes(request.AnoMes),
            PendenciaTexto = request.PendenciaTexto,
            PendenciaResponsavelId = request.PendenciaResponsavelId,
            PendenciaRegistradaEm = request.PendenciaTexto is not null ? DateTimeOffset.UtcNow : null,
            RealizadoPor = autor.Id,
        };

        await repository.AdicionarAsync(linha, cancellationToken);

        return await ObterStatusAtualAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);
    }

    public async Task<FechamentoValidacaoViewModel> ConferirAsync(
        ChaveFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken)
    {
        var historico = await repository.ListarHistoricoAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);

        if (historico.Count == 0 || historico[^1].Status != StatusFechamento.Preparado)
        {
            throw new RegraDeNegocioException("Só é possível conferir uma conta que esteja Preparada.");
        }

        await repository.AdicionarAsync(
            ClonarParaNovoStatus(historico[^1], StatusFechamento.Conferido, autor.Id), cancellationToken);

        return await ObterStatusAtualAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);
    }

    public async Task<FechamentoValidacaoViewModel> RealizarFechamentoAsync(
        RealizarFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken)
    {
        var historico = await repository.ListarHistoricoAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);

        if (historico.Count == 0 || historico[^1].Status != StatusFechamento.Conferido)
        {
            throw new RegraDeNegocioException("Só é possível realizar o fechamento de uma conta que esteja Conferida.");
        }

        var linha = ClonarParaNovoStatus(historico[^1], StatusFechamento.Fechado, autor.Id);
        if (request.SaldoAtual is { } novoSaldo)
        {
            linha.SaldoAtual = novoSaldo;
        }

        await repository.AdicionarAsync(linha, cancellationToken);

        return await ObterStatusAtualAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);
    }

    public async Task<FechamentoValidacaoViewModel> ReverterAsync(
        ChaveFechamentoRequest request, UsuarioAutorizado autor, CancellationToken cancellationToken)
    {
        var historico = await repository.ListarHistoricoAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);

        if (historico.Count == 0)
        {
            throw new RegraDeNegocioException("Não há nada para reverter nessa conta/mês.");
        }

        var atual = historico[^1];

        // Quem reverte uma etapa é sempre quem tem o perfil que a executaria: Analista,
        // Gerente e Administrador preparam e fecham; só Gerente e Administrador conferem.
        var (statusAnterior, perfisExigidos) = atual.Status switch
        {
            StatusFechamento.Fechado => (StatusFechamento.Conferido, PerfisPreparaEFecha),
            StatusFechamento.Conferido => (StatusFechamento.Preparado, PerfisConfere),
            StatusFechamento.Preparado => (StatusFechamento.Pendente, PerfisPreparaEFecha),
            _ => throw new RegraDeNegocioException("Não há nada para reverter nessa conta/mês."),
        };

        if (!autor.TemAlgumPerfilNoModulo(Modulos.Conciliacao, perfisExigidos))
        {
            throw new RegraDeNegocioException(
                $"Só quem tem o perfil {string.Join(" ou ", perfisExigidos)} pode reverter esse status.");
        }

        await repository.AdicionarAsync(ClonarParaNovoStatus(atual, statusAnterior, autor.Id), cancellationToken);

        return await ObterStatusAtualAsync(
            request.CodColigada, request.CodFilial, request.CodConta, request.AnoMes, cancellationToken);
    }

    private async Task<FechamentoValidacaoViewModel> MontarViewModelAsync(
        List<FechamentoValidacao> historico, CancellationToken cancellationToken)
    {
        if (historico.Count == 0)
        {
            return new FechamentoValidacaoViewModel { Status = StatusFechamento.Pendente };
        }

        var ultima = historico[^1];
        var preparado = historico.LastOrDefault(h => h.Status == StatusFechamento.Preparado);
        var conferido = historico.LastOrDefault(h => h.Status == StatusFechamento.Conferido);
        var fechado = historico.LastOrDefault(h => h.Status == StatusFechamento.Fechado);

        var ids = new HashSet<Guid>();
        if (preparado is not null) ids.Add(preparado.RealizadoPor);
        if (conferido is not null) ids.Add(conferido.RealizadoPor);
        if (fechado is not null) ids.Add(fechado.RealizadoPor);
        if (ultima.PendenciaResponsavelId is { } pendenciaId) ids.Add(pendenciaId);

        var nomes = await repository.ObterNomesAsync(ids, cancellationToken);

        return new FechamentoValidacaoViewModel
        {
            Status = ultima.Status,
            Prioridade = ultima.Prioridade,
            SaldoAnterior = ultima.SaldoAnterior,
            SaldoAtual = ultima.SaldoAtual,

            PreparadoPorId = preparado?.RealizadoPor,
            PreparadoPorNome = preparado is null ? null : nomes.GetValueOrDefault(preparado.RealizadoPor),
            PreparadoEm = preparado?.RealizadoEm,

            ConferidoPorId = conferido?.RealizadoPor,
            ConferidoPorNome = conferido is null ? null : nomes.GetValueOrDefault(conferido.RealizadoPor),
            ConferidoEm = conferido?.RealizadoEm,

            FechadoPorId = fechado?.RealizadoPor,
            FechadoPorNome = fechado is null ? null : nomes.GetValueOrDefault(fechado.RealizadoPor),
            FechadoEm = fechado?.RealizadoEm,

            PendenciaTexto = ultima.PendenciaTexto,
            PendenciaResponsavelId = ultima.PendenciaResponsavelId,
            PendenciaResponsavelNome = ultima.PendenciaResponsavelId is { } respId
                ? nomes.GetValueOrDefault(respId)
                : null,
            PendenciaRegistradaEm = ultima.PendenciaRegistradaEm,
        };
    }

    private static FechamentoValidacao ClonarParaNovoStatus(
        FechamentoValidacao anterior, string novoStatus, Guid realizadoPor) => new()
    {
        CodColigada = anterior.CodColigada,
        CodFilial = anterior.CodFilial,
        CodConta = anterior.CodConta,
        AnoMes = anterior.AnoMes,
        Status = novoStatus,
        Prioridade = anterior.Prioridade,
        SaldoAnterior = anterior.SaldoAnterior,
        SaldoAtual = anterior.SaldoAtual,
        DataFechamento = anterior.DataFechamento,
        PendenciaTexto = anterior.PendenciaTexto,
        PendenciaResponsavelId = anterior.PendenciaResponsavelId,
        PendenciaRegistradaEm = anterior.PendenciaRegistradaEm,
        RealizadoPor = realizadoPor,
    };

    private static DateOnly? PrimeiroDiaDoMes(string anoMes) =>
        DateOnly.TryParseExact($"{anoMes}-01", "yyyy-MM-dd", out var data) ? data : null;
}
