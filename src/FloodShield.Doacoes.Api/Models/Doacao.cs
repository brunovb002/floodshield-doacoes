using System.ComponentModel.DataAnnotations;

namespace FloodShield.Doacoes.Api.Models;

public class Doacao
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int NecessidadeId { get; set; }
    public Necessidade? Necessidade { get; set; }

    [Range(1, 100000)]
    public int Quantidade { get; set; }

    public StatusDoacao Status { get; set; } = StatusDoacao.Prometida;

    [Required, StringLength(6)]
    public string CodigoEntrega { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? EntregueEm { get; set; }
}
