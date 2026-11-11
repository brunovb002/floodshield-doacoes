using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FloodShield.Doacoes.Api.Endpoints;

public class IdempotenciaFilter<TRequest> : IEndpointFilter
{
    public const string Header = "Idempotency-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var chave = http.Request.Headers[Header].ToString().Trim();

        if (string.IsNullOrEmpty(chave) || chave.Length > 100)
            return Results.Problem($"Envie o header {Header} com até 100 caracteres.", statusCode: StatusCodes.Status400BadRequest);

        var db = http.RequestServices.GetRequiredService<AppDbContext>();
        var jsonOptions = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

        var usuarioId = http.User.GetUsuarioId();
        var rota = $"{http.Request.Method} {http.Request.Path}";
        var corpo = context.Arguments.OfType<TRequest>().FirstOrDefault();
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(corpo, jsonOptions)));

        var existente = await db.IdempotencyKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.UsuarioId == usuarioId && k.Chave == chave);

        if (existente is not null)
            return Repetir(existente, rota, hash, http);

        var registro = new IdempotencyKey { Chave = chave, UsuarioId = usuarioId, Rota = rota, HashRequisicao = hash };
        db.IdempotencyKeys.Add(registro);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // outra requisição com a mesma chave chegou junto e registrou antes
            db.Entry(registro).State = EntityState.Detached;
            return Results.Conflict(new { mensagem = "Uma requisição com essa chave ainda está sendo processada." });
        }

        object? resultado;
        try
        {
            resultado = await next(context);
        }
        catch
        {
            await db.IdempotencyKeys.Where(k => k.Id == registro.Id).ExecuteDeleteAsync();
            throw;
        }

        if (resultado is IStatusCodeHttpResult { StatusCode: < 500 } comStatus)
        {
            var status = comStatus.StatusCode ?? StatusCodes.Status200OK;
            var resposta = resultado is IValueHttpResult { Value: not null } comValor
                ? JsonSerializer.Serialize(comValor.Value, jsonOptions)
                : null;

            await db.IdempotencyKeys.Where(k => k.Id == registro.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(k => k.StatusCode, status)
                    .SetProperty(k => k.Resposta, resposta));
        }
        else
        {
            // erro do servidor: libera a chave para o cliente tentar de novo
            await db.IdempotencyKeys.Where(k => k.Id == registro.Id).ExecuteDeleteAsync();
        }

        return resultado;
    }

    private static IResult Repetir(IdempotencyKey existente, string rota, string hash, HttpContext http)
    {
        if (existente.Rota != rota || existente.HashRequisicao != hash)
            return Results.Conflict(new { mensagem = "Essa chave já foi usada em uma requisição diferente." });

        if (existente.StatusCode == 0)
            return Results.Conflict(new { mensagem = "Uma requisição com essa chave ainda está sendo processada." });

        http.Response.Headers["Idempotent-Replayed"] = "true";

        return existente.Resposta is null
            ? Results.StatusCode(existente.StatusCode)
            : Results.Content(existente.Resposta, "application/json", Encoding.UTF8, existente.StatusCode);
    }
}
