using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public static class AdminSeedData
{
    public static async Task InitializeAsync(GameDbContext context, IConfiguration config)
    {
        var username = config["Admin:SeedUsername"];
        var email = config["Admin:SeedEmail"];
        var password = config["Admin:SeedPassword"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return;

        var existingPlayer = await context.Players.FirstOrDefaultAsync(p => p.Username == username);

        if (existingPlayer != null)
        {
            // Ensure admin flag is set
            if (!existingPlayer.IsAdmin)
            {
                existingPlayer.IsAdmin = true;
                existingPlayer.AdminRole = AdminRole.SuperAdmin;
                await context.SaveChangesAsync();
            }
            return;
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email ?? $"{username}@apicombatgame.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Level = 1,
            Currency = 99999,
            Rating = 1500,
            IsAdmin = true,
            AdminRole = AdminRole.SuperAdmin,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        context.Players.Add(player);
        await context.SaveChangesAsync();
    }
}
