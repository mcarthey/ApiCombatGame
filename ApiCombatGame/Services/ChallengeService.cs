using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Challenges;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ChallengeService : IChallengeService
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progressionService;
    private readonly List<IChallengeGenerator> _generators;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        GameDbContext context,
        IPlayerProgressionService progressionService,
        ILogger<ChallengeService> logger)
    {
        _context = context;
        _progressionService = progressionService;
        _logger = logger;

        // Register challenge generators (Extension Point: add new challenge types here)
        _generators = new List<IChallengeGenerator>
        {
            new TeamCompositionChallenge(),
            new WinStreakChallenge(),
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

        // Pick random challenge types (no duplicates)
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

        // Award rewards via progression service (applies tier multiplier)
        await _progressionService.AwardCurrencyAsync(playerId, challenge.RewardCurrency);
        if (challenge.RewardExperience > 0)
            await _progressionService.AwardExperienceAsync(playerId, challenge.RewardExperience);

        // Remove the challenge (it's been claimed)
        _context.DailyChallenges.Remove(challenge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} claimed reward for challenge {ChallengeId}: {Currency} currency, {XP} XP",
            playerId, challengeId, challenge.RewardCurrency, challenge.RewardExperience);
    }
}
