using System.Net;
using System.Net.Http.Json;
using FloodShield.Doacoes.IntegrationTests.FactoryFixture;

namespace FloodShield.Doacoes.IntegrationTests.Integration;

[Collection("ApiCollection")]
public class AuthIntegrationTests
{
    private readonly ApiFactoryFixture _factory;

    public AuthIntegrationTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Registrar_DadosValidos_RetornaCreatedComoCidadao()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"maria{Guid.NewGuid():N}@teste.com";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/registrar", new { nome = "Maria", email, senha = "senha123" });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var corpo = await Cenario.CorpoAsync(response);
        Assert.Equal("Cidadao", corpo.GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Registrar_EmailRepetido_RetornaConflict()
    {
        var client = _factory.CreateClient();
        var email = $"repetido{Guid.NewGuid():N}@teste.com";
        await client.PostAsJsonAsync("/api/auth/registrar", new { nome = "Ana", email, senha = "senha123" });

        var response = await client.PostAsJsonAsync("/api/auth/registrar", new { nome = "Ana", email, senha = "senha123" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_DadosInvalidos_RetornaBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/registrar", new { nome = "", email = "nao-e-email", senha = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_SenhaErrada_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"jose{Guid.NewGuid():N}@teste.com";
        await client.PostAsJsonAsync("/api/auth/registrar", new { nome = "José", email, senha = "senha123" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_SemToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/usuarios/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RotaDeGestor_ComCidadao_RetornaForbidden()
    {
        var cidadao = await _factory.ClienteCidadaoAsync();

        var response = await cidadao.PostAsJsonAsync("/api/itens", new { nome = "Arroz", categoria = "Alimento", unidade = "Quilo" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExcluirConta_SemDoacoes_RetornaNoContent()
    {
        var cidadao = await _factory.ClienteCidadaoAsync();

        var response = await cidadao.DeleteAsync("/api/usuarios/me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cidadao.GetAsync("/api/usuarios/me")).StatusCode);
    }
}
