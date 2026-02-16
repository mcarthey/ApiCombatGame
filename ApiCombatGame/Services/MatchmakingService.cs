using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly GameDbContext _context;
    private readonly ILogger<MatchmakingService> _logger;
    private const int RatingRange = 200;

    public MatchmakingService(GameDbContext context, ILogger<MatchmakingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(Battle battle1, Battle battle2)?> FindMatchAsync()
    {
        var queuedBattles = await _context.Battles
            .Where(b => b.Status == BattleStatus.Queued && b.Player2Id == null)
            .Include(b => b.Player1)
            .ThenInclude(p => p.Subscription)
            .ToListAsync();

        if (queuedBattles.Count < 2)
            return null;

        // Priority matchmaking: Premium+ users get matched first
        var sortedBattles = queuedBattles
            .OrderByDescending(b => b.Player1.CurrentTier != SubscriptionTier.Free) // Premium+ first
            .ThenBy(b => b.QueuedAt) // Then by wait time
            .ToList();

        // Try to find a match based on rating proximity
        for (int i = 0; i < sortedBattles.Count; i++)
        {
            var battle1 = sortedBattles[i];
            var player1Rating = battle1.Player1.Rating;
            var player1Tier = battle1.Player1.CurrentTier;
            var waitTime = (DateTime.UtcNow - battle1.QueuedAt).TotalSeconds;

            // Tier-based rating range: Premium+ gets tighter matches
            int baseRange = player1Tier == SubscriptionTier.Free ? 300 : RatingRange;

            // Premium+ users get faster matching after 10s wait
            int waitBonus = player1Tier != SubscriptionTier.Free && waitTime > 10
                ? (int)(waitTime / 5) * 50  // Faster range expansion
                : (int)(waitTime / 10) * 50;

            int effectiveRange = baseRange + waitBonus;

            for (int j = i + 1; j < sortedBattles.Count; j++)
            {
                var battle2 = sortedBattles[j];

                // Don't match player against themselves
                if (battle1.Player1Id == battle2.Player1Id)
                    continue;

                var player2Rating = battle2.Player1.Rating;

                if (Math.Abs(player1Rating - player2Rating) <= effectiveRange)
                {
                    _logger.LogInformation(
                        "Matched players {P1} ({Tier1}, rating: {R1}) vs {P2} ({Tier2}, rating: {R2})",
                        battle1.Player1Id, player1Tier, player1Rating,
                        battle2.Player1Id, battle2.Player1.CurrentTier, player2Rating);

                    return (battle1, battle2);
                }
            }
        }

        // If no rating-based match, match the two longest-waiting players (if waiting > 30s for Free, 20s for Premium+)
        if (sortedBattles.Count >= 2)
        {
            var oldest = sortedBattles[0];
            var waitThreshold = oldest.Player1.CurrentTier == SubscriptionTier.Free ? 30 : 20;

            if ((DateTime.UtcNow - oldest.QueuedAt).TotalSeconds > waitThreshold)
            {
                var opponent = sortedBattles.FirstOrDefault(b => b.Player1Id != oldest.Player1Id);
                if (opponent != null)
                {
                    _logger.LogInformation("Force-matched players after timeout: {P1} ({Tier1}) vs {P2} ({Tier2})",
                        oldest.Player1Id, oldest.Player1.CurrentTier, opponent.Player1Id, opponent.Player1.CurrentTier);
                    return (oldest, opponent);
                }
            }
        }

        return null;
    }
}
