using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IStrategyMarketplaceService
{
    Task<Strategy> UploadStrategy(Guid playerId, string name, string description, string strategyJson, int price);
    Task<List<Strategy>> BrowseStrategies(string sortBy, int limit, int offset);
    Task<Strategy> DownloadStrategy(Guid strategyId, Guid playerId);
    Task RateStrategy(Guid strategyId, Guid playerId, int rating, string comment);
    Task ApplyStrategyDecay();
}
