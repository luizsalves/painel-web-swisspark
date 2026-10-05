using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>Composição do saldo de uma conta numa base: soma do que está em aberto.</summary>
public class ComposicaoContaLinha
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Documentos { get; set; }
    public decimal EmAberto { get; set; }
}

public class SaldoRazaoContaLinha
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal SaldoRazao { get; set; }
}

/// <summary>
/// Composição do saldo e saldo do razão, lidos direto do TOTVS (só SELECT),
/// reproduzindo a lógica do fc_fech_composicao_v12 do sistema antigo:
/// - Fornecedores de Materiais/Serviços: NFs lançadas a crédito − pagamentos (FLAN ligada à TMOV);
/// - Adiantamentos: títulos ADF/ADC baixados − NFs vinculadas (cascata 401 → 501 → 601 → 701 → 801).
/// O corte em 2025-01-01 é o mesmo período que a carga do sistema antigo copiava do TOTVS.
/// </summary>
public class DiferencasTotvsRepository(AppDbContext context) : IDiferencasTotvsRepository
{
    private static readonly TimeSpan TempoLimiteConsulta = TimeSpan.FromMinutes(5);

    private const string ContasFornecedores =
        "N'FORNECEDORES DE MATERIAIS', N'FORNECEDORES DE SERVIÇOS'";

    private const string ContasAdiantamento =
        "N'ADIANTAMENTO A FORNECEDORES', N'ADIANTAMENTO DE FORNECEDORES', N'ADIANTAMENTO DIVERSOS', " +
        "N'ADIANTAMENTOS DIVERSOS', N'ADIANTAMENTO DE CLIENTES', N'ADIANTAMENTO DE CLIENTES VILLA SUIÇA'";

    // Número do documento sem o sufixo "/..." e sem zeros à esquerda: "000123/01" → "123".
    private static string DocumentoTratado(string coluna) =>
        $"NULLIF(SUBSTRING(LEFT({coluna}, CHARINDEX('/', {coluna} + '/') - 1), " +
        $"PATINDEX('%[^0]%', LEFT({coluna}, CHARINDEX('/', {coluna} + '/') - 1) + 'x'), 100), '')";

    private static readonly string SqlFornecedores = $"""
        WITH CONTAS AS (
            SELECT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA, UPPER(LTRIM(RTRIM(DESCRICAO))) AS DESCRICAO
            FROM __BASE__.dbo.CCONTA WITH (NOLOCK)
            WHERE CODCOLIGADA <> 0 AND UPPER(LTRIM(RTRIM(DESCRICAO))) IN ({ContasFornecedores})
        ),
        NF AS (
            SELECT P.CODCOLIGADA, P.CODFILIAL, P.CREDITO AS CODCONTA, P.VALOR,
                   CASE WHEN P.COMPLEMENTO LIKE '[0-9]%'
                         AND LEFT(LTRIM(SUBSTRING(P.COMPLEMENTO, PATINDEX('%[^0-9]%', P.COMPLEMENTO + 'x'), 500)), 1) = '-'
                        THEN LEFT(P.COMPLEMENTO, PATINDEX('%[^0-9]%', P.COMPLEMENTO + 'x') - 1) END AS CODCFO,
                   {DocumentoTratado("P.DOCUMENTO")} AS DOC
            FROM __BASE__.dbo.CLANCA P WITH (NOLOCK)
            WHERE P.CODCOLIGADA <> 0 AND P.DATA >= '2025-01-01' AND P.DATA <= CONVERT(date, {"{0}"}) AND P.CREDITO IS NOT NULL
              AND UPPER(P.COMPLEMENTO) LIKE '%NF%'
              AND UPPER(P.COMPLEMENTO) NOT LIKE '%PAGAM%'
              AND UPPER(P.COMPLEMENTO) NOT LIKE '%S/NF%'
              AND UPPER(P.COMPLEMENTO) NOT LIKE '%S/ NF%'
        ),
        PAG AS (
            SELECT F.CODCOLIGADA, F.CODFILIAL, F.CODCFO, {DocumentoTratado("M.NUMEROMOV")} AS DOC,
                   SUM(F.VALORORIGINAL) AS PAGO
            FROM __BASE__.dbo.FLAN F WITH (NOLOCK)
            JOIN __BASE__.dbo.TMOV M WITH (NOLOCK)
              ON M.CODCOLIGADA = F.CODCOLIGADA AND M.IDMOV = F.IDMOV AND M.CODFILIAL = F.CODFILIAL
            WHERE F.CODCOLIGADA <> 0 AND F.DATACANCELAMENTO IS NULL AND F.DATABAIXA IS NOT NULL AND F.DATABAIXA <= CONVERT(date, {"{0}"})
              AND F.DATAVENCIMENTO >= '2025-01-01' AND M.DATAEMISSAO >= '2025-01-01'
            GROUP BY F.CODCOLIGADA, F.CODFILIAL, F.CODCFO, {DocumentoTratado("M.NUMEROMOV")}
        )
        SELECT N.CODCOLIGADA AS CodColigada, N.CODFILIAL AS CodFilial, N.CODCONTA AS CodConta,
               MAX(C.DESCRICAO) AS Descricao, COUNT(*) AS Documentos,
               CAST(SUM(N.VALOR - ISNULL(P.PAGO, 0)) AS numeric(18, 4)) AS EmAberto
        FROM NF N
        JOIN CONTAS C ON C.CODCOLIGADA = N.CODCOLIGADA AND C.CODCONTA = N.CODCONTA
        LEFT JOIN PAG P ON P.CODCOLIGADA = N.CODCOLIGADA AND P.CODFILIAL = N.CODFILIAL
                       AND P.CODCFO = N.CODCFO AND P.DOC = N.DOC
        GROUP BY N.CODCOLIGADA, N.CODFILIAL, N.CODCONTA
        """;

