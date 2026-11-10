using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Services;

public static class RegrasNecessidade
{
    public static int Falta(int necessaria, int entregue, int prometido) =>
        Math.Max(0, necessaria - entregue - prometido);

    public static double PercentualAtendido(int necessaria, int entregue) =>
        necessaria <= 0 ? 0 : Math.Round(Math.Min(entregue, necessaria) * 100.0 / necessaria, 1);

    public static int PesoUrgencia(Urgencia urgencia) => urgencia switch
    {
        Urgencia.Critica => 4,
        Urgencia.Alta => 3,
        Urgencia.Media => 2,
        _ => 1
    };

    public static StatusNecessidade StatusAtualizado(StatusNecessidade atual, int necessaria, int entregue)
    {
        if (atual == StatusNecessidade.Cancelada)
            return atual;

        return entregue >= necessaria ? StatusNecessidade.Atendida : StatusNecessidade.Aberta;
    }

    public static string? ValidarNovaQuantidade(int novaQuantidade, int entregue)
    {
        if (novaQuantidade < entregue)
            return $"A quantidade não pode ser menor que o que já foi entregue ({entregue}).";

        return null;
    }

    public static IEnumerable<NecessidadeResumo> OrdenarPorPrioridade(IEnumerable<NecessidadeResumo> necessidades) =>
        necessidades
            .Where(n => n.Status == StatusNecessidade.Aberta && n.Falta > 0)
            .OrderByDescending(n => PesoUrgencia(n.Urgencia))
            .ThenByDescending(n => (double)n.Falta / n.QuantidadeNecessaria)
            .ThenBy(n => n.CriadoEm);
}
