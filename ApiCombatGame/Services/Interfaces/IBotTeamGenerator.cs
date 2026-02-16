using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

/// <summary>
/// Generates team compositions and strategies for AI bot players.
/// </summary>
public interface IBotTeamGenerator
{
    /// <summary>
    /// Creates a team for a bot player based on their rating tier.
    /// Team composition scales with rating: higher rating = better units and strategies.
    /// </summary>
    /// <param name="bot">The bot player to create a team for</param>
    /// <param name="teamNumber">Team slot number (for team name)</param>
    /// <returns>The created team with units and strategy</returns>
    Task<Team> CreateBotTeamAsync(Player bot, int teamNumber = 1);

    /// <summary>
    /// Ensures a bot has at least one team. Creates one if needed.
    /// </summary>
    Task EnsureBotHasTeamAsync(Player bot);
}
