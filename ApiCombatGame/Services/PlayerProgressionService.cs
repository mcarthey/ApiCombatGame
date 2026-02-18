using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class PlayerProgressionService : IPlayerProgressionService
{
    private readonly GameDbContext _context;
    private readonly ILogger<PlayerProgressionService> _logger;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;

    public PlayerProgressionService(GameDbContext context, ILogger<PlayerProgressionService> logger, INotificationService notifications, IActivityLedger ledger)
    {
        _context = context;
        _logger = logger;
        _notifications = notifications;
        _ledger = ledger;
    }

    public async Task<BattleRewardsSummary> ProcessBattleRewardsAsync(Guid playerId, bool won, int ratingChange)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found");

        var oldCurrency = player.Currency;
        var oldXp = player.ExperiencePoints;
        var oldLevel = player.Level;
        var oldWinStreak = player.WinStreak;

        var summary = new BattleRewardsSummary();

        // Base gold: Win = 50, Loss = 10
        int baseGold = won ? 50 : 10;

        // Win streak tracking
        if (won)
        {
            player.WinStreak++;
            summary.WinStreakBonus = player.WinStreak * 10;
            baseGold += summary.WinStreakBonus;
        }
        else
        {
            player.WinStreak = 0;
        }
        summary.WinStreak = player.WinStreak;

        // First battle of day bonus
        if (player.LastFirstBattleBonus?.Date != DateTime.UtcNow.Date)
        {
            summary.FirstBattleBonus = true;
            player.LastFirstBattleBonus = DateTime.UtcNow;
            baseGold += 100;
        }

        // Apply tier multiplier
        summary.TierMultiplier = GetGoldMultiplier(player.CurrentTier);
        int gold = (int)(baseGold * summary.TierMultiplier);

        // Apply guild gold bonus if player is in a guild
        var guildMembership = await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);
        if (guildMembership?.Guild?.GoldBonusPercent > 0)
        {
            gold += (int)(gold * guildMembership.Guild.GoldBonusPercent / 100.0);
        }

        summary.GoldEarned = gold;
        player.Currency += summary.GoldEarned;

        // XP: Win = 100, Loss = 25, with Premium Plus multiplier
        int baseXp = won ? 100 : 25;
        decimal xpMultiplier = player.CurrentTier == SubscriptionTier.PremiumPlus ? 1.5m : 1.0m;
        int finalXp = (int)(baseXp * xpMultiplier);
        summary.XpEarned = finalXp;
        player.ExperiencePoints += finalXp;

        // Level-up check
        int xpNeeded = GetXpForLevel(player.Level);
        while (player.ExperiencePoints >= xpNeeded && player.Level < 100)
        {
            player.ExperiencePoints -= xpNeeded;
            player.Level++;
            player.Currency += 250; // Level-up gold reward
            summary.GoldEarned += 250;
            summary.NewLevel = player.Level;
            await _notifications.SendAsync(playerId, Models.Enums.NotificationType.LevelUp, "Level Up!",
                $"You reached level {player.Level}! +250g bonus.");
            xpNeeded = GetXpForLevel(player.Level);

            _logger.LogInformation("Player {PlayerId} leveled up to {Level}", playerId, player.Level);
        }

        var action = won ? "BattleWon" : "BattleLost";
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "Battle", action);
        _ledger.LogPlayer(playerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "Battle", action);
        _ledger.LogPlayer(playerId, "Level", oldLevel, player.Level, "LevelUp", "LevelUp");
        _ledger.LogPlayer(playerId, "WinStreak", oldWinStreak, player.WinStreak, "Battle", action);

        await _context.SaveChangesAsync();
        return summary;
    }

    public async Task<int> AwardCurrencyAsync(Guid playerId, int baseGold)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found");

        var oldCurrency = player.Currency;
        var multiplier = GetGoldMultiplier(player.CurrentTier);
        int actual = (int)(baseGold * multiplier);
        player.Currency += actual;
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "Battle", "CurrencyAwarded");
        await _context.SaveChangesAsync();
        return actual;
    }

    public async Task<int?> AwardExperienceAsync(Guid playerId, int baseXp)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found");

        var oldXp = player.ExperiencePoints;
        var oldLevel = player.Level;
        var oldCurrency = player.Currency;

        player.ExperiencePoints += baseXp;

        int? newLevel = null;
        int xpNeeded = GetXpForLevel(player.Level);
        while (player.ExperiencePoints >= xpNeeded && player.Level < 100)
        {
            player.ExperiencePoints -= xpNeeded;
            player.Level++;
            player.Currency += 250;
            newLevel = player.Level;
            await _notifications.SendAsync(playerId, Models.Enums.NotificationType.LevelUp, "Level Up!",
                $"You reached level {player.Level}! +250g bonus.");
            xpNeeded = GetXpForLevel(player.Level);

            _logger.LogInformation("Player {PlayerId} leveled up to {Level}", playerId, player.Level);
        }

        _ledger.LogPlayer(playerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "Battle", "XpAwarded");
        _ledger.LogPlayer(playerId, "Level", oldLevel, player.Level, "LevelUp", "LevelUp");
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "LevelUp", "LevelUpBonus");

        await _context.SaveChangesAsync();
        return newLevel;
    }

    public int GetXpForLevel(int level) => (int)(500 * level * 1.5);

    public decimal GetGoldMultiplier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 1.0m,
        SubscriptionTier.Premium => 1.5m,
        SubscriptionTier.PremiumPlus => 2.0m,
        _ => 1.0m
    };
}
