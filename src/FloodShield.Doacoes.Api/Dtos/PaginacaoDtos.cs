using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Dtos;

public record PaginaResponse<T>(IEnumerable<T> Itens, int Pagina, int TamanhoPagina, int Total, int TotalPaginas);

public static class PaginacaoExtensions
{
    public const int TamanhoMaximo = 50;

    public static async Task<PaginaResponse<TResponse>> PaginarAsync<T, TResponse>(
        this IQueryable<T> query, int pagina, int tamanhoPagina, Func<T, TResponse> converter)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, TamanhoMaximo);

        var total = await query.CountAsync();
        var itens = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);
        return new PaginaResponse<TResponse>(itens.Select(converter), pagina, tamanhoPagina, total, totalPaginas);
    }

    public static bool Descendente(string? direcao) =>
        string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase);
}
