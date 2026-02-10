using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Example challenge: Win N battles in a row without losing.
/// Demonstrates a streak-based challenge that resets on loss.
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
        // TODO: Track win streak, reset progress on loss
        // 1. Check if player won the battle
        // 2. If won, increment progress
        // 3. If lost, reset progress to 0
        // 4. Return true if won (progress was updated)
        return false; // Stub
    }
}
