using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Dtos;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class NecessidadeEndpoints
{
    public static void MapNecessidadeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/necessidades").WithTags("Necessidades").RequireAuthorization();

        group.MapGet("/", Listar)
            .Produces<PaginaResponse<NecessidadeResponse>>();

        group.MapGet("/prioritarias", Prioritarias)
            .WithSummary("Onde minha doação ajuda mais")
            .WithDescription("Necessidades abertas em pontos ativos, ordenadas por urgência e pelo quanto ainda falta.")
            .Produces<PaginaResponse<NecessidadeResponse>>();

        group.MapGet("/{id:int}", ObterPorId)
            .Produces<NecessidadeResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Criar)
            .RequireAuthorization("Gestor")
            .Produces<NecessidadeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:int}", Atualizar)
            .RequireAuthorization("Gestor")
            .Produces<NecessidadeResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:int}/cancelar", Cancelar)
            .RequireAuthorization("Gestor")
            .Produces<NecessidadeResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:int}", Excluir)
            .RequireAuthorization("Gestor")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Listar([AsParameters] NecessidadeFiltro filtro, AppDbContext db)
    {
        var query = db.Necessidades.AsNoTracking().AsQueryable();

        if (filtro.PontoColetaId.HasValue)
            query = query.Where(n => n.PontoColetaId == filtro.PontoColetaId.Value);

        if (filtro.Status.HasValue)
            query = query.Where(n => n.Status == filtro.Status.Value);

        if (filtro.Categoria.HasValue)
            query = query.Where(n => n.Item!.Categoria == filtro.Categoria.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Bairro))
            query = query.Where(n => n.PontoColeta!.Bairro.Contains(filtro.Bairro));

        var desc = PaginacaoExtensions.Descendente(filtro.Direcao);
        query = filtro.OrdenarPor?.ToLower() switch
        {
            "urgencia" => desc
                ? query.OrderByDescending(n => n.Urgencia == Urgencia.Critica ? 4 : n.Urgencia == Urgencia.Alta ? 3 : n.Urgencia == Urgencia.Media ? 2 : 1)
                : query.OrderBy(n => n.Urgencia == Urgencia.Critica ? 4 : n.Urgencia == Urgencia.Alta ? 3 : n.Urgencia == Urgencia.Media ? 2 : 1),
            "quantidade" => desc ? query.OrderByDescending(n => n.QuantidadeNecessaria) : query.OrderBy(n => n.QuantidadeNecessaria),
            _ => desc ? query.OrderBy(n => n.CriadoEm) : query.OrderByDescending(n => n.CriadoEm)
        };

        var pagina = await query.ComSaldo().PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, NecessidadeResponse.De);
        return Results.Ok(pagina);
    }

    private static async Task<IResult> Prioritarias([AsParameters] PrioritariasFiltro filtro, AppDbContext db)
    {
        var query = db.Necessidades.AsNoTracking()
            .Where(n => n.Status == StatusNecessidade.Aberta && n.PontoColeta!.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro.Bairro))
            query = query.Where(n => n.PontoColeta!.Bairro.Contains(filtro.Bairro));

        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
            query = query.Where(n => n.PontoColeta!.Cidade.Contains(filtro.Cidade));

        if (filtro.Categoria.HasValue)
            query = query.Where(n => n.Item!.Categoria == filtro.Categoria.Value);

        var ordenadas = RegrasNecessidade.OrdenarPorPrioridade(await query.ComSaldo().ToListAsync()).ToList();

        var pagina = Math.Max(filtro.Pagina, 1);
        var tamanho = Math.Clamp(filtro.TamanhoPagina, 1, PaginacaoExtensions.TamanhoMaximo);
        var itens = ordenadas.Skip((pagina - 1) * tamanho).Take(tamanho).Select(NecessidadeResponse.De);
        var totalPaginas = (int)Math.Ceiling(ordenadas.Count / (double)tamanho);

        return Results.Ok(new PaginaResponse<NecessidadeResponse>(itens, pagina, tamanho, ordenadas.Count, totalPaginas));
    }

    private static async Task<IResult> ObterPorId(int id, AppDbContext db)
    {
        var resumo = await BuscarResumo(db, id);
        return resumo is null ? Results.NotFound() : Results.Ok(NecessidadeResponse.De(resumo));
    }

    private static async Task<IResult> Criar(CriarNecessidadeRequest request, AppDbContext db)
    {
        var ponto = await db.PontosColeta.FindAsync(request.PontoColetaId);
        if (ponto is null)
            return Results.UnprocessableEntity(new { mensagem = "Ponto de coleta não encontrado." });
        if (!ponto.Ativo)
            return Results.UnprocessableEntity(new { mensagem = "O ponto de coleta está desativado." });

        if (!await db.Itens.AnyAsync(i => i.Id == request.ItemId))
            return Results.UnprocessableEntity(new { mensagem = "Item não encontrado." });

        var jaExiste = await db.Necessidades.AnyAsync(n =>
            n.PontoColetaId == request.PontoColetaId && n.ItemId == request.ItemId && n.Status == StatusNecessidade.Aberta);
        if (jaExiste)
            return Results.Conflict(new { mensagem = "Já existe uma necessidade aberta desse item nesse ponto. Atualize a quantidade dela." });

        var necessidade = new Necessidade
        {
            PontoColetaId = request.PontoColetaId,
            ItemId = request.ItemId,
            QuantidadeNecessaria = request.QuantidadeNecessaria,
            Urgencia = request.Urgencia
        };

        db.Necessidades.Add(necessidade);
        await db.SaveChangesAsync();

        var resumo = await BuscarResumo(db, necessidade.Id);
        return Results.Created($"/api/necessidades/{necessidade.Id}", NecessidadeResponse.De(resumo!));
    }

    private static async Task<IResult> Atualizar(int id, AtualizarNecessidadeRequest request, AppDbContext db)
    {
        var necessidade = await db.Necessidades.FindAsync(id);
        if (necessidade is null)
            return Results.NotFound();

        if (necessidade.Status == StatusNecessidade.Cancelada)
            return Results.UnprocessableEntity(new { mensagem = "Necessidade cancelada não pode ser alterada." });

        var entregue = await db.Doacoes
            .Where(d => d.NecessidadeId == id && d.Status == StatusDoacao.Entregue)
            .SumAsync(d => d.Quantidade);

        var erro = RegrasNecessidade.ValidarNovaQuantidade(request.QuantidadeNecessaria, entregue);
        if (erro is not null)
            return Results.UnprocessableEntity(new { mensagem = erro });

        necessidade.QuantidadeNecessaria = request.QuantidadeNecessaria;
        necessidade.Urgencia = request.Urgencia;
        necessidade.Status = RegrasNecessidade.StatusAtualizado(necessidade.Status, necessidade.QuantidadeNecessaria, entregue);
        await db.SaveChangesAsync();

        return Results.Ok(NecessidadeResponse.De((await BuscarResumo(db, id))!));
    }

    private static async Task<IResult> Cancelar(int id, AppDbContext db)
    {
        var necessidade = await db.Necessidades.Include(n => n.Doacoes).FirstOrDefaultAsync(n => n.Id == id);
        if (necessidade is null)
            return Results.NotFound();

        if (necessidade.Status != StatusNecessidade.Aberta)
            return Results.UnprocessableEntity(new { mensagem = "Só é possível cancelar uma necessidade aberta." });

        necessidade.Status = StatusNecessidade.Cancelada;
        foreach (var doacao in necessidade.Doacoes.Where(d => d.Status == StatusDoacao.Prometida))
            doacao.Status = StatusDoacao.Cancelada;

        await db.SaveChangesAsync();
        return Results.Ok(NecessidadeResponse.De((await BuscarResumo(db, id))!));
    }

    private static async Task<IResult> Excluir(int id, AppDbContext db)
    {
        var necessidade = await db.Necessidades.FindAsync(id);
        if (necessidade is null)
            return Results.NotFound();

        if (await db.Doacoes.AnyAsync(d => d.NecessidadeId == id))
            return Results.Conflict(new { mensagem = "A necessidade já recebeu doações. Use o cancelamento em vez de excluir." });

        db.Necessidades.Remove(necessidade);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static Task<NecessidadeResumo?> BuscarResumo(AppDbContext db, int id) =>
        db.Necessidades.AsNoTracking().Where(n => n.Id == id).ComSaldo().FirstOrDefaultAsync();
}
