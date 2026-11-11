using FloodShield.Doacoes.Api.Configuracao;
using FloodShield.Doacoes.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace FloodShield.Doacoes.UnitTests.HealthChecks;

public class ViaCepHealthCheckTests
{
    private readonly Mock<IViaCepService> _viaCep = new();

    [Fact]
    public async Task CheckHealth_ViaCepRespondendo_RetornaHealthy()
    {
        // Arrange
        _viaCep.Setup(v => v.BuscarAsync(It.IsAny<string>()))
            .ReturnsAsync(new EnderecoCep("Praça da Sé", "Sé", "São Paulo", "SP"));
        var check = new ViaCepHealthCheck(_viaCep.Object);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
    }

    [Fact]
    public async Task CheckHealth_ViaCepForaDoAr_RetornaDegraded()
    {
        _viaCep.Setup(v => v.BuscarAsync(It.IsAny<string>()))
            .ThrowsAsync(new ViaCepIndisponivelException(new HttpRequestException()));
        var check = new ViaCepHealthCheck(_viaCep.Object);

        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }

    [Fact]
    public async Task CheckHealth_CepDeTesteNaoEncontrado_RetornaDegraded()
    {
        _viaCep.Setup(v => v.BuscarAsync(It.IsAny<string>())).ReturnsAsync((EnderecoCep?)null);
        var check = new ViaCepHealthCheck(_viaCep.Object);

        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }
}
