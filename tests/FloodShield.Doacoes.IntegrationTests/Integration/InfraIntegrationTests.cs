using System.Net;
using System.Net.Http.Json;
using FloodShield.Doacoes.IntegrationTests.FactoryFixture;

namespace FloodShield.Doacoes.IntegrationTests.Integration;

[Collection("ApiCollection")]
public class HealthCheckIntegrationTests
{
    private readonly ApiFactoryFixture _factory;

    public HealthCheckIntegrationTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthCheck_TudoFuncionando_RetornaOk(string rota)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(rota);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }
}

public class RateLimitFactory : ApiFactoryFixture
{
    protected override int LimiteAuthPorMinuto => 2;
}

public class RateLimitIntegrationTests : IClassFixture<RateLimitFactory>
{
    private readonly RateLimitFactory _factory;

    public RateLimitIntegrationTests(RateLimitFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_AcimaDoLimite_RetornaTooManyRequestsComRetryAfter()
    {
        var client = _factory.CreateClient();
        var login = new { email = "ninguem@teste.com", senha = "x" };

        await client.PostAsJsonAsync("/api/auth/login", login);
        await client.PostAsJsonAsync("/api/auth/login", login);
        var terceira = await client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        Assert.NotNull(terceira.Headers.RetryAfter);
    }
}