    private static readonly string SqlAdiantamentos = $"""
        WITH CONTAS AS (
            SELECT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA, UPPER(LTRIM(RTRIM(DESCRICAO))) AS DESCRICAO
            FROM __BASE__.dbo.CCONTA WITH (NOLOCK)
            WHERE CODCOLIGADA <> 0 AND UPPER(LTRIM(RTRIM(DESCRICAO))) IN ({ContasAdiantamento})
        ),
        PART AS (
            SELECT P.CODCOLIGADA, P.CODFILIAL, K.CODCONTA, 'D' AS LADO, P.DOCUMENTO, P.VALOR
            FROM __BASE__.dbo.CPARTIDA P WITH (NOLOCK)
            JOIN CONTAS K ON K.CODCOLIGADA = P.CODCOLIGADA AND K.CODCONTA = P.DEBITO
            WHERE P.CODCOLIGADA <> 0 AND P.DATA >= '2025-01-01'
            UNION ALL
            SELECT P.CODCOLIGADA, P.CODFILIAL, K.CODCONTA, 'C', P.DOCUMENTO, P.VALOR
            FROM __BASE__.dbo.CPARTIDA P WITH (NOLOCK)
            JOIN CONTAS K ON K.CODCOLIGADA = P.CODCOLIGADA AND K.CODCONTA = P.CREDITO
            WHERE P.CODCOLIGADA <> 0 AND P.DATA >= '2025-01-01'
        ),
        CONTA_DO_DOC AS (
            SELECT DISTINCT CODCOLIGADA, {DocumentoTratado("DOCUMENTO")} AS DOC, CODCONTA FROM PART
        ),
        LAN AS (
            SELECT DISTINCT F.CODCOLIGADA, F.CODFILIAL, F.IDLAN, F.CODCFO, F.NUMERODOCUMENTO, F.SEGUNDONUMERO,
                   C.IDLANREL, C.TIPOREL,
                   F.VALORORIGINAL + F.VALORJUROS + F.VALORMULTA - F.VALORDESCONTO AS VALORLIQUIDO,
                   NULLIF(SUBSTRING(F.NUMERODOCUMENTO, PATINDEX('%[^0]%', F.NUMERODOCUMENTO + 'x'), 100), '') AS DOC
            FROM __BASE__.dbo.FLAN F WITH (NOLOCK)
            LEFT JOIN __BASE__.dbo.FRELLAN C WITH (NOLOCK) ON C.CODCOLIGADA = F.CODCOLIGADA AND C.IDLAN = F.IDLAN
            WHERE F.CODCOLIGADA <> 0 AND F.CODTDO IN ('ADF', 'ADC') AND F.DATABAIXA <= CONVERT(date, {"{0}"}) AND F.DATACANCELAMENTO IS NULL
              AND F.DATAVENCIMENTO >= '2025-01-01'
        ),
        BASE AS (
            SELECT L.*, D.CODCONTA, K.DESCRICAO
            FROM LAN L
            JOIN CONTA_DO_DOC D ON D.CODCOLIGADA = L.CODCOLIGADA AND D.DOC = L.DOC
            JOIN CONTAS K ON K.CODCOLIGADA = D.CODCOLIGADA AND K.CODCONTA = D.CODCONTA
        ),
        PARC AS (
            SELECT DISTINCT B.CODCOLIGADA, B.CODFILIAL, F.CODCFO, F.SEGUNDONUMERO, F.NUMERODOCUMENTO,
                   C.IDLAN, C.IDLANREL, C.IDBAIXA, C.IDBAIXAREL, C.VALORVINCULO, B.DATABAIXA, B.DATACANCELBAIXA
            FROM (SELECT DISTINCT CODCOLIGADA, CODCFO, SEGUNDONUMERO, NUMERODOCUMENTO, IDLANREL
                  FROM BASE WHERE IDLANREL IS NOT NULL) F
            JOIN __BASE__.dbo.FRELLAN C WITH (NOLOCK)
              ON C.CODCOLIGADA = F.CODCOLIGADA AND C.IDLANREL = F.IDLANREL AND C.TIPOREL = 28
            JOIN __BASE__.dbo.FLANBAIXA B WITH (NOLOCK) ON B.CODCOLIGADA = C.CODCOLIGADA AND B.IDBAIXA = C.IDBAIXA
            JOIN __BASE__.dbo.FLAN L WITH (NOLOCK) ON L.CODCOLIGADA = B.CODCOLIGADA AND L.IDLAN = B.IDLAN
            WHERE B.CODCOLIGADA <> 0 AND B.DATABAIXA <= CONVERT(date, {"{0}"}) AND L.DATAEMISSAO <= CONVERT(date, {"{0}"})
        ),
        G401 AS (SELECT CODCOLIGADA, CODFILIAL, CODCFO, SEGUNDONUMERO, IDLANREL, SUM(VALORVINCULO) AS ACUM FROM PARC
                 WHERE IDBAIXAREL IS NOT NULL AND DATACANCELBAIXA IS NULL
                 GROUP BY CODCOLIGADA, CODFILIAL, CODCFO, SEGUNDONUMERO, IDLANREL),
        G501 AS (SELECT CODCOLIGADA, CODFILIAL, CODCFO, NUMERODOCUMENTO, IDLANREL, SUM(VALORVINCULO) AS ACUM FROM PARC
                 WHERE IDBAIXAREL IS NULL AND DATACANCELBAIXA IS NULL AND DATABAIXA IS NOT NULL
                 GROUP BY CODCOLIGADA, CODFILIAL, CODCFO, NUMERODOCUMENTO, IDLANREL),
        G601 AS (SELECT CODCOLIGADA, CODFILIAL, CODCFO, NUMERODOCUMENTO, IDLANREL, SUM(VALORVINCULO) AS ACUM FROM PARC
                 WHERE IDBAIXAREL IS NOT NULL AND DATACANCELBAIXA IS NULL AND DATABAIXA IS NOT NULL
                 GROUP BY CODCOLIGADA, CODFILIAL, CODCFO, NUMERODOCUMENTO, IDLANREL),
        G701 AS (SELECT CODCOLIGADA, CODFILIAL, CODCONTA, DOCUMENTO, SUM(VALOR) AS ACUM FROM PART
                 WHERE LADO = 'C' GROUP BY CODCOLIGADA, CODFILIAL, CODCONTA, DOCUMENTO),
        G801 AS (SELECT CODCOLIGADA, CODFILIAL, SEGUNDONUMERO, IDLANREL, SUM(VALORVINCULO) AS ACUM FROM PARC
                 WHERE IDBAIXAREL IS NOT NULL AND DATACANCELBAIXA IS NULL
                 GROUP BY CODCOLIGADA, CODFILIAL, SEGUNDONUMERO, IDLANREL),
        C1 AS (SELECT b.*, CASE WHEN g.IDLANREL IS NOT NULL THEN 401 ELSE b.TIPOREL END AS T1,
                      CASE WHEN g.IDLANREL IS NOT NULL THEN g.ACUM ELSE 0 END AS A1
               FROM BASE b LEFT JOIN G401 g ON g.CODCOLIGADA = b.CODCOLIGADA AND g.CODFILIAL = b.CODFILIAL
                   AND g.CODCFO = b.CODCFO AND g.SEGUNDONUMERO = b.SEGUNDONUMERO AND g.IDLANREL = b.IDLANREL),
        C2 AS (SELECT c.*,
                      CASE WHEN c.T1 IS NOT NULL AND c.T1 <> 401 AND g.IDLANREL IS NOT NULL AND ISNULL(c.A1, 0) = 0 THEN 501 ELSE c.T1 END AS T2,
                      CASE WHEN c.T1 IS NOT NULL AND c.T1 <> 401 AND g.IDLANREL IS NOT NULL AND ISNULL(c.A1, 0) = 0 THEN g.ACUM ELSE c.A1 END AS A2
               FROM C1 c LEFT JOIN G501 g ON g.CODCOLIGADA = c.CODCOLIGADA AND g.CODFILIAL = c.CODFILIAL
                   AND g.CODCFO = c.CODCFO AND g.NUMERODOCUMENTO = c.NUMERODOCUMENTO AND g.IDLANREL = c.IDLANREL),
        C3 AS (SELECT c.*,
                      CASE WHEN c.T2 IS NOT NULL AND c.T2 NOT IN (401, 501) AND g.IDLANREL IS NOT NULL AND ISNULL(c.A2, 0) = 0 THEN 601 ELSE c.T2 END AS T3,
                      CASE WHEN c.T2 IS NOT NULL AND c.T2 NOT IN (401, 501) AND g.IDLANREL IS NOT NULL AND ISNULL(c.A2, 0) = 0 THEN g.ACUM ELSE c.A2 END AS A3
               FROM C2 c LEFT JOIN G601 g ON g.CODCOLIGADA = c.CODCOLIGADA AND g.CODFILIAL = c.CODFILIAL
                   AND g.CODCFO = c.CODCFO AND g.NUMERODOCUMENTO = c.NUMERODOCUMENTO AND g.IDLANREL = c.IDLANREL),
        C4 AS (SELECT c.*,
                      CASE WHEN c.T3 IS NOT NULL AND c.T3 NOT IN (401, 501, 601) AND g.DOCUMENTO IS NOT NULL AND ISNULL(c.A3, 0) = 0 THEN 701 ELSE c.T3 END AS T4,
                      CASE WHEN c.T3 IS NOT NULL AND c.T3 NOT IN (401, 501, 601) AND g.DOCUMENTO IS NOT NULL AND ISNULL(c.A3, 0) = 0 THEN g.ACUM ELSE c.A3 END AS A4
               FROM C3 c LEFT JOIN G701 g ON g.CODCOLIGADA = c.CODCOLIGADA AND g.CODFILIAL = c.CODFILIAL
                   AND g.DOCUMENTO = c.NUMERODOCUMENTO AND g.CODCONTA = c.CODCONTA),
        C5 AS (SELECT c.*,
                      CASE WHEN c.T4 IS NOT NULL AND c.T4 NOT IN (401, 501, 601, 701) AND g.IDLANREL IS NOT NULL THEN g.ACUM ELSE c.A4 END AS ACUM
               FROM C4 c LEFT JOIN G801 g ON g.CODCOLIGADA = c.CODCOLIGADA AND g.CODFILIAL = c.CODFILIAL
                   AND g.SEGUNDONUMERO = c.SEGUNDONUMERO AND g.IDLANREL = c.IDLANREL)
        SELECT CODCOLIGADA AS CodColigada, CODFILIAL AS CodFilial, CODCONTA AS CodConta, MAX(DESCRICAO) AS Descricao,
               COUNT(*) AS Documentos, CAST(SUM(VALORLIQUIDO - ISNULL(ACUM, 0)) AS numeric(18, 4)) AS EmAberto
        FROM C5
        GROUP BY CODCOLIGADA, CODFILIAL, CODCONTA
        """;

