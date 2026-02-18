using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class PlayerAnalyticsService : IPlayerAnalyticsService
{
    private readonly GameDbContext _context;

    public PlayerAnalyticsService(GameDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerAnalyticsViewModel> GetPlayerAnalyticsAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException("Player not found");

        var battles = await _context.Battles
            .Where(b => (b.Player1Id == playerId || b.Player2Id == playerId) &&
                       b.Status == BattleStatus.Completed)
            .OrderByDescending(b => b.CompletedAt)
            .Include(b => b.Player1)
            .Include(b => b.Player2)
            .ToListAsync();

        var viewModel = new PlayerAnalyticsViewModel
        {
            TotalBattles = battles.Count
        };

        if (battles.Count == 0)
            return viewModel;

        // Calculate wins and losses
        var wins = battles.Where(b => b.WinnerId == playerId).ToList();
        var losses = battles.Where(b => b.WinnerId != null && b.WinnerId != playerId).ToList();

        viewModel.TotalWins = wins.Count;
        viewModel.TotalLosses = losses.Count;
        viewModel.WinRate = battles.Count > 0 ? (decimal)wins.Count / battles.Count * 100 : 0;

        // Calculate streaks
        CalculateStreaks(battles, playerId, viewModel);

        // Performance metrics
        viewModel.AverageTurnsPerBattle = battles.Count > 0 ? (decimal)battles.Average(b => b.Turns) : 0;

        var player1Wins = wins.Where(b => b.Player1Id == playerId && b.Player1RatingChange.HasValue).ToList();
        var player2Wins = wins.Where(b => b.Player2Id == playerId && b.Player2RatingChange.HasValue).ToList();
        var allWinRatingChanges = player1Wins.Select(b => b.Player1RatingChange!.Value)
            .Concat(player2Wins.Select(b => b.Player2RatingChange!.Value)).ToList();

        viewModel.AverageRatingChangePerWin = allWinRatingChanges.Count > 0
            ? (decimal)allWinRatingChanges.Average()
            : 0;

        var player1Losses = losses.Where(b => b.Player1Id == playerId && b.Player1RatingChange.HasValue).ToList();
        var player2Losses = losses.Where(b => b.Player2Id == playerId && b.Player2RatingChange.HasValue).ToList();
        var allLossRatingChanges = player1Losses.Select(b => b.Player1RatingChange!.Value)
            .Concat(player2Losses.Select(b => b.Player2RatingChange!.Value)).ToList();

        viewModel.AverageRatingChangePerLoss = allLossRatingChanges.Count > 0
            ? (decimal)allLossRatingChanges.Average()
            : 0;

        var allRatingChanges = allWinRatingChanges.Concat(allLossRatingChanges).ToList();
        viewModel.HighestRatingChange = allRatingChanges.Count > 0 ? allRatingChanges.Max() : 0;
        viewModel.LowestRatingChange = allRatingChanges.Count > 0 ? allRatingChanges.Min() : 0;

        // Total currency earned
        viewModel.TotalCurrencyEarned = battles.Where(b => b.CurrencyReward.HasValue)
            .Sum(b => b.CurrencyReward!.Value);

        // Time-based analytics
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        viewModel.BattlesToday = battles.Count(b => b.CompletedAt >= todayStart);
        viewModel.BattlesThisWeek = battles.Count(b => b.CompletedAt >= weekStart);
        viewModel.BattlesThisMonth = battles.Count(b => b.CompletedAt >= monthStart);

        viewModel.WinsToday = wins.Count(b => b.CompletedAt >= todayStart);
        viewModel.WinsThisWeek = wins.Count(b => b.CompletedAt >= weekStart);
        viewModel.WinsThisMonth = wins.Count(b => b.CompletedAt >= monthStart);

        // Most used classes
        var classUsage = new Dictionary<string, int>();
        foreach (var battle in battles)
        {
            var teamClassesJson = battle.Player1Id == playerId
                ? battle.Team1ClassesJson
                : battle.Team2ClassesJson;

            if (string.IsNullOrEmpty(teamClassesJson)) continue;

            try
            {
                var classes = JsonSerializer.Deserialize<List<string>>(teamClassesJson);
                if (classes != null)
                {
                    foreach (var className in classes)
                    {
                        if (!classUsage.ContainsKey(className))
                            classUsage[className] = 0;
                        classUsage[className]++;
                    }
                }
            }
            catch
            {
                // Skip invalid JSON
            }
        }

        var totalClassUsages = classUsage.Values.Sum();
        viewModel.MostUsedClasses = classUsage
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => new ClassUsageData
            {
                ClassName = kvp.Key,
                TimesUsed = kvp.Value,
                Percentage = totalClassUsages > 0 ? (decimal)kvp.Value / totalClassUsages * 100 : 0
            })
            .ToList();

        // Recent battles (last 20)
        viewModel.RecentBattles = battles.Take(20)
            .Select(b => new BattleResultData
            {
                CompletedAt = b.CompletedAt!.Value,
                IsWin = b.WinnerId == playerId,
                RatingChange = b.Player1Id == playerId
                    ? b.Player1RatingChange ?? 0
                    : b.Player2RatingChange ?? 0,
                Mode = b.Mode,
                Turns = b.Turns,
                OpponentName = b.Player1Id == playerId
                    ? (b.Player2?.Username ?? "Unknown")
                    : b.Player1.Username
            })
            .ToList();

        // Win rate by class
        var classWinRates = new Dictionary<string, (int wins, int losses)>();
        foreach (var battle in battles)
        {
            var isPlayer1 = battle.Player1Id == playerId;
            var teamClassesJson = isPlayer1 ? battle.Team1ClassesJson : battle.Team2ClassesJson;

            if (string.IsNullOrEmpty(teamClassesJson)) continue;

            try
            {
                var classes = JsonSerializer.Deserialize<List<string>>(teamClassesJson);
                if (classes != null)
                {
                    var isWin = battle.WinnerId == playerId;
                    foreach (var className in classes.Distinct())
                    {
                        if (!classWinRates.ContainsKey(className))
                            classWinRates[className] = (0, 0);

                        var (currentWins, currentLosses) = classWinRates[className];
                        classWinRates[className] = isWin
                            ? (currentWins + 1, currentLosses)
                            : (currentWins, currentLosses + 1);
                    }
                }
            }
            catch
            {
                // Skip invalid JSON
            }
        }

        viewModel.WinRateByClass = classWinRates
            .Select(kvp => new ClassWinRateData
            {
                ClassName = kvp.Key,
                Wins = kvp.Value.wins,
                Losses = kvp.Value.losses,
                TotalBattles = kvp.Value.wins + kvp.Value.losses,
                WinRate = (kvp.Value.wins + kvp.Value.losses) > 0
                    ? (decimal)kvp.Value.wins / (kvp.Value.wins + kvp.Value.losses) * 100
                    : 0
            })
            .OrderByDescending(c => c.TotalBattles)
            .ToList();

        // Peak performance
        var battlesByDay = battles
            .GroupBy(b => b.CompletedAt!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalBattles = g.Count(),
                Wins = g.Count(b => b.WinnerId == playerId)
            })
            .ToList();

        if (battlesByDay.Count > 0)
        {
            var bestDay = battlesByDay.OrderByDescending(d => d.Wins).First();
            viewModel.BestDayDate = bestDay.Date;
            viewModel.BestDayWins = bestDay.Wins;

            var mostActiveDay = battlesByDay.OrderByDescending(d => d.TotalBattles).First();
            viewModel.MostActiveDayDate = mostActiveDay.Date;
            viewModel.MostActiveDayBattles = mostActiveDay.TotalBattles;
        }

        return viewModel;
    }

    private void CalculateStreaks(List<Battle> battles, Guid playerId, PlayerAnalyticsViewModel viewModel)
    {
        if (battles.Count == 0) return;

        var orderedBattles = battles.OrderBy(b => b.CompletedAt).ToList();

        int currentWinStreak = 0;
        int currentLossStreak = 0;
        int longestWinStreak = 0;
        int longestLossStreak = 0;

        foreach (var battle in orderedBattles)
        {
            var isWin = battle.WinnerId == playerId;

            if (isWin)
            {
                currentWinStreak++;
                currentLossStreak = 0;
                longestWinStreak = Math.Max(longestWinStreak, currentWinStreak);
            }
            else
            {
                currentLossStreak++;
                currentWinStreak = 0;
                longestLossStreak = Math.Max(longestLossStreak, currentLossStreak);
            }
        }

        viewModel.CurrentWinStreak = currentWinStreak;
        viewModel.CurrentLossStreak = currentLossStreak;
        viewModel.LongestWinStreak = longestWinStreak;
        viewModel.LongestLossStreak = longestLossStreak;
    }
}
