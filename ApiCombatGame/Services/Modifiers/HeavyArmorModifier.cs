using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Modifiers;

/// <summary>
/// Example modifier: All units gain +50% defense, healer abilities 2x effectiveness.
/// Demonstrates how to implement a global stat modifier.
/// </summary>
public class HeavyArmorModifier : BaseModifierEffect
{
    public override string Name => "Heavy Armor";
    public override string Description => "All units gain +50% defense, healer abilities 2x effectiveness";

    public override void ModifyUnitStats(Unit unit)
    {
        // All units gain +50% defense
        unit.Defense = (int)(unit.Defense * 1.5);
    }

    public override void ApplyToBattle(BattleContext context)
    {
        // Double healing multiplier for the entire battle
        // TODO: Integrate with battle engine's healing calculation
        context.HealingMultiplier *= 2.0;
        context.CustomData["HeavyArmorActive"] = true;
    }
}
