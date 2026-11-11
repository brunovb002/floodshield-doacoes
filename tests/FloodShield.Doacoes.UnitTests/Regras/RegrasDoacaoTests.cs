using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;

namespace FloodShield.Doacoes.UnitTests.Regras;

public class RegrasDoacaoTests
{
    [Theory]
    [InlineData(10, 70)]
    [InlineData(70, 70)]
    public void ValidarQuantidade_DentroDoQueFalta_Aceita(int quantidade, int falta)
    {
        Assert.Null(RegrasDoacao.ValidarQuantidade(quantidade, falta));
    }

    [Fact]
    public void ValidarQuantidade_AcimaDoQueFalta_RetornaErroComOQueFalta()
    {
        var erro = RegrasDoacao.ValidarQuantidade(quantidade: 80, falta: 70);

        Assert.NotNull(erro);
        Assert.Contains("70", erro);
    }

    [Fact]
    public void ValidarQuantidade_NadaFaltando_RetornaErro()
    {
        Assert.NotNull(RegrasDoacao.ValidarQuantidade(quantidade: 1, falta: 0));
    }

    [Theory]
    [InlineData(StatusNecessidade.Atendida, true)]
    [InlineData(StatusNecessidade.Cancelada, true)]
    [InlineData(StatusNecessidade.Aberta, false)]
    public void ValidarNecessidade_SoAceitaAbertaEmPontoAtivo(StatusNecessidade status, bool pontoAtivo)
    {
        Assert.NotNull(RegrasDoacao.ValidarNecessidade(status, pontoAtivo));
    }

    [Fact]
    public void ValidarNecessidade_AbertaEmPontoAtivo_Aceita()
    {
        Assert.Null(RegrasDoacao.ValidarNecessidade(StatusNecessidade.Aberta, pontoAtivo: true));
    }

    [Theory]
    [InlineData(StatusDoacao.Prometida, true)]
    [InlineData(StatusDoacao.Entregue, false)]
    [InlineData(StatusDoacao.Cancelada, false)]
    public void PodeAlterar_SoDoacaoPrometida(StatusDoacao status, bool esperado)
    {
        Assert.Equal(esperado, RegrasDoacao.PodeAlterar(status));
    }

    [Theory]
    [InlineData("XGQRC8", "XGQRC8", true)]
    [InlineData("XGQRC8", "xgqrc8", true)]
    [InlineData("XGQRC8", " XGQRC8 ", true)]
    [InlineData("XGQRC8", "XGQRC9", false)]
    [InlineData("XGQRC8", null, false)]
    public void CodigoConfere_IgnoraMaiusculasEEspacos(string esperado, string? informado, bool confere)
    {
        Assert.Equal(confere, RegrasDoacao.CodigoConfere(esperado, informado));
    }

    [Fact]
    public void GerarCodigo_TemSeisCaracteresSemLetrasQueConfundem()
    {
        for (var i = 0; i < 200; i++)
        {
            var codigo = RegrasDoacao.GerarCodigo();

            Assert.Equal(6, codigo.Length);
            Assert.DoesNotContain(codigo, c => "0O1I".Contains(c));
            Assert.All(codigo, c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
        }
    }
}
