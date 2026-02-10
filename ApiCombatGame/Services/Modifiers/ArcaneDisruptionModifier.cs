using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Modifiers;

/// <summary>
/// Example modifier: Mage abilities are weakened, physical attacks deal +20% damage.
/// Demonstrates how to implement a class-targeted modifier.
/// </summary>
public class ArcaneDisruptionModifier : BaseModifierEffect
{
    public override string Name => "Arcane Disruption";
    public override string Description => "Mage abilities cost 2x mana, physical attacks deal +20% damage";

    public override void ModifyUnitStats(Unit unit)
    {
        if (unit.Class == UnitClass.Mage)
        {
            // Reduce mage attack by 30% to simulate disruption
            // TODO: When mana system is implemented, double mana costs instead
            unit.Attack = (int)(unit.Attack * 0.7);
        }
        else if (unit.Class == UnitClass.Warrior || unit.Class == UnitClass.Ranger)
        {
            // Physical attackers get +20% damage
            unit.Attack = (int)(unit.Attack * 1.2);
        }
    }

    public override void ApplyToBattle(BattleContext context)
    {
        // Could set a flag in CustomData for the battle engine to check
        context.CustomData["ArcaneDisrupted"] = true;
    }
}
