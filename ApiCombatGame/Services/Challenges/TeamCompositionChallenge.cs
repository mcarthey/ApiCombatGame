using System.Text.Json;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Challenge: Win N battles using only units of a specific class.
/// Uses Battle.Team1ClassesJson / Team2ClassesJson populated during battle processing.
/// </summary>
public class TeamCompositionChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "TeamComposition";

    public override DailyChallenge Generate(Player player)
    {
        var requiredClass = PickRandomClass();

        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = $"Win with {requiredClass}s",
            Description = $"Win 5 battles using only {requiredClass} units in your team",
            RequirementsJson = JsonSerializer.Serialize(new { Class = requiredClass.ToString() }),
            Progress = 0,
            RequiredProgress = 5,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 500,
            RewardExperience = 100
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        // Must have won
        if (battle.WinnerId != challenge.PlayerId)
            return false;

        // Deserialize requirements to get the required class
        var requirements = JsonSerializer.Deserialize<JsonElement>(challenge.RequirementsJson);
        if (!requirements.TryGetProperty("Class", out var classProp))
            return false;

        var requiredClass = classProp.GetString();
        if (string.IsNullOrEmpty(requiredClass))
            return false;

        // Determine which team the player was on and get their unit classes
        string classesJson;
        if (battle.Player1Id == challenge.PlayerId)
            classesJson = battle.Team1ClassesJson;
        else if (battle.Player2Id == challenge.PlayerId)
            classesJson = battle.Team2ClassesJson;
        else
            return false;

        var classes = JsonSerializer.Deserialize<List<string>>(classesJson) ?? new();
        if (classes.Count == 0)
            return false;

        // All units must be the required class
        return classes.All(c => c.Equals(requiredClass, StringComparison.OrdinalIgnoreCase));
    }

    private static UnitClass PickRandomClass()
    {
        var classes = Enum.GetValues<UnitClass>();
        return classes[Random.Shared.Next(classes.Length)];
    }
}
