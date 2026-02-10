using System.Text.Json;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Example challenge: Win N battles using only units of a specific class.
/// Demonstrates personalized challenge generation based on player data.
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
        // TODO: Check if the winning team used only the required class
        // 1. Deserialize RequirementsJson to get required class
        // 2. Check if player won the battle
        // 3. Check if all units in the player's team are of the required class
        // 4. Return true if all conditions met
        return false; // Stub
    }

    private static UnitClass PickRandomClass()
    {
        var classes = Enum.GetValues<UnitClass>();
        return classes[Random.Shared.Next(classes.Length)];
    }
}
