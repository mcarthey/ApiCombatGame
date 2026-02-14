using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Easy challenge: Complete N battles (win or lose).
/// Low barrier, encourages participation.
/// </summary>
public class BattleCountChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "BattleCount";

    public override DailyChallenge Generate(Player player)
    {
        int required = Random.Shared.Next(3, 6); // 3-5 battles

        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = $"Fight {required} Battles",
            Description = $"Complete {required} battles (win or lose)",
            Difficulty = "easy",
            RequirementsJson = JsonSerializer.Serialize(new { Count = required }),
            Progress = 0,
            RequiredProgress = required,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 100 + (required * 20),
            RewardExperience = 50 + (required * 10)
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        // Any completed battle counts
        return battle.Player1Id == challenge.PlayerId || battle.Player2Id == challenge.PlayerId;
    }
}
