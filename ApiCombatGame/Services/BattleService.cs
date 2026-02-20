using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class BattleService : IBattleService
{
    private readonly GameDbContext _context;
    private readonly IStrategyEngine _strategyEngine;
    private readonly IMatchmakingService _matchmaking;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IAchievementService _achievementService;
    private readonly ISeasonService _seasonService;
    private readonly ILootService _lootService;
    private readonly IRivalService _rivalService;
    private readonly IBattlePassService _battlePassService;
    private readonly IGuildWarService _guildWarService;
    private readonly IActivityFeedService _activityFeedService;
    private readonly IActivityLedger _ledger;
    private readonly IConfiguration _config;
    private readonly INotificationService _notifications;
    private readonly ILogger<BattleService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BattleService(
        GameDbContext context,
        IStrategyEngine strategyEngine,
        IMatchmakingService matchmaking,
        IPlayerProgressionService progressionService,
        IAchievementService achievementService,
        ISeasonService seasonService,
        ILootService lootService,
        IRivalService rivalService,
        IBattlePassService battlePassService,
        IGuildWarService guildWarService,
        IActivityFeedService activityFeedService,
        IActivityLedger ledger,
        INotificationService notifications,
        IConfiguration config,
        ILogger<BattleService> logger)
    {
        _achievementService = achievementService;
        _seasonService = seasonService;
        _lootService = lootService;
        _rivalService = rivalService;
        _battlePassService = battlePassService;
        _guildWarService = guildWarService;
        _activityFeedService = activityFeedService;
        _ledger = ledger;
        _notifications = notifications;
        _context = context;
        _strategyEngine = strategyEngine;
        _matchmaking = matchmaking;
        _progressionService = progressionService;
        _config = config;
        _logger = logger;
    }

    public async Task<BattleStatusResponse> QueueBattleAsync(Guid playerId, BattleQueueRequest request)
    {
        // Validate team ownership
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId && t.PlayerId == playerId);
        if (team == null)
            throw new InvalidOperationException("Team not found or doesn't belong to you.");

        var unitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson, JsonOptions) ?? new();
        if (unitIds.Count == 0)
            throw new InvalidOperationException("Team has no units configured.");

        // Check player doesn't already have a queued battle
        var existingQueued = await _context.Battles
            .AnyAsync(b => b.Player1Id == playerId && b.Status == BattleStatus.Queued);
        if (existingQueued)
            throw new InvalidOperationException("You already have a battle in the queue.");

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = playerId,
            Team1Id = request.TeamId,
            Status = BattleStatus.Queued,
            Mode = request.Mode ?? "ranked",
            QueuedAt = DateTime.UtcNow
        };

        _context.Battles.Add(battle);
        await _context.SaveChangesAsync();

        // Count queue position
        var queuePosition = await _context.Battles
            .CountAsync(b => b.Status == BattleStatus.Queued && b.QueuedAt <= battle.QueuedAt);

        _logger.LogInformation("Player {PlayerId} queued for battle with team {TeamId}", playerId, request.TeamId);

        return new BattleStatusResponse
        {
            BattleId = battle.Id,
            Status = "queued",
            QueuePosition = queuePosition,
            EstimatedWaitSeconds = queuePosition * 5,
            QueuedAt = battle.QueuedAt
        };
    }

    public async Task<BattleStatusResponse> GetBattleStatusAsync(Guid battleId, Guid playerId)
    {
        var battle = await _context.Battles
            .FirstOrDefaultAsync(b => b.Id == battleId && (b.Player1Id == playerId || b.Player2Id == playerId));

        if (battle == null)
            throw new KeyNotFoundException("Battle not found.");

        int? queuePosition = null;
        int? estimatedWait = null;

        if (battle.Status == BattleStatus.Queued)
        {
            queuePosition = await _context.Battles
                .CountAsync(b => b.Status == BattleStatus.Queued && b.QueuedAt <= battle.QueuedAt);
            estimatedWait = queuePosition * 5;
        }

        return new BattleStatusResponse
        {
            BattleId = battle.Id,
            Status = battle.Status.ToString().ToLower(),
            QueuePosition = queuePosition,
            EstimatedWaitSeconds = estimatedWait,
            QueuedAt = battle.QueuedAt,
            StartedAt = battle.StartedAt,
            CompletedAt = battle.CompletedAt
        };
    }

    public async Task<BattleResultResponse> GetBattleResultAsync(Guid battleId, Guid playerId)
    {
        var battle = await _context.Battles
            .FirstOrDefaultAsync(b => b.Id == battleId && (b.Player1Id == playerId || b.Player2Id == playerId));

        if (battle == null)
            throw new KeyNotFoundException("Battle not found.");

        var logEntries = JsonSerializer.Deserialize<List<BattleLogEntry>>(battle.BattleLogJson, JsonOptions)
            ?? new List<BattleLogEntry>();

        Guid? loserId = null;
        if (battle.WinnerId.HasValue && battle.Player2Id.HasValue)
        {
            loserId = battle.WinnerId == battle.Player1Id ? battle.Player2Id : battle.Player1Id;
        }

        int ratingChange = 0;
        if (battle.WinnerId == playerId)
            ratingChange = battle.Player1Id == playerId ? (battle.Player1RatingChange ?? 0) : (battle.Player2RatingChange ?? 0);
        else if (battle.WinnerId.HasValue)
            ratingChange = battle.Player1Id == playerId ? (battle.Player1RatingChange ?? 0) : (battle.Player2RatingChange ?? 0);

        return new BattleResultResponse
        {
            BattleId = battle.Id,
            Status = battle.Status.ToString().ToLower(),
            WinnerId = battle.WinnerId,
            LoserId = loserId,
            Turns = battle.Turns,
            BattleLog = logEntries,
            Rewards = battle.Status == BattleStatus.Completed ? new BattleRewards
            {
                Currency = battle.CurrencyReward ?? 0,
                RatingChange = ratingChange
            } : null,
            CompletedAt = battle.CompletedAt
        };
    }

    public async Task<List<BattleResultResponse>> GetBattleHistoryAsync(Guid playerId, int limit, int offset)
    {
        var battles = await _context.Battles
            .Where(b => (b.Player1Id == playerId || b.Player2Id == playerId) && b.Status == BattleStatus.Completed)
            .OrderByDescending(b => b.CompletedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return battles.Select(battle =>
        {
            Guid? loserId = null;
            if (battle.WinnerId.HasValue && battle.Player2Id.HasValue)
                loserId = battle.WinnerId == battle.Player1Id ? battle.Player2Id : battle.Player1Id;

            int ratingChange = battle.Player1Id == playerId
                ? (battle.Player1RatingChange ?? 0)
                : (battle.Player2RatingChange ?? 0);

            return new BattleResultResponse
            {
                BattleId = battle.Id,
                Status = "completed",
                WinnerId = battle.WinnerId,
                LoserId = loserId,
                Turns = battle.Turns,
                BattleLog = new List<BattleLogEntry>(), // Omit log in history for brevity
                Rewards = new BattleRewards
                {
                    Currency = battle.CurrencyReward ?? 0,
                    RatingChange = ratingChange
                },
                CompletedAt = battle.CompletedAt
            };
        }).ToList();
    }

    public async Task ProcessQueuedBattlesAsync(CancellationToken cancellationToken)
    {
        var match = await _matchmaking.FindMatchAsync();
        if (match == null) return;

        var (battle1, battle2) = match.Value;

        try
        {
            // Merge into single battle record
            battle1.Player2Id = battle2.Player1Id;
            battle1.Team2Id = battle2.Team1Id;
            battle1.Status = BattleStatus.InProgress;
            battle1.StartedAt = DateTime.UtcNow;

            // Remove the second queue entry
            battle2.Status = BattleStatus.Cancelled;

            await _context.SaveChangesAsync(cancellationToken);

            // Load teams and units
            var team1 = await _context.Teams.FindAsync(new object[] { battle1.Team1Id }, cancellationToken);
            var team2 = await _context.Teams.FindAsync(new object[] { battle1.Team2Id!.Value }, cancellationToken);

            if (team1 == null || team2 == null)
            {
                battle1.Status = BattleStatus.Cancelled;
                await RefundDailyBattleUsageAsync(battle1.Player1Id, cancellationToken);
                if (battle1.Player2Id.HasValue)
                    await RefundDailyBattleUsageAsync(battle1.Player2Id.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var team1UnitIds = JsonSerializer.Deserialize<List<Guid>>(team1.UnitIdsJson, JsonOptions) ?? new();
            var team2UnitIds = JsonSerializer.Deserialize<List<Guid>>(team2.UnitIdsJson, JsonOptions) ?? new();

            var team1Units = await _context.Units
                .Include(u => u.Abilities)
                .Where(u => team1UnitIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var team2Units = await _context.Units
                .Include(u => u.Abilities)
                .Where(u => team2UnitIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            if (team1Units.Count == 0 || team2Units.Count == 0 ||
                team1Units.Any(u => !u.Abilities.Any()) || team2Units.Any(u => !u.Abilities.Any()))
            {
                _logger.LogError("Battle {BattleId} cancelled: units missing or have no abilities (T1: {T1Count} units, T2: {T2Count} units)",
                    battle1.Id, team1Units.Count, team2Units.Count);
                battle1.Status = BattleStatus.Cancelled;
                await RefundDailyBattleUsageAsync(battle1.Player1Id, cancellationToken);
                if (battle1.Player2Id.HasValue)
                    await RefundDailyBattleUsageAsync(battle1.Player2Id.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Notify affected players about the cancellation
                try
                {
                    await _notifications.SendAsync(battle1.Player1Id, NotificationType.BattleCancelled,
                        "Battle Cancelled", "Your battle was cancelled because team units were unavailable. Please reconfigure your team.");
                    if (battle1.Player2Id.HasValue)
                        await _notifications.SendAsync(battle1.Player2Id.Value, NotificationType.BattleCancelled,
                            "Battle Cancelled", "Your battle was cancelled because the opponent's team was unavailable.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send battle cancellation notification for {BattleId}", battle1.Id);
                }

                return;
            }

            // Store team class compositions for challenge checking
            battle1.Team1ClassesJson = JsonSerializer.Serialize(
                team1Units.Select(u => u.Class.ToString()).ToList(), JsonOptions);
            battle1.Team2ClassesJson = JsonSerializer.Serialize(
                team2Units.Select(u => u.Class.ToString()).ToList(), JsonOptions);

            var strategy1 = !string.IsNullOrEmpty(team1.StrategyJson) && team1.StrategyJson != "{}"
                ? JsonSerializer.Deserialize<StrategyConfig>(team1.StrategyJson, JsonOptions) ?? new StrategyConfig()
                : new StrategyConfig();

            var strategy2 = !string.IsNullOrEmpty(team2.StrategyJson) && team2.StrategyJson != "{}"
                ? JsonSerializer.Deserialize<StrategyConfig>(team2.StrategyJson, JsonOptions) ?? new StrategyConfig()
                : new StrategyConfig();

            int maxTurns = _config.GetValue<int>("GameSettings:MaxTurnsPerBattle", 50);

            // Resolve battle
            var resolution = _strategyEngine.ResolveBattle(team1Units, strategy1, team2Units, strategy2, maxTurns);

            // Update battle record
            battle1.Turns = resolution.TotalTurns;
            battle1.BattleLogJson = JsonSerializer.Serialize(resolution.Log, JsonOptions);
            battle1.Status = BattleStatus.Completed;
            battle1.CompletedAt = DateTime.UtcNow;

            // Determine winner and update ratings + rewards
            if (resolution.WinnerTeam == 1)
            {
                battle1.WinnerId = battle1.Player1Id;
                await UpdateRatingsAndRewards(battle1.Player1Id, battle1.Player2Id!.Value, battle1, cancellationToken);
            }
            else if (resolution.WinnerTeam == 2)
            {
                battle1.WinnerId = battle1.Player2Id;
                await UpdateRatingsAndRewards(battle1.Player2Id!.Value, battle1.Player1Id, battle1, cancellationToken);
            }
            else
            {
                // Draw - process rewards for both
                battle1.WinnerId = null;
                battle1.Player1RatingChange = 0;
                battle1.Player2RatingChange = 0;
                var p1Rewards = await _progressionService.ProcessBattleRewardsAsync(battle1.Player1Id, false, 0);
                var p2Rewards = await _progressionService.ProcessBattleRewardsAsync(battle1.Player2Id!.Value, false, 0);
                battle1.CurrencyReward = p1Rewards.GoldEarned;

                // Update season rankings for draws
                if (battle1.Mode == "ranked")
                {
                    try
                    {
                        await _seasonService.UpdateSeasonRatingAsync(battle1.Player1Id, 0, false, true);
                        await _seasonService.UpdateSeasonRatingAsync(battle1.Player2Id!.Value, 0, false, true);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Season update failed for draw battle {BattleId}", battle1.Id); }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Auto-create replay so HATEOAS links work immediately
            try
            {
                var replay = new BattleReplay
                {
                    Id = Guid.NewGuid(),
                    BattleId = battle1.Id,
                    ShareUrl = GenerateShareUrl(),
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.BattleReplays.Add(replay);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-replay creation failed for battle {BattleId}", battle1.Id);
            }

            _logger.LogInformation(
                "Battle {BattleId} completed: {Turns} turns, Winner: {Winner}",
                battle1.Id, resolution.TotalTurns, battle1.WinnerId?.ToString() ?? "Draw");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing battle {BattleId}", battle1.Id);
            battle1.Status = BattleStatus.Cancelled;
            await RefundDailyBattleUsageAsync(battle1.Player1Id, cancellationToken);
            if (battle1.Player2Id.HasValue)
                await RefundDailyBattleUsageAsync(battle1.Player2Id.Value, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateShareUrl()
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var bytes = Guid.NewGuid().ToByteArray();
        var result = new char[8];
        for (int i = 0; i < 8; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    private async Task RefundDailyBattleUsageAsync(Guid playerId, CancellationToken ct)
    {
        var player = await _context.Players.FindAsync(new object[] { playerId }, ct);
        if (player != null && player.CurrentTier == SubscriptionTier.Free && player.DailyBattlesUsed > 0)
        {
            player.DailyBattlesUsed--;
            _logger.LogInformation("Refunded daily battle usage for player {PlayerId} (now {Used})", playerId, player.DailyBattlesUsed);
        }
    }

    private async Task UpdateRatingsAndRewards(Guid winnerId, Guid loserId, Battle battle, CancellationToken ct)
    {
        var winner = await _context.Players.FindAsync(new object[] { winnerId }, ct);
        var loser = await _context.Players.FindAsync(new object[] { loserId }, ct);

        if (winner == null || loser == null) return;

        // API (Arena Power Index) rating calculation — ranked only
        int winnerChange = 0;
        int loserChange = 0;
        var winnerOldRating = winner.Rating;
        var loserOldRating = loser.Rating;

        if (battle.Mode == "ranked")
        {
            double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loser.Rating - winner.Rating) / 400.0));
            int kFactor = 32;
            winnerChange = (int)(kFactor * (1 - expectedWinner));
            loserChange = -(int)(kFactor * expectedWinner);

            winner.Rating += winnerChange;
            loser.Rating += loserChange;

            // Ensure rating doesn't go below 100
            if (loser.Rating < 100) loser.Rating = 100;

            _ledger.LogPlayer(winnerId, "Rating", winnerOldRating, winner.Rating, "Battle", "BattleWon", battle.Id);
            _ledger.LogPlayer(loserId, "Rating", loserOldRating, loser.Rating, "Battle", "BattleLost", battle.Id);

            // Rating milestone notifications (crossing 500/1000/1500/2000/2500/3000)
            int[] milestones = [500, 1000, 1500, 2000, 2500, 3000];
            foreach (var m in milestones)
            {
                if (winner.Rating >= m && winnerOldRating < m)
                    await _notifications.SendAsync(winnerId, NotificationType.RatingMilestone,
                        $"Rating Milestone: {m}!", $"You've reached {m} API rating! Keep climbing.");
                if (loser.Rating < m && loserOldRating >= m)
                    await _notifications.SendAsync(loserId, NotificationType.RatingMilestone,
                        $"Dropped Below {m}", $"Your rating fell below {m}. Time for a comeback!");
            }
        }

        battle.Player1RatingChange = winnerId == battle.Player1Id ? winnerChange : loserChange;
        battle.Player2RatingChange = winnerId == battle.Player2Id ? winnerChange : loserChange;

        // Process gold/XP rewards via progression service
        var winnerRewards = await _progressionService.ProcessBattleRewardsAsync(winnerId, true, winnerChange);
        var loserRewards = await _progressionService.ProcessBattleRewardsAsync(loserId, false, loserChange);

        battle.CurrencyReward = winnerRewards.GoldEarned;

        // Update season rankings for ranked battles
        if (battle.Mode == "ranked")
        {
            try
            {
                await _seasonService.UpdateSeasonRatingAsync(winnerId, winnerChange, true, false);
                await _seasonService.UpdateSeasonRatingAsync(loserId, loserChange, false, false);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Season update failed for battle {BattleId}", battle.Id); }
        }

        // Check achievements
        try
        {
            await _achievementService.CheckAndAwardAsync(winnerId, "battle_won");
            await _achievementService.CheckAndAwardAsync(loserId, "battle_lost");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Achievement check failed for battle {BattleId}", battle.Id);
        }

        // Roll loot drops for both players
        try
        {
            await _lootService.RollLootAsync(winnerId, battle.Id, true, winner.WinStreak);
            await _lootService.RollLootAsync(loserId, battle.Id, false, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loot roll failed for battle {BattleId}", battle.Id);
        }

        // Check rival matchups
        try
        {
            await _rivalService.CheckRivalBattleAsync(winnerId, loserId, battle.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rival check failed for battle {BattleId}", battle.Id);
        }

        // Award battle pass XP (100 for win, 25 for loss)
        try
        {
            await _battlePassService.AddXpAsync(winnerId, 100, "battle_win");
            await _battlePassService.AddXpAsync(loserId, 25, "battle_loss");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Battle pass XP failed for battle {BattleId}", battle.Id);
        }

        // Record guild war contributions (ranked wins only)
        if (battle.Mode == "ranked")
        {
            try
            {
                await _guildWarService.RecordWarContributionAsync(winnerId, battle.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Guild war contribution failed for battle {BattleId}", battle.Id);
            }
        }

        // Notify both players
        try
        {
            await _notifications.SendAsync(winnerId, Models.Enums.NotificationType.BattleCompleted, "Battle Won!",
                $"You defeated {loser.Username}! Rating: {(winnerChange >= 0 ? "+" : "")}{winnerChange}", $"/api/v1/battle/results/{battle.Id}");
            await _notifications.SendAsync(loserId, Models.Enums.NotificationType.BattleCompleted, "Battle Lost",
                $"You were defeated by {winner.Username}. Rating: {loserChange}", $"/api/v1/battle/results/{battle.Id}");

            // Win streak milestone notifications (every 5 wins)
            if (winner.WinStreak > 0 && winner.WinStreak % 5 == 0)
            {
                await _notifications.SendAsync(winnerId, Models.Enums.NotificationType.WinStreakMilestone, "Win Streak!",
                    $"You're on a {winner.WinStreak}-game win streak! Keep it up!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification failed for battle {BattleId}", battle.Id);
        }

        // Revenge alert and rank change notifications
        try
        {
            await _notifications.SendRevengeAlertAsync(loserId, winnerId, battle.Id);
            await _notifications.SendRankChangeAlertAsync(winnerId, winnerOldRating, winner.Rating);
            await _notifications.SendRankChangeAlertAsync(loserId, loserOldRating, loser.Rating);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Enhanced notification failed for battle {BattleId}", battle.Id);
        }

        // Record activity feed entries and lifetime stats
        try
        {
            await _activityFeedService.RecordBattleStatsAsync(winnerId, true, winnerRewards.GoldEarned, winnerRewards.XpEarned, winner.Rating);
            await _activityFeedService.RecordBattleStatsAsync(loserId, false, loserRewards.GoldEarned, loserRewards.XpEarned, loser.Rating);
            await _activityFeedService.LogActivityAsync(winnerId, "battle_won",
                $"Defeated {loser.Username} (+{winnerChange} rating)", battle.Id);
            await _activityFeedService.LogActivityAsync(loserId, "battle_lost",
                $"Lost to {winner.Username} ({loserChange} rating)", battle.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Activity feed logging failed for battle {BattleId}", battle.Id);
        }
    }
}
