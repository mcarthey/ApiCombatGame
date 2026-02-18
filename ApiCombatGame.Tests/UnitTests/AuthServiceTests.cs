using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly GameDbContext _context;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly IConfiguration _config;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<AuthService>>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!!!",
                ["JWT:Issuer"] = "ApiCombatGame.Tests",
                ["JWT:Audience"] = "ApiCombatGame.Tests",
                ["JWT:ExpirationMinutes"] = "60",
                ["GameSettings:StartingCurrency"] = "1000",
                ["GameSettings:StartingRating"] = "1000",
                ["AppSettings:BaseUrl"] = "https://test.apicombat.com"
            })
            .Build();

        _service = new AuthService(_context, _config, _emailService.Object, _logger.Object);

        // Seed template units with abilities for registration tests
        SeedTemplateUnits();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTemplateUnits()
    {
        var templates = new[]
        {
            new Unit
            {
                Id = Guid.NewGuid(), Name = "Squire", Class = UnitClass.Warrior,
                Health = 120, Attack = 18, Defense = 14, Speed = 10,
                IsTemplate = true, UnlockCost = 200,
                Abilities = new List<Ability>
                {
                    new() { Id = Guid.NewGuid(), Name = "Basic Attack", Type = AbilityType.BasicAttack, Damage = 15, Description = "A basic strike" },
                    new() { Id = Guid.NewGuid(), Name = "Shield Bash", Type = AbilityType.ClassAbility, Damage = 20, CooldownTurns = 2, Description = "Bash with shield" }
                }
            },
            new Unit
            {
                Id = Guid.NewGuid(), Name = "Apprentice", Class = UnitClass.Mage,
                Health = 80, Attack = 25, Defense = 8, Speed = 12,
                IsTemplate = true, UnlockCost = 200,
                Abilities = new List<Ability>
                {
                    new() { Id = Guid.NewGuid(), Name = "Basic Attack", Type = AbilityType.BasicAttack, Damage = 12, Description = "Magic bolt" },
                    new() { Id = Guid.NewGuid(), Name = "Fireball", Type = AbilityType.ClassAbility, Damage = 30, CooldownTurns = 3, Description = "Ball of fire" }
                }
            },
            new Unit
            {
                Id = Guid.NewGuid(), Name = "Acolyte", Class = UnitClass.Healer,
                Health = 90, Attack = 10, Defense = 12, Speed = 14,
                IsTemplate = true, UnlockCost = 200,
                Abilities = new List<Ability>
                {
                    new() { Id = Guid.NewGuid(), Name = "Basic Attack", Type = AbilityType.BasicAttack, Damage = 8, Description = "Staff whack" },
                    new() { Id = Guid.NewGuid(), Name = "Heal", Type = AbilityType.ClassAbility, Healing = 25, CooldownTurns = 2, Description = "Restore HP", TargetsAllies = true }
                }
            }
        };

        // Set UnitId on abilities
        foreach (var t in templates)
            foreach (var a in t.Abilities)
                a.UnitId = t.Id;

        _context.Units.AddRange(templates);
        _context.SaveChanges();
    }

    // ==================== Register Tests ====================

    [Fact]
    public async Task Register_CreatesPlayerWith1000StartingRating()
    {
        var result = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "newplayer",
            Email = "new@test.com",
            Password = "SecurePass123!"
        });

        var player = await _context.Players.FindAsync(result.PlayerId);
        Assert.NotNull(player);
        Assert.Equal(1000, player!.Rating);
        Assert.Equal(1000, player.Currency);
    }

    [Fact]
    public async Task Register_CreatesStarterUnitsWithAbilities()
    {
        // BUG REGRESSION: Starter units were created without abilities
        var result = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "abilitytest",
            Email = "ability@test.com",
            Password = "SecurePass123!"
        });

        var units = await _context.Units
            .Include(u => u.Abilities)
            .Where(u => u.PlayerId == result.PlayerId)
            .ToListAsync();

        Assert.Equal(3, units.Count);
        Assert.All(units, u => Assert.True(u.Abilities.Count > 0,
            $"Unit '{u.Name}' should have abilities but has {u.Abilities.Count}"));
    }

    [Fact]
    public async Task Register_StarterUnitsHaveCorrectAbilityCount()
    {
        var result = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "counttest",
            Email = "count@test.com",
            Password = "SecurePass123!"
        });

        var units = await _context.Units
            .Include(u => u.Abilities)
            .Where(u => u.PlayerId == result.PlayerId)
            .ToListAsync();

        // Each template has 2 abilities (BasicAttack + ClassAbility)
        Assert.All(units, u => Assert.Equal(2, u.Abilities.Count));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await _service.RegisterAsync(new RegisterRequest
        {
            Username = "first",
            Email = "dupe@test.com",
            Password = "SecurePass123!"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequest
            {
                Username = "second",
                Email = "dupe@test.com",
                Password = "SecurePass123!"
            }));

        Assert.Contains("Email already registered", ex.Message);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Throws()
    {
        await _service.RegisterAsync(new RegisterRequest
        {
            Username = "dupeuser",
            Email = "first@test.com",
            Password = "SecurePass123!"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterRequest
            {
                Username = "dupeuser",
                Email = "second@test.com",
                Password = "SecurePass123!"
            }));

        Assert.Contains("Username already taken", ex.Message);
    }

    // ==================== Login Tests ====================

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await _service.RegisterAsync(new RegisterRequest
        {
            Username = "logintest",
            Email = "login@test.com",
            Password = "MyPassword123!"
        });

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "login@test.com",
            Password = "MyPassword123!"
        });

        Assert.NotNull(result.Token);
        Assert.NotEqual(Guid.Empty, result.PlayerId);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        await _service.RegisterAsync(new RegisterRequest
        {
            Username = "wrongpw",
            Email = "wrongpw@test.com",
            Password = "CorrectPassword123!"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest
            {
                Email = "wrongpw@test.com",
                Password = "WrongPassword456!"
            }));
    }

    [Fact]
    public async Task Login_DeletedAccount_Throws()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "deletedlogin",
            Email = "deleted@test.com",
            Password = "SecurePass123!"
        });

        // Soft-delete the account
        await _service.DeleteAccountAsync(auth.PlayerId, "SecurePass123!");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest
            {
                Email = "deleted@test.com",
                Password = "SecurePass123!"
            }));
    }

    [Fact]
    public async Task Login_NonexistentEmail_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest
            {
                Email = "doesnotexist@test.com",
                Password = "SomePassword123!"
            }));
    }

    // ==================== Password Reset Tests ====================

    [Fact]
    public async Task RequestPasswordReset_ValidEmail_SetsTokenAndExpiry()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "resetplayer",
            Email = "reset@test.com",
            Password = "SecurePass123!"
        });

        await _service.RequestPasswordResetAsync("reset@test.com");

        var player = await _context.Players.FindAsync(auth.PlayerId);
        Assert.NotNull(player!.PasswordResetToken);
        Assert.NotNull(player.PasswordResetExpiresAt);
        Assert.True(player.PasswordResetExpiresAt > DateTime.UtcNow);
        Assert.True(player.PasswordResetExpiresAt <= DateTime.UtcNow.AddHours(1).AddMinutes(1));
    }

    [Fact]
    public async Task RequestPasswordReset_UnknownEmail_NoError()
    {
        // Should not throw or reveal that email doesn't exist
        await _service.RequestPasswordResetAsync("nonexistent@test.com");

        // No exception = pass
    }

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesHash()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "resetvalid",
            Email = "resetvalid@test.com",
            Password = "OldPassword123!"
        });

        await _service.RequestPasswordResetAsync("resetvalid@test.com");
        var player = await _context.Players.FindAsync(auth.PlayerId);
        var token = player!.PasswordResetToken!;

        var result = await _service.ResetPasswordAsync(token, "NewPassword456!");

        Assert.True(result);

        // Verify can login with new password
        var loginResult = await _service.LoginAsync(new LoginRequest
        {
            Email = "resetvalid@test.com",
            Password = "NewPassword456!"
        });
        Assert.NotNull(loginResult.Token);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsFalse()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "resetexp",
            Email = "resetexp@test.com",
            Password = "SecurePass123!"
        });

        await _service.RequestPasswordResetAsync("resetexp@test.com");
        var player = await _context.Players.FindAsync(auth.PlayerId);
        var token = player!.PasswordResetToken!;

        // Manually expire the token
        player.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _context.SaveChangesAsync();

        var result = await _service.ResetPasswordAsync(token, "NewPassword456!");

        Assert.False(result);
    }

    // ==================== Email Verification Tests ====================

    [Fact]
    public async Task SendVerificationEmail_SetsTokenAndExpiry()
    {
        // BUG REGRESSION: Verification tokens had no expiry
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "verifyplayer",
            Email = "verify@test.com",
            Password = "SecurePass123!"
        });

        // Registration already sends verification email, check token was set
        var player = await _context.Players.FindAsync(auth.PlayerId);
        Assert.NotNull(player!.EmailConfirmationToken);
        Assert.NotNull(player.EmailConfirmationExpiresAt);
        Assert.True(player.EmailConfirmationExpiresAt <= DateTime.UtcNow.AddHours(24).AddMinutes(1));
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_SetsConfirmed()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "verifyvalid",
            Email = "verifyvalid@test.com",
            Password = "SecurePass123!"
        });

        var player = await _context.Players.FindAsync(auth.PlayerId);
        var token = player!.EmailConfirmationToken!;

        var result = await _service.VerifyEmailAsync(token);

        Assert.True(result);
        await _context.Entry(player).ReloadAsync();
        Assert.True(player.EmailConfirmed);
        Assert.Null(player.EmailConfirmationToken); // Token consumed
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_ReturnsFalse()
    {
        // BUG REGRESSION: Expired tokens were being accepted
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "verifyexp",
            Email = "verifyexp@test.com",
            Password = "SecurePass123!"
        });

        var player = await _context.Players.FindAsync(auth.PlayerId);
        var token = player!.EmailConfirmationToken!;

        // Manually expire the token
        player.EmailConfirmationExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _context.SaveChangesAsync();

        var result = await _service.VerifyEmailAsync(token);

        Assert.False(result);
    }

    // ==================== Account Deletion Tests ====================

    [Fact]
    public async Task DeleteAccount_ValidPassword_SoftDeletes()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "deleteme",
            Email = "deleteme@test.com",
            Password = "SecurePass123!"
        });

        var result = await _service.DeleteAccountAsync(auth.PlayerId, "SecurePass123!");

        Assert.True(result);

        var player = await _context.Players.FindAsync(auth.PlayerId);
        Assert.True(player!.IsDeleted);
        Assert.NotNull(player.DeletedAt);
        Assert.StartsWith("deleted-", player.Username);
        Assert.Contains("@removed", player.Email);
        Assert.Equal(string.Empty, player.PasswordHash);
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_Throws()
    {
        var auth = await _service.RegisterAsync(new RegisterRequest
        {
            Username = "nodelete",
            Email = "nodelete@test.com",
            Password = "SecurePass123!"
        });

        var result = await _service.DeleteAccountAsync(auth.PlayerId, "WrongPassword!");

        Assert.False(result);

        var player = await _context.Players.FindAsync(auth.PlayerId);
        Assert.False(player!.IsDeleted);
    }
}
