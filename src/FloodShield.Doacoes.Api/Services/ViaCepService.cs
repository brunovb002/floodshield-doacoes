namespace FloodShield.Doacoes.Api.Services;

public record EnderecoCep(string Logradouro, string Bairro, string Cidade, string Uf);

public class ViaCepIndisponivelException : Exception
{
    public ViaCepIndisponivelException(Exception inner) : base("ViaCEP indisponível.", inner)
    {
    }
}

public interface IViaCepService
{
    Task<EnderecoCep?> BuscarAsync(string cep);
}

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _http;

    public ViaCepService(HttpClient http)
    {
        _http = http;
    }

    public async Task<EnderecoCep?> BuscarAsync(string cep)
    {
        ViaCepResposta? resposta;
        try
        {
            using var response = await _http.GetAsync($"{cep}/json/");
            if (!response.IsSuccessStatusCode)
                return null;

            resposta = await response.Content.ReadFromJsonAsync<ViaCepResposta>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ViaCepIndisponivelException(ex);
        }

        // quando o CEP não existe o ViaCEP responde só {"erro": "true"}
        if (resposta?.Cep is null)
            return null;

        return new EnderecoCep(resposta.Logradouro ?? "", resposta.Bairro ?? "", resposta.Localidade ?? "", resposta.Uf ?? "");
    }

    private record ViaCepResposta(string? Cep, string? Logradouro, string? Bairro, string? Localidade, string? Uf);
}
