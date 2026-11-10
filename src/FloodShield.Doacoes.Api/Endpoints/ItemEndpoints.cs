using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Dtos;
using FloodShield.Doacoes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/itens").WithTags("Itens").RequireAuthorization();

        group.MapGet("/", Listar)
            .Produces<PaginaResponse<ItemResponse>>();

        group.MapGet("/{id:int}", ObterPorId)
            .Produces<ItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Criar)
            .RequireAuthorization("Gestor")
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Atualizar)
            .RequireAuthorization("Gestor")
            .Produces<ItemResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", Excluir)
            .RequireAuthorization("Gestor")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Listar([AsParameters] ItemFiltro filtro, AppDbContext db)
    {
        var query = db.Itens.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Nome))
            query = query.Where(i => i.Nome.Contains(filtro.Nome));

        if (filtro.Categoria.HasValue)
            query = query.Where(i => i.Categoria == filtro.Categoria.Value);

        var desc = PaginacaoExtensions.Descendente(filtro.Direcao);
        query = filtro.OrdenarPor?.ToLower() == "categoria"
            ? (desc ? query.OrderByDescending(i => i.Categoria) : query.OrderBy(i => i.Categoria)).ThenBy(i => i.Nome)
            : desc ? query.OrderByDescending(i => i.Nome) : query.OrderBy(i => i.Nome);

        return Results.Ok(await query.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, ItemResponse.De));
    }

    private static async Task<IResult> ObterPorId(int id, AppDbContext db)
    {
        var item = await db.Itens.FindAsync(id);
        return item is null ? Results.NotFound() : Results.Ok(ItemResponse.De(item));
    }

    private static async Task<IResult> Criar(ItemRequest request, AppDbContext db)
    {
        var nome = request.Nome.Trim();
        if (await db.Itens.AnyAsync(i => i.Nome == nome))
            return Results.Conflict(new { mensagem = "Já existe um item com esse nome." });

        var item = new Item { Nome = nome, Categoria = request.Categoria, Unidade = request.Unidade };
        db.Itens.Add(item);
        await db.SaveChangesAsync();

        return Results.Created($"/api/itens/{item.Id}", ItemResponse.De(item));
    }

    private static async Task<IResult> Atualizar(int id, ItemRequest request, AppDbContext db)
    {
        var item = await db.Itens.FindAsync(id);
        if (item is null)
            return Results.NotFound();

        var nome = request.Nome.Trim();
        if (await db.Itens.AnyAsync(i => i.Nome == nome && i.Id != id))
            return Results.Conflict(new { mensagem = "Já existe um item com esse nome." });

        item.Nome = nome;
        item.Categoria = request.Categoria;
        item.Unidade = request.Unidade;
        await db.SaveChangesAsync();

        return Results.Ok(ItemResponse.De(item));
    }

    private static async Task<IResult> Excluir(int id, AppDbContext db)
    {
        var item = await db.Itens.FindAsync(id);
        if (item is null)
            return Results.NotFound();

        if (await db.Necessidades.AnyAsync(n => n.ItemId == id))
            return Results.Conflict(new { mensagem = "O item está sendo usado em necessidades e não pode ser excluído." });

        db.Itens.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
