using System.ComponentModel.DataAnnotations;
using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Dtos;

public record ItemRequest(
    [property: Required, MaxLength(100)] string Nome,
    CategoriaItem Categoria,
    UnidadeMedida Unidade);

public record ItemFiltro(
    string? Nome,
    CategoriaItem? Categoria,
    string? OrdenarPor,
    string? Direcao,
    int Pagina = 1,
    int TamanhoPagina = 10);

public record ItemResponse(int Id, string Nome, CategoriaItem Categoria, UnidadeMedida Unidade)
{
    public static ItemResponse De(Item i) => new(i.Id, i.Nome, i.Categoria, i.Unidade);
}
