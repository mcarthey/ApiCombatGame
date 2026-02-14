using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Hard challenge: Win N battles against players with higher rating.
/// Rewards upsets and skillful play.
/// </summary>
public class UnderdogChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "Underdog";

    public override DailyChallenge Generate(Player player)
    {
        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = "Giant Slayer",
            Description = "Win 2 battles against players with higher rating than you",
            Difficulty = "hard",
            RequirementsJson = JsonSerializer.Serialize(new { RatingAdvantage = 0 }),
            Progress = 0,
            RequiredProgress = 2,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 1000,
            RewardExperience = 300
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        if (battle.WinnerId != challenge.PlayerId)
            return false;

        // Check if opponent had higher rating (positive rating change means you beat someone higher)
        int ratingChange;
        if (battle.Player1Id == challenge.PlayerId)
            ratingChange = battle.Player1RatingChange ?? 0;
        else
            ratingChange = battle.Player2RatingChange ?? 0;

        // Higher than average K-factor gain means opponent was rated higher
        return ratingChange > 16;
    }
}
