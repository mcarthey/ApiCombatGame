using System.Text.Json;
using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Hard challenge: Win a battle with all units surviving.
/// Requires excellent strategy and execution.
/// </summary>
public class FlawlessVictoryChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "FlawlessVictory";

    public override DailyChallenge Generate(Player player)
    {
        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = "Flawless Victory",
            Description = "Win a battle where all your units survive",
            Difficulty = "hard",
            RequirementsJson = "{}",
            Progress = 0,
            RequiredProgress = 1,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1),
            RewardCurrency = 800,
            RewardExperience = 250
        };
    }

    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        if (battle.WinnerId != challenge.PlayerId)
            return false;

        // Check battle log for any ally deaths
        if (string.IsNullOrEmpty(battle.BattleLogJson))
            return false;

        try
        {
            var log = JsonSerializer.Deserialize<List<JsonElement>>(battle.BattleLogJson);
            if (log == null) return false;

            // Check if any unit on the winner's team was knocked out
            bool isPlayer1 = battle.Player1Id == challenge.PlayerId;
            foreach (var entry in log)
            {
                if (entry.TryGetProperty("event", out var evt) && evt.GetString() == "unit_defeated")
                {
                    if (entry.TryGetProperty("team", out var team))
                    {
                        int teamNum = team.GetInt32();
                        if ((isPlayer1 && teamNum == 1) || (!isPlayer1 && teamNum == 2))
                            return false; // One of our units died
                    }
                }
            }

            return true; // All units survived
        }
        catch
        {
            return false;
        }
    }
}
