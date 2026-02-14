using ApiCombatGame.Models.DTOs.Loot;

namespace ApiCombatGame.Services.Interfaces;

public interface ILootService
{
    /// <summary>Roll for loot drops after a battle. Returns any drops earned.</summary>
    Task<List<LootDropResponse>> RollLootAsync(Guid playerId, Guid battleId, bool won, int winStreak);

    /// <summary>Get all unclaimed loot drops for a player.</summary>
    Task<PendingLootResponse> GetPendingLootAsync(Guid playerId);

    /// <summary>Claim all pending loot drops, awarding currency/XP/titles.</summary>
    Task<ClaimLootResponse> ClaimAllLootAsync(Guid playerId);
}
