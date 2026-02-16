using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Application version information.
/// </summary>
[ApiController]
[Route("api/v1/version")]
[Tags("System")]
public class VersionController : ControllerBase
{
    /// <summary>Get application version information.</summary>
    /// <response code="200">Version information including build hash.</response>
    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "unknown";

        var buildDate = System.IO.File.GetLastWriteTimeUtc(assembly.Location);

        return Ok(new VersionResponse
        {
            Version = version,
            BuildDate = buildDate,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}

public class VersionResponse
{
    public string Version { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public string Environment { get; set; } = string.Empty;
}
