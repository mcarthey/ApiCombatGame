using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Modifiers;

namespace ApiCombatGame.Services.Bosses;

/// <summary>
/// Plugin architecture interface for boss abilities.
/// Implement this to create new boss mechanics.
///
/// Extension Point: Create a new class implementing IBossAbility, then serialize its
/// type name into the GuildBoss.AbilitiesJson field when spawning a boss.
/// </summary>
public interface IBossAbility
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Execute this ability during a boss battle.
    /// </summary>
    void Execute(GuildBoss boss, BattleContext context);

    /// <summary>
    /// Check if this ability should trigger this turn.
    /// </summary>
    bool ShouldTrigger(GuildBoss boss, BattleContext context);
}
