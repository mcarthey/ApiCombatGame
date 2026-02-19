using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Services;

public class DeclarativeStrategyEngine : IStrategyEngine
{
    private readonly ILogger<DeclarativeStrategyEngine> _logger;

    public DeclarativeStrategyEngine(ILogger<DeclarativeStrategyEngine> logger)
    {
        _logger = logger;
    }

    public BattleResolution ResolveBattle(
        List<Unit> team1Units,
        StrategyConfig team1Strategy,
        List<Unit> team2Units,
        StrategyConfig team2Strategy,
        int maxTurns = 50,
        int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var log = new List<BattleLogEntry>();

        // Create battle state copies and apply formations from strategy configs
        var team1 = team1Units.Select(u => new BattleUnitState(u, 1) { Formation = team1Strategy.Formation }).ToList();
        var team2 = team2Units.Select(u => new BattleUnitState(u, 2) { Formation = team2Strategy.Formation }).ToList();

        int turn = 0;

        while (turn < maxTurns)
        {
            turn++;

            // Get all alive units sorted by speed (descending)
            var allUnits = team1.Concat(team2)
                .Where(u => u.CurrentHp > 0)
                .OrderByDescending(u => u.Speed)
                .ThenBy(_ => rng.Next()) // random tiebreaker
                .ToList();

            foreach (var actor in allUnits)
            {
                if (actor.CurrentHp <= 0) continue;

                var allies = actor.TeamNumber == 1 ? team1 : team2;
                var enemies = actor.TeamNumber == 1 ? team2 : team1;
                var strategy = actor.TeamNumber == 1 ? team1Strategy : team2Strategy;

                var aliveAllies = allies.Where(u => u.CurrentHp > 0).ToList();
                var aliveEnemies = enemies.Where(u => u.CurrentHp > 0).ToList();

                if (aliveEnemies.Count == 0) break;

                // Tick cooldowns
                actor.TickCooldowns();

                // Determine action
                var (ability, targets, isHeal) = DetermineAction(actor, aliveAllies, aliveEnemies, strategy, rng);

                if (isHeal)
                {
                    foreach (var target in targets)
                    {
                        int healAmount = CalculateHealing(actor, ability);
                        target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + healAmount);

                        log.Add(new BattleLogEntry
                        {
                            Turn = turn,
                            Actor = $"{actor.Name}_{actor.TeamNumber}",
                            Action = ability.Name,
                            Target = $"{target.Name}_{target.TeamNumber}",
                            Damage = 0,
                            Healing = healAmount,
                            Effects = new List<string> { "heal" },
                            TargetHpRemaining = target.CurrentHp
                        });
                    }
                }
                else
                {
                    foreach (var target in targets)
                    {
                        var effects = new List<string>();
                        int damage = CalculateDamage(actor, target, ability, rng, effects);
                        target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

                        log.Add(new BattleLogEntry
                        {
                            Turn = turn,
                            Actor = $"{actor.Name}_{actor.TeamNumber}",
                            Action = ability.Name,
                            Target = $"{target.Name}_{target.TeamNumber}",
                            Damage = damage,
                            Healing = 0,
                            Effects = effects,
                            TargetHpRemaining = target.CurrentHp
                        });
                    }
                }

                // Set cooldown for non-basic abilities
                if (ability.CooldownTurns > 0)
                {
                    actor.SetCooldown(ability.Name, ability.CooldownTurns);
                }

                // Check victory
                if (team1.All(u => u.CurrentHp <= 0) || team2.All(u => u.CurrentHp <= 0))
                    break;
            }

            // Check victory after full turn
            if (team1.All(u => u.CurrentHp <= 0) || team2.All(u => u.CurrentHp <= 0))
                break;
        }

        // Determine winner
        bool team1Alive = team1.Any(u => u.CurrentHp > 0);
        bool team2Alive = team2.Any(u => u.CurrentHp > 0);

        int winner;
        if (team1Alive && !team2Alive) winner = 1;
        else if (team2Alive && !team1Alive) winner = 2;
        else if (team1Alive && team2Alive)
        {
            // Tiebreaker: team with more total HP remaining wins
            int team1Hp = team1.Sum(u => u.CurrentHp);
            int team2Hp = team2.Sum(u => u.CurrentHp);
            winner = team1Hp >= team2Hp ? 1 : 2;
        }
        else
        {
            winner = 0; // Draw (both wiped on same turn)
        }

