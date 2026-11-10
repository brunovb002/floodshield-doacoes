using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Services;

public class NecessidadeResumo
{
    public int Id { get; set; }
    public int PontoColetaId { get; set; }
    public string PontoColeta { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string Item { get; set; } = string.Empty;
    public CategoriaItem Categoria { get; set; }
    public UnidadeMedida Unidade { get; set; }
    public int QuantidadeNecessaria { get; set; }
    public int Entregue { get; set; }
    public int Prometido { get; set; }
    public Urgencia Urgencia { get; set; }
    public StatusNecessidade Status { get; set; }
    public DateTime CriadoEm { get; set; }

    public int Falta => RegrasNecessidade.Falta(QuantidadeNecessaria, Entregue, Prometido);
}

public static class NecessidadeQueries
{
    public static IQueryable<NecessidadeResumo> ComSaldo(this IQueryable<Necessidade> query) =>
        query.Select(n => new NecessidadeResumo
        {
            Id = n.Id,
            PontoColetaId = n.PontoColetaId,
            PontoColeta = n.PontoColeta!.Nome,
            Bairro = n.PontoColeta.Bairro,
            Cidade = n.PontoColeta.Cidade,
            ItemId = n.ItemId,
            Item = n.Item!.Nome,
            Categoria = n.Item.Categoria,
            Unidade = n.Item.Unidade,
            QuantidadeNecessaria = n.QuantidadeNecessaria,
            Entregue = n.Doacoes.Where(d => d.Status == StatusDoacao.Entregue).Sum(d => d.Quantidade),
            Prometido = n.Doacoes.Where(d => d.Status == StatusDoacao.Prometida).Sum(d => d.Quantidade),
            Urgencia = n.Urgencia,
            Status = n.Status,
            CriadoEm = n.CriadoEm
        });
}
