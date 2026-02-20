using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Loot;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class LootService : ILootService
{
    private readonly GameDbContext _context;
    private readonly IActivityLedger _ledger;
    private readonly ILogger<LootService> _logger;
    private static readonly Random Rng = new();

    // Base drop chance: 15%, increases with win streak
    private const double BaseDropChance = 0.15;
    private const double WinStreakBonusPerWin = 0.033; // +3.3% per streak win, caps at ~25% at 3+
    private const double CriticalGoldChance = 0.05; // 5% of battles give 3x gold
    private const int PremiumGuaranteedEveryN = 5; // Premium gets guaranteed drop every 5 battles
    private const int PremiumPlusGuaranteedEveryN = 3; // Premium Plus gets guaranteed drop every 3 battles
    private const double PremiumPlusRarityBoost = 0.15; // 15% better rarity distribution for PP

    public LootService(GameDbContext context, IActivityLedger ledger, ILogger<LootService> logger)
    {
        _context = context;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<List<LootDropResponse>> RollLootAsync(Guid playerId, Guid battleId, bool won, int winStreak)
    {
        var drops = new List<LootDrop>();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
        {
            _logger.LogWarning("Player {PlayerId} not found in RollLoot", playerId);
            return new List<LootDropResponse>();
        }

        // Calculate effective drop chance
        double dropChance = BaseDropChance;
        if (won && winStreak >= 3)
            dropChance += Math.Min(winStreak * WinStreakBonusPerWin, 0.10); // Cap at +10%

        // Premium guaranteed drop every N battles (Premium Plus gets more frequent drops)
        bool premiumGuarantee = false;
        if (player.CurrentTier >= SubscriptionTier.Premium)
        {
            var recentBattleCount = await _context.Battles
                .CountAsync(b => b.Player1Id == playerId && b.Status == BattleStatus.Completed
                    && b.CompletedAt > DateTime.UtcNow.AddHours(-24));
            int guaranteeInterval = player.CurrentTier == SubscriptionTier.PremiumPlus
                ? PremiumPlusGuaranteedEveryN
                : PremiumGuaranteedEveryN;
            premiumGuarantee = recentBattleCount > 0 && recentBattleCount % guaranteeInterval == 0;
        }

        // Roll for standard loot drop (Premium Plus gets rarity-boosted drops)
        bool isPremiumPlus = player.CurrentTier == SubscriptionTier.PremiumPlus;
        if (Rng.NextDouble() < dropChance || premiumGuarantee)
        {
            var drop = GenerateRandomDrop(playerId, battleId, isPremiumPlus);
            drops.Add(drop);
        }

        // Roll for critical gold (independent of standard drop)
        if (Rng.NextDouble() < CriticalGoldChance)
        {
            var critDrop = new LootDrop
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                BattleId = battleId,
                DropType = LootDropType.CriticalGold,
                Description = "JACKPOT! Critical Gold — 3x battle reward!",
                Value = (won ? 50 : 10) * 3, // 3x base gold
                DroppedAt = DateTime.UtcNow
            };
            drops.Add(critDrop);
        }

        if (drops.Count > 0)
        {
            _context.LootDrops.AddRange(drops);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Player {PlayerId} received {Count} loot drop(s) from battle {BattleId}",
                playerId, drops.Count, battleId);
        }

        return drops.Select(ToResponse).ToList();
    }

    public async Task<PendingLootResponse> GetPendingLootAsync(Guid playerId)
    {
        var drops = await _context.LootDrops
            .Where(d => d.PlayerId == playerId && !d.Claimed)
            .OrderByDescending(d => d.DroppedAt)
            .ToListAsync();

        return new PendingLootResponse
        {
            Drops = drops.Select(ToResponse).ToList(),
            TotalUnclaimed = drops.Count
        };
    }

    public async Task<ClaimLootResponse> ClaimAllLootAsync(Guid playerId)
    {
        var drops = await _context.LootDrops
            .Where(d => d.PlayerId == playerId && !d.Claimed)
            .ToListAsync();

        if (drops.Count == 0)
            throw new InvalidOperationException("No unclaimed loot drops.");

        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        int totalGold = 0;
        int totalXp = 0;
        var titles = new List<string>();

        foreach (var drop in drops)
        {
            drop.Claimed = true;

            switch (drop.DropType)
            {
                case LootDropType.CurrencyPack:
                case LootDropType.CriticalGold:
                    totalGold += drop.Value;
                    break;
                case LootDropType.XpBoost:
                    totalXp += drop.Value;
                    break;
                case LootDropType.RareTitle:
                    var titleName = drop.Description.Replace("Rare Title: ", "");
                    var existingTitle = await _context.PlayerTitles.FirstOrDefaultAsync(t => t.Name == titleName);
                    if (existingTitle == null)
                    {
                        existingTitle = new PlayerTitle
                        {
                            Id = Guid.NewGuid(),
                            Name = titleName,
                            Description = "Earned from a lucky loot drop",
                            ColorHex = "#9333ea"
                        };
                        _context.PlayerTitles.Add(existingTitle);
                    }
                    titles.Add(titleName);
                    break;
            }
        }

        var oldCurrency = player.Currency;
        var oldXp = player.ExperiencePoints;
        player.Currency += totalGold;
        player.ExperiencePoints += totalXp;
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "Loot", "LootClaimed");
        _ledger.LogPlayer(playerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "Loot", "LootClaimed");
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ClaimLootResponse
        {
            GoldAwarded = totalGold,
            XpAwarded = totalXp,
            TitlesUnlocked = titles,
            DropsClaimed = drops.Count
        };
    }

    private static LootDrop GenerateRandomDrop(Guid playerId, Guid battleId, bool premiumPlusBoost = false)
    {
        double roll = Rng.NextDouble();

        // Premium Plus shifts distribution toward higher-value drops
        // Normal: 50% currency, 30% XP, 15% rare currency, 5% title
        // PP:     35% currency, 30% XP, 25% rare currency, 10% title
        double currencyThreshold = premiumPlusBoost ? 0.35 : 0.50;
        double xpThreshold = premiumPlusBoost ? 0.65 : 0.80;
        double rareThreshold = premiumPlusBoost ? 0.90 : 0.95;

        if (roll < currencyThreshold)
        {
            int gold = Rng.Next(50, 501); // 50-500g
            return new LootDrop
            {
                Id = Guid.NewGuid(), PlayerId = playerId, BattleId = battleId,
                DropType = LootDropType.CurrencyPack,
                Description = $"Currency Pack: {gold}g",
                Value = gold
            };
        }
        else if (roll < xpThreshold)
        {
            int xp = Rng.Next(50, 201); // 50-200 XP
            return new LootDrop
            {
                Id = Guid.NewGuid(), PlayerId = playerId, BattleId = battleId,
                DropType = LootDropType.XpBoost,
                Description = $"XP Boost: {xp} XP",
                Value = xp
            };
        }
        else if (roll < rareThreshold)
        {
            int gold = Rng.Next(200, 801); // 200-800g rare pack
            return new LootDrop
            {
                Id = Guid.NewGuid(), PlayerId = playerId, BattleId = battleId,
                DropType = LootDropType.CurrencyPack,
                Description = $"Rare Currency Pack: {gold}g",
                Value = gold
            };
        }
        else
        {
            var rareTitles = new[]
            {
                "Lucky Strike", "Fortune's Favorite", "Blessed by RNG",
                "The Fortunate", "Jackpot Hunter", "Loot Goblin",
                "Treasure Seeker", "Golden Touch"
            };
            var title = rareTitles[Rng.Next(rareTitles.Length)];
            return new LootDrop
            {
                Id = Guid.NewGuid(), PlayerId = playerId, BattleId = battleId,
                DropType = LootDropType.RareTitle,
                Description = $"Rare Title: {title}",
                Value = 0
            };
        }
    }

    private static LootDropResponse ToResponse(LootDrop drop) => new()
    {
        Id = drop.Id,
        DropType = drop.DropType.ToString(),
        Description = drop.Description,
        Value = drop.Value,
        BattleId = drop.BattleId,
        DroppedAt = drop.DroppedAt
    };
}
