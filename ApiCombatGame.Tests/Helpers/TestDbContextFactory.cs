using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Shared factory for creating in-memory GameDbContext instances for unit tests.
/// Each call returns a fresh context backed by a unique in-memory database.
/// </summary>
public static class TestDbContextFactory
{
    public static GameDbContext Create(string? dbName = null)
    {
        dbName ??= $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new GameDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Creates a player with sensible defaults and adds it to the context.</summary>
    public static Player CreatePlayer(
        GameDbContext context,
        string username = "testplayer",
        SubscriptionTier tier = SubscriptionTier.Free,
        int currency = 1000,
        int level = 1)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            CurrentTier = tier,
            Currency = currency,
            Level = level,
            Rating = 1000,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastBattleResetDate = DateTime.UtcNow.Date
        };
        context.Players.Add(player);
        context.SaveChanges();
        return player;
    }

    /// <summary>Creates a guild with its leader's membership and returns both.</summary>
    public static (Guild Guild, GuildMembership LeaderMembership) CreateGuildWithLeader(
        GameDbContext context,
        Player leader,
        string name = "Test Guild",
        string tag = "TEST")
    {
        var guild = new Guild
        {
            Id = Guid.NewGuid(),
            Name = name,
            Tag = tag,
            Description = "A test guild",
            LeaderId = leader.Id,
            Level = 1,
            MaxMembers = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership = new GuildMembership
        {
            Id = Guid.NewGuid(),
            GuildId = guild.Id,
            PlayerId = leader.Id,
            Role = GuildRole.Leader,
            JoinedAt = DateTime.UtcNow
        };

        context.Guilds.Add(guild);
        context.GuildMemberships.Add(membership);
        context.SaveChanges();
        return (guild, membership);
    }

    /// <summary>Adds a member to an existing guild and returns the membership.</summary>
    public static GuildMembership AddGuildMember(
        GameDbContext context,
        Guid guildId,
        Player player,
        GuildRole role = GuildRole.Member)
    {
        var membership = new GuildMembership
        {
            Id = Guid.NewGuid(),
            GuildId = guildId,
            PlayerId = player.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        context.GuildMemberships.Add(membership);
        context.SaveChanges();
        return membership;
    }
}
