using PainelWebSwissPark.Api.Extensions;
using PainelWebSwissPark.Api.Middleware;
using PainelWebSwissPark.Api.Options;
using PainelWebSwissPark.Api.Services.BusinessLogic;
using PainelWebSwissPark.Api.Services.Erp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSqlServerData(builder.Configuration);

builder.Services.AddScoped<IRestaurantesErpService, RestaurantesErpService>();
builder.Services.AddScoped<IOutraFonteRestaurantesService, OutraFonteRestaurantesService>();
builder.Services.AddScoped<IRestaurantesService, RestaurantesService>();

builder.Services.Configure<ErpApiOptions>(
    builder.Configuration.GetSection(ErpApiOptions.SectionName));
builder.Services.Configure<OutraFonteApiOptions>(
    builder.Configuration.GetSection(OutraFonteApiOptions.SectionName));

builder.Services.AddHttpClient("ErpApi", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddHttpClient("OutraFonteApi", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OutraFonteApiOptions>>().Value;
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
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.MapControllers();

app.Run();