    // Mesma regra de sinal da consulta de lançamentos: conta do grupo 1 soma
    // débito e subtrai crédito; as demais, o contrário. Todo o histórico até o fim do mês.
    private static readonly string SqlSaldoRazao = $"""
        WITH CONTAS AS (
            SELECT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA, UPPER(LTRIM(RTRIM(DESCRICAO))) AS DESCRICAO
            FROM __BASE__.dbo.CCONTA WITH (NOLOCK)
            WHERE CODCOLIGADA <> 0 AND UPPER(LTRIM(RTRIM(DESCRICAO))) IN ({ContasFornecedores}, {ContasAdiantamento})
        ),
        MOV AS (
            SELECT A.CODCOLIGADA, A.CODFILIAL, K.CODCONTA, K.DESCRICAO, A.VALOR AS DEB, CAST(0 AS numeric(18, 4)) AS CRED
            FROM __BASE__.dbo.CPARTIDA A WITH (NOLOCK)
            JOIN CONTAS K ON K.CODCOLIGADA = A.CODCOLIGADA AND K.CODCONTA = A.DEBITO
            WHERE A.CODCOLIGADA <> 0 AND A.DATA <= CONVERT(date, {"{0}"})
            UNION ALL
            SELECT A.CODCOLIGADA, A.CODFILIAL, K.CODCONTA, K.DESCRICAO, 0, A.VALOR
            FROM __BASE__.dbo.CPARTIDA A WITH (NOLOCK)
            JOIN CONTAS K ON K.CODCOLIGADA = A.CODCOLIGADA AND K.CODCONTA = A.CREDITO
            WHERE A.CODCOLIGADA <> 0 AND A.DATA <= CONVERT(date, {"{0}"})
        )
        SELECT CODCOLIGADA AS CodColigada, CODFILIAL AS CodFilial, CODCONTA AS CodConta, MAX(DESCRICAO) AS Descricao,
               CAST(CASE WHEN CODCONTA LIKE '1%' THEN SUM(DEB) - SUM(CRED) ELSE SUM(CRED) - SUM(DEB) END AS numeric(18, 4)) AS SaldoRazao
        FROM MOV
        GROUP BY CODCOLIGADA, CODFILIAL, CODCONTA
        """;

