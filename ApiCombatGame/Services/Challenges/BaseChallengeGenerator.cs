using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Challenges;

/// <summary>
/// Template method base class for challenge generators.
/// Subclasses implement Generate and CheckProgress.
/// </summary>
public abstract class BaseChallengeGenerator : IChallengeGenerator
{
    public abstract string ChallengeType { get; }
    public abstract DailyChallenge Generate(Player player);
    public abstract bool CheckProgress(DailyChallenge challenge, Battle battle);
}
