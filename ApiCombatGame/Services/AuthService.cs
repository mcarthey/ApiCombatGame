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
    private readonly ILogger<AuthService> _logger;

    public AuthService(GameDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
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
        if (player == null)
            throw new UnauthorizedAccessException("Player not found.");

        return new AuthResponse
        {
            PlayerId = player.Id,
            Token = GenerateJwtToken(player),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("JWT:ExpirationMinutes", 60))
        };
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
