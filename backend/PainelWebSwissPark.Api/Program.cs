using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
// Autenticação e autorização
//
// A bi_users guarda o hash PBKDF2 da senha e é quem prova a identidade. O login
// devolve um token assinado que carrega apenas quem a pessoa é.
//
// Perfil e situação NÃO viajam no token: são lidos do banco a cada requisição.
// É isso que faz desativar ou rebaixar alguém valer no request seguinte, em vez
// de esperar o token expirar.
// ==========================================================================
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

if (Encoding.UTF8.GetByteCount(auth.JwtSigningKey) < 32)
{
    var ondeProcurou = arquivoDeChaves is null
        ? $"Nenhum keys.json encontrado a partir de {builder.Environment.ContentRootPath}."
        : $"keys.json lido: {arquivoDeChaves}";

    throw new InvalidOperationException(
        "Auth:JwtSigningKey ausente ou curta demais — HMAC-SHA256 exige no mínimo 32 bytes. "
        + "Gere uma com: openssl rand -base64 48, e guarde no keys.json "
        + "(ou na variável de ambiente Auth__JwtSigningKey). " + ondeProcurou);
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem o mapeamento legado, "sub" continua sendo "sub" em vez de virar
        // um claim de nome longo do WS-Federation.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimsPainel.Nome,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddSingleton<IPasswordHasher<BiUser>, PasswordHasher<BiUser>>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();
builder.Services.AddScoped<IAuthorizationHandler, UsuarioAtivoHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdministradorDeModuloHandler>();

var politicaUsuarioAtivo = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .AddRequirements(new UsuarioAtivoRequirement())
    .Build();

builder.Services.AddAuthorizationBuilder()
    // Política padrão: endpoint sem atributo de autorização já nasce protegido.
    // Para abrir um endpoint é preciso dizer [AllowAnonymous] explicitamente.
    .SetFallbackPolicy(politicaUsuarioAtivo)
    .AddPolicy(PoliticasAutorizacao.UsuarioAtivo, politicaUsuarioAtivo)
    // Abre a área administrativa para quem administra algum módulo. Quais
    // usuários ela mostra é decidido no serviço, módulo a módulo.
    .AddPolicy(PoliticasAutorizacao.AdministradorDeModulo, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new AdministradorDeModuloRequirement()));

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

// Primeiro administrador, quando PAINEL_ADMIN_EMAIL/PASSWORD estiverem definidos
// e a bi_users ainda não tiver nenhum administrador ativo.
await AdministradorInicial.GarantirAsync(app.Services, app.Logger);

app.Run();
