using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildTreasuryService
{
    Task<(Guild guild, List<(string Id, string Name, string Description, int Cost, bool CanAfford, bool AlreadyPurchased)> upgrades)> GetTreasuryAsync(Guid guildId);
    Task<Guild> PurchaseUpgradeAsync(Guid guildId, Guid playerId, string upgradeId);
    Task<Guild> DepositAsync(Guid guildId, Guid playerId, int amount);
}
