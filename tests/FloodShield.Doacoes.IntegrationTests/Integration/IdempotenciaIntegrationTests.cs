using System.Net;
using System.Net.Http.Json;
using FloodShield.Doacoes.IntegrationTests.FactoryFixture;

namespace FloodShield.Doacoes.IntegrationTests.Integration;

[Collection("ApiCollection")]
public class IdempotenciaIntegrationTests
{
    private readonly ApiFactoryFixture _factory;

    public IdempotenciaIntegrationTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Doar_SemIdempotencyKey_RetornaBadRequest()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 10);

        var response = await cidadao.PostAsJsonAsync("/api/doacoes", new { necessidadeId = necessidade, quantidade = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Doar_MesmaChaveDuasVezes_CriaUmaDoacaoSo()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 100);

        var primeira = await Cenario.DoarAsync(cidadao, necessidade, 10, "chave-repetida");
        var segunda = await Cenario.DoarAsync(cidadao, necessidade, 10, "chave-repetida");

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(await Cenario.IdAsync(primeira), await Cenario.IdAsync(segunda));
        Assert.True(segunda.Headers.Contains("Idempotent-Replayed"));

        var saldo = await Cenario.CorpoAsync(await gestor.GetAsync($"/api/necessidades/{necessidade}"));
        Assert.Equal(10, saldo.GetProperty("prometido").GetInt32());
    }

    [Fact]
    public async Task Doar_MesmaChaveComOutroCorpo_RetornaConflict()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 100);
        await Cenario.DoarAsync(cidadao, necessidade, 10, "chave-x");

        var response = await Cenario.DoarAsync(cidadao, necessidade, 20, "chave-x");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Doar_MesmaChaveDeOutroUsuario_NaoConflita()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var maria = await _factory.ClienteCidadaoAsync();
        var joao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 100);

        var daMaria = await Cenario.DoarAsync(maria, necessidade, 5, "chave-comum");
        var doJoao = await Cenario.DoarAsync(joao, necessidade, 5, "chave-comum");

        Assert.Equal(HttpStatusCode.Created, doJoao.StatusCode);
        Assert.NotEqual(await Cenario.IdAsync(daMaria), await Cenario.IdAsync(doJoao));
    }
}
