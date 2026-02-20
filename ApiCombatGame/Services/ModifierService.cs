using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Services.Modifiers;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ModifierService : IModifierService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly Dictionary<string, IModifierEffect> _modifiers;

    public ModifierService(GameDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;

        // Register available modifiers (Extension Point: add new modifiers here)
        _modifiers = new Dictionary<string, IModifierEffect>
        {
            { "ArcaneDisruption", new ArcaneDisruptionModifier() },
            { "HeavyArmor", new HeavyArmorModifier() },
            // TODO: Add more modifiers as they're implemented
            // { "FrostBite", new FrostBiteModifier() },
            // { "BloodMoon", new BloodMoonModifier() },
        };
    }

    public async Task<EnvironmentalModifier?> GetCurrentModifier()
    {
        var now = DateTime.UtcNow;
        return await _context.EnvironmentalModifiers
            .FirstOrDefaultAsync(m => m.IsActive && m.StartDate <= now && m.EndDate >= now);
    }

    public async Task<EnvironmentalModifier?> GetUpcomingModifier()
    {
        var now = DateTime.UtcNow;
        return await _context.EnvironmentalModifiers
            .Where(m => m.StartDate > now)
            .OrderBy(m => m.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task RotateModifier()
    {
        var now = DateTime.UtcNow;

        // Deactivate expired modifiers
        var expiredModifiers = await _context.EnvironmentalModifiers
            .Where(m => m.IsActive && m.EndDate <= now)
            .ToListAsync();

        foreach (var modifier in expiredModifiers)
        {
            modifier.IsActive = false;
        }

        // Activate upcoming modifier if its start date has arrived
        var nextModifier = await _context.EnvironmentalModifiers
            .Where(m => !m.IsActive && m.StartDate <= now && m.EndDate > now)
            .FirstOrDefaultAsync();

        if (nextModifier != null)
        {
            nextModifier.IsActive = true;

            // Notify all active players about the new modifier
            var activePlayers = await _context.Players
                .Where(p => !p.IsDeleted)
                .Select(p => p.Id)
                .Take(500)
                .ToListAsync();

            foreach (var pid in activePlayers)
            {
                await _notifications.SendAsync(pid, NotificationType.NewModifierActive,
                    "New Modifier Active!", $"{nextModifier.Name} is now active — {nextModifier.Description}");
            }
        }

        await _context.SaveChangesAsync();
    }

    public IModifierEffect? GetModifierEffect(string modifierType)
    {
        return _modifiers.GetValueOrDefault(modifierType);
    }
}
