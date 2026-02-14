using ApiCombatGame.Models.DTOs.Cosmetic;

namespace ApiCombatGame.Services.Interfaces;

public interface ICosmeticService
{
    Task<CosmeticShopResponse> GetShopAsync(Guid playerId);
    Task<List<PlayerCosmeticResponse>> GetOwnedCosmeticsAsync(Guid playerId);
    Task<PlayerCosmeticResponse> PurchaseCosmeticAsync(Guid playerId, Guid cosmeticId, string paymentMethod);
    Task EquipCosmeticAsync(Guid playerId, Guid cosmeticId);
    Task<GemBalanceResponse> GetGemBalanceAsync(Guid playerId);
}
