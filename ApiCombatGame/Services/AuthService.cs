using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ApiCombatGame.Services;

public class AuthService : IAuthService
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(GameDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if username or email already exists
        if (await _context.Players.AnyAsync(p => p.Username == request.Username))
            throw new InvalidOperationException("Username already taken.");

        if (await _context.Players.AnyAsync(p => p.Email == request.Email))
            throw new InvalidOperationException("Email already registered.");

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Level = 1,
            Currency = _config.GetValue<int>("GameSettings:StartingCurrency", 1000),
            Rating = _config.GetValue<int>("GameSettings:StartingRating", 1000),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        // Give the player 3 free starter units (one cheap from each of 3 random classes)
        var starterTemplates = await _context.Units
            .Include(u => u.Abilities)
            .Where(u => u.IsTemplate && u.UnlockCost == 200)
            .ToListAsync();

        var starterUnits = starterTemplates.Take(3).Select(template => new Unit
        {
            Id = Guid.NewGuid(),
            Name = template.Name,
            Class = template.Class,
            Level = 1,
            Health = template.Health,
            Attack = template.Attack,
            Defense = template.Defense,
            Speed = template.Speed,
            UnlockCost = 0,
            IsTemplate = false,
            PlayerId = player.Id,
            Abilities = template.Abilities.Select(a => new Ability
            {
                Id = Guid.NewGuid(),
                Name = a.Name,
                Type = a.Type,
                Damage = a.Damage,
                Healing = a.Healing,
                CooldownTurns = a.CooldownTurns,
                Description = a.Description,
                TargetsAllies = a.TargetsAllies,
                IsAoE = a.IsAoE
            }).ToList()
        }).ToList();

        // Assign abilities' UnitId after creation
        foreach (var unit in starterUnits)
        {
            foreach (var ability in unit.Abilities)
            {
                ability.UnitId = unit.Id;
            }
        }

        _context.Players.Add(player);
        _context.Units.AddRange(starterUnits);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New player registered: {Username} ({PlayerId})", player.Username, player.Id);

        // Fire-and-forget: don't let email failure block registration
        try { await _emailService.SendWelcomeEmailAsync(player.Email, player.Username); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send welcome email to {Email}", player.Email); }

        // Send verification email (fire-and-forget)
        try { await SendVerificationEmailAsync(player.Id); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send verification email to {Email}", player.Email); }

        return new AuthResponse
        {
            PlayerId = player.Id,
            Token = GenerateJwtToken(player),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("JWT:ExpirationMinutes", 60))
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Email == request.Email);

        if (player == null || !BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (player.IsDeleted)
            throw new UnauthorizedAccessException("Invalid email or password.");

        player.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player logged in: {Username} ({PlayerId})", player.Username, player.Id);

        return new AuthResponse
        {
            PlayerId = player.Id,
            Token = GenerateJwtToken(player),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("JWT:ExpirationMinutes", 60))
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null || player.IsDeleted)
            throw new UnauthorizedAccessException("Player not found.");

        return new AuthResponse
        {
            PlayerId = player.Id,
            Token = GenerateJwtToken(player),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("JWT:ExpirationMinutes", 60))
        };
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Email == email && !p.IsDeleted);

        if (player == null)
        {
            // Don't reveal whether email exists
            _logger.LogInformation("Password reset requested for unknown email {Email}", email);
            return;
        }

        // Generate URL-safe token
        var tokenBytes = Guid.NewGuid().ToByteArray();
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        player.PasswordResetToken = token;
        player.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var configUrl = _config["AppSettings:BaseUrl"]?.TrimEnd('/');
        var baseUrl = string.IsNullOrEmpty(configUrl) ? "https://apicombat.com" : configUrl;
        var resetLink = $"{baseUrl}/Auth/ResetPassword?token={token}";

        try { await _emailService.SendPasswordResetEmailAsync(player.Email, player.Username, resetLink); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send password reset email to {Email}", email); }

        _logger.LogInformation("Password reset token generated for {Username} ({PlayerId})", player.Username, player.Id);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.PasswordResetToken == token && !p.IsDeleted);

        if (player == null || player.PasswordResetExpiresAt == null || player.PasswordResetExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired password reset token attempted");
            return false;
        }

        player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        player.PasswordResetToken = null;
        player.PasswordResetExpiresAt = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset completed for {Username} ({PlayerId})", player.Username, player.Id);
        return true;
    }

    public async Task<bool> DeleteAccountAsync(Guid playerId, string password)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null || player.IsDeleted)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(password, player.PasswordHash))
            return false;

        // Capture original info for confirmation email before anonymizing
        var originalEmail = player.Email;
        var originalUsername = player.Username;

        // Soft delete: anonymize PII
        var anonId = Guid.NewGuid().ToString("N");
        player.IsDeleted = true;
        player.DeletedAt = DateTime.UtcNow;
        player.Username = $"deleted-{anonId}";
        player.Email = $"deleted-{anonId}@removed";
        player.PasswordHash = string.Empty;
        player.PasswordResetToken = null;
        player.PasswordResetExpiresAt = null;
        player.EmailConfirmationToken = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Account deleted (soft) for {Username} ({PlayerId})", originalUsername, playerId);

        // Fire-and-forget confirmation email
        try { await _emailService.SendAccountDeletionEmailAsync(originalEmail, originalUsername); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send account deletion email to {Email}", originalEmail); }

        return true;
    }

    public async Task SendVerificationEmailAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null || player.IsDeleted || player.EmailConfirmed)
            return;

        var tokenBytes = Guid.NewGuid().ToByteArray();
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        player.EmailConfirmationToken = token;
        player.EmailConfirmationExpiresAt = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        var configUrl = _config["AppSettings:BaseUrl"]?.TrimEnd('/');
        var baseUrl = string.IsNullOrEmpty(configUrl) ? "https://apicombat.com" : configUrl;
        var verifyLink = $"{baseUrl}/Auth/VerifyEmail?token={token}";

        try { await _emailService.SendVerificationEmailAsync(player.Email, player.Username, verifyLink); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send verification email to {Email}", player.Email); }
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.EmailConfirmationToken == token && !p.IsDeleted);

        if (player == null || (player.EmailConfirmationExpiresAt.HasValue && player.EmailConfirmationExpiresAt < DateTime.UtcNow))
            return false;

        player.EmailConfirmed = true;
        player.EmailConfirmationToken = null;
        player.EmailConfirmationExpiresAt = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verified for {Username} ({PlayerId})", player.Username, player.Id);
        return true;
    }

    private string GenerateJwtToken(Player player)
    {
        var secret = _config["JWT:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
            new Claim(ClaimTypes.Name, player.Username),
            new Claim(ClaimTypes.Email, player.Email),
            new Claim("PlayerId", player.Id.ToString()),
            new Claim("CurrentTier", player.CurrentTier.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["JWT:Issuer"],
            audience: _config["JWT:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("JWT:ExpirationMinutes", 60)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
