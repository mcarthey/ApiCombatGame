using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IMasteryService
{
    Task<List<UnitMastery>> GetPlayerMastery(Guid playerId);
    Task<UnitMastery?> GetUnitMastery(Guid playerId, Guid unitId);
    Task IncrementMastery(Guid playerId, Guid unitId, bool won);
}