        return new BattleResolution
        {
            WinnerTeam = winner,
            TotalTurns = turn,
            Log = log
        };
    }

    private (BattleAbility ability, List<BattleUnitState> targets, bool isHeal) DetermineAction(
        BattleUnitState actor,
        List<BattleUnitState> allies,
        List<BattleUnitState> enemies,
        StrategyConfig strategy,
        Random rng)
    {
        // Try abilities from Ultimate → ClassAbility → BasicAttack
        var sortedAbilities = actor.Abilities
            .OrderByDescending(a => a.Type)
            .ToList();

        foreach (var ability in sortedAbilities)
        {
            if (ability.CooldownTurns > 0 && actor.IsOnCooldown(ability.Name))
                continue;

            // Check strategy conditions
            if (strategy.Abilities.TryGetValue(ability.Name, out var condition))
            {
                if (!EvaluateCondition(condition.When, actor, allies, enemies))
                    continue;

                var targets = SelectTargets(condition.Target, ability, actor, allies, enemies, strategy);
                if (targets.Count > 0)
                    return (ability, targets, ability.TargetsAllies);
            }
            else
            {
                // Default behavior for abilities not in strategy
                if (ability.TargetsAllies)
                {
                    // Healers default: heal when ally below 50%
                    var injured = allies.Where(a => a.CurrentHp < a.MaxHp * 0.5).ToList();
                    if (injured.Count > 0 || ability.Type == AbilityType.BasicAttack)
                    {
                        var targets = ability.IsAoE
                            ? allies
                            : new List<BattleUnitState> { allies.OrderBy(a => (double)a.CurrentHp / a.MaxHp).First() };
                        return (ability, targets, true);
                    }
                }
                else
                {
                    var targets = ability.IsAoE
                        ? enemies
                        : SelectTargetByPriority(strategy.TargetPriority, enemies);
                    if (targets.Count > 0)
                        return (ability, targets, false);
                }
            }
        }

        // Fallback: basic attack on first enemy
        var basicAttack = actor.Abilities.First(a => a.Type == AbilityType.BasicAttack);
        return (basicAttack, new List<BattleUnitState> { enemies.First() }, false);
    }

    private bool EvaluateCondition(AbilityWhen condition, BattleUnitState actor, List<BattleUnitState> allies, List<BattleUnitState> enemies)
    {
        return condition switch
        {
            AbilityWhen.always => true,
            AbilityWhen.ally_hp_below_50 => allies.Any(a => (double)a.CurrentHp / a.MaxHp < 0.5),
            AbilityWhen.ally_hp_below_30 => allies.Any(a => (double)a.CurrentHp / a.MaxHp < 0.3),
            AbilityWhen.enemy_count_gte_2 => enemies.Count >= 2,
            AbilityWhen.enemy_count_gte_3 => enemies.Count >= 3,
            AbilityWhen.self_hp_below_50 => (double)actor.CurrentHp / actor.MaxHp < 0.5,
            _ => true
        };
    }

    private List<BattleUnitState> SelectTargets(AbilityTarget targetMode, BattleAbility ability, BattleUnitState actor,
        List<BattleUnitState> allies, List<BattleUnitState> enemies, StrategyConfig strategy)
    {
        if (ability.IsAoE)
            return ability.TargetsAllies ? allies : enemies;

        if (ability.TargetsAllies)
        {
            return targetMode switch
            {
                AbilityTarget.lowest_ally_hp => new List<BattleUnitState> { allies.OrderBy(a => a.CurrentHp).First() },
                AbilityTarget.self => new List<BattleUnitState> { actor },
                _ => new List<BattleUnitState> { allies.OrderBy(a => (double)a.CurrentHp / a.MaxHp).First() }
            };
        }

        return targetMode switch
        {
            AbilityTarget.priority => SelectTargetByPriority(strategy.TargetPriority, enemies),
            AbilityTarget.lowest_hp => new List<BattleUnitState> { enemies.OrderBy(e => e.CurrentHp).First() },
            AbilityTarget.highest_threat => new List<BattleUnitState> { enemies.OrderByDescending(e => e.Attack).First() },
            AbilityTarget.all_enemies => enemies,
            _ => SelectTargetByPriority(strategy.TargetPriority, enemies)
        };
    }

    private List<BattleUnitState> SelectTargetByPriority(List<TargetPriority> priorities, List<BattleUnitState> enemies)
    {
        foreach (var priority in priorities)
        {
            switch (priority)
            {
                case TargetPriority.lowest_hp:
                    return new List<BattleUnitState> { enemies.OrderBy(e => e.CurrentHp).First() };
                case TargetPriority.healers:
                    var healers = enemies.Where(e => e.Class == UnitClass.Healer).ToList();
                    if (healers.Count > 0) return new List<BattleUnitState> { healers.First() };
                    break;
                case TargetPriority.highest_threat:
                    return new List<BattleUnitState> { enemies.OrderByDescending(e => e.Attack).First() };
                case TargetPriority.mages:
                    var mages = enemies.Where(e => e.Class == UnitClass.Mage).ToList();
                    if (mages.Count > 0) return new List<BattleUnitState> { mages.First() };
                    break;
                case TargetPriority.tanks:
                    var tanks = enemies.Where(e => e.Class == UnitClass.Tank).ToList();
                    if (tanks.Count > 0) return new List<BattleUnitState> { tanks.First() };
                    break;
            }
        }

        // Default: lowest HP
        return new List<BattleUnitState> { enemies.OrderBy(e => e.CurrentHp).First() };
    }

    private int CalculateDamage(BattleUnitState attacker, BattleUnitState target, BattleAbility ability, Random rng, List<string> effects)
    {
        // Ability damage is a percentage of the attack stat
        int effectiveAttack = (int)(attacker.Attack * (ability.Damage / 100.0));

        // Tank Shield Bash gets bonus from defense
        if (attacker.Class == UnitClass.Tank && ability.Name == "Shield Bash")
        {
            effectiveAttack += attacker.Defense / 2;
        }

        int baseDamage = effectiveAttack - target.Defense;
        if (baseDamage < 1) baseDamage = 1;

        // Formation bonus
        if (attacker.Formation == Formation.aggressive)
        {
            baseDamage = (int)(baseDamage * 1.15);
            effects.Add("aggressive_bonus");
        }
        else if (target.Formation == Formation.defensive)
        {
            baseDamage = (int)(baseDamage * 0.85);
            effects.Add("defensive_reduction");
        }

        // Critical hit (10% chance)
        if (rng.NextDouble() < 0.1)
        {
            baseDamage = (int)(baseDamage * 1.5);
            effects.Add("critical_hit");
        }

        // Class advantage
        if (HasClassAdvantage(attacker.Class, target.Class))
        {
            baseDamage = (int)(baseDamage * 1.2);
            effects.Add("class_advantage");
        }
        else if (HasClassAdvantage(target.Class, attacker.Class))
        {
            baseDamage = (int)(baseDamage * 0.8);
            effects.Add("class_disadvantage");
        }

        return Math.Max(1, baseDamage);
    }

    private int CalculateHealing(BattleUnitState healer, BattleAbility ability)
    {
        int healAmount = (int)(healer.Attack * (ability.Healing / 100.0));

        // Tank Fortify uses max HP instead
        if (healer.Class == UnitClass.Tank && ability.Name == "Fortify")
        {
            healAmount = (int)(healer.MaxHp * 0.5);
        }

        return Math.Max(1, healAmount);
    }

    public static bool HasClassAdvantage(UnitClass attacker, UnitClass defender)
    {
        return (attacker, defender) switch
        {
            (UnitClass.Warrior, UnitClass.Ranger) => true,
            (UnitClass.Ranger, UnitClass.Mage) => true,
            (UnitClass.Mage, UnitClass.Warrior) => true,
            _ => false
        };
    }
}

