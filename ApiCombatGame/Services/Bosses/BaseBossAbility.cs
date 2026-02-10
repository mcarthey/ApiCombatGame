using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Modifiers;

namespace ApiCombatGame.Services.Bosses;

/// <summary>
/// Abstract base class for boss abilities. Provides default trigger behavior
/// so implementations only need to override Execute and conditionals.
/// </summary>
public abstract class BaseBossAbility : IBossAbility
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract void Execute(GuildBoss boss, BattleContext context);

    public virtual bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        return true; // Default: always trigger
    }
}
