using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Medium challenge: Win a battle using at least N different unit classes.
/// Encourages team diversity.
/// </summary>
public class VarietySquadChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "VarietySquad";

    public override DailyChallenge Generate(Player player)
    {
        int requiredClasses = Random.Shared.Next(3, 5); // 3-4 different classes

        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = $"Diverse Tactics",
            Description = $"Win a battle using at least {requiredClasses} different unit classes",
            Difficulty = "medium",
            RequirementsJson = JsonSerializer.Serialize(new { MinClasses = requiredClasses }),
            Progress = 0,
            RequiredProgress = 1,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 300 + (requiredClasses * 50),
            RewardExperience = 100 + (requiredClasses * 25)
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        if (battle.WinnerId != challenge.PlayerId)
            return false;

        var requirements = JsonSerializer.Deserialize<JsonElement>(challenge.RequirementsJson);
        if (!requirements.TryGetProperty("MinClasses", out var minProp))
            return false;

        int minClasses = minProp.GetInt32();

        string classesJson = battle.Player1Id == challenge.PlayerId
            ? battle.Team1ClassesJson
            : battle.Team2ClassesJson;

        var classes = JsonSerializer.Deserialize<List<string>>(classesJson) ?? new();
        int distinctClasses = classes.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        return distinctClasses >= minClasses;
    }
}
