using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Extensions;
using PainelWebSwissPark.Api.Middleware;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using PainelWebSwissPark.Api.Services.Erp;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================================
// Segredos
//
// Connection strings ficam no keys.json, que não entra no repositório. Na
// prática ele fica na raiz do projeto, ao lado de backend/ e frontend/ — e não
// dentro da pasta do projeto .NET. Por isso a busca sobe de diretório em vez de
// olhar só o ContentRoot.
//
// PAINEL_KEYS_FILE tem precedência e aponta para qualquer caminho.
// Modelo do formato: keys.example.json, na raiz.
// ==========================================================================
var arquivoDeChaves = Environment.GetEnvironmentVariable("PAINEL_KEYS_FILE");

if (string.IsNullOrWhiteSpace(arquivoDeChaves))
{
    var diretorio = new DirectoryInfo(builder.Environment.ContentRootPath);

    for (var nivel = 0; nivel < 5 && diretorio is not null; nivel++)
    {
        var candidato = Path.Combine(diretorio.FullName, "keys.json");

        if (File.Exists(candidato))
        {
            arquivoDeChaves = candidato;
            break;
        }

        diretorio = diretorio.Parent;
    }
}

if (!string.IsNullOrWhiteSpace(arquivoDeChaves))
{
    builder.Configuration.AddJsonFile(arquivoDeChaves, optional: true, reloadOnChange: true);
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

// ==========================================================================
// Banco do painel (Postgres) — só as tabelas do próprio painel
// ==========================================================================
builder.Services.Configure<PainelDbOptions>(
    builder.Configuration.GetSection(PainelDbOptions.SectionName));

// "Contabil" é o nome usado no keys.example.json da raiz, junto das outras cinco
// conexões. PainelDb:ConnectionString foi a grafia inicial e continua aceita.
var conexaoDoPainel = builder.Configuration.GetConnectionString("Contabil")
    ?? builder.Configuration["PainelDb:ConnectionString"];

if (string.IsNullOrWhiteSpace(conexaoDoPainel))
{
    // Dizer qual arquivo foi lido poupa a meia hora de procurar por que o valor
    // "está lá" mas a aplicação não enxerga.
    var ondeProcurou = arquivoDeChaves is null
        ? $"Nenhum keys.json encontrado a partir de {builder.Environment.ContentRootPath} (subindo até 5 níveis)."
        : $"keys.json lido: {arquivoDeChaves}";

    throw new InvalidOperationException(
        "Connection string do Postgres do painel não encontrada. Preencha ConnectionStrings:Contabil "
        + "no keys.json, ou defina a variável de ambiente ConnectionStrings__Contabil. "
        + ondeProcurou);
}

builder.Services.AddDbContext<PainelDbContext>(options => options.UseNpgsql(conexaoDoPainel));

// ==========================================================================
// Bases do TOTVS RM (SQL Server) — de onde vêm os fechamentos
// ==========================================================================
builder.Services.AddSqlServerData(builder.Configuration);

// ==========================================================================
// Autenticação (Entra ID) e autorização (bi_users)
//
// O Entra ID prova quem a pessoa é; a bi_users diz se ela pode entrar.
// Nenhum token próprio é emitido: o front manda o access token do Entra e o
// backend consulta a bi_users a cada requisição. É isso que faz a desativação
// valer no request seguinte, sem esperar token expirar.
// ==========================================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd");

builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();
builder.Services.AddScoped<IClaimsTransformation, BiUsersClaimsTransformation>();
builder.Services.AddScoped<IAuthorizationHandler, UsuarioAtivoHandler>();

var politicaUsuarioAtivo = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .AddRequirements(new UsuarioAtivoRequirement())
    .Build();

builder.Services.AddAuthorizationBuilder()
    // Política padrão: endpoint sem atributo de autorização já nasce protegido.
    // Para abrir um endpoint é preciso dizer [AllowAnonymous] explicitamente.
    .SetFallbackPolicy(politicaUsuarioAtivo)
    .AddPolicy(PoliticasAutorizacao.UsuarioAtivo, politicaUsuarioAtivo)
    .AddPolicy(PoliticasAutorizacao.Administrador, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new UsuarioAtivoRequirement())
        .RequireRole(PerfisUsuario.Administrador));

// ==========================================================================
// Serviços da aplicação
// ==========================================================================
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IRestaurantesErpService, RestaurantesErpService>();
builder.Services.AddScoped<IOutraFonteRestaurantesService, OutraFonteRestaurantesService>();
builder.Services.AddScoped<IRestaurantesService, RestaurantesService>();

builder.Services.Configure<ErpApiOptions>(
    builder.Configuration.GetSection(ErpApiOptions.SectionName));
builder.Services.Configure<OutraFonteApiOptions>(
    builder.Configuration.GetSection(OutraFonteApiOptions.SectionName));

builder.Services.AddHttpClient("ErpApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ErpApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddHttpClient("OutraFonteApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OutraFonteApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// ==========================================================================
// Hospedagem atrás de proxy reverso
//
// Em container o TLS termina no proxy (o nginx do serviço web), e a aplicação
// fala só HTTP. Redirecionar para HTTPS aqui dentro criaria laço de redirect.
// ==========================================================================
var atrasDeProxyReverso = builder.Configuration.GetValue("Hosting:UseForwardedHeaders", false);
var redirecionarParaHttps = builder.Configuration.GetValue("Hosting:UseHttpsRedirection", true);

if (atrasDeProxyReverso)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // O IP do proxy muda a cada recriação do container; o limite de confiança
        // é a rede interna, que não é alcançável de fora.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Só faz falta quando o front é servido de outra origem. Atrás do nginx, front e
// API dividem o mesmo domínio e o navegador nem chega a checar CORS.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Antes de qualquer coisa que leia esquema ou IP de origem.
if (atrasDeProxyReverso)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (redirecionarParaHttps)
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
