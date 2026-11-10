using System.Security.Claims;
using FloodShield.Doacoes.Api.Data;
using FloodShield.Doacoes.Api.Dtos;
using FloodShield.Doacoes.Api.Models;
using FloodShield.Doacoes.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/registrar", Registrar)
            .Produces<UsuarioResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);

        auth.MapPost("/login", Login)
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        var usuarios = app.MapGroup("/api/usuarios").WithTags("Usuários").RequireAuthorization();

        usuarios.MapGet("/me", ObterPerfil)
            .Produces<UsuarioResponse>();

        usuarios.MapPut("/me", AtualizarPerfil)
            .Produces<UsuarioResponse>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> Registrar(RegistrarRequest request, AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        var email = request.Email.Trim().ToLower();

        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return Results.Conflict(new { mensagem = "Já existe um usuário com esse e-mail." });

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            Papel = Papel.Cidadao
        };
        usuario.SenhaHash = hasher.HashPassword(usuario, request.Senha);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return Results.Created("/api/usuarios/me", UsuarioResponse.De(usuario));
    }

    private static async Task<IResult> Login(LoginRequest request, AppDbContext db, IPasswordHasher<Usuario> hasher, TokenService tokenService)
    {
        var email = request.Email.Trim().ToLower();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null)
            return Results.Unauthorized();

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.Senha);
        if (resultado == PasswordVerificationResult.Failed)
            return Results.Unauthorized();

        var (token, expiraEm) = tokenService.GerarToken(usuario);
        return Results.Ok(new LoginResponse(token, expiraEm));
    }

    private static async Task<IResult> ObterPerfil(ClaimsPrincipal user, AppDbContext db)
    {
        var usuario = await db.Usuarios.FindAsync(user.GetUsuarioId());
        return usuario is null ? Results.NotFound() : Results.Ok(UsuarioResponse.De(usuario));
    }

    private static async Task<IResult> AtualizarPerfil(AtualizarPerfilRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        var usuario = await db.Usuarios.FindAsync(user.GetUsuarioId());
        if (usuario is null)
            return Results.NotFound();

        usuario.Nome = request.Nome.Trim();
        await db.SaveChangesAsync();

        return Results.Ok(UsuarioResponse.De(usuario));
    }
}
