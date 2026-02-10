using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ReplayService : IReplayService
{
    private readonly GameDbContext _context;

    public ReplayService(GameDbContext context)
    {
        _context = context;
    }

    public async Task<BattleReplay> CreateReplay(Guid battleId)
    {
        // Check if replay already exists
        var existing = await _context.BattleReplays
            .FirstOrDefaultAsync(r => r.BattleId == battleId);

        if (existing != null)
            return existing;

        // Verify battle exists and is completed
        var battle = await _context.Battles.FindAsync(battleId);
        if (battle == null)
            throw new KeyNotFoundException("Battle not found");

        if (battle.Status != Models.Enums.BattleStatus.Completed)
            throw new InvalidOperationException("Can only create replays for completed battles");

        var replay = new BattleReplay
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            ShareUrl = GenerateShareUrl(),
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.BattleReplays.Add(replay);
        await _context.SaveChangesAsync();

        return replay;
    }

    public async Task<BattleReplay?> GetReplay(string shareUrl)
    {
        return await _context.BattleReplays
            .Include(r => r.Battle)
                .ThenInclude(b => b.Player1)
            .Include(r => r.Battle)
                .ThenInclude(b => b.Player2)
            .FirstOrDefaultAsync(r => r.ShareUrl == shareUrl);
    }

    public async Task IncrementViewCount(Guid replayId)
    {
        var replay = await _context.BattleReplays.FindAsync(replayId);
        if (replay != null)
        {
            replay.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Generate a short share URL using base62 encoding of a GUID segment.
    /// </summary>
    private static string GenerateShareUrl()
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var bytes = Guid.NewGuid().ToByteArray();
        var result = new char[8];
        for (int i = 0; i < 8; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }
}
