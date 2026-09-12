using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using PainelWebSwissPark.Api.Authorization;
using PainelWebSwissPark.Api.Data;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Middleware;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.Auth;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using PainelWebSwissPark.Api.Services.Erp;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================================
// Segredos
// Connection strings ficam no keys.json, que não entra no repositório.
// Aponte PAINEL_KEYS_FILE para o arquivo em outro diretório se preferir.
// Modelo do formato: keys.example.json
// ==========================================================================
var arquivoDeChaves = Environment.GetEnvironmentVariable("PAINEL_KEYS_FILE")
    ?? Path.Combine(builder.Environment.ContentRootPath, "keys.json");

builder.Configuration.AddJsonFile(arquivoDeChaves, optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

// ==========================================================================
// Banco do painel (Postgres) — só as tabelas do próprio painel
// ==========================================================================
builder.Services.AddOptions<PainelDbOptions>()
    .Bind(builder.Configuration.GetSection(PainelDbOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        $"{PainelDbOptions.SectionName}:ConnectionString não configurado. Crie o keys.json a partir do keys.example.json.")
    .ValidateOnStart();

builder.Services.AddDbContext<PainelDbContext>((serviceProvider, options) =>
{
    var painelDb = serviceProvider.GetRequiredService<IOptions<PainelDbOptions>>().Value;
    options.UseNpgsql(painelDb.ConnectionString);
});

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
