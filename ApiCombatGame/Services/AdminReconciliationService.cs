using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AdminReconciliationService : IAdminReconciliationService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<AdminReconciliationService> _logger;

    public AdminReconciliationService(GameDbContext context, INotificationService notifications, ILogger<AdminReconciliationService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    private async Task AuditLogAsync(Guid adminPlayerId, string action, Guid? targetPlayerId = null, string? detailsJson = null)
    {
        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminPlayerId = adminPlayerId,
            Action = action,
            TargetPlayerId = targetPlayerId,
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public Task<ReconciliationPreview> PreviewReconciliationAsync(DateTime? since = null)
        => RunReconciliationAsync(applyChanges: false, since: since);

    public Task<ReconciliationPreview> PreviewPlayerReconciliationAsync(Guid playerId, DateTime? since = null)
        => RunReconciliationAsync(applyChanges: false, filterPlayerId: playerId, since: since);

    public Task<ReconciliationPreview> ExecuteReconciliationAsync(Guid adminPlayerId, DateTime? since = null)
        => RunReconciliationAsync(applyChanges: true, adminPlayerId: adminPlayerId, since: since);

    private async Task<ReconciliationPreview> RunReconciliationAsync(
        bool applyChanges,
        Guid? adminPlayerId = null,
        Guid? filterPlayerId = null,
        DateTime? since = null)
    {
        const int startingRating = 1000;
        const int kFactor = 32;
        const int ratingFloor = 100;

        var allBattles = await _context.Battles
            .Where(b => b.Status == BattleStatus.Completed && b.Player2Id != null)
            .OrderBy(b => b.CompletedAt)
            .ToListAsync();

        var allPlayers = await _context.Players.ToListAsync();

        var simulatedRatings = new Dictionary<Guid, int>();
        var rankedReplayed = new Dictionary<Guid, int>();
        var casualFixed = new Dictionary<Guid, int>();

        foreach (var p in allPlayers)
        {
            simulatedRatings[p.Id] = startingRating;
            rankedReplayed[p.Id] = 0;
            casualFixed[p.Id] = 0;
        }

        List<Models.Domain.Battle> trustedBattles;
        List<Models.Domain.Battle> replayBattles;

        if (since.HasValue)
        {
            trustedBattles = allBattles.Where(b => b.CompletedAt < since.Value).ToList();
            replayBattles = allBattles.Where(b => b.CompletedAt >= since.Value).ToList();

            foreach (var battle in trustedBattles)
            {
                var p1Id = battle.Player1Id;
                var p2Id = battle.Player2Id!.Value;

                if (!simulatedRatings.ContainsKey(p1Id) || !simulatedRatings.ContainsKey(p2Id))
                    continue;

                if (battle.Mode == "ranked")
                {
                    simulatedRatings[p1Id] += battle.Player1RatingChange ?? 0;
                    simulatedRatings[p2Id] += battle.Player2RatingChange ?? 0;

                    if (simulatedRatings[p1Id] < ratingFloor) simulatedRatings[p1Id] = ratingFloor;
                    if (simulatedRatings[p2Id] < ratingFloor) simulatedRatings[p2Id] = ratingFloor;
                }
            }
        }
        else
        {
            trustedBattles = new List<Models.Domain.Battle>();
            replayBattles = allBattles;
        }

        int totalCasualFixed = 0;
        var involvedPlayerIds = new HashSet<Guid>();

        foreach (var battle in replayBattles)
        {
            var p1Id = battle.Player1Id;
            var p2Id = battle.Player2Id!.Value;

            if (!simulatedRatings.ContainsKey(p1Id) || !simulatedRatings.ContainsKey(p2Id))
                continue;

            involvedPlayerIds.Add(p1Id);
            involvedPlayerIds.Add(p2Id);

            if (battle.Mode == "casual")
            {
                bool needsFix = (battle.Player1RatingChange ?? 0) != 0
                             || (battle.Player2RatingChange ?? 0) != 0;

                if (needsFix)
                {
                    totalCasualFixed++;
                    casualFixed[p1Id]++;
                    casualFixed[p2Id]++;

                    if (applyChanges)
                    {
                        battle.Player1RatingChange = 0;
                        battle.Player2RatingChange = 0;
                    }
                }
            }
            else if (battle.Mode == "ranked")
            {
                if (battle.WinnerId == null)
                {
                    if (applyChanges)
                    {
                        battle.Player1RatingChange = 0;
                        battle.Player2RatingChange = 0;
                    }
                }
                else
                {
                    var winnerId = battle.WinnerId.Value;
                    var loserId = winnerId == p1Id ? p2Id : p1Id;

                    double expectedWinner = 1.0 / (1.0 + Math.Pow(10,
                        (simulatedRatings[loserId] - simulatedRatings[winnerId]) / 400.0));
                    int winnerChange = (int)(kFactor * (1 - expectedWinner));
                    int loserChange = -(int)(kFactor * expectedWinner);

                    simulatedRatings[winnerId] += winnerChange;
                    simulatedRatings[loserId] += loserChange;

                    if (simulatedRatings[loserId] < ratingFloor)
                        simulatedRatings[loserId] = ratingFloor;

                    rankedReplayed[p1Id]++;
                    rankedReplayed[p2Id]++;

                    if (applyChanges)
                    {
                        battle.Player1RatingChange = winnerId == p1Id ? winnerChange : loserChange;
                        battle.Player2RatingChange = winnerId == p2Id ? winnerChange : loserChange;
                    }
                }
            }
        }

        var deltas = new List<PlayerReconciliationDelta>();
        int affectedCount = 0;

        foreach (var player in allPlayers)
        {
            if (!involvedPlayerIds.Contains(player.Id) && player.Id != filterPlayerId)
                continue;

            var recalculated = simulatedRatings.GetValueOrDefault(player.Id, startingRating);
            var current = player.Rating;
            var delta = recalculated - current;

            if (delta != 0 || player.Id == filterPlayerId)
            {
                if (delta != 0) affectedCount++;

                if (filterPlayerId == null || player.Id == filterPlayerId)
                {
                    deltas.Add(new PlayerReconciliationDelta
                    {
                        PlayerId = player.Id,
                        Username = player.Username,
                        CurrentRating = current,
                        RecalculatedRating = recalculated,
                        RankedBattlesReplayed = rankedReplayed.GetValueOrDefault(player.Id),
                        CasualBattlesFixed = casualFixed.GetValueOrDefault(player.Id)
                    });
                }
            }
        }

        if (applyChanges && adminPlayerId.HasValue)
        {
            foreach (var d in deltas.Where(d => d.Delta != 0))
            {
                var player = allPlayers.First(p => p.Id == d.PlayerId);
                player.Rating = d.RecalculatedRating;

                if (player.Rating > player.HighestRating)
                    player.HighestRating = player.Rating;
            }

            await _context.SaveChangesAsync();

            foreach (var d in deltas.Where(d => d.Delta != 0))
            {
                var direction = d.Delta > 0 ? "increased" : "decreased";
                await AuditLogAsync(adminPlayerId.Value, "ReconcileRating", d.PlayerId,
                    $"{{\"oldRating\":{d.CurrentRating},\"newRating\":{d.RecalculatedRating},\"delta\":{d.Delta}}}");

                if (!allPlayers.First(p => p.Id == d.PlayerId).IsDeleted)
                {
                    await _notifications.SendAsync(d.PlayerId,
                        NotificationType.AdminActionOnAccount,
                        "Rating Corrected",
                        $"Your rating has been {direction} by {Math.Abs(d.Delta)} (from {d.CurrentRating} to {d.RecalculatedRating}) as part of a data reconciliation.");
                }
            }

            await AuditLogAsync(adminPlayerId.Value, "ReconcileAll", null,
                $"{{\"playersAffected\":{affectedCount},\"battlesReprocessed\":{replayBattles.Count},\"casualBattlesFixed\":{totalCasualFixed},\"since\":\"{since?.ToString("o") ?? "full"}\"}}");

            _logger.LogInformation(
                "Rating reconciliation completed: {Affected} players affected, {Battles} battles reprocessed, {Casual} casual fixed, since={Since}",
                affectedCount, replayBattles.Count, totalCasualFixed, since?.ToString("o") ?? "full");
        }

        deltas = deltas.OrderByDescending(d => Math.Abs(d.Delta)).ToList();

        return new ReconciliationPreview
        {
            TotalPlayersAffected = affectedCount,
            TotalBattlesReprocessed = replayBattles.Count,
            CasualBattlesFixed = totalCasualFixed,
            Since = since,
            PlayerDeltas = deltas
        };
    }
}
