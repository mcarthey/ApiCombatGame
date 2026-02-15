using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IEasterEggService
{
    /// <summary>Check if there's an active egg on the given page for the current week.</summary>
    Task<EasterEgg?> GetActiveEggForPageAsync(string pagePath);

    /// <summary>Check if a player has already claimed a specific egg.</summary>
    Task<bool> HasPlayerClaimedAsync(Guid playerId, Guid eggId);

    /// <summary>Redeem an egg code and award random loot. Returns the claim, or null if invalid.</summary>
    Task<EggClaim?> RedeemCodeAsync(Guid playerId, string code);

    /// <summary>Get all claims for a player (history).</summary>
    Task<List<EggClaim>> GetPlayerClaimsAsync(Guid playerId);

    /// <summary>Get summary stats for the current week's hunt.</summary>
    Task<EggHuntStats> GetWeeklyStatsAsync(Guid playerId);

    /// <summary>Generate new eggs for the upcoming week (called by scheduled job or admin).</summary>
    Task<List<EasterEgg>> GenerateWeeklyEggsAsync(DateTime weekStart);
}

public class EggHuntStats
{
    public int TotalEggsThisWeek { get; set; }
    public int EggsFoundThisWeek { get; set; }
    public int TotalLootEarned { get; set; }
    public int AllTimeEggsFound { get; set; }
}
