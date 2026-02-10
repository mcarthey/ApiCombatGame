using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Modifiers;

namespace ApiCombatGame.Services.Bosses;

/// <summary>
/// Example boss ability: Defense increases by 10% each turn.
/// Demonstrates a cumulative turn-based boss mechanic.
/// </summary>
public class ScalesHardenAbility : BaseBossAbility
{
    public override string Name => "Scales Harden";
    public override string Description => "Defense increases by 10% each turn";

    private int _turnsElapsed = 0;

    public override void Execute(GuildBoss boss, BattleContext context)
    {
        _turnsElapsed++;
        var defenseMultiplier = 1.0 + (_turnsElapsed * 0.1);
        // TODO: Apply defense multiplier to boss stats in battle engine
        // boss.Defense = (int)(boss.Defense * defenseMultiplier);
        context.CustomData["BossDefenseMultiplier"] = defenseMultiplier;
    }

    public override bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        // Trigger at the start of each turn
        return true;
    }
}
