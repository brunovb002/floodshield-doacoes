using System.ComponentModel.DataAnnotations;

namespace FloodShield.Doacoes.Api.Models;

public class Necessidade
{
    public int Id { get; set; }

    public int PontoColetaId { get; set; }
    public PontoColeta? PontoColeta { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Range(1, 100000)]
    public int QuantidadeNecessaria { get; set; }

    public Urgencia Urgencia { get; set; } = Urgencia.Media;

    public StatusNecessidade Status { get; set; } = StatusNecessidade.Aberta;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public List<Doacao> Doacoes { get; set; } = new();
}
