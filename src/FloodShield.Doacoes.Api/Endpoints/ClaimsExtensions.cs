using System.Security.Claims;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class ClaimsExtensions
{
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId) ? usuarioId : 0;
    }

    public static bool IsGestor(this ClaimsPrincipal user) => user.IsInRole("Gestor");
}
