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
            .OrderBy(b => b.QueuedAt)
            .Include(b => b.Player1)
            .ToListAsync();

        if (queuedBattles.Count < 2)
            return null;

        // Try to find a match based on rating proximity
        for (int i = 0; i < queuedBattles.Count; i++)
        {
            var battle1 = queuedBattles[i];
            var player1Rating = battle1.Player1.Rating;
            var waitTime = (DateTime.UtcNow - battle1.QueuedAt).TotalSeconds;

            // Expand search range based on wait time
            int effectiveRange = RatingRange + (int)(waitTime / 10) * 50;

            for (int j = i + 1; j < queuedBattles.Count; j++)
            {
                var battle2 = queuedBattles[j];

                // Don't match player against themselves
                if (battle1.Player1Id == battle2.Player1Id)
                    continue;

                var player2Rating = battle2.Player1.Rating;

                if (Math.Abs(player1Rating - player2Rating) <= effectiveRange)
                {
                    _logger.LogInformation(
                        "Matched players {P1} (rating: {R1}) vs {P2} (rating: {R2})",
                        battle1.Player1Id, player1Rating, battle2.Player1Id, player2Rating);

                    return (battle1, battle2);
                }
            }
        }

        // If no rating-based match, match the two longest-waiting players (if waiting > 30s)
        if (queuedBattles.Count >= 2)
        {
            var oldest = queuedBattles[0];
            if ((DateTime.UtcNow - oldest.QueuedAt).TotalSeconds > 30)
            {
                var opponent = queuedBattles.FirstOrDefault(b => b.Player1Id != oldest.Player1Id);
                if (opponent != null)
                {
                    _logger.LogInformation("Force-matched players after timeout: {P1} vs {P2}",
                        oldest.Player1Id, opponent.Player1Id);
                    return (oldest, opponent);
                }
            }
        }

        return null;
    }
}
