using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Premium;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class PremiumPlusService : IPremiumPlusService
{
    private readonly GameDbContext _context;
    private readonly ILogger<PremiumPlusService> _logger;

    private const int MonthlyGemStipend = 500;

    // Available badges by tier
    private static readonly Dictionary<SubscriptionTier, List<string>> BadgesByTier = new()
    {
        [SubscriptionTier.Free] = new() { "newcomer" },
        [SubscriptionTier.Premium] = new() { "newcomer", "premium", "supporter" },
        [SubscriptionTier.PremiumPlus] = new() { "newcomer", "premium", "supporter", "premium_plus", "creator", "elite", "vip" }
    };

    public PremiumPlusService(GameDbContext context, ILogger<PremiumPlusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PremiumPerksResponse> GetPerksAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        var tier = player.CurrentTier;

        DateTime? nextStipend = null;
        if (tier == SubscriptionTier.PremiumPlus)
        {
            nextStipend = player.LastGemStipendClaimedAt?.AddDays(30) ?? DateTime.UtcNow;
        }

        return new PremiumPerksResponse
        {
            CurrentTier = tier.ToString(),
            GoldMultiplier = GetGoldMultiplier(tier),
            XpMultiplier = GetXpMultiplier(tier),
            BattlePassXpBonus = tier == SubscriptionTier.PremiumPlus ? 0.25m : 0m,
            DailyBattleLimit = tier == SubscriptionTier.Free ? 10 : -1, // -1 = unlimited
            MaxTeamSlots = tier == SubscriptionTier.Free ? 3 : 10,
            CanCreateGuild = tier >= SubscriptionTier.Premium,
            CanRefreshChallenges = tier >= SubscriptionTier.Premium,
            GuaranteedLootEveryNBattles = tier switch
            {
                SubscriptionTier.PremiumPlus => 3,
                SubscriptionTier.Premium => 5,
                _ => 0
            },
            HasPremiumBattlePassTrack = tier >= SubscriptionTier.Premium,
            MonthlyGemStipend = tier == SubscriptionTier.PremiumPlus ? MonthlyGemStipend : 0,
            HasCreatorBadge = tier == SubscriptionTier.PremiumPlus,
            HasExclusiveCosmetics = tier == SubscriptionTier.PremiumPlus,
            LootRarityBoost = tier == SubscriptionTier.PremiumPlus ? 0.15 : 0.0,
            Badge = player.Badge,
            NextGemStipendAvailable = nextStipend
        };
    }

    public async Task<GemStipendClaimResponse> ClaimMonthlyGemsAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        if (player.CurrentTier != SubscriptionTier.PremiumPlus)
            throw new InvalidOperationException("Monthly gem stipend is a Premium Plus exclusive perk.");

        if (player.LastGemStipendClaimedAt != null &&
            player.LastGemStipendClaimedAt.Value.AddDays(30) > DateTime.UtcNow)
        {
            var nextDate = player.LastGemStipendClaimedAt.Value.AddDays(30);
            throw new InvalidOperationException(
                $"Gem stipend already claimed this period. Next claim available: {nextDate:yyyy-MM-dd}");
        }

        player.Gems += MonthlyGemStipend;
        player.GemsEarnedTotal += MonthlyGemStipend;
        player.LastGemStipendClaimedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} claimed monthly gem stipend: {Gems} gems", playerId, MonthlyGemStipend);

        return new GemStipendClaimResponse
        {
            GemsAwarded = MonthlyGemStipend,
            TotalGems = player.Gems,
            NextClaimAvailable = DateTime.UtcNow.AddDays(30)
        };
    }

    public async Task<BadgeResponse> SetBadgeAsync(Guid playerId, string? badge)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        if (badge != null)
        {
            var available = GetBadgesForTier(player.CurrentTier);
            if (!available.Contains(badge))
                throw new InvalidOperationException(
                    $"Badge '{badge}' is not available for your tier. Available: {string.Join(", ", available)}");
        }

        player.Badge = badge;
        await _context.SaveChangesAsync();

        return new BadgeResponse
        {
            Badge = player.Badge,
            Message = badge == null ? "Badge removed." : $"Badge set to '{badge}'."
        };
    }

    public Task<List<string>> GetAvailableBadgesAsync(Guid playerId)
    {
        // We need to look up the player synchronously... let's just do it properly
        return GetAvailableBadgesInternalAsync(playerId);
    }

    private async Task<List<string>> GetAvailableBadgesInternalAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        return GetBadgesForTier(player.CurrentTier);
    }

    private static List<string> GetBadgesForTier(SubscriptionTier tier) =>
        BadgesByTier.TryGetValue(tier, out var badges) ? badges : new() { "newcomer" };

    public static decimal GetGoldMultiplier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 1.0m,
        SubscriptionTier.Premium => 1.5m,
        SubscriptionTier.PremiumPlus => 2.0m,
        _ => 1.0m
    };

    public static decimal GetXpMultiplier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 1.0m,
        SubscriptionTier.Premium => 1.0m,
        SubscriptionTier.PremiumPlus => 1.5m,
        _ => 1.0m
    };
}
