using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Rival;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class RivalService : IRivalService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;
    private readonly ILogger<RivalService> _logger;

    private const int RivalBonusGold = 100;
    private const int RivalDurationDays = 7;

    public RivalService(GameDbContext context, INotificationService notifications, IActivityLedger ledger, ILogger<RivalService> logger)
    {
        _context = context;
        _notifications = notifications;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<RivalInfoResponse> GetRivalAsync(Guid playerId)
    {
        // Check for active rival
        var assignment = await _context.RivalAssignments
            .Include(r => r.Rival)
            .FirstOrDefaultAsync(r => r.PlayerId == playerId && r.ExpiresAt > DateTime.UtcNow);

        if (assignment == null)
        {
            // Auto-assign a new rival
            assignment = await AssignRivalAsync(playerId);
        }

        if (assignment == null)
        {
            return new RivalInfoResponse { HasRival = false };
        }

        return new RivalInfoResponse
        {
            RivalId = assignment.RivalId,
            RivalUsername = assignment.Rival.Username,
            RivalRating = assignment.Rival.Rating,
            RivalLevel = assignment.Rival.Level,
            WinsAgainstRival = assignment.WinsAgainstRival,
            LossesAgainstRival = assignment.LossesAgainstRival,
            BonusGoldEarned = assignment.BonusGoldEarned,
            BonusPerWin = RivalBonusGold,
            ExpiresAt = assignment.ExpiresAt,
            HasRival = true
        };
    }

    public async Task CheckRivalBattleAsync(Guid winnerId, Guid loserId, Guid battleId)
    {
        // Check if winner has loser as rival
        var winnerRival = await _context.RivalAssignments
            .FirstOrDefaultAsync(r => r.PlayerId == winnerId && r.RivalId == loserId && r.ExpiresAt > DateTime.UtcNow);

        if (winnerRival != null)
        {
            winnerRival.WinsAgainstRival++;
            winnerRival.BonusGoldEarned += RivalBonusGold;

            var player = await _context.Players.FindAsync(winnerId);
            if (player != null)
            {
                var oldCurrency = player.Currency;
                player.Currency += RivalBonusGold;
                _ledger.LogPlayer(winnerId, "Currency", oldCurrency, player.Currency, "Rival", "RivalDefeated", battleId);
                await _notifications.SendAsync(winnerId, NotificationType.BattleCompleted,
                    "Rival Defeated!",
                    $"You beat your rival! +{RivalBonusGold}g bonus. Total rival wins: {winnerRival.WinsAgainstRival}",
                    $"/api/v1/battle/results/{battleId}");
            }
        }

        // Check if loser has winner as rival (update their loss count)
        var loserRival = await _context.RivalAssignments
            .FirstOrDefaultAsync(r => r.PlayerId == loserId && r.RivalId == winnerId && r.ExpiresAt > DateTime.UtcNow);

        if (loserRival != null)
        {
            loserRival.LossesAgainstRival++;
            await _notifications.SendAsync(loserId, NotificationType.BattleCompleted,
                "Rival Victory",
                $"Your rival beat you! Time for revenge. Rival record: {loserRival.WinsAgainstRival}W / {loserRival.LossesAgainstRival}L",
                $"/api/v1/battle/results/{battleId}");
        }

        await _context.SaveChangesAsync();
    }

    private async Task<RivalAssignment?> AssignRivalAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return null;

        // Find a player with similar rating (within 200 points) who isn't already a rival
        var candidates = await _context.Players
            .Where(p => p.Id != playerId
                && Math.Abs(p.Rating - player.Rating) <= 200
                && p.Level > 0) // Ensure they've actually played
            .OrderBy(p => Math.Abs(p.Rating - player.Rating))
            .Take(10)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            // Widen search if no close matches
            candidates = await _context.Players
                .Where(p => p.Id != playerId && p.Level > 0)
                .OrderBy(p => Math.Abs(p.Rating - player.Rating))
                .Take(5)
                .ToListAsync();
        }

        if (candidates.Count == 0) return null;

        var rival = candidates[new Random().Next(candidates.Count)];

        var assignment = new RivalAssignment
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            RivalId = rival.Id,
            AssignedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RivalDurationDays)
        };

        _context.RivalAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rival assigned: {Player} vs {Rival} (rating diff: {Diff})",
            player.Username, rival.Username, Math.Abs(player.Rating - rival.Rating));

        // Reload with navigation
        await _context.Entry(assignment).Reference(r => r.Rival).LoadAsync();
        return assignment;
    }
}
