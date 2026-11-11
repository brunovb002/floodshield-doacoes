using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FloodShield.Doacoes.IntegrationTests.FactoryFixture;

namespace FloodShield.Doacoes.IntegrationTests.Integration;

[Collection("ApiCollection")]
public class DoacaoIntegrationTests
{
    private readonly ApiFactoryFixture _factory;

    public DoacaoIntegrationTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Doar_DentroDoSaldo_ReservaQuantidadeNaNecessidade()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 100);

        var response = await Cenario.DoarAsync(cidadao, necessidade, 30);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var doacao = await Cenario.CorpoAsync(response);
        Assert.Equal("Prometida", doacao.GetProperty("status").GetString());
        Assert.Equal(6, doacao.GetProperty("codigoEntrega").GetString()!.Length);

        var saldo = await Saldo(gestor, necessidade);
        Assert.Equal(30, saldo.GetProperty("prometido").GetInt32());
        Assert.Equal(70, saldo.GetProperty("falta").GetInt32());
    }

    [Fact]
    public async Task Doar_AcimaDoQueFalta_RetornaUnprocessableEntity()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 10);

        var response = await Cenario.DoarAsync(cidadao, necessidade, 11);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Doar_ComoGestor_RetornaForbidden()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 10);

        var response = await Cenario.DoarAsync(gestor, necessidade, 1);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmarEntrega_CompletandoANecessidade_MarcaComoAtendida()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 20);
        var doacao = await Cenario.CorpoAsync(await Cenario.DoarAsync(cidadao, necessidade, 20));
        var id = doacao.GetProperty("id").GetInt32();
        var codigo = doacao.GetProperty("codigoEntrega").GetString()!.ToLower();

        var response = await gestor.PatchAsJsonAsync($"/api/doacoes/{id}/confirmar-entrega", new { codigo });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Entregue", (await Cenario.CorpoAsync(response)).GetProperty("status").GetString());
        Assert.Equal("Atendida", (await Saldo(gestor, necessidade)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConfirmarEntrega_CodigoErrado_RetornaUnprocessableEntity()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 20);
        var id = await Cenario.IdAsync(await Cenario.DoarAsync(cidadao, necessidade, 5));

        var response = await gestor.PatchAsJsonAsync($"/api/doacoes/{id}/confirmar-entrega", new { codigo = "ZZZZZZ" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Excluir_DoacaoPrometida_DevolveOSaldo()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var cidadao = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 50);
        var id = await Cenario.IdAsync(await Cenario.DoarAsync(cidadao, necessidade, 50));

        var response = await cidadao.DeleteAsync($"/api/doacoes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(50, (await Saldo(gestor, necessidade)).GetProperty("falta").GetInt32());
    }

    [Fact]
    public async Task ObterPorId_DoacaoDeOutroCidadao_RetornaNotFound()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var dono = await _factory.ClienteCidadaoAsync();
        var outro = await _factory.ClienteCidadaoAsync();
        var necessidade = await Cenario.CriarNecessidadeAsync(gestor, 10);
        var id = await Cenario.IdAsync(await Cenario.DoarAsync(dono, necessidade, 1));

        var response = await outro.GetAsync($"/api/doacoes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Prioritarias_MostraCriticaAntesDeMedia()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var media = await Cenario.CriarNecessidadeAsync(gestor, 10, "Media");
        var critica = await Cenario.CriarNecessidadeAsync(gestor, 10, "Critica");

        var corpo = await Cenario.CorpoAsync(await gestor.GetAsync("/api/necessidades/prioritarias?tamanhoPagina=50"));
        var ids = corpo.GetProperty("itens").EnumerateArray().Select(n => n.GetProperty("id").GetInt32()).ToList();

        Assert.True(ids.IndexOf(critica) < ids.IndexOf(media));
    }

    private static async Task<JsonElement> Saldo(HttpClient client, int necessidadeId) =>
        await Cenario.CorpoAsync(await client.GetAsync($"/api/necessidades/{necessidadeId}"));
}
