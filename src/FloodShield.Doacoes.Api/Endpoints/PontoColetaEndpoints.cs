using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Dtos;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class PontoColetaEndpoints
{
    public static void MapPontoColetaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pontos-coleta").WithTags("Pontos de coleta").RequireAuthorization();

        group.MapGet("/", Listar)
            .Produces<PaginaResponse<PontoColetaResponse>>();

        group.MapGet("/{id:int}", ObterPorId)
            .Produces<PontoColetaResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Criar)
            .RequireAuthorization("Gestor")
            .Produces<PontoColetaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        group.MapPut("/{id:int}", Atualizar)
            .RequireAuthorization("Gestor")
            .Produces<PontoColetaResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        group.MapDelete("/{id:int}", Excluir)
            .RequireAuthorization("Gestor")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Listar([AsParameters] PontoColetaFiltro filtro, AppDbContext db)
    {
        var query = db.PontosColeta.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Bairro))
            query = query.Where(p => p.Bairro.Contains(filtro.Bairro));

        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
            query = query.Where(p => p.Cidade.Contains(filtro.Cidade));

        if (filtro.Ativo.HasValue)
            query = query.Where(p => p.Ativo == filtro.Ativo.Value);

        var desc = PaginacaoExtensions.Descendente(filtro.Direcao);
        query = filtro.OrdenarPor?.ToLower() switch
        {
            "bairro" => desc ? query.OrderByDescending(p => p.Bairro) : query.OrderBy(p => p.Bairro),
            "criadoem" => desc ? query.OrderByDescending(p => p.CriadoEm) : query.OrderBy(p => p.CriadoEm),
            _ => desc ? query.OrderByDescending(p => p.Nome) : query.OrderBy(p => p.Nome)
        };

        var pagina = await query.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, PontoColetaResponse.De);

        return Results.Ok(pagina);
    }

    private static async Task<IResult> ObterPorId(int id, AppDbContext db)
    {
        var ponto = await db.PontosColeta.FindAsync(id);
        return ponto is null ? Results.NotFound() : Results.Ok(PontoColetaResponse.De(ponto));
    }

    private static async Task<IResult> Criar(PontoColetaRequest request, AppDbContext db, IViaCepService viaCep)
    {
        var ponto = new PontoColeta();
        var erro = await PreencherAsync(ponto, request, viaCep);
        if (erro is not null)
            return erro;

        db.PontosColeta.Add(ponto);
        await db.SaveChangesAsync();

        return Results.Created($"/api/pontos-coleta/{ponto.Id}", PontoColetaResponse.De(ponto));
    }

    private static async Task<IResult> Atualizar(int id, PontoColetaRequest request, AppDbContext db, IViaCepService viaCep)
    {
        var ponto = await db.PontosColeta.FindAsync(id);
        if (ponto is null)
            return Results.NotFound();

        var erro = await PreencherAsync(ponto, request, viaCep);
        if (erro is not null)
            return erro;

        await db.SaveChangesAsync();
        return Results.Ok(PontoColetaResponse.De(ponto));
    }

    private static async Task<IResult> Excluir(int id, AppDbContext db)
    {
        var ponto = await db.PontosColeta.FindAsync(id);
        if (ponto is null)
            return Results.NotFound();

        if (await db.Necessidades.AnyAsync(n => n.PontoColetaId == id))
            return Results.Conflict(new { mensagem = "O ponto tem necessidades cadastradas. Desative o ponto em vez de excluir." });

        db.PontosColeta.Remove(ponto);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult?> PreencherAsync(PontoColeta ponto, PontoColetaRequest request, IViaCepService viaCep)
    {
        var cep = request.Cep.Replace("-", "");

        if (ponto.Cep != cep || string.IsNullOrEmpty(ponto.Logradouro))
        {
            try
            {
                var endereco = await viaCep.BuscarAsync(cep);
                if (endereco is null)
                    return Results.UnprocessableEntity(new { mensagem = "CEP não encontrado." });

                ponto.Logradouro = endereco.Logradouro;
                ponto.Bairro = endereco.Bairro;
                ponto.Cidade = endereco.Cidade;
                ponto.Uf = endereco.Uf;
            }
            catch (ViaCepIndisponivelException)
            {
                // sem ViaCEP aceita o endereço digitado, se vier completo
                if (string.IsNullOrWhiteSpace(request.Logradouro) || string.IsNullOrWhiteSpace(request.Bairro)
                    || string.IsNullOrWhiteSpace(request.Cidade) || string.IsNullOrWhiteSpace(request.Uf))
                {
                    return Results.Problem("Não foi possível consultar o CEP agora. Informe o endereço completo ou tente mais tarde.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                ponto.Logradouro = request.Logradouro;
                ponto.Bairro = request.Bairro;
                ponto.Cidade = request.Cidade;
                ponto.Uf = request.Uf.ToUpper();
            }
        }

        ponto.Nome = request.Nome.Trim();
        ponto.Cep = cep;
        ponto.Numero = request.Numero;
        ponto.Horario = request.Horario;
        ponto.Ativo = request.Ativo;
        return null;
    }
}
