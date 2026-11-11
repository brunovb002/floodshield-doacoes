using System.Net.Http.Json;
using System.Text.Json;

namespace FloodShield.Doacoes.IntegrationTests.FactoryFixture;

public static class Cenario
{
    public static async Task<int> CriarPontoAsync(HttpClient gestor, bool ativo = true)
    {
        var response = await gestor.PostAsJsonAsync("/api/pontos-coleta",
            new { nome = $"Ponto {Guid.NewGuid():N}", cep = "01310100", numero = "100", ativo });
        response.EnsureSuccessStatusCode();
        return await IdAsync(response);
    }

    public static async Task<int> CriarItemAsync(HttpClient gestor)
    {
        var response = await gestor.PostAsJsonAsync("/api/itens",
            new { nome = $"Item {Guid.NewGuid():N}", categoria = "Agua", unidade = "Litro" });
        response.EnsureSuccessStatusCode();
        return await IdAsync(response);
    }

    public static async Task<int> CriarNecessidadeAsync(HttpClient gestor, int quantidade, string urgencia = "Media")
    {
        var ponto = await CriarPontoAsync(gestor);
        var item = await CriarItemAsync(gestor);

        var response = await gestor.PostAsJsonAsync("/api/necessidades",
            new { pontoColetaId = ponto, itemId = item, quantidadeNecessaria = quantidade, urgencia });
        response.EnsureSuccessStatusCode();
        return await IdAsync(response);
    }

    public static Task<HttpResponseMessage> DoarAsync(HttpClient cidadao, int necessidadeId, int quantidade, string? chave = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/doacoes")
        {
            Content = JsonContent.Create(new { necessidadeId, quantidade })
        };
        request.Headers.Add("Idempotency-Key", chave ?? Guid.NewGuid().ToString());
        return cidadao.SendAsync(request);
    }

    public static async Task<JsonElement> CorpoAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<int> IdAsync(HttpResponseMessage response) =>
        (await CorpoAsync(response)).GetProperty("id").GetInt32();
}
