using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildTreasuryService : IGuildTreasuryService
{
    private readonly GameDbContext _context;
    private readonly ILogger<GuildTreasuryService> _logger;

    private static readonly List<UpgradeDefinition> Upgrades = new()
    {
        new("max_members_30", "Expand Guild (30)", "Increase max members from 20 to 30", 50_000, g => g.MaxMembers < 30, g => g.MaxMembers = 30),
        new("max_members_50", "Expand Guild (50)", "Increase max members from 30 to 50", 100_000, g => g.MaxMembers >= 30 && g.MaxMembers < 50, g => g.MaxMembers = 50),
        new("gold_bonus_10", "Gold Bonus I (+10%)", "All members earn 10% bonus gold from battles", 30_000, g => g.GoldBonusPercent < 10, g => g.GoldBonusPercent = 10),
        new("gold_bonus_20", "Gold Bonus II (+20%)", "All members earn 20% bonus gold from battles", 60_000, g => g.GoldBonusPercent >= 10 && g.GoldBonusPercent < 20, g => g.GoldBonusPercent = 20),
        new("raid_attempts_4", "Raid Stamina I", "Increase daily raid attempts from 3 to 4", 40_000, g => g.MaxRaidAttempts < 4, g => g.MaxRaidAttempts = 4),
        new("raid_attempts_5", "Raid Stamina II", "Increase daily raid attempts from 4 to 5", 80_000, g => g.MaxRaidAttempts >= 4 && g.MaxRaidAttempts < 5, g => g.MaxRaidAttempts = 5),
    };

    public GuildTreasuryService(GameDbContext context, ILogger<GuildTreasuryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(Guild guild, List<(string Id, string Name, string Description, int Cost, bool CanAfford, bool AlreadyPurchased)> upgrades)> GetTreasuryAsync(Guid guildId)
    {
        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        var upgradeList = Upgrades.Select(u =>
        {
            var available = u.IsAvailable(guild);
            var alreadyPurchased = !available && !u.IsAvailable(guild);
            return (u.Id, u.Name, u.Description, u.Cost, CanAfford: guild.TreasuryBalance >= u.Cost && available, AlreadyPurchased: !available);
        }).ToList();

        return (guild, upgradeList);
    }

    public async Task<Guild> PurchaseUpgradeAsync(Guid guildId, Guid playerId, string upgradeId)
    {
        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        if (guild.LeaderId != playerId)
            throw new InvalidOperationException("Only the guild leader can purchase upgrades.");

        var upgrade = Upgrades.FirstOrDefault(u => u.Id == upgradeId)
            ?? throw new KeyNotFoundException($"Upgrade '{upgradeId}' not found.");

        if (!upgrade.IsAvailable(guild))
            throw new InvalidOperationException("This upgrade is not available (already purchased or prerequisites not met).");

        if (guild.TreasuryBalance < upgrade.Cost)
            throw new InvalidOperationException($"Insufficient treasury balance. Need {upgrade.Cost:N0}, have {guild.TreasuryBalance:N0}.");

        guild.TreasuryBalance -= upgrade.Cost;
        upgrade.Apply(guild);
        guild.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Guild {GuildId} purchased upgrade {UpgradeId} for {Cost}g",
            guildId, upgradeId, upgrade.Cost);

        return guild;
    }

    public async Task<Guild> DepositAsync(Guid guildId, Guid playerId, int amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive.");

        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == playerId)
            ?? throw new InvalidOperationException("You are not a member of this guild.");

        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        if (player.Currency < amount)
            throw new InvalidOperationException($"Insufficient currency. You have {player.Currency:N0}, tried to deposit {amount:N0}.");

        player.Currency -= amount;
        guild.TreasuryBalance += amount;
        membership.ContributionPoints += amount;
        guild.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} deposited {Amount}g to guild {GuildId} treasury",
            playerId, amount, guildId);

        return guild;
    }

    private record UpgradeDefinition(
        string Id,
        string Name,
        string Description,
        int Cost,
        Func<Guild, bool> IsAvailable,
        Action<Guild> Apply);
}
