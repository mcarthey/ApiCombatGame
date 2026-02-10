using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Factory pattern interface for daily challenge generation.
/// Implement this to create new challenge types.
///
/// Extension Point: Create a new class implementing IChallengeGenerator, then register it
/// in the ChallengeService constructor's _generators list.
/// </summary>
public interface IChallengeGenerator
{
    /// <summary>
    /// Unique type identifier stored in DailyChallenge.ChallengeType.
    /// </summary>
    string ChallengeType { get; }

    /// <summary>
    /// Generate a personalized challenge for the player.
    /// </summary>
    DailyChallenge Generate(Player player);

    /// <summary>
    /// Check if a completed battle satisfies challenge requirements.
    /// Returns true if the battle counts toward progress.
    /// </summary>
    bool CheckProgress(DailyChallenge challenge, Battle battle);
}