/// <summary>
/// Runtime state for a unit during battle.
/// </summary>
public class BattleUnitState
{
    public string Name { get; }
    public UnitClass Class { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; set; }
    public int Attack { get; }
    public int Defense { get; }
    public int Speed { get; }
    public int TeamNumber { get; }
    public Formation Formation { get; set; } = Formation.balanced;
    public List<BattleAbility> Abilities { get; }
    private Dictionary<string, int> Cooldowns { get; } = new();

    public BattleUnitState(Unit unit, int teamNumber)
    {
        Name = unit.Name;
        Class = unit.Class;
        MaxHp = unit.Health;
        CurrentHp = unit.Health;
        Attack = unit.Attack;
        Defense = unit.Defense;
        Speed = unit.Speed;
        TeamNumber = teamNumber;
        Abilities = unit.Abilities.Select(a => new BattleAbility
        {
            Name = a.Name,
            Type = a.Type,
            Damage = a.Damage,
            Healing = a.Healing,
            CooldownTurns = a.CooldownTurns,
            TargetsAllies = a.TargetsAllies,
            IsAoE = a.IsAoE
        }).ToList();
    }

    // Test constructor
    public BattleUnitState(string name, UnitClass unitClass, int hp, int atk, int def, int spd, int teamNumber, List<BattleAbility>? abilities = null)
    {
        Name = name;
        Class = unitClass;
        MaxHp = hp;
        CurrentHp = hp;
        Attack = atk;
        Defense = def;
        Speed = spd;
        TeamNumber = teamNumber;
        Abilities = abilities ?? new List<BattleAbility>
        {
            new() { Name = "Basic Attack", Type = AbilityType.BasicAttack, Damage = 100, CooldownTurns = 0 }
        };
    }

    public void TickCooldowns()
    {
        var keys = Cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            Cooldowns[key]--;
            if (Cooldowns[key] <= 0)
                Cooldowns.Remove(key);
        }
    }

    public bool IsOnCooldown(string abilityName) => Cooldowns.ContainsKey(abilityName);

    public void SetCooldown(string abilityName, int turns)
    {
        Cooldowns[abilityName] = turns;
    }
}

public class BattleAbility
{
    public string Name { get; set; } = string.Empty;
    public AbilityType Type { get; set; }
    public int Damage { get; set; }
    public int Healing { get; set; }
    public int CooldownTurns { get; set; }
    public bool TargetsAllies { get; set; }
    public bool IsAoE { get; set; }
}
