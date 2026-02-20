using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Challenges;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ChallengeService : IChallengeService
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IBattlePassService _battlePassService;
    private readonly INotificationService _notifications;
    private readonly List<IChallengeGenerator> _generators;
    private readonly ILogger<ChallengeService> _logger;

    // Generators grouped by difficulty
    private readonly List<IChallengeGenerator> _easyGenerators;
    private readonly List<IChallengeGenerator> _mediumGenerators;
    private readonly List<IChallengeGenerator> _hardGenerators;

    // Battle pass XP per difficulty
    private static readonly Dictionary<string, int> BattlePassXpByDifficulty = new()
    {
        ["easy"] = 50,
        ["medium"] = 100,
        ["hard"] = 200
    };

    public ChallengeService(
        GameDbContext context,
        IPlayerProgressionService progressionService,
        IBattlePassService battlePassService,
        INotificationService notifications,
        ILogger<ChallengeService> logger)
    {
        _context = context;
        _progressionService = progressionService;
        _battlePassService = battlePassService;
        _notifications = notifications;
        _logger = logger;

        // Register challenge generators by difficulty tier
        _easyGenerators = new List<IChallengeGenerator>
        {
            new BattleCountChallenge(),
        };

        _mediumGenerators = new List<IChallengeGenerator>
        {
            new TeamCompositionChallenge(),
            new WinStreakChallenge(),
            new VarietySquadChallenge(),
        };

        _hardGenerators = new List<IChallengeGenerator>
        {
            new UnderdogChallenge(),
            new FlawlessVictoryChallenge(),
        };

        _generators = _easyGenerators
            .Concat(_mediumGenerators)
            .Concat(_hardGenerators)
            .ToList();
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
        if (player == null)
        {
            _logger.LogWarning("Player {PlayerId} not found in GenerateDailyChallenges", playerId);
            return;
        }

        int needed = 3 - existingChallenges;
        var newChallenges = GenerateMixedDifficulty(player, needed);

        foreach (var challenge in newChallenges)
        {
            _context.DailyChallenges.Add(challenge);
        }

        await _context.SaveChangesAsync();

        if (newChallenges.Count > 0)
        {
            await _notifications.SendAsync(playerId, NotificationType.DailyChallengesAvailable,
                "Daily Challenges Ready!", $"{newChallenges.Count} new challenges await you today!");
        }

        _logger.LogInformation("Generated {Count} daily challenges for player {PlayerId}",
            newChallenges.Count, playerId);
    }

    public async Task CheckChallengeProgress(Guid playerId, Battle battle)
    {
        var challenges = await GetActiveChallenges(playerId);
        foreach (var challenge in challenges.Where(c => !c.IsCompleted))
        {
            var generator = _generators.FirstOrDefault(g => g.ChallengeType == challenge.ChallengeType);
            if (generator == null)
            {
                _logger.LogWarning("No generator found for challenge type {Type}", challenge.ChallengeType);
                continue;
            }
            if (generator.CheckProgress(challenge, battle))
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

        // Award battle pass XP based on difficulty
        int bpXp = BattlePassXpByDifficulty.GetValueOrDefault(challenge.Difficulty, 100);
        try
        {
            await _battlePassService.AddXpAsync(playerId, bpXp, $"challenge_{challenge.Difficulty}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Battle pass XP failed for challenge claim");
        }

        // Remove the challenge (it's been claimed)
        _context.DailyChallenges.Remove(challenge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} claimed {Difficulty} challenge: {Currency}g, {XP} XP, {BpXp} BP XP",
            playerId, challenge.Difficulty, challenge.RewardCurrency, challenge.RewardExperience, bpXp);
    }

    public async Task<bool> RefreshChallengesAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new KeyNotFoundException("Player not found.");

        // Only Premium and above can refresh
        if (player.CurrentTier < SubscriptionTier.Premium)
            throw new InvalidOperationException("Challenge refresh requires Premium subscription.");

        var activeChallenges = await _context.DailyChallenges
            .Where(c => c.PlayerId == playerId && c.ExpiresAt > DateTime.UtcNow && !c.IsCompleted)
            .ToListAsync();

        if (activeChallenges.Count == 0)
            throw new InvalidOperationException("No active challenges to refresh.");

        // Remove existing uncompleted challenges
        _context.DailyChallenges.RemoveRange(activeChallenges);

        // Generate fresh ones
        var newChallenges = GenerateMixedDifficulty(player, activeChallenges.Count);
        foreach (var challenge in newChallenges)
        {
            _context.DailyChallenges.Add(challenge);
        }

        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.DailyChallengesAvailable,
            "Challenges Refreshed!",
            $"Your daily challenges have been refreshed. {newChallenges.Count} new challenges await!");

        _logger.LogInformation("Player {PlayerId} refreshed daily challenges", playerId);
        return true;
    }

    /// <summary>
    /// Generates a mix of easy/medium/hard challenges (1 easy, 1 medium, 1 hard for 3 challenges).
    /// </summary>
    private List<DailyChallenge> GenerateMixedDifficulty(Player player, int count)
    {
        var result = new List<DailyChallenge>();

        if (count >= 1)
        {
            var easyGen = _easyGenerators[Random.Shared.Next(_easyGenerators.Count)];
            result.Add(easyGen.Generate(player));
        }

        if (count >= 2)
        {
            var medGen = _mediumGenerators[Random.Shared.Next(_mediumGenerators.Count)];
            result.Add(medGen.Generate(player));
        }

        if (count >= 3)
        {
            var hardGen = _hardGenerators[Random.Shared.Next(_hardGenerators.Count)];
            result.Add(hardGen.Generate(player));
        }

        return result;
    }
}
