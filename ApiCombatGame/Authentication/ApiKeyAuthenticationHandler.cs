using System.Security.Claims;
using System.Text.Encodings.Web;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly IServiceScopeFactory _scopeFactory;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var plainTextKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return AuthenticateResult.NoResult();

        using var scope = _scopeFactory.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var result = await apiKeyService.ValidateApiKeyAsync(plainTextKey);
        if (result == null)
            return AuthenticateResult.Fail("Invalid API key.");

        // Log usage
        var ip = Context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var endpoint = Request.Path.Value;

        context.ApiKeyUsageLogs.Add(new ApiKeyUsageLog
        {
            Id = Guid.NewGuid(),
            ApiKeyId = result.ApiKeyId,
            IpAddress = ip.Length > 45 ? ip[..45] : ip,
            UserAgent = string.IsNullOrEmpty(userAgent) ? null : (userAgent.Length > 500 ? userAgent[..500] : userAgent),
            Endpoint = string.IsNullOrEmpty(endpoint) ? null : (endpoint.Length > 200 ? endpoint[..200] : endpoint),
            Timestamp = DateTime.UtcNow
        });

        // New-IP detection: check if this IP has been seen before for this key
        var knownIp = await context.ApiKeyUsageLogs
            .AnyAsync(l => l.ApiKeyId == result.ApiKeyId && l.IpAddress == ip);

        if (!knownIp)
        {
            try
            {
                await notificationService.SendAsync(result.PlayerId,
                    NotificationType.ApiKeyNewIp,
                    "New IP Detected for API Key",
                    $"Your API key was used from a new IP address: {ip}. If this wasn't you, revoke the key immediately from your Account settings.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to send new-IP notification for player {PlayerId}", result.PlayerId);
            }
        }

        await context.SaveChangesAsync();

        // Build claims principal (same shape as JWT)
        var player = await context.Players.FindAsync(result.PlayerId);
        if (player == null)
            return AuthenticateResult.Fail("Player not found.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.PlayerId.ToString()),
            new(ClaimTypes.Name, player.Username),
            new("PlayerId", result.PlayerId.ToString()),
            new("CurrentTier", player.CurrentTier.ToString()),
        };

        if (player.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
