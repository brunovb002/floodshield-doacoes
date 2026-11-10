using System.ComponentModel.DataAnnotations;

namespace FloodShield.Doacoes.Api.Models;

public class Item
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public CategoriaItem Categoria { get; set; }

    public UnidadeMedida Unidade { get; set; }
}
