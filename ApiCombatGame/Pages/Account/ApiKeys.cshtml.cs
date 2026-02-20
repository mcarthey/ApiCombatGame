using System.Security.Claims;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class ApiKeysModel : PageModel
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysModel> _logger;

    public ApiKeysModel(IApiKeyService apiKeyService, ILogger<ApiKeysModel> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    public List<ApiKey> ApiKeys { get; set; } = new();
    public string? NewlyCreatedKey { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        ApiKeys = await _apiKeyService.GetApiKeysAsync(GetPlayerId());
    }

    public async Task<IActionResult> OnPostCreateAsync(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
        {
            ErrorMessage = "Key name is required.";
            ApiKeys = await _apiKeyService.GetApiKeysAsync(GetPlayerId());
            return Page();
        }

        try
        {
            var (key, plainTextKey) = await _apiKeyService.CreateApiKeyAsync(GetPlayerId(), keyName);
            NewlyCreatedKey = plainTextKey;
            ApiKeys = await _apiKeyService.GetApiKeysAsync(GetPlayerId());
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            ApiKeys = await _apiKeyService.GetApiKeysAsync(GetPlayerId());
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid apiKeyId)
    {
        try
        {
            await _apiKeyService.RevokeApiKeyAsync(GetPlayerId(), apiKeyId);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        ApiKeys = await _apiKeyService.GetApiKeysAsync(GetPlayerId());
        return Page();
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim?.Value == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("PlayerId claim not found.");
        return playerId;
    }
}
