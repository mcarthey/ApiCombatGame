using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Modifiers;

namespace ApiCombatGame.Services.Bosses;

/// <summary>
/// Example boss ability: At 25% HP, attack doubles.
/// Demonstrates a threshold-triggered boss mechanic.
/// </summary>
public class EnrageAbility : BaseBossAbility
{
    public override string Name => "Enrage";
    public override string Description => "At 25% HP, attack doubles and speed increases";

    private bool _hasEnraged = false;

    public override void Execute(GuildBoss boss, BattleContext context)
    {
        if (!_hasEnraged)
        {
            boss.Attack *= 2;
            _hasEnraged = true;
            // TODO: Add visual indicator in battle log
            context.CustomData["BossEnraged"] = true;
        }
    }

    public override bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        return !_hasEnraged && boss.CurrentHp <= boss.MaxHp * 0.25;
    }
}
