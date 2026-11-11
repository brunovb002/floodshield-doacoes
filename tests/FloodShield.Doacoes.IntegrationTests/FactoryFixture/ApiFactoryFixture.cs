using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FloodShield.Doacoes.IntegrationTests.FactoryFixture;

public class ApiFactoryFixture : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected virtual int LimiteAuthPorMinuto => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "chave-de-teste-com-mais-de-32-caracteres-123456");
        builder.UseSetting("RateLimit:GeralPorMinuto", "100000");
        builder.UseSetting("RateLimit:AuthPorMinuto", LimiteAuthPorMinuto.ToString());
        builder.UseSetting("RateLimit:DoacoesPorMinuto", "100000");

        builder.ConfigureServices(services =>
        {
            // troca o SQL Server por um SQLite em memória
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));

            _conexao.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_conexao));

            services.RemoveAll<IViaCepService>();
            services.AddSingleton<IViaCepService, ViaCepFake>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    public async Task<HttpClient> ClienteGestorAsync()
    {
        var email = $"gestor{Guid.NewGuid():N}@teste.com";
        const string senha = "senha-gestor";

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            var gestor = new Usuario { Nome = "Gestor Teste", Email = email, Papel = Papel.Gestor };
            gestor.SenhaHash = hasher.HashPassword(gestor, senha);
            db.Usuarios.Add(gestor);
            await db.SaveChangesAsync();
        }

        return await ClienteLogadoAsync(email, senha);
    }

    public async Task<HttpClient> ClienteCidadaoAsync()
    {
        var email = $"cidadao{Guid.NewGuid():N}@teste.com";
        const string senha = "senha123";

        var anonimo = CreateClient();
        var registro = await anonimo.PostAsJsonAsync("/api/auth/registrar", new { nome = "Cidadão Teste", email, senha });
        registro.EnsureSuccessStatusCode();

        return await ClienteLogadoAsync(email, senha);
    }

    private async Task<HttpClient> ClienteLogadoAsync(string email, string senha)
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        login.EnsureSuccessStatusCode();

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.GetProperty("token").GetString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conexao.Dispose();
    }
}

public class ViaCepFake : IViaCepService
{
    public const string CepInexistente = "99999999";
    public const string CepForaDoAr = "00000000";

    public Task<EnderecoCep?> BuscarAsync(string cep) => cep switch
    {
        CepInexistente => Task.FromResult<EnderecoCep?>(null),
        CepForaDoAr => throw new ViaCepIndisponivelException(new HttpRequestException("fora do ar")),
        _ => Task.FromResult<EnderecoCep?>(new EnderecoCep("Avenida Paulista", "Bela Vista", "São Paulo", "SP"))
    };
}

[CollectionDefinition("ApiCollection")]
public class ApiCollection : ICollectionFixture<ApiFactoryFixture>
{
}
