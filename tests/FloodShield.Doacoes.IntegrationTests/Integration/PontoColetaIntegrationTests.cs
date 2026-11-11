using System.Net;
using System.Net.Http.Json;
using FloodShield.Doacoes.IntegrationTests.FactoryFixture;

namespace FloodShield.Doacoes.IntegrationTests.Integration;

[Collection("ApiCollection")]
public class PontoColetaIntegrationTests
{
    private readonly ApiFactoryFixture _factory;

    public PontoColetaIntegrationTests(ApiFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Criar_CepValido_PreencheEnderecoPeloViaCep()
    {
        var gestor = await _factory.ClienteGestorAsync();

        var response = await gestor.PostAsJsonAsync("/api/pontos-coleta", new { nome = "Ginásio", cep = "01310-100", numero = "1578" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var corpo = await Cenario.CorpoAsync(response);
        Assert.Equal("Avenida Paulista", corpo.GetProperty("logradouro").GetString());
        Assert.Equal("01310100", corpo.GetProperty("cep").GetString());
    }

    [Fact]
    public async Task Criar_CepInexistente_RetornaUnprocessableEntity()
    {
        var gestor = await _factory.ClienteGestorAsync();

        var response = await gestor.PostAsJsonAsync("/api/pontos-coleta", new { nome = "X", cep = ViaCepFake.CepInexistente });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ViaCepForaDoArSemEndereco_RetornaServiceUnavailable()
    {
        var gestor = await _factory.ClienteGestorAsync();

        var response = await gestor.PostAsJsonAsync("/api/pontos-coleta", new { nome = "X", cep = ViaCepFake.CepForaDoAr });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ViaCepForaDoArComEndereco_AceitaOEnderecoDigitado()
    {
        var gestor = await _factory.ClienteGestorAsync();

        var response = await gestor.PostAsJsonAsync("/api/pontos-coleta", new
        {
            nome = "Escola",
            cep = ViaCepFake.CepForaDoAr,
            logradouro = "Rua A",
            bairro = "Centro",
            cidade = "Canoas",
            uf = "rs"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("RS", (await Cenario.CorpoAsync(response)).GetProperty("uf").GetString());
    }

    [Fact]
    public async Task Listar_ComPaginacao_RetornaTotalEPaginas()
    {
        var gestor = await _factory.ClienteGestorAsync();
        for (var i = 0; i < 3; i++)
            await Cenario.CriarPontoAsync(gestor);

        var corpo = await Cenario.CorpoAsync(await gestor.GetAsync("/api/pontos-coleta?pagina=1&tamanhoPagina=2"));

        Assert.Equal(2, corpo.GetProperty("itens").GetArrayLength());
        Assert.True(corpo.GetProperty("total").GetInt32() >= 3);
        Assert.True(corpo.GetProperty("totalPaginas").GetInt32() >= 2);
    }

    [Fact]
    public async Task Excluir_PontoComNecessidades_RetornaConflict()
    {
        var gestor = await _factory.ClienteGestorAsync();
        var necessidadeId = await Cenario.CriarNecessidadeAsync(gestor, 10);
        var necessidade = await Cenario.CorpoAsync(await gestor.GetAsync($"/api/necessidades/{necessidadeId}"));
        var pontoId = necessidade.GetProperty("pontoColetaId").GetInt32();

        var response = await gestor.DeleteAsync($"/api/pontos-coleta/{pontoId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
