using ApiCombatGame.Models.DTOs.Sdk;

namespace ApiCombatGame.Services.Interfaces;

public interface ISdkService
{
    QuickStartResponse GetQuickStart(string baseUrl);
    EndpointCatalogResponse GetEndpointCatalog();
    Task<GameStatusResponse> GetGameStatusAsync();
}
