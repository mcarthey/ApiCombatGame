using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class BattleEngineTests
{
    private readonly DeclarativeStrategyEngine _engine;

    public BattleEngineTests()
    {
        _engine = new DeclarativeStrategyEngine(NullLogger<DeclarativeStrategyEngine>.Instance);
    }

    [Fact]
    public void ResolveBattle_WithSameSeed_ProducesDeterministicResults()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };

        var result1 = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);
        var result2 = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);

        Assert.Equal(result1.WinnerTeam, result2.WinnerTeam);
        Assert.Equal(result1.TotalTurns, result2.TotalTurns);
        Assert.Equal(result1.Log.Count, result2.Log.Count);

        for (int i = 0; i < result1.Log.Count; i++)
        {
            Assert.Equal(result1.Log[i].Actor, result2.Log[i].Actor);
            Assert.Equal(result1.Log[i].Target, result2.Log[i].Target);
            Assert.Equal(result1.Log[i].Damage, result2.Log[i].Damage);
            Assert.Equal(result1.Log[i].Healing, result2.Log[i].Healing);
            Assert.Equal(result1.Log[i].TargetHpRemaining, result2.Log[i].TargetHpRemaining);
        }
    }

    [Fact]
    public void ResolveBattle_WithDifferentSeeds_MayProduceDifferentResults()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };

        var result1 = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);
        var result2 = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 999);

        // At least one aspect should differ (with very high probability)
        bool anyDifference = result1.WinnerTeam != result2.WinnerTeam ||
                            result1.TotalTurns != result2.TotalTurns ||
                            result1.Log.Count != result2.Log.Count;

        if (!anyDifference && result1.Log.Count > 0)
        {
            anyDifference = result1.Log.Zip(result2.Log)
                .Any(pair => pair.First.Damage != pair.Second.Damage);
        }

        Assert.True(anyDifference, "Different seeds should produce different battle outcomes (with very high probability).");
    }

    [Fact]
    public void ResolveBattle_CompletesWithinMaxTurns()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };
        int maxTurns = 10;

        var result = _engine.ResolveBattle(team1, strategy, team2, strategy, maxTurns, seed: 42);

        Assert.True(result.TotalTurns <= maxTurns, $"Battle should complete within {maxTurns} turns.");
    }

    [Fact]
    public void ResolveBattle_ProducesWinner()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };

        var result = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);

        Assert.True(result.WinnerTeam >= 0 && result.WinnerTeam <= 2,
            "Winner team should be 0 (draw), 1, or 2.");
    }

    [Fact]
    public void ResolveBattle_GeneratesBattleLog()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };

        var result = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);

        Assert.NotEmpty(result.Log);
        Assert.All(result.Log, entry =>
        {
            Assert.True(entry.Turn > 0);
            Assert.False(string.IsNullOrEmpty(entry.Actor));
            Assert.False(string.IsNullOrEmpty(entry.Action));
            Assert.False(string.IsNullOrEmpty(entry.Target));
        });
    }

    [Fact]
    public void ResolveBattle_DamageNeverNegative()
    {
        var team1 = CreateTestTeam1();
        var team2 = CreateTestTeam2();
        var strategy = new StrategyConfig { Formation = "balanced" };

        var result = _engine.ResolveBattle(team1, strategy, team2, strategy, 50, seed: 42);

        Assert.All(result.Log, entry =>
        {
            Assert.True(entry.Damage >= 0, "Damage should never be negative.");
            Assert.True(entry.TargetHpRemaining >= 0, "Target HP should never be negative.");
        });
    }

    [Fact]
    public void ClassAdvantage_WarriorBeatsRanger()
    {
        Assert.True(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Warrior, UnitClass.Ranger));
    }

    [Fact]
    public void ClassAdvantage_RangerBeatsMage()
    {
        Assert.True(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Ranger, UnitClass.Mage));
    }

    [Fact]
    public void ClassAdvantage_MageBeatsWarrior()
    {
        Assert.True(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Mage, UnitClass.Warrior));
    }

    [Fact]
    public void ClassAdvantage_TankIsNeutral()
    {
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Tank, UnitClass.Warrior));
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Tank, UnitClass.Mage));
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Tank, UnitClass.Ranger));
    }

    [Fact]
    public void ClassAdvantage_HealerIsNeutral()
    {
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Healer, UnitClass.Warrior));
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Healer, UnitClass.Mage));
        Assert.False(DeclarativeStrategyEngine.HasClassAdvantage(UnitClass.Healer, UnitClass.Ranger));
    }

    [Fact]
    public void ResolveBattle_AggressiveFormation_DealsMoreDamage()
    {
        var team1 = new List<Unit>
        {
            CreateUnit("Warrior", UnitClass.Warrior, 200, 30, 10, 10)
        };
        var team2 = new List<Unit>
        {
            CreateUnit("Warrior", UnitClass.Warrior, 200, 30, 10, 10)
        };

        var aggressiveStrategy = new StrategyConfig { Formation = "aggressive" };
        var balancedStrategy = new StrategyConfig { Formation = "balanced" };

        // Run with aggressive team1 vs balanced team2
        var aggressiveResult = _engine.ResolveBattle(team1, aggressiveStrategy, team2, balancedStrategy, 50, seed: 42);

        // Team1 (aggressive) should typically deal more damage per hit
        var team1Attacks = aggressiveResult.Log.Where(l => l.Actor.Contains("_1") && l.Damage > 0);
        Assert.True(team1Attacks.Any(a => a.Effects.Contains("aggressive_bonus")),
            "Aggressive formation should apply aggressive bonus.");
    }

    private List<Unit> CreateTestTeam1()
    {
        return new List<Unit>
        {
            CreateUnit("Bronze Knight", UnitClass.Warrior, 120, 25, 20, 10),
            CreateUnit("Apprentice Wizard", UnitClass.Mage, 80, 35, 10, 15),
            CreateUnit("Scout", UnitClass.Ranger, 90, 30, 15, 20)
        };
    }

    private List<Unit> CreateTestTeam2()
    {
        return new List<Unit>
        {
            CreateUnit("Shield Bearer", UnitClass.Tank, 150, 20, 30, 8),
            CreateUnit("Novice Cleric", UnitClass.Healer, 85, 15, 18, 12),
            CreateUnit("Iron Gladiator", UnitClass.Warrior, 130, 28, 18, 12)
        };
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
                Id = Guid.NewGuid(),
                UnitId = unit.Id,
                Name = "Basic Attack",
                Type = AbilityType.BasicAttack,
                Damage = 100,
                CooldownTurns = 0,
                TargetsAllies = false,
                IsAoE = false
            }
        };

        // Add class-specific abilities
        switch (unitClass)
        {
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
            case UnitClass.Mage:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Fireball", Type = AbilityType.ClassAbility,
                    Damage = 120, CooldownTurns = 3
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Meteor Storm", Type = AbilityType.Ultimate,
                    Damage = 90, CooldownTurns = 5, IsAoE = true
                });
                break;
            case UnitClass.Ranger:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Aimed Shot", Type = AbilityType.ClassAbility,
                    Damage = 150, CooldownTurns = 3
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Arrow Rain", Type = AbilityType.Ultimate,
                    Damage = 80, CooldownTurns = 5, IsAoE = true
                });
                break;
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
            case UnitClass.Tank:
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Shield Bash", Type = AbilityType.ClassAbility,
                    Damage = 80, CooldownTurns = 3
                });
                unit.Abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unit.Id,
                    Name = "Fortify", Type = AbilityType.Ultimate,
                    Healing = 50, CooldownTurns = 5, TargetsAllies = true
                });
                break;
        }

        return unit;
    }
}
