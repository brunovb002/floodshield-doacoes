using System.Threading.RateLimiting;
using FloodShield.Doacoes.Api.Endpoints;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodShield.Doacoes.Api.Configuracao;

public static class RateLimitConfig
{
    public const string Auth = "auth";
    public const string Doacoes = "doacoes";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var limiteGeral = configuration.GetValue("RateLimit:GeralPorMinuto", 100);
        var limiteAuth = configuration.GetValue("RateLimit:AuthPorMinuto", 5);
        var limiteDoacoes = configuration.GetValue("RateLimit:DoacoesPorMinuto", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejected;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/health")
                    ? RateLimitPartition.GetNoLimiter("health")
                    : PorMinuto(Ip(context), limiteGeral));

            options.AddPolicy(Auth, context => PorMinuto(Ip(context), limiteAuth));

            options.AddPolicy(Doacoes, context =>
            {
                var usuarioId = context.User.GetUsuarioId();
                return PorMinuto(usuarioId > 0 ? $"usuario-{usuarioId}" : Ip(context), limiteDoacoes);
            });
        });

        return services;
    }

    private static RateLimitPartition<string> PorMinuto(string chave, int limite) =>
        RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });

    private static string Ip(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static async ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var segundos = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            segundos = (int)Math.Ceiling(retryAfter.TotalSeconds);

        context.HttpContext.Response.Headers.RetryAfter = segundos.ToString();

        await Results.Problem(
                $"Muitas requisições. Tente de novo em {segundos} segundos.",
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests")
            .ExecuteAsync(context.HttpContext);
    }
}
