using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Modifiers;

/// <summary>
/// Abstract base class for modifier effects. Provides no-op defaults
/// so implementations only need to override what they change.
/// </summary>
public abstract class BaseModifierEffect : IModifierEffect
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual void ApplyToBattle(BattleContext context)
    {
        // Default: no battle-wide effects
    }

    public virtual void ModifyUnitStats(Unit unit)
    {
        // Default: no stat modifications
    }
}
