using System.Security.Claims;
using FloodShield.Doacoes.Api.Configuracao;
using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Dtos;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class DoacaoEndpoints
{
    public static void MapDoacaoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/doacoes").WithTags("Doações").RequireAuthorization();

        group.MapPost("/", Criar)
            .RequireAuthorization("Cidadao")
            .RequireRateLimiting(RateLimitConfig.Doacoes)
            .AddEndpointFilter<IdempotenciaFilter<CriarDoacaoRequest>>()
            .WithDescription("Exige o header Idempotency-Key. Repetir a mesma chave com o mesmo corpo devolve a resposta original, sem criar outra doação.")
            .Produces<DoacaoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", Listar)
            .WithDescription("O cidadão vê só as próprias doações. O gestor vê todas.")
            .Produces<PaginaResponse<DoacaoResponse>>();

        group.MapGet("/{id:int}", ObterPorId)
            .Produces<DoacaoResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:int}", Atualizar)
            .RequireAuthorization("Cidadao")
            .Produces<DoacaoResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:int}", Excluir)
            .RequireAuthorization("Cidadao")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:int}/confirmar-entrega", ConfirmarEntrega)
            .RequireAuthorization("Gestor")
            .Produces<DoacaoResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:int}/cancelar", Cancelar)
            .RequireAuthorization("Gestor")
            .Produces<DoacaoResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> Criar(
        CriarDoacaoRequest request,
        [FromHeader(Name = IdempotenciaFilter<CriarDoacaoRequest>.Header)] string idempotencyKey,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        await TravarNecessidade(db, request.NecessidadeId);

        var necessidade = await db.Necessidades
            .Where(n => n.Id == request.NecessidadeId)
            .Select(n => new { n.Status, PontoAtivo = n.PontoColeta!.Ativo })
            .FirstOrDefaultAsync();

        if (necessidade is null)
            return Results.UnprocessableEntity(new { mensagem = "Necessidade não encontrada." });

        var erro = RegrasDoacao.ValidarNecessidade(necessidade.Status, necessidade.PontoAtivo)
            ?? RegrasDoacao.ValidarQuantidade(request.Quantidade, await CalcularFalta(db, request.NecessidadeId));
        if (erro is not null)
            return Results.UnprocessableEntity(new { mensagem = erro });

        var doacao = new Doacao
        {
            UsuarioId = user.GetUsuarioId(),
            NecessidadeId = request.NecessidadeId,
            Quantidade = request.Quantidade,
            CodigoEntrega = await GerarCodigoUnico(db)
        };

        db.Doacoes.Add(doacao);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        return Results.Created($"/api/doacoes/{doacao.Id}", DoacaoResponse.De((await Buscar(db, doacao.Id))!));
    }

    private static async Task<IResult> Listar([AsParameters] DoacaoFiltro filtro, ClaimsPrincipal user, AppDbContext db)
    {
        var query = ComDetalhes(db.Doacoes.AsNoTracking());

        if (!user.IsGestor())
        {
            var usuarioId = user.GetUsuarioId();
            query = query.Where(d => d.UsuarioId == usuarioId);
        }

        if (filtro.Status.HasValue)
            query = query.Where(d => d.Status == filtro.Status.Value);

        if (filtro.NecessidadeId.HasValue)
            query = query.Where(d => d.NecessidadeId == filtro.NecessidadeId.Value);

        if (filtro.PontoColetaId.HasValue)
            query = query.Where(d => d.Necessidade!.PontoColetaId == filtro.PontoColetaId.Value);

        var pagina = await query
            .OrderByDescending(d => d.CriadoEm)
            .PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, DoacaoResponse.De);

        return Results.Ok(pagina);
    }

    private static async Task<IResult> ObterPorId(int id, ClaimsPrincipal user, AppDbContext db)
    {
        var doacao = await Buscar(db, id);
        if (doacao is null || !PodeVer(user, doacao))
            return Results.NotFound();

        return Results.Ok(DoacaoResponse.De(doacao));
    }

    private static async Task<IResult> Atualizar(int id, AtualizarDoacaoRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        var doacao = await db.Doacoes.FindAsync(id);
        if (doacao is null || doacao.UsuarioId != user.GetUsuarioId())
            return Results.NotFound();

        if (!RegrasDoacao.PodeAlterar(doacao.Status))
            return Results.UnprocessableEntity(new { mensagem = "Só é possível alterar uma doação que ainda não foi entregue." });

        await using var transacao = await db.Database.BeginTransactionAsync();
        await TravarNecessidade(db, doacao.NecessidadeId);

        // a quantidade que a própria doação já reservou volta para o saldo antes de validar
        var falta = await CalcularFalta(db, doacao.NecessidadeId) + doacao.Quantidade;
        var erro = RegrasDoacao.ValidarQuantidade(request.Quantidade, falta);
        if (erro is not null)
            return Results.UnprocessableEntity(new { mensagem = erro });

        doacao.Quantidade = request.Quantidade;
        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        return Results.Ok(DoacaoResponse.De((await Buscar(db, id))!));
    }

    private static async Task<IResult> Excluir(int id, ClaimsPrincipal user, AppDbContext db)
    {
        var doacao = await db.Doacoes.FindAsync(id);
        if (doacao is null || doacao.UsuarioId != user.GetUsuarioId())
            return Results.NotFound();

        if (!RegrasDoacao.PodeAlterar(doacao.Status))
            return Results.Conflict(new { mensagem = "Doação entregue ou cancelada não pode ser excluída." });

        db.Doacoes.Remove(doacao);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmarEntrega(int id, ConfirmarEntregaRequest request, AppDbContext db)
    {
        var doacao = await db.Doacoes.FindAsync(id);
        if (doacao is null)
            return Results.NotFound();

        if (!RegrasDoacao.CodigoConfere(doacao.CodigoEntrega, request.Codigo))
            return Results.UnprocessableEntity(new { mensagem = "Código de entrega não confere." });

        // confirmar de novo uma entrega já confirmada só devolve o mesmo resultado
        if (doacao.Status == StatusDoacao.Entregue)
            return Results.Ok(DoacaoResponse.De((await Buscar(db, id))!));

        if (doacao.Status == StatusDoacao.Cancelada)
            return Results.UnprocessableEntity(new { mensagem = "Essa doação foi cancelada." });

        await using var transacao = await db.Database.BeginTransactionAsync();
        await TravarNecessidade(db, doacao.NecessidadeId);

        doacao.Status = StatusDoacao.Entregue;
        doacao.EntregueEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var necessidade = await db.Necessidades.FindAsync(doacao.NecessidadeId);
        var entregue = await db.Doacoes
            .Where(d => d.NecessidadeId == doacao.NecessidadeId && d.Status == StatusDoacao.Entregue)
            .SumAsync(d => d.Quantidade);
        necessidade!.Status = RegrasNecessidade.StatusAtualizado(necessidade.Status, necessidade.QuantidadeNecessaria, entregue);

        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        return Results.Ok(DoacaoResponse.De((await Buscar(db, id))!));
    }

    private static async Task<IResult> Cancelar(int id, AppDbContext db)
    {
        var doacao = await db.Doacoes.FindAsync(id);
        if (doacao is null)
            return Results.NotFound();

        if (!RegrasDoacao.PodeAlterar(doacao.Status))
            return Results.UnprocessableEntity(new { mensagem = "Só é possível cancelar uma doação que ainda não foi entregue." });

        doacao.Status = StatusDoacao.Cancelada;
        await db.SaveChangesAsync();

        return Results.Ok(DoacaoResponse.De((await Buscar(db, id))!));
    }

    // trava a linha da necessidade até o fim da transação, para duas doações
    // simultâneas não passarem juntas do que falta
    private static Task TravarNecessidade(AppDbContext db, int necessidadeId) =>
        db.Necessidades
            .Where(n => n.Id == necessidadeId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, n => n.Status));

    private static async Task<int> CalcularFalta(AppDbContext db, int necessidadeId)
    {
        var saldo = await db.Necessidades
            .Where(n => n.Id == necessidadeId)
            .ComSaldo()
            .FirstAsync();

        return saldo.Falta;
    }

    private static async Task<string> GerarCodigoUnico(AppDbContext db)
    {
        string codigo;
        do
        {
            codigo = RegrasDoacao.GerarCodigo();
        } while (await db.Doacoes.AnyAsync(d => d.CodigoEntrega == codigo));

        return codigo;
    }

    private static bool PodeVer(ClaimsPrincipal user, Doacao doacao) =>
        user.IsGestor() || doacao.UsuarioId == user.GetUsuarioId();

    private static IQueryable<Doacao> ComDetalhes(IQueryable<Doacao> query) =>
        query
            .Include(d => d.Usuario)
            .Include(d => d.Necessidade!).ThenInclude(n => n.Item)
            .Include(d => d.Necessidade!).ThenInclude(n => n.PontoColeta);

    private static Task<Doacao?> Buscar(AppDbContext db, int id) =>
        ComDetalhes(db.Doacoes.AsNoTracking()).FirstOrDefaultAsync(d => d.Id == id);
}
