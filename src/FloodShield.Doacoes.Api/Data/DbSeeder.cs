using FloodShield.Doacoes.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Data;

public static class DbSeeder
{
    public static async Task CriarGestorAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Gestor:Email"]?.Trim().ToLower();
        var senha = configuration["Gestor:Senha"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var gestor = new Usuario
        {
            Nome = "Gestor Defesa Civil",
            Email = email,
            Papel = Papel.Gestor
        };
        gestor.SenhaHash = hasher.HashPassword(gestor, senha);

        db.Usuarios.Add(gestor);
        await db.SaveChangesAsync();
    }
}
