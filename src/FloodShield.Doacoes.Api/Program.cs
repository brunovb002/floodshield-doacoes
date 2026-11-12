using System.Text;
using System.Text.Json.Serialization;
using FloodShield.Doacoes.Api.Configuracao;
using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Endpoints;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Configure a chave Jwt:Key (user-secrets ou variável de ambiente).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddHttpClient<IViaCepService, ViaCepService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ViaCep:BaseUrl"] ?? "https://viacep.com.br/ws/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Gestor", policy => policy.RequireRole(nameof(Papel.Gestor)))
    .AddPolicy("Cidadao", policy => policy.RequireRole(nameof(Papel.Cidadao)));

builder.Services.AddValidation();
builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token gerado em /api/auth/login"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddHealthCheckConfig();
builder.Services.AddRateLimitPolicies(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("FloodShield Doações API")
        .AddPreferredSecuritySchemes("Bearer");
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthCheckConfig();
app.MapAuthEndpoints();
app.MapPontoColetaEndpoints();
app.MapItemEndpoints();
app.MapNecessidadeEndpoints();
app.MapDoacaoEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

await DbSeeder.CriarGestorAsync(app.Services, app.Configuration);

app.Run();

public partial class Program { }
