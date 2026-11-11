using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FloodShield.Doacoes.Api.Configuracao;

public static class HealthCheckConfig
{
    public static IServiceCollection AddHealthCheckConfig(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<AppDbContext>("banco", tags: ["ready"])
            .AddCheck<ViaCepHealthCheck>("viacep", HealthStatus.Degraded, tags: ["ready"]);

        return services;
    }

    public static void MapHealthCheckConfig(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}

public class ViaCepHealthCheck : IHealthCheck
{
    private readonly IViaCepService _viaCep;

    public ViaCepHealthCheck(IViaCepService viaCep)
    {
        _viaCep = viaCep;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var endereco = await _viaCep.BuscarAsync("01001000");
            return endereco is null
                ? HealthCheckResult.Degraded("ViaCEP respondeu, mas não encontrou o CEP de teste.")
                : HealthCheckResult.Healthy("ViaCEP respondendo.");
        }
        catch (ViaCepIndisponivelException ex)
        {
            // sem ViaCEP a API continua funcionando, só pede o endereço completo
            return HealthCheckResult.Degraded("ViaCEP fora do ar.", ex);
        }
    }
}
