using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Data;

public static class SeedData
{
    public static async Task InitializeAsync(GameDbContext context)
    {
        if (context.Units.Any(u => u.IsTemplate))
            return;

        var units = CreateTemplateUnits();
        context.Units.AddRange(units);
        await context.SaveChangesAsync();
    }

    public static List<Unit> CreateTemplateUnits()
    {
        var units = new List<Unit>();

        // Warriors
        units.Add(CreateUnit("Bronze Knight", UnitClass.Warrior, 120, 25, 20, 10, 200));
        units.Add(CreateUnit("Iron Gladiator", UnitClass.Warrior, 130, 28, 18, 12, 300));
        units.Add(CreateUnit("Steel Berserker", UnitClass.Warrior, 140, 30, 15, 15, 400));
        units.Add(CreateUnit("Silver Champion", UnitClass.Warrior, 150, 32, 22, 11, 500));
        units.Add(CreateUnit("Gold Warlord", UnitClass.Warrior, 160, 35, 25, 10, 600));

        // Mages
        units.Add(CreateUnit("Apprentice Wizard", UnitClass.Mage, 80, 35, 10, 15, 200));
        units.Add(CreateUnit("Fire Sorcerer", UnitClass.Mage, 85, 38, 12, 16, 300));
        units.Add(CreateUnit("Ice Conjurer", UnitClass.Mage, 90, 40, 10, 18, 400));
        units.Add(CreateUnit("Lightning Warlock", UnitClass.Mage, 95, 42, 13, 17, 500));
        units.Add(CreateUnit("Archmage", UnitClass.Mage, 100, 45, 15, 20, 600));

        // Rangers
        units.Add(CreateUnit("Scout", UnitClass.Ranger, 90, 30, 15, 20, 200));
        units.Add(CreateUnit("Hunter", UnitClass.Ranger, 95, 32, 16, 22, 300));
        units.Add(CreateUnit("Marksman", UnitClass.Ranger, 100, 35, 14, 25, 400));
        units.Add(CreateUnit("Sniper", UnitClass.Ranger, 105, 38, 17, 23, 500));
        units.Add(CreateUnit("Master Archer", UnitClass.Ranger, 110, 40, 18, 28, 600));

        // Healers
        units.Add(CreateUnit("Novice Cleric", UnitClass.Healer, 85, 15, 18, 12, 200));
        units.Add(CreateUnit("Priest", UnitClass.Healer, 90, 18, 20, 13, 300));
        units.Add(CreateUnit("Bishop", UnitClass.Healer, 95, 20, 22, 14, 400));
        units.Add(CreateUnit("High Priest", UnitClass.Healer, 100, 22, 24, 15, 500));
        units.Add(CreateUnit("Divine Oracle", UnitClass.Healer, 105, 25, 26, 16, 600));

        // Tanks
        units.Add(CreateUnit("Shield Bearer", UnitClass.Tank, 150, 20, 30, 8, 200));
        units.Add(CreateUnit("Fortress Guard", UnitClass.Tank, 160, 22, 32, 7, 300));
        units.Add(CreateUnit("Iron Wall", UnitClass.Tank, 170, 24, 35, 6, 400));
        units.Add(CreateUnit("Bastion", UnitClass.Tank, 180, 26, 38, 7, 500));
        units.Add(CreateUnit("Immovable", UnitClass.Tank, 200, 28, 40, 5, 600));

        return units;
    }

    private static Unit CreateUnit(string name, UnitClass unitClass, int hp, int atk, int def, int spd, int cost)
    {
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Class = unitClass,
            Health = hp,
            Attack = atk,
            Defense = def,
            Speed = spd,
            UnlockCost = cost,
            IsTemplate = true,
            PlayerId = null
        };

        unit.Abilities = CreateAbilitiesForClass(unit.Id, unitClass);

        return unit;
    }

    private static List<Ability> CreateAbilitiesForClass(Guid unitId, UnitClass unitClass)
    {
        var abilities = new List<Ability>();

        // Basic Attack (all classes)
        abilities.Add(new Ability
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            Name = "Basic Attack",
            Type = AbilityType.BasicAttack,
            Damage = 100, // 100% of attack stat
            Healing = 0,
            CooldownTurns = 0,
            Description = "A standard attack dealing damage based on Attack stat.",
            TargetsAllies = false,
            IsAoE = false
        });

        switch (unitClass)
        {
            case UnitClass.Warrior:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Cleave",
                    Type = AbilityType.ClassAbility,
                    Damage = 75, // 75% of attack to all enemies
                    Healing = 0,
                    CooldownTurns = 3,
                    Description = "Sweeping attack that hits all enemies for 75% damage.",
                    TargetsAllies = false,
                    IsAoE = true
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Berserker Rage",
                    Type = AbilityType.Ultimate,
                    Damage = 200, // 200% of attack
                    Healing = 0,
                    CooldownTurns = 5,
                    Description = "Devastating single-target strike for 200% damage.",
                    TargetsAllies = false,
                    IsAoE = false
                });
                break;

            case UnitClass.Mage:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Fireball",
                    Type = AbilityType.ClassAbility,
                    Damage = 120, // 120% of attack
                    Healing = 0,
                    CooldownTurns = 3,
                    Description = "Launches a fireball for 120% damage.",
                    TargetsAllies = false,
                    IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Meteor Storm",
                    Type = AbilityType.Ultimate,
                    Damage = 90, // 90% to all enemies
                    Healing = 0,
                    CooldownTurns = 5,
                    Description = "Rains meteors on all enemies for 90% damage each.",
                    TargetsAllies = false,
                    IsAoE = true
                });
                break;

            case UnitClass.Ranger:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Aimed Shot",
                    Type = AbilityType.ClassAbility,
                    Damage = 150, // 150% of attack
                    Healing = 0,
                    CooldownTurns = 3,
                    Description = "Carefully aimed shot for 150% damage.",
                    TargetsAllies = false,
                    IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Arrow Rain",
                    Type = AbilityType.Ultimate,
                    Damage = 80, // 80% to all enemies
                    Healing = 0,
                    CooldownTurns = 5,
                    Description = "Fires a volley of arrows hitting all enemies for 80% damage.",
                    TargetsAllies = false,
                    IsAoE = true
                });
                break;

            case UnitClass.Healer:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Heal",
                    Type = AbilityType.ClassAbility,
                    Damage = 0,
                    Healing = 150, // heals 150% of attack stat
                    CooldownTurns = 3,
                    Description = "Heals the most injured ally for 150% of Attack stat.",
                    TargetsAllies = true,
                    IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Divine Light",
                    Type = AbilityType.Ultimate,
                    Damage = 0,
                    Healing = 100, // heals all allies 100% of attack
                    CooldownTurns = 5,
                    Description = "Bathes all allies in divine light, healing 100% of Attack stat.",
                    TargetsAllies = true,
                    IsAoE = true
                });
                break;

            case UnitClass.Tank:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Shield Bash",
                    Type = AbilityType.ClassAbility,
                    Damage = 80, // 80% of attack, uses defense for bonus
                    Healing = 0,
                    CooldownTurns = 3,
                    Description = "Bash an enemy with your shield for 80% damage (scales with Defense).",
                    TargetsAllies = false,
                    IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(),
                    UnitId = unitId,
                    Name = "Fortify",
                    Type = AbilityType.Ultimate,
                    Damage = 0,
                    Healing = 50, // restores 50% of max HP
                    CooldownTurns = 5,
                    Description = "Fortifies defenses, restoring 50% of max HP.",
                    TargetsAllies = true,
                    IsAoE = false
                });
                break;
        }

        return abilities;
    }
}
