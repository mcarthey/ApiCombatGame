using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Challenges;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ChallengeService : IChallengeService
{
    private readonly GameDbContext _context;
    private readonly List<IChallengeGenerator> _generators;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(GameDbContext context, ILogger<ChallengeService> logger)
    {
        _context = context;
        _logger = logger;

        // Register challenge generators (Extension Point: add new challenge types here)
        _generators = new List<IChallengeGenerator>
        {
            new TeamCompositionChallenge(),
            new WinStreakChallenge(),
            // TODO: Add more challenge types
            // new NoDamageChallenge(),
            // new SpeedRunChallenge(),
            // new UnderDogChallenge(),
        };
    }

    public async Task<List<DailyChallenge>> GetActiveChallenges(Guid playerId)
    {
        var now = DateTime.UtcNow;
        return await _context.DailyChallenges
            .Where(c => c.PlayerId == playerId && c.ExpiresAt > now)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task GenerateDailyChallenges(Guid playerId)
    {
        // Check if player already has challenges for today
        var today = DateTime.UtcNow.Date;
        var existingChallenges = await _context.DailyChallenges
            .Where(c => c.PlayerId == playerId && c.CreatedAt >= today)
            .CountAsync();

        if (existingChallenges >= 3)
        {
            _logger.LogInformation("Player {PlayerId} already has challenges for today", playerId);
            return;
        }

        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        // Pick 3 random challenge types (no duplicates)
        var selectedGenerators = _generators
            .OrderBy(_ => Random.Shared.Next())
            .Take(3 - existingChallenges)
            .ToList();

        foreach (var generator in selectedGenerators)
        {
            var challenge = generator.Generate(player);
            _context.DailyChallenges.Add(challenge);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Generated {Count} daily challenges for player {PlayerId}",
            selectedGenerators.Count, playerId);
    }

    public async Task CheckChallengeProgress(Guid playerId, Battle battle)
    {
        // TODO: Implement progress checking
        // 1. Get active challenges for the player
        // 2. For each challenge, find the matching generator by ChallengeType
        // 3. Call generator.CheckProgress(challenge, battle)
        // 4. If true, increment challenge.Progress
        // 5. If Progress >= RequiredProgress, mark as completed
        // 6. Save changes

        var challenges = await GetActiveChallenges(playerId);
        foreach (var challenge in challenges.Where(c => !c.IsCompleted))
        {
            var generator = _generators.FirstOrDefault(g => g.ChallengeType == challenge.ChallengeType);
            if (generator != null && generator.CheckProgress(challenge, battle))
            {
                challenge.Progress++;
                if (challenge.Progress >= challenge.RequiredProgress)
                {
                    challenge.IsCompleted = true;
                    challenge.CompletedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task ClaimReward(Guid challengeId, Guid playerId)
    {
        var challenge = await _context.DailyChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.PlayerId == playerId);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found");

        if (!challenge.IsCompleted)
            throw new InvalidOperationException("Challenge is not yet completed");

        // Award rewards
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new KeyNotFoundException("Player not found");

        player.Currency += challenge.RewardCurrency;
        // TODO: Award RewardExperience when experience/leveling system is implemented

        // Remove the challenge (it's been claimed)
        _context.DailyChallenges.Remove(challenge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} claimed reward for challenge {ChallengeId}: {Currency} currency",
            playerId, challengeId, challenge.RewardCurrency);
    }
}
