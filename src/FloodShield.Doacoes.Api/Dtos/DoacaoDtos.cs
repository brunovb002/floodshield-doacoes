using System.ComponentModel.DataAnnotations;
using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Dtos;

public record CriarDoacaoRequest(
    [property: Range(1, int.MaxValue)] int NecessidadeId,
    [property: Range(1, 100000)] int Quantidade);

public record AtualizarDoacaoRequest(
    [property: Range(1, 100000)] int Quantidade);

public record ConfirmarEntregaRequest(
    [property: Required, StringLength(6, MinimumLength = 6)] string Codigo);

public record DoacaoFiltro(
    StatusDoacao? Status,
    int? NecessidadeId,
    int? PontoColetaId,
    int Pagina = 1,
    int TamanhoPagina = 10);

public record DoacaoResponse(
    int Id,
    int UsuarioId,
    string Doador,
    int NecessidadeId,
    string Item,
    UnidadeMedida Unidade,
    int PontoColetaId,
    string PontoColeta,
    string Endereco,
    int Quantidade,
    StatusDoacao Status,
    string CodigoEntrega,
    DateTime CriadoEm,
    DateTime? EntregueEm)
{
    public static DoacaoResponse De(Doacao d)
    {
        var ponto = d.Necessidade!.PontoColeta!;
        var endereco = $"{ponto.Logradouro}, {ponto.Numero ?? "s/n"} - {ponto.Bairro}, {ponto.Cidade}/{ponto.Uf}";

        return new(d.Id, d.UsuarioId, d.Usuario!.Nome, d.NecessidadeId, d.Necessidade.Item!.Nome, d.Necessidade.Item.Unidade,
            ponto.Id, ponto.Nome, endereco, d.Quantidade, d.Status, d.CodigoEntrega, d.CriadoEm, d.EntregueEm);
    }
}
