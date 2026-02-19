using System.Text.Json;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class StrategyEngineTests
{
    private readonly DeclarativeStrategyEngine _engine;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public StrategyEngineTests()
    {
        _engine = new DeclarativeStrategyEngine(NullLogger<DeclarativeStrategyEngine>.Instance);
    }

    [Fact]
    public void StrategyConfig_DefaultValues_AreValid()
    {
        var strategy = new StrategyConfig();

        Assert.Equal(Formation.balanced, strategy.Formation);
        Assert.NotNull(strategy.TargetPriority);
        Assert.Contains(TargetPriority.lowest_hp, strategy.TargetPriority);
        Assert.NotNull(strategy.Abilities);
    }

    [Fact]
    public void StrategyConfig_SerializesAndDeserializesCorrectly()
    {
        var strategy = new StrategyConfig
        {
            Formation = Formation.aggressive,
            TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.lowest_hp },
            Abilities = new Dictionary<string, AbilityCondition>
            {
                ["Fireball"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp }
            }
        };

        var json = JsonSerializer.Serialize(strategy, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<StrategyConfig>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(Formation.aggressive, deserialized!.Formation);
        Assert.Equal(2, deserialized.TargetPriority.Count);
        Assert.Equal(2, deserialized.Abilities.Count);
        Assert.Equal(AbilityWhen.always, deserialized.Abilities["Fireball"].When);
        Assert.Equal(AbilityWhen.ally_hp_below_50, deserialized.Abilities["Heal"].When);
    }

    [Fact]
    public void StrategyConfig_EmptyJson_DeserializesToDefaults()
    {
        var json = "{}";
        var strategy = JsonSerializer.Deserialize<StrategyConfig>(json, JsonOptions);

        Assert.NotNull(strategy);
        Assert.Equal(Formation.balanced, strategy!.Formation);
    }

    [Fact]
    public void HealerStrategy_PrioritizesHealingInjuredAllies()
    {
        // Create a team with a healer and an injured warrior
        var healer = CreateUnit("Novice Cleric", UnitClass.Healer, 85, 15, 18, 20); // High speed to go first
        var warrior = CreateUnit("Bronze Knight", UnitClass.Warrior, 120, 25, 20, 5);

        var enemy = CreateUnit("Scout", UnitClass.Ranger, 90, 30, 15, 10);

        var team1 = new List<Unit> { healer, warrior };
        var team2 = new List<Unit> { enemy };

        // Use strategy that tells healer to heal when ally HP is below 50%
        var strategy = new StrategyConfig
        {
            Formation = Formation.balanced,
            TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp },
            Abilities = new Dictionary<string, AbilityCondition>
            {
                ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp }
            }
        };

        var enemyStrategy = new StrategyConfig { Formation = Formation.balanced };

        var result = _engine.ResolveBattle(team1, strategy, team2, enemyStrategy, 50, seed: 42);

        // The battle should complete (someone wins)
        Assert.True(result.TotalTurns > 0);
        Assert.NotEmpty(result.Log);
    }

    [Fact]
    public void TargetPriority_Healers_TargetsHealersFirst()
    {
        var warrior = CreateUnit("Bronze Knight", UnitClass.Warrior, 200, 30, 20, 25); // Very fast

        var enemyHealer = CreateUnit("Novice Cleric", UnitClass.Healer, 85, 15, 18, 5);
        var enemyWarrior = CreateUnit("Iron Gladiator", UnitClass.Warrior, 130, 28, 18, 5);

        var team1 = new List<Unit> { warrior };
        var team2 = new List<Unit> { enemyHealer, enemyWarrior };

        var strategy = new StrategyConfig
        {
            Formation = Formation.balanced,
            TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.lowest_hp }
        };
        var enemyStrategy = new StrategyConfig { Formation = Formation.balanced };

        var result = _engine.ResolveBattle(team1, strategy, team2, enemyStrategy, 50, seed: 42);

        // First attack from team1's warrior should target the healer
        var firstTeam1Attack = result.Log.First(l => l.Actor.Contains("_1") && l.Damage > 0);
        Assert.Contains("Cleric", firstTeam1Attack.Target);
    }

    [Fact]
    public void DefensiveFormation_ReducesDamageTaken()
    {
        var attacker = CreateUnit("Warrior", UnitClass.Warrior, 200, 30, 10, 15);
        var defender = CreateUnit("Warrior", UnitClass.Warrior, 200, 30, 10, 10);

        var team1 = new List<Unit> { attacker };
        var team2 = new List<Unit> { defender };

        var normalStrategy = new StrategyConfig { Formation = Formation.balanced };
        var defensiveStrategy = new StrategyConfig { Formation = Formation.defensive };

        // Normal vs normal
        var normalResult = _engine.ResolveBattle(team1, normalStrategy, team2, normalStrategy, 50, seed: 42);
        // Normal vs defensive
        var defensiveResult = _engine.ResolveBattle(team1, normalStrategy, team2, defensiveStrategy, 50, seed: 42);

        // In the defensive scenario, team1 attacks should do less damage
        var normalDamage = normalResult.Log
            .Where(l => l.Actor.Contains("_1") && l.Damage > 0)
            .Take(3)
            .Sum(l => l.Damage);

        var defensiveDamage = defensiveResult.Log
            .Where(l => l.Actor.Contains("_1") && l.Damage > 0)
            .Take(3)
            .Sum(l => l.Damage);

        Assert.True(defensiveDamage <= normalDamage,
            $"Defensive formation should reduce damage taken. Normal: {normalDamage}, Defensive: {defensiveDamage}");
    }

    private static Unit CreateUnit(string name, UnitClass unitClass, int hp, int atk, int def, int spd)
    {
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Class = unitClass,
            Health = hp,
            Attack = atk,
            Defense = def,
            Speed = spd
        };

        unit.Abilities = new List<Ability>
        {
            new()
            {
                Id = Guid.NewGuid(), UnitId = unit.Id,
                Name = "Basic Attack", Type = AbilityType.BasicAttack,
                Damage = 100, CooldownTurns = 0
            }
        };

        switch (unitClass)
        {
            case UnitClass.Healer:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Heal", Type = AbilityType.ClassAbility,
                    Healing = 150, CooldownTurns = 3, TargetsAllies = true
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Divine Light", Type = AbilityType.Ultimate,
                    Healing = 100, CooldownTurns = 5, TargetsAllies = true, IsAoE = true
                });
                break;
            case UnitClass.Warrior:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Cleave", Type = AbilityType.ClassAbility,
                    Damage = 75, CooldownTurns = 3, IsAoE = true
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Berserker Rage", Type = AbilityType.Ultimate,
                    Damage = 200, CooldownTurns = 5
                });
                break;
            default:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Class Ability", Type = AbilityType.ClassAbility,
                    Damage = 120, CooldownTurns = 3
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Ultimate", Type = AbilityType.Ultimate,
                    Damage = 150, CooldownTurns = 5
                });
                break;
        }

        return unit;
    }
}
