using System.ComponentModel.DataAnnotations;

namespace FloodShield.Doacoes.Api.Models;

public class IdempotencyKey
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Chave { get; set; } = string.Empty;

    public int UsuarioId { get; set; }

    [Required, MaxLength(100)]
    public string Rota { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string HashRequisicao { get; set; } = string.Empty;

    // 0 enquanto a primeira requisição ainda está sendo processada
    public int StatusCode { get; set; }

    public string? Resposta { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
