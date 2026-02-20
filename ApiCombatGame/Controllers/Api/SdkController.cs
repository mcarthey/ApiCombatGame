using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Sdk;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// SDK helper endpoints for client builders. No authentication required.
/// </summary>
[ApiController]
[Route("api/v1/sdk")]
[Tags("SDK")]
[ApiCategoryMeta("code", "#64748b", Order = 26)]
public class SdkController : ControllerBase
{
    private readonly ISdkService _sdkService;

    public SdkController(ISdkService sdkService)
    {
        _sdkService = sdkService;
    }

    /// <summary>Quick-start guide for new API consumers.</summary>
    /// <remarks>
    /// Returns a structured quick-start guide with step-by-step instructions,
    /// authentication details, and example code snippets in multiple languages.
    /// Use this to bootstrap your client application.
    /// </remarks>
    /// <response code="200">Quick-start guide with steps and code examples.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Start here! This endpoint gives you everything you need to build your first client.")]
    [HttpGet("quickstart")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType(typeof(QuickStartResponse), StatusCodes.Status200OK)]
    public ActionResult<QuickStartResponse> GetQuickStart()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var guide = _sdkService.GetQuickStart(baseUrl);
        return Ok(guide);
    }

    /// <summary>Full endpoint catalog organized by category.</summary>
    /// <remarks>
    /// Lists all available API endpoints grouped by feature category.
    /// Use this to discover endpoints and plan your client integration.
    /// </remarks>
    /// <response code="200">Categorized endpoint catalog.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Pair this with the OpenAPI spec at /openapi/v1.json for full request/response schemas.")]
    [HttpGet("endpoints")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType(typeof(EndpointCatalogResponse), StatusCodes.Status200OK)]
    public ActionResult<EndpointCatalogResponse> GetEndpointCatalog()
    {
        var catalog = _sdkService.GetEndpointCatalog();
        return Ok(catalog);
    }

    /// <summary>Game status and public metrics.</summary>
    /// <remarks>
    /// Returns server health status and public game metrics — total players,
    /// active seasons, running tournaments, and completed battles. Useful for
    /// monitoring dashboards and bot status checks.
    /// </remarks>
    /// <response code="200">Game status with public metrics.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Use this endpoint for health checks and status monitoring in your bot or client.")]
    [HttpGet("status")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(GameStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GameStatusResponse>> GetGameStatus()
    {
        var status = await _sdkService.GetGameStatusAsync();
        return Ok(status);
    }
}
