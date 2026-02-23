using System.Reflection;
using ApiCombatGame.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Admin-only environment diagnostics for monitoring and troubleshooting.
/// </summary>
[ApiController]
[Route("api/v1/admin/diagnostics")]
[Authorize(Policy = "Admin")]
[Tags("Admin")]
public class AdminDiagnosticsController : ControllerBase
{
    private static readonly DateTime StartupTime = DateTime.UtcNow;
    private readonly GameDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminDiagnosticsController(GameDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>Get environment diagnostics snapshot.</summary>
    /// <response code="200">Diagnostics including uptime, DB connectivity, and runtime info.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiagnostics()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "unknown";

        bool dbHealthy;
        int tableCount = 0;
        try
        {
            dbHealthy = await _context.Database.CanConnectAsync();
            if (dbHealthy)
            {
                tableCount = await _context.Players.AsNoTracking().CountAsync();
            }
        }
        catch
        {
            dbHealthy = false;
        }

        return Ok(new DiagnosticsResponse
        {
            Version = version,
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Uptime = DateTime.UtcNow - StartupTime,
            ServerTimeUtc = DateTime.UtcNow,
            Runtime = $".NET {System.Environment.Version}",
            OsPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ProcessMemoryMb = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1),
            DatabaseHealthy = dbHealthy,
            PlayerCount = tableCount,
            HostedServicesConfigured = CountHostedServices(),
            SmtpConfigured = !string.IsNullOrEmpty(_configuration["Email:SmtpHost"])
        });
    }

    private int CountHostedServices()
    {
        try
        {
            var services = HttpContext.RequestServices.GetServices<IHostedService>();
            return services.Count();
        }
        catch
        {
            return -1;
        }
    }
}

public class DiagnosticsResponse
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public DateTime ServerTimeUtc { get; set; }
    public string Runtime { get; set; } = string.Empty;
    public string OsPlatform { get; set; } = string.Empty;
    public double ProcessMemoryMb { get; set; }
    public bool DatabaseHealthy { get; set; }
    public int PlayerCount { get; set; }
    public int HostedServicesConfigured { get; set; }
    public bool SmtpConfigured { get; set; }
}
