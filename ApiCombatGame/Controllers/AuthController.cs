using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Authentication endpoints for player registration and login.
/// </summary>
[ApiCategoryMeta("shield", "#3b82f6", Order = 1)]
[ApiController]
[Tags("Auth")]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new player account.</summary>
    /// <remarks>
    /// Creates a new player with 1000 starting currency and an empty unit roster.
    /// Returns a JWT token you can use immediately to start building your army.
    ///
    /// Usernames must be unique and between 3-50 characters. Passwords require at least 8 characters.
    /// </remarks>
    /// <param name="request">Registration credentials.</param>
    /// <response code="201">Account created. Contains your player ID and JWT token.</response>
    /// <response code="400">Validation failed (e.g., password too short).</response>
    /// <response code="409">Username or email already taken.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Save the JWT token from the response — you'll need it in the Authorization header for every subsequent request.")]
    [ApiGameTip("Choose your username carefully! It's permanent and visible to other players on leaderboards.")]
    [ApiPrerequisite("None — this is your starting point!")]
    [ApiExample("Register a new player", Request = "{\n  \"username\": \"DragonSlayer42\",\n  \"email\": \"player@example.com\",\n  \"password\": \"SecurePass123!\"\n}", Response = "{\n  \"playerId\": \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\",\n  \"username\": \"DragonSlayer42\",\n  \"token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\",\n  \"expiresAt\": \"2025-06-15T15:30:00Z\"\n}")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Created($"/api/v1/player/profile", response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Authenticate and receive a JWT token.</summary>
    /// <remarks>
    /// Returns a JWT Bearer token valid for 60 minutes. Include it in the Authorization header
    /// of all subsequent requests as `Bearer &lt;token&gt;`.
    /// </remarks>
    /// <param name="request">Login credentials (username and password).</param>
    /// <response code="200">Authentication successful. Contains player ID, token, and expiration.</response>
    /// <response code="401">Invalid username or password.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Tokens expire after 60 minutes. Set a timer or use the /auth/refresh endpoint before it lapses to avoid re-entering credentials.")]
    [ApiPrerequisite("Register an account")]
    [ApiExample("Login with credentials", Request = "{\n  \"username\": \"DragonSlayer42\",\n  \"password\": \"SecurePass123!\"\n}", Response = "{\n  \"playerId\": \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\",\n  \"username\": \"DragonSlayer42\",\n  \"token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\",\n  \"expiresAt\": \"2025-06-15T16:30:00Z\"\n}")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Refresh an expiring JWT token.</summary>
    /// <remarks>
    /// Exchange your current valid token for a fresh one with a new expiration.
    /// Call this before your token expires to maintain an active session without re-entering credentials.
    /// </remarks>
    /// <response code="200">New token issued.</response>
    /// <response code="401">Current token is invalid or already expired.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Set up an auto-refresh timer in your code to call this endpoint every 50 minutes, so your session never drops mid-battle.")]
    [ApiPrerequisite("Register or login to get a token")]
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _authService.RefreshTokenAsync(playerId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
