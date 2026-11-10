using System.ComponentModel.DataAnnotations;
using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Dtos;

public record PontoColetaRequest(
    [property: Required, MaxLength(120)] string Nome,
    [property: Required, RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP deve ter 8 dígitos.")] string Cep,
    [property: MaxLength(10)] string? Numero,
    [property: MaxLength(100)] string? Horario,
    bool Ativo = true,
    [property: MaxLength(150)] string? Logradouro = null,
    [property: MaxLength(80)] string? Bairro = null,
    [property: MaxLength(80)] string? Cidade = null,
    [property: StringLength(2)] string? Uf = null);

public record PontoColetaFiltro(
    string? Bairro,
    string? Cidade,
    bool? Ativo,
    string? OrdenarPor,
    string? Direcao,
    int Pagina = 1,
    int TamanhoPagina = 10);

public record PontoColetaResponse(
    int Id,
    string Nome,
    string Cep,
    string Logradouro,
    string? Numero,
    string Bairro,
    string Cidade,
    string Uf,
    string? Horario,
    bool Ativo)
{
    public static PontoColetaResponse De(PontoColeta p) =>
        new(p.Id, p.Nome, p.Cep, p.Logradouro, p.Numero, p.Bairro, p.Cidade, p.Uf, p.Horario, p.Ativo);
}
