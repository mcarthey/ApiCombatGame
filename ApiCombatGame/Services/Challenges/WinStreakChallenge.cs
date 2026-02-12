using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Challenge: Win N battles in a row without losing.
/// Streak-based challenge that resets progress on loss.
/// </summary>
public class WinStreakChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "WinStreak";

    public override DailyChallenge Generate(Player player)
    {
        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = "Win Streak",
            Description = "Win 3 battles in a row without losing",
            RequirementsJson = JsonSerializer.Serialize(new { Streak = 3 }),
            Progress = 0,
            RequiredProgress = 3,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 750,
            RewardExperience = 150
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        if (battle.WinnerId == challenge.PlayerId)
        {
            // Won — progress incremented by ChallengeService caller
            return true;
        }

        // Lost — reset streak progress to 0
        challenge.Progress = 0;
        return false;
    }
}
