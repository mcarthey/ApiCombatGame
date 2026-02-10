using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ApiCombatGame.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(GameDbContext context, ILogger<SettingsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public SettingsInputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string CurrentUsername { get; set; } = string.Empty;
    public string CurrentEmail { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }

    public async Task OnGetAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        CurrentUsername = player.Username;
        CurrentEmail = player.Email;
        MemberSince = player.CreatedAt;
        Input.Email = player.Email;
    }

    public async Task<IActionResult> OnPostUpdateEmailAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(Input.Email))
        {
            ErrorMessage = "Email is required.";
            await LoadPlayerInfo(player);
            return Page();
        }

        // Check if email is already in use
        var exists = await _context.Players.AnyAsync(p => p.Email == Input.Email && p.Id != playerId);
        if (exists)
        {
            ErrorMessage = "This email is already in use.";
            await LoadPlayerInfo(player);
            return Page();
        }

        player.Email = Input.Email;
        await _context.SaveChangesAsync();

        SuccessMessage = "Email updated successfully.";
        await LoadPlayerInfo(player);
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return RedirectToPage("/Auth/Login");

        if (string.IsNullOrWhiteSpace(Input.CurrentPassword) || string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            ErrorMessage = "Both current and new passwords are required.";
            await LoadPlayerInfo(player);
            return Page();
        }

        if (!BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, player.PasswordHash))
        {
            ErrorMessage = "Current password is incorrect.";
            await LoadPlayerInfo(player);
            return Page();
        }

        if (Input.NewPassword.Length < 8)
        {
            ErrorMessage = "New password must be at least 8 characters.";
            await LoadPlayerInfo(player);
            return Page();
        }

        player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
        await _context.SaveChangesAsync();

        SuccessMessage = "Password changed successfully.";
        await LoadPlayerInfo(player);
        return Page();
    }

    private Task LoadPlayerInfo(Models.Domain.Player player)
    {
        CurrentUsername = player.Username;
        CurrentEmail = player.Email;
        MemberSince = player.CreatedAt;
        Input.Email = player.Email;
        return Task.CompletedTask;
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }

    public class SettingsInputModel
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [MinLength(8)]
        public string? NewPassword { get; set; }
    }
}
