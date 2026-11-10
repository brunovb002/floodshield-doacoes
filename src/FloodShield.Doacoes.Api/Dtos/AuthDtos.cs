using System.ComponentModel.DataAnnotations;
using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Dtos;

public record RegistrarRequest(
    [property: Required, MaxLength(100)] string Nome,
    [property: Required, EmailAddress, MaxLength(150)] string Email,
    [property: Required, MinLength(6), MaxLength(50)] string Senha);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Senha);

public record AtualizarPerfilRequest(
    [property: Required, MaxLength(100)] string Nome);

public record LoginResponse(string Token, DateTime ExpiraEm);

public record UsuarioResponse(int Id, string Nome, string Email, Papel Papel)
{
    public static UsuarioResponse De(Usuario u) => new(u.Id, u.Nome, u.Email, u.Papel);
}
