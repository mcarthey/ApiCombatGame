namespace ApiCombatGame.Services.Interfaces;

/// <summary>
/// Generates dev-culture themed names for AI bot players.
/// </summary>
public interface IBotNameGenerator
{
    /// <summary>
    /// Generates a unique bot name with dev-culture flair.
    /// Format: [Prefix]_[Number] (e.g., "CodeMonkey_42")
    /// </summary>
    string GenerateBotName();

    /// <summary>
    /// Generates a specified number of unique bot names.
    /// </summary>
    List<string> GenerateBotNames(int count);
}
