using System.ComponentModel.DataAnnotations;

namespace FloodShield.Doacoes.Api.Models;

public class PontoColeta
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(8)]
    public string Cep { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Logradouro { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Numero { get; set; }

    [MaxLength(80)]
    public string Bairro { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Cidade { get; set; } = string.Empty;

    [StringLength(2)]
    public string Uf { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Horario { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public List<Necessidade> Necessidades { get; set; } = new();
}
