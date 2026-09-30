using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>Tabelas do TOTVS liberadas pra leitura genérica — o nome vai direto no SQL, então só o que está aqui.</summary>
public enum TabelaTotvs
{
    CEXERCICIO,
    CRATEIOLC,
    CSALDOEXERCICIO,
    FCFO,
    FLAN,
    FLANBAIXA,
    FRELLAN,
    FTDO,
    GCCUSTO,
    TMOV,
}

public enum TipoFiltroTotvs
{
    Igual,
    /// <summary>Coluna = valor OU coluna = 0 — CODCOLIGADA 0 é o cadastro global do TOTVS.</summary>
    IgualOuGlobal,
    MaiorOuIgual,
    Menor,
    Contem,
}

/// <summary>
/// Coluna sempre é constante escrita no código (controller), nunca texto vindo
/// da requisição; o valor vai como parâmetro.
/// </summary>
public record FiltroTotvs(string Coluna, TipoFiltroTotvs Tipo, object Valor);

public class TotvsConsultaRepository(AppDbContext context) : ITotvsConsultaRepository
{
    // Somente SELECT: o TOTVS é só leitura pro painel.
    public async Task<List<Dictionary<string, object?>>> ListarAsync(
        BaseTotvs baseDados, TabelaTotvs tabela, IReadOnlyList<FiltroTotvs> filtros, string ordenacao,
        int limite, CancellationToken cancellationToken)
    {
        var nomeDoBanco = BasesTotvs.NomeDoBanco(baseDados);
        var conexao = context.Database.GetDbConnection();
        var abriuAqui = conexao.State != ConnectionState.Open;
        if (abriuAqui) await conexao.OpenAsync(cancellationToken);

        try
        {
            await using var comando = conexao.CreateCommand();
            comando.CommandTimeout = context.Database.GetCommandTimeout() ?? 60;

            var condicoes = new List<string>();
            for (var i = 0; i < filtros.Count; i++)
            {
                var filtro = filtros[i];
                var parametro = $"@p{i}";
                condicoes.Add(filtro.Tipo switch
                {
                    TipoFiltroTotvs.Igual => $"[{filtro.Coluna}] = {parametro}",
                    TipoFiltroTotvs.IgualOuGlobal => $"([{filtro.Coluna}] = {parametro} OR [{filtro.Coluna}] = 0)",
                    TipoFiltroTotvs.MaiorOuIgual => $"[{filtro.Coluna}] >= {parametro}",
                    TipoFiltroTotvs.Menor => $"[{filtro.Coluna}] < {parametro}",
                    TipoFiltroTotvs.Contem => $"[{filtro.Coluna}] LIKE {parametro}",
                    _ => throw new ArgumentOutOfRangeException(nameof(filtros)),
                });
                AdicionarParametro(comando, parametro,
                    filtro.Tipo == TipoFiltroTotvs.Contem ? $"%{filtro.Valor}%" : filtro.Valor);
            }

            AdicionarParametro(comando, "@limite", limite);

            var where = condicoes.Count == 0 ? "" : "WHERE " + string.Join(" AND ", condicoes);
            comando.CommandText =
                $"SELECT TOP (@limite) * FROM {nomeDoBanco}.dbo.[{tabela}] WITH (NOLOCK) {where} ORDER BY {ordenacao}";

            var linhas = new List<Dictionary<string, object?>>();
            await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
            while (await leitor.ReadAsync(cancellationToken))
            {
                var linha = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["BASE"] = nomeDoBanco };
                for (var coluna = 0; coluna < leitor.FieldCount; coluna++)
                {
                    var valor = leitor.IsDBNull(coluna) ? null : leitor.GetValue(coluna);
                    // Campos CHAR do TOTVS vêm com espaço sobrando à direita.
                    linha[leitor.GetName(coluna)] = valor is string texto ? texto.TrimEnd() : valor;
                }
                linhas.Add(linha);
            }

            return linhas;
        }
        finally
        {
            if (abriuAqui) await conexao.CloseAsync();
        }
    }

    private static void AdicionarParametro(DbCommand comando, string nome, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
