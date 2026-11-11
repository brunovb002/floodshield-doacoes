using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;

namespace FloodShield.Doacoes.UnitTests.Regras;

public class RegrasNecessidadeTests
{
    [Theory]
    [InlineData(100, 0, 0, 100)]
    [InlineData(100, 60, 30, 10)]
    [InlineData(100, 100, 0, 0)]
    [InlineData(100, 80, 40, 0)]
    public void Falta_CalculaQuantoAindaFalta_NuncaNegativo(int necessaria, int entregue, int prometido, int esperado)
    {
        var falta = RegrasNecessidade.Falta(necessaria, entregue, prometido);

        Assert.Equal(esperado, falta);
    }

    [Theory]
    [InlineData(200, 120, 60)]
    [InlineData(3, 1, 33.3)]
    [InlineData(100, 150, 100)]
    [InlineData(0, 10, 0)]
    public void PercentualAtendido_ConsideraSoOEntregue(int necessaria, int entregue, double esperado)
    {
        var percentual = RegrasNecessidade.PercentualAtendido(necessaria, entregue);

        Assert.Equal(esperado, percentual);
    }

    [Fact]
    public void PesoUrgencia_CriticaMaiorQueAltaMaiorQueMediaMaiorQueBaixa()
    {
        Assert.True(RegrasNecessidade.PesoUrgencia(Urgencia.Critica) > RegrasNecessidade.PesoUrgencia(Urgencia.Alta));
        Assert.True(RegrasNecessidade.PesoUrgencia(Urgencia.Alta) > RegrasNecessidade.PesoUrgencia(Urgencia.Media));
        Assert.True(RegrasNecessidade.PesoUrgencia(Urgencia.Media) > RegrasNecessidade.PesoUrgencia(Urgencia.Baixa));
    }

    [Theory]
    [InlineData(StatusNecessidade.Aberta, 100, 100, StatusNecessidade.Atendida)]
    [InlineData(StatusNecessidade.Aberta, 100, 99, StatusNecessidade.Aberta)]
    [InlineData(StatusNecessidade.Atendida, 150, 100, StatusNecessidade.Aberta)]
    [InlineData(StatusNecessidade.Cancelada, 100, 100, StatusNecessidade.Cancelada)]
    public void StatusAtualizado_DependeDoEntregue_EMantemCancelada(
        StatusNecessidade atual, int necessaria, int entregue, StatusNecessidade esperado)
    {
        var status = RegrasNecessidade.StatusAtualizado(atual, necessaria, entregue);

        Assert.Equal(esperado, status);
    }

    [Fact]
    public void ValidarNovaQuantidade_AbaixoDoEntregue_RetornaErro()
    {
        var erro = RegrasNecessidade.ValidarNovaQuantidade(novaQuantidade: 50, entregue: 60);

        Assert.NotNull(erro);
        Assert.Contains("60", erro);
    }

    [Theory]
    [InlineData(60, 60)]
    [InlineData(100, 60)]
    public void ValidarNovaQuantidade_IgualOuAcimaDoEntregue_Aceita(int novaQuantidade, int entregue)
    {
        Assert.Null(RegrasNecessidade.ValidarNovaQuantidade(novaQuantidade, entregue));
    }

    [Fact]
    public void OrdenarPorPrioridade_OrdenaPorUrgenciaDepoisPeloQueFalta()
    {
        var hoje = DateTime.UtcNow;
        var lista = new[]
        {
            Resumo(1, Urgencia.Media, necessaria: 80, entregue: 0, hoje),
            Resumo(2, Urgencia.Alta, necessaria: 100, entregue: 90, hoje),
            Resumo(3, Urgencia.Critica, necessaria: 50, entregue: 0, hoje),
            Resumo(4, Urgencia.Alta, necessaria: 200, entregue: 150, hoje)
        };

        var ids = RegrasNecessidade.OrdenarPorPrioridade(lista).Select(n => n.Id).ToList();

        // crítica primeiro; entre as altas, a que falta 25% vem antes da que falta 10%
        Assert.Equal([3, 4, 2, 1], ids);
    }

    [Fact]
    public void OrdenarPorPrioridade_EmpateUsaAMaisAntiga()
    {
        var lista = new[]
        {
            Resumo(1, Urgencia.Alta, 10, 0, DateTime.UtcNow),
            Resumo(2, Urgencia.Alta, 10, 0, DateTime.UtcNow.AddDays(-1))
        };

        var ids = RegrasNecessidade.OrdenarPorPrioridade(lista).Select(n => n.Id).ToList();

        Assert.Equal([2, 1], ids);
    }

    [Fact]
    public void OrdenarPorPrioridade_IgnoraFechadasESemFalta()
    {
        var hoje = DateTime.UtcNow;
        var cancelada = Resumo(1, Urgencia.Critica, 10, 0, hoje);
        cancelada.Status = StatusNecessidade.Cancelada;
        var semFalta = Resumo(2, Urgencia.Critica, 10, 0, hoje);
        semFalta.Prometido = 10;
        var aberta = Resumo(3, Urgencia.Baixa, 10, 0, hoje);

        var resultado = RegrasNecessidade.OrdenarPorPrioridade([cancelada, semFalta, aberta]).ToList();

        Assert.Single(resultado);
        Assert.Equal(3, resultado[0].Id);
    }

    private static NecessidadeResumo Resumo(int id, Urgencia urgencia, int necessaria, int entregue, DateTime criadoEm) => new()
    {
        Id = id,
        Urgencia = urgencia,
        QuantidadeNecessaria = necessaria,
        Entregue = entregue,
        Status = StatusNecessidade.Aberta,
        CriadoEm = criadoEm
    };
}
