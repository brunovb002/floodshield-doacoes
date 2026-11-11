using System.Security.Cryptography;
using FloodShield.Doacoes.Api.Models;

namespace FloodShield.Doacoes.Api.Services;

public static class RegrasDoacao
{
    private const string CaracteresCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string? ValidarQuantidade(int quantidade, int falta)
    {
        if (falta <= 0)
            return "Essa necessidade já foi completamente atendida.";

        if (quantidade > falta)
            return $"A quantidade passa do que falta para essa necessidade (faltam {falta}).";

        return null;
    }

    public static string? ValidarNecessidade(StatusNecessidade status, bool pontoAtivo)
    {
        if (status != StatusNecessidade.Aberta)
            return "Essa necessidade não está aberta para doações.";

        if (!pontoAtivo)
            return "O ponto de coleta está desativado.";

        return null;
    }

    public static bool PodeAlterar(StatusDoacao status) => status == StatusDoacao.Prometida;

    public static bool CodigoConfere(string codigoEsperado, string? codigoInformado) =>
        string.Equals(codigoEsperado, codigoInformado?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static string GerarCodigo() =>
        new string(RandomNumberGenerator.GetItems<char>(CaracteresCodigo, 6));
}