    public async Task<List<ComposicaoContaLinha>> ListarComposicaoAsync(
        BaseTotvs baseDados, DateOnly fimDoMes, CancellationToken cancellationToken)
    {
        context.Database.SetCommandTimeout(TempoLimiteConsulta);
        var nomeDoBanco = BasesTotvs.NomeDoBanco(baseDados);
        var fim = fimDoMes.ToString("yyyy-MM-dd");

        // Sequencial: o DbContext não aceita duas consultas ao mesmo tempo.
        var fornecedores = await context.Database
            .SqlQueryRaw<ComposicaoContaLinha>(SqlFornecedores.Replace("__BASE__", nomeDoBanco), fim)
            .ToListAsync(cancellationToken);
        var adiantamentos = await context.Database
            .SqlQueryRaw<ComposicaoContaLinha>(SqlAdiantamentos.Replace("__BASE__", nomeDoBanco), fim)
            .ToListAsync(cancellationToken);

        return [.. fornecedores, .. adiantamentos];
    }

    public async Task<List<SaldoRazaoContaLinha>> ListarSaldoRazaoAsync(
        BaseTotvs baseDados, DateOnly fimDoMes, CancellationToken cancellationToken)
    {
        context.Database.SetCommandTimeout(TempoLimiteConsulta);
        return await context.Database
            .SqlQueryRaw<SaldoRazaoContaLinha>(
                SqlSaldoRazao.Replace("__BASE__", BasesTotvs.NomeDoBanco(baseDados)), fimDoMes.ToString("yyyy-MM-dd"))
            .ToListAsync(cancellationToken);
    }
}
