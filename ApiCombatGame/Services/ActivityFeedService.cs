using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Activity;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ActivityFeedService : IActivityFeedService
{
    private readonly GameDbContext _context;
    private readonly ILogger<ActivityFeedService> _logger;

    public ActivityFeedService(GameDbContext context, ILogger<ActivityFeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(Guid playerId, string activityType, string description,
        Guid? relatedEntityId = null, string? metadataJson = null, bool isPublic = true)
    {
        var entry = new ActivityFeedEntry
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            ActivityType = activityType,
            Description = description,
            RelatedEntityId = relatedEntityId,
            MetadataJson = metadataJson,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ActivityFeedEntry>().Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<ActivityFeedResponse> GetFeedAsync(Guid playerId, int page = 1, int pageSize = 20)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        var query = _context.Set<ActivityFeedEntry>()
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();

        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityFeedItem
            {
                Id = a.Id,
                ActivityType = a.ActivityType,
                Description = a.Description,
                RelatedEntityId = a.RelatedEntityId,
                MetadataJson = a.MetadataJson,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new ActivityFeedResponse
        {
            Username = player.Username,
            Activities = activities,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ActivityFeedResponse> GetPublicFeedAsync(string username, int page = 1, int pageSize = 20)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == username)
            ?? throw new KeyNotFoundException($"Player '{username}' not found.");

        var query = _context.Set<ActivityFeedEntry>()
            .AsNoTracking()
            .Where(a => a.PlayerId == player.Id && a.IsPublic)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();

        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityFeedItem
            {
                Id = a.Id,
                ActivityType = a.ActivityType,
                Description = a.Description,
                RelatedEntityId = a.RelatedEntityId,
                MetadataJson = a.MetadataJson,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new ActivityFeedResponse
        {
            Username = player.Username,
            Activities = activities,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PlayerLifetimeStatsResponse> GetLifetimeStatsAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        return await BuildStatsResponse(player);
    }

    public async Task<PlayerLifetimeStatsResponse> GetPublicStatsAsync(string username)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == username)
            ?? throw new KeyNotFoundException($"Player '{username}' not found.");

        return await BuildStatsResponse(player);
    }

    public async Task RecordBattleStatsAsync(Guid playerId, bool won, int goldEarned, int xpEarned, int newRating)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        player.TotalBattlesPlayed++;
        if (won) player.TotalBattlesWon++;
        player.TotalGoldEarned += goldEarned;
        player.TotalXpEarned += xpEarned;
        if (newRating > player.HighestRating) player.HighestRating = newRating;
        if (player.WinStreak > player.HighestWinStreak) player.HighestWinStreak = player.WinStreak;

        await _context.SaveChangesAsync();
    }

    private async Task<PlayerLifetimeStatsResponse> BuildStatsResponse(Player player)
    {
        var achievementCount = await _context.Set<PlayerAchievement>()
            .CountAsync(a => a.PlayerId == player.Id);

        var cosmeticCount = await _context.Set<PlayerCosmetic>()
            .CountAsync(c => c.PlayerId == player.Id);

        var daysPlayed = (int)(DateTime.UtcNow - player.CreatedAt).TotalDays + 1;

        int totalLost = player.TotalBattlesPlayed - player.TotalBattlesWon;
        double winRate = player.TotalBattlesPlayed > 0
            ? Math.Round((double)player.TotalBattlesWon / player.TotalBattlesPlayed * 100, 1)
            : 0;

        string summary = BuildInvestmentSummary(player, daysPlayed);

        return new PlayerLifetimeStatsResponse
        {
            Username = player.Username,
            Level = player.Level,
            Rating = player.Rating,
            CurrentTier = player.CurrentTier.ToString(),
            Badge = player.Badge,
            TotalBattlesPlayed = player.TotalBattlesPlayed,
            TotalBattlesWon = player.TotalBattlesWon,
            TotalBattlesLost = totalLost,
            WinRate = winRate,
            CurrentWinStreak = player.WinStreak,
            HighestWinStreak = player.HighestWinStreak,
            HighestRating = player.HighestRating,
            TotalGoldEarned = player.TotalGoldEarned,
            CurrentGold = player.Currency,
            TotalXpEarned = player.TotalXpEarned,
            GemsEarnedTotal = player.GemsEarnedTotal,
            GemsSpentTotal = player.GemsSpentTotal,
            UnitsUnlocked = player.Roster?.Count ?? 0,
            TeamsCreated = player.Teams?.Count ?? 0,
            AchievementsUnlocked = achievementCount,
            AchievementPoints = player.AchievementPoints,
            CosmeticsOwned = cosmeticCount,
            LoginStreak = player.LoginStreak,
            DaysPlayed = daysPlayed,
            MemberSince = player.CreatedAt,
            InvestmentSummary = summary
        };
    }

    private static string BuildInvestmentSummary(Player player, int daysPlayed)
    {
        var parts = new List<string>();

        if (player.TotalBattlesPlayed > 0)
            parts.Add($"{player.TotalBattlesPlayed} battles fought");
        if (player.TotalGoldEarned > 0)
            parts.Add($"{player.TotalGoldEarned:N0}g earned");
        if (player.Level > 1)
            parts.Add($"reached level {player.Level}");
        if (daysPlayed > 1)
            parts.Add($"playing for {daysPlayed} days");

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Just getting started!";
    }
}
