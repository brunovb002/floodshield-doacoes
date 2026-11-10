using System.ComponentModel.DataAnnotations;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;

namespace FloodShield.Doacoes.Api.Dtos;

public record CriarNecessidadeRequest(
    [property: Range(1, int.MaxValue)] int PontoColetaId,
    [property: Range(1, int.MaxValue)] int ItemId,
    [property: Range(1, 100000)] int QuantidadeNecessaria,
    Urgencia Urgencia = Urgencia.Media);

public record AtualizarNecessidadeRequest(
    [property: Range(1, 100000)] int QuantidadeNecessaria,
    Urgencia Urgencia);

public record NecessidadeFiltro(
    int? PontoColetaId,
    StatusNecessidade? Status,
    CategoriaItem? Categoria,
    string? Bairro,
    string? OrdenarPor,
    string? Direcao,
    int Pagina = 1,
    int TamanhoPagina = 10);

public record PrioritariasFiltro(
    string? Bairro,
    string? Cidade,
    CategoriaItem? Categoria,
    int Pagina = 1,
    int TamanhoPagina = 10);

public record NecessidadeResponse(
    int Id,
    int PontoColetaId,
    string PontoColeta,
    string Bairro,
    string Cidade,
    int ItemId,
    string Item,
    CategoriaItem Categoria,
    UnidadeMedida Unidade,
    int QuantidadeNecessaria,
    int Entregue,
    int Prometido,
    int Falta,
    double PercentualAtendido,
    Urgencia Urgencia,
    StatusNecessidade Status,
    DateTime CriadoEm)
{
    public static NecessidadeResponse De(NecessidadeResumo n) => new(
        n.Id, n.PontoColetaId, n.PontoColeta, n.Bairro, n.Cidade, n.ItemId, n.Item, n.Categoria, n.Unidade,
        n.QuantidadeNecessaria, n.Entregue, n.Prometido, n.Falta,
        RegrasNecessidade.PercentualAtendido(n.QuantidadeNecessaria, n.Entregue),
        n.Urgencia, n.Status, n.CriadoEm);
}
