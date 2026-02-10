using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Modifiers;

/// <summary>
/// Strategy pattern interface for environmental modifier effects.
/// Implement this to create new modifier types.
///
/// Extension Point: Create a new class implementing IModifierEffect, then register it
/// in the ModifierService constructor's _modifiers dictionary.
/// </summary>
public interface IModifierEffect
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Apply this modifier's effects to a battle context.
    /// Called once at the start of battle resolution.
    /// </summary>
    void ApplyToBattle(BattleContext context);

    /// <summary>
    /// Modify unit stats based on this modifier.
    /// Called for each unit before battle begins.
    /// </summary>
    void ModifyUnitStats(Unit unit);
}

/// <summary>
/// Shared battle context passed to modifiers and boss abilities.
/// Contains mutable state that effects can modify.
/// </summary>
public class BattleContext
{
    public List<Unit> Team1Units { get; set; } = new();
    public List<Unit> Team2Units { get; set; } = new();
    public int CurrentTurn { get; set; }
    public double DamageMultiplier { get; set; } = 1.0;
    public double HealingMultiplier { get; set; } = 1.0;
    public Dictionary<string, object> CustomData { get; set; } = new();
}
