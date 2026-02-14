using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Season;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class SeasonService : ISeasonService
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progression;
    private readonly INotificationService _notifications;
    private readonly ILogger<SeasonService> _logger;

    // Rating thresholds for each tier
    public static readonly Dictionary<SeasonTier, int> TierThresholds = new()
    {
        [SeasonTier.Bronze] = 0,
        [SeasonTier.Silver] = 1000,
        [SeasonTier.Gold] = 1200,
        [SeasonTier.Platinum] = 1400,
        [SeasonTier.Diamond] = 1600,
        [SeasonTier.Legend] = 1800
    };

    // End-of-season rewards by peak tier
    private static readonly Dictionary<SeasonTier, (int Gold, int Xp, string? Title)> TierRewards = new()
    {
        [SeasonTier.Bronze] = (100, 50, null),
        [SeasonTier.Silver] = (250, 100, null),
        [SeasonTier.Gold] = (500, 200, "Gold Gladiator"),
        [SeasonTier.Platinum] = (1000, 400, "Platinum Warrior"),
        [SeasonTier.Diamond] = (2000, 800, "Diamond Champion"),
        [SeasonTier.Legend] = (5000, 1500, "Legendary Conqueror")
    };

    public SeasonService(
        GameDbContext context,
        IPlayerProgressionService progression,
        INotificationService notifications,
        ILogger<SeasonService> logger)
    {
        _context = context;
        _progression = progression;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task EnsureActiveSeasonAsync()
    {
        var activeSeason = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);

        if (activeSeason != null)
        {
            // Check if current season has ended
            if (DateTime.UtcNow > activeSeason.EndDate)
            {
                activeSeason.IsActive = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Season '{Name}' ended", activeSeason.Name);
                activeSeason = null;
            }
            else
            {
                return; // Active season still running
            }
        }

        // Create a new season
        var lastSeason = await _context.Seasons
            .OrderByDescending(s => s.SeasonNumber)
            .FirstOrDefaultAsync();

        int nextNumber = (lastSeason?.SeasonNumber ?? 0) + 1;
        var seasonNames = new[] { "Dawn of Battle", "Rising Storm", "Iron Conquest", "Shadow War", "Dragon's Fury", "Eternal Clash" };
        var seasonName = $"Season {nextNumber}: {seasonNames[(nextNumber - 1) % seasonNames.Length]}";

        var newSeason = new Season
        {
            Id = Guid.NewGuid(),
            Name = seasonName,
            SeasonNumber = nextNumber,
            IsActive = true,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(56), // 8-week seasons
            CreatedAt = DateTime.UtcNow
        };

        _context.Seasons.Add(newSeason);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New season created: '{Name}' (ends {EndDate})", newSeason.Name, newSeason.EndDate);
    }

    public async Task<CurrentSeasonResponse> GetCurrentSeasonAsync(Guid playerId)
    {
        await EnsureActiveSeasonAsync();

        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season found.");

        // Get or create player's season rank
        var rank = await GetOrCreateRankAsync(playerId, season.Id);

        int daysRemaining = Math.Max(0, (int)(season.EndDate - DateTime.UtcNow).TotalDays);
        int totalGames = rank.Wins + rank.Losses + rank.Draws;
        decimal winRate = totalGames > 0 ? Math.Round((decimal)rank.Wins / totalGames * 100, 1) : 0;

        // Calculate next tier info
        var nextTier = GetNextTier(rank.CurrentTier);
        int? ratingToNext = nextTier.HasValue ? TierThresholds[nextTier.Value] - rank.SeasonRating : null;

        return new CurrentSeasonResponse
        {
            SeasonId = season.Id,
            Name = season.Name,
            SeasonNumber = season.SeasonNumber,
            StartDate = season.StartDate,
            EndDate = season.EndDate,
            DaysRemaining = daysRemaining,
            CurrentTier = rank.CurrentTier.ToString(),
            SeasonRating = rank.SeasonRating,
            PeakRating = rank.PeakRating,
            PeakTier = rank.PeakTier.ToString(),
            Wins = rank.Wins,
            Losses = rank.Losses,
            Draws = rank.Draws,
            WinRate = winRate,
            RatingToNextTier = ratingToNext > 0 ? ratingToNext : null,
            NextTier = nextTier?.ToString(),
            TierThresholds = TierThresholds.ToDictionary(t => t.Key.ToString(), t => t.Value)
        };
    }

    public async Task<SeasonLeaderboardResponse> GetSeasonLeaderboardAsync(Guid playerId, int limit, int offset)
    {
        await EnsureActiveSeasonAsync();

        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season found.");

        var totalPlayers = await _context.PlayerSeasonRanks
            .CountAsync(r => r.SeasonId == season.Id && (r.Wins + r.Losses + r.Draws) > 0);

        var rankings = await _context.PlayerSeasonRanks
            .Include(r => r.Player)
            .Where(r => r.SeasonId == season.Id && (r.Wins + r.Losses + r.Draws) > 0)
            .OrderByDescending(r => r.SeasonRating)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var entries = rankings.Select((r, index) =>
        {
            int total = r.Wins + r.Losses + r.Draws;
            return new SeasonLeaderboardEntry
            {
                Rank = offset + index + 1,
                PlayerId = r.PlayerId,
                Username = r.Player.Username,
                Tier = r.CurrentTier.ToString(),
                Rating = r.SeasonRating,
                Wins = r.Wins,
                Losses = r.Losses,
                WinRate = total > 0 ? Math.Round((decimal)r.Wins / total * 100, 1) : 0
            };
        }).ToList();

        // Find player's own rank
        int? yourRank = null;
        var playerRank = await _context.PlayerSeasonRanks
            .FirstOrDefaultAsync(r => r.SeasonId == season.Id && r.PlayerId == playerId);
        if (playerRank != null && (playerRank.Wins + playerRank.Losses + playerRank.Draws) > 0)
        {
            yourRank = await _context.PlayerSeasonRanks
                .CountAsync(r => r.SeasonId == season.Id
                    && (r.Wins + r.Losses + r.Draws) > 0
                    && r.SeasonRating > playerRank.SeasonRating) + 1;
        }

        return new SeasonLeaderboardResponse
        {
            SeasonName = season.Name,
            Rankings = entries,
            TotalPlayers = totalPlayers,
            YourRank = yourRank
        };
    }

    public async Task<SeasonRewardsResponse> ClaimSeasonRewardsAsync(Guid playerId, Guid seasonId)
    {
        var season = await _context.Seasons.FindAsync(seasonId)
            ?? throw new KeyNotFoundException("Season not found.");

        if (season.IsActive)
            throw new InvalidOperationException("Cannot claim rewards for an active season. Wait until the season ends.");

        var rank = await _context.PlayerSeasonRanks
            .FirstOrDefaultAsync(r => r.PlayerId == playerId && r.SeasonId == seasonId)
            ?? throw new KeyNotFoundException("You have no ranking in this season.");

        if (rank.RewardsClaimed)
            throw new InvalidOperationException("Rewards already claimed for this season.");

        var (gold, xp, title) = TierRewards[rank.PeakTier];

        // Award rewards
        await _progression.AwardCurrencyAsync(playerId, gold);
        await _progression.AwardExperienceAsync(playerId, xp);

        // Grant exclusive title if earned
        if (title != null)
        {
            var existingTitle = await _context.PlayerTitles.FirstOrDefaultAsync(t => t.Name == title);
            if (existingTitle == null)
            {
                existingTitle = new PlayerTitle
                {
                    Id = Guid.NewGuid(),
                    Name = title,
                    Description = $"Achieved {rank.PeakTier} tier in {season.Name}",
                    ColorHex = rank.PeakTier >= SeasonTier.Diamond ? "#FFD700" : "#C0C0C0"
                };
                _context.PlayerTitles.Add(existingTitle);
            }

            // Check if player already has this title
            var player = await _context.Players.FindAsync(playerId);
            if (player != null && player.ActiveTitleId == null)
            {
                player.ActiveTitleId = existingTitle.Id;
            }
        }

        rank.RewardsClaimed = true;
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.SeasonReward,
            $"{season.Name} Rewards Claimed!",
            $"Peak tier: {rank.PeakTier}. Earned {gold}g + {xp} XP" + (title != null ? $" + \"{title}\" title!" : "!"));

        return new SeasonRewardsResponse
        {
            SeasonName = season.Name,
            PeakTier = rank.PeakTier.ToString(),
            GoldReward = gold,
            XpReward = xp,
            ExclusiveTitle = title,
            Claimed = true
        };
    }

    public async Task UpdateSeasonRatingAsync(Guid playerId, int ratingChange, bool won, bool isDraw)
    {
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
        if (season == null) return; // No active season, skip

        var rank = await GetOrCreateRankAsync(playerId, season.Id);

        rank.SeasonRating += ratingChange;
        if (rank.SeasonRating < 100) rank.SeasonRating = 100;

        if (won) rank.Wins++;
        else if (isDraw) rank.Draws++;
        else rank.Losses++;

        // Update peak
        if (rank.SeasonRating > rank.PeakRating)
            rank.PeakRating = rank.SeasonRating;

        // Recalculate tier
        var oldTier = rank.CurrentTier;
        rank.CurrentTier = CalculateTier(rank.SeasonRating);

        if (rank.CurrentTier > rank.PeakTier)
            rank.PeakTier = rank.CurrentTier;

        rank.LastRankedBattleAt = DateTime.UtcNow;

        // Notify on tier promotion
        if (rank.CurrentTier > oldTier)
        {
            await _notifications.SendAsync(playerId, NotificationType.SeasonRankChange,
                $"Promoted to {rank.CurrentTier}!",
                $"You've reached {rank.CurrentTier} tier in {season.Name}! Rating: {rank.SeasonRating}");
        }
        else if (rank.CurrentTier < oldTier)
        {
            await _notifications.SendAsync(playerId, NotificationType.SeasonRankChange,
                $"Demoted to {rank.CurrentTier}",
                $"You've dropped to {rank.CurrentTier} tier. Rating: {rank.SeasonRating}. Fight back!");
        }

        await _context.SaveChangesAsync();
    }

    private async Task<PlayerSeasonRank> GetOrCreateRankAsync(Guid playerId, Guid seasonId)
    {
        var rank = await _context.PlayerSeasonRanks
            .FirstOrDefaultAsync(r => r.PlayerId == playerId && r.SeasonId == seasonId);

        if (rank != null) return rank;

        // Initialize with player's current global rating
        var player = await _context.Players.FindAsync(playerId);
        int startingRating = player?.Rating ?? 1000;

        rank = new PlayerSeasonRank
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            SeasonId = seasonId,
            SeasonRating = startingRating,
            PeakRating = startingRating,
            CurrentTier = CalculateTier(startingRating),
            PeakTier = CalculateTier(startingRating)
        };

        _context.PlayerSeasonRanks.Add(rank);
        await _context.SaveChangesAsync();
        return rank;
    }

    private static SeasonTier CalculateTier(int rating)
    {
        if (rating >= 1800) return SeasonTier.Legend;
        if (rating >= 1600) return SeasonTier.Diamond;
        if (rating >= 1400) return SeasonTier.Platinum;
        if (rating >= 1200) return SeasonTier.Gold;
        if (rating >= 1000) return SeasonTier.Silver;
        return SeasonTier.Bronze;
    }

    private static SeasonTier? GetNextTier(SeasonTier current)
    {
        return current switch
        {
            SeasonTier.Bronze => SeasonTier.Silver,
            SeasonTier.Silver => SeasonTier.Gold,
            SeasonTier.Gold => SeasonTier.Platinum,
            SeasonTier.Platinum => SeasonTier.Diamond,
            SeasonTier.Diamond => SeasonTier.Legend,
            SeasonTier.Legend => null,
            _ => null
        };
    }
}
