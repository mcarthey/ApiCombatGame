using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.AI;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AiOpponentService : IAiOpponentService
{
    private readonly GameDbContext _context;
    private readonly IStrategyEngine _strategyEngine;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IAchievementService _achievementService;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;
    private readonly IConfiguration _config;
    private readonly ILogger<AiOpponentService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Reward multiplier for practice battles (50% of normal).</summary>
    private const decimal PracticeRewardMultiplier = 0.5m;

    public AiOpponentService(
        GameDbContext context,
        IStrategyEngine strategyEngine,
        IPlayerProgressionService progressionService,
        IAchievementService achievementService,
        INotificationService notifications,
        IActivityLedger ledger,
        IConfiguration config,
        ILogger<AiOpponentService> logger)
    {
        _context = context;
        _strategyEngine = strategyEngine;
        _progressionService = progressionService;
        _achievementService = achievementService;
        _notifications = notifications;
        _ledger = ledger;
        _config = config;
        _logger = logger;
    }

    public AiOpponentListResponse GetAvailableOpponents()
    {
        return new AiOpponentListResponse
        {
            Opponents = AiPresets.Select(p => new AiOpponentResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Difficulty = p.Difficulty,
                ApproximateRating = p.ApproximateRating,
                TeamSize = p.Units.Count,
                TeamClasses = p.Units.Select(u => u.Class.ToString()).ToList(),
                Formation = p.Strategy.Formation.ToString()
            }).ToList(),
            RewardMultiplier = PracticeRewardMultiplier,
            RatingAffected = false,
            CountsTowardDailyLimit = false
        };
    }

    public async Task<BattleResultResponse> FightAiOpponentAsync(Guid playerId, PracticeBattleRequest request)
    {
        var preset = AiPresets.FirstOrDefault(p => p.Id == request.OpponentId)
            ?? throw new KeyNotFoundException($"AI opponent '{request.OpponentId}' not found.");

        // Validate team ownership
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId && t.PlayerId == playerId)
            ?? throw new InvalidOperationException("Team not found or doesn't belong to you.");

        var playerUnitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson, JsonOptions) ?? new();
        if (playerUnitIds.Count == 0)
            throw new InvalidOperationException("Team has no units configured.");

        var playerUnits = await _context.Units
            .Include(u => u.Abilities)
            .Where(u => playerUnitIds.Contains(u.Id))
            .ToListAsync();

        if (playerUnits.Count == 0)
            throw new InvalidOperationException("No valid units found in team.");

        var playerStrategy = !string.IsNullOrEmpty(team.StrategyJson) && team.StrategyJson != "{}"
            ? JsonSerializer.Deserialize<StrategyConfig>(team.StrategyJson, JsonOptions) ?? new StrategyConfig()
            : new StrategyConfig();

        // Generate AI units (in-memory, not persisted)
        var aiUnits = GenerateAiUnits(preset);

        int maxTurns = _config.GetValue<int>("GameSettings:MaxTurnsPerBattle", 50);

        // Resolve battle synchronously — no matchmaking needed
        var resolution = _strategyEngine.ResolveBattle(
            playerUnits, playerStrategy,
            aiUnits, preset.Strategy,
            maxTurns);

        bool playerWon = resolution.WinnerTeam == 1;
        bool isDraw = resolution.WinnerTeam == 0;

        // Create battle record (mode = "practice")
        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = playerId,
            Player2Id = null,
            Team1Id = request.TeamId,
            Team2Id = null,
            Status = BattleStatus.Completed,
            Mode = "practice",
            Turns = resolution.TotalTurns,
            BattleLogJson = JsonSerializer.Serialize(resolution.Log, JsonOptions),
            Team1ClassesJson = JsonSerializer.Serialize(
                playerUnits.Select(u => u.Class.ToString()).ToList(), JsonOptions),
            Team2ClassesJson = JsonSerializer.Serialize(
                aiUnits.Select(u => u.Class.ToString()).ToList(), JsonOptions),
            WinnerId = playerWon ? playerId : null,
            Player1RatingChange = 0,
            Player2RatingChange = null,
            QueuedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _context.Battles.Add(battle);

        // Process reduced rewards (50% gold/XP, no rating change)
        int baseGold = playerWon ? 25 : 5; // Half of normal 50/10
        int baseXp = playerWon ? 50 : 12;  // Half of normal 100/25

        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new KeyNotFoundException("Player not found.");

        var oldCurrency = player.Currency;
        var oldXp = player.ExperiencePoints;
        var oldLevel = player.Level;

        // Apply tier multiplier to gold
        decimal tierMultiplier = _progressionService.GetGoldMultiplier(player.CurrentTier);
        int gold = (int)(baseGold * tierMultiplier);

        player.Currency += gold;
        player.ExperiencePoints += baseXp;
        battle.CurrencyReward = gold;

        // Level-up check
        int? newLevel = null;
        int xpNeeded = _progressionService.GetXpForLevel(player.Level);
        while (player.ExperiencePoints >= xpNeeded && player.Level < 100)
        {
            player.ExperiencePoints -= xpNeeded;
            player.Level++;
            player.Currency += 250;
            gold += 250;
            newLevel = player.Level;
            await _notifications.SendAsync(playerId, NotificationType.LevelUp, "Level Up!",
                $"You reached level {player.Level}! +250g bonus.");
            xpNeeded = _progressionService.GetXpForLevel(player.Level);
        }

        var practiceAction = playerWon ? "PracticeWon" : "PracticeLost";
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "AiOpponent", practiceAction, battle.Id);
        _ledger.LogPlayer(playerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "AiOpponent", practiceAction, battle.Id);
        _ledger.LogPlayer(playerId, "Level", oldLevel, player.Level, "LevelUp", "LevelUp", battle.Id);

        // Practice battles don't affect win streak or rating, but track achievement
        try
        {
            if (playerWon)
                await _achievementService.CheckAndAwardAsync(playerId, "battle_won");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Achievement check failed for practice battle {BattleId}", battle.Id);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Practice battle {BattleId}: Player {PlayerId} vs AI '{AiName}' — {Result} in {Turns} turns",
            battle.Id, playerId, preset.Name, playerWon ? "Won" : isDraw ? "Draw" : "Lost", resolution.TotalTurns);

        return new BattleResultResponse
        {
            BattleId = battle.Id,
            Status = "completed",
            WinnerId = battle.WinnerId,
            LoserId = playerWon ? null : playerId,
            Turns = resolution.TotalTurns,
            BattleLog = resolution.Log,
            Rewards = new BattleRewards
            {
                Currency = gold,
                RatingChange = 0,
                ExperienceEarned = baseXp,
                WinStreak = player.WinStreak,
                FirstBattleBonus = false,
                NewLevel = newLevel,
                TierMultiplier = tierMultiplier
            },
            CompletedAt = battle.CompletedAt
        };
    }

    // ─────────────────────────────────────────────────
    //  AI Opponent Presets
    // ─────────────────────────────────────────────────

    private static List<Unit> GenerateAiUnits(AiPreset preset)
    {
        return preset.Units.Select(template =>
        {
            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                Class = template.Class,
                Health = template.Health,
                Attack = template.Attack,
                Defense = template.Defense,
                Speed = template.Speed,
                Level = 1,
                IsTemplate = false,
                PlayerId = null
            };
            unit.Abilities = CreateAbilitiesForClass(unit.Id, unit.Class);
            return unit;
        }).ToList();
    }

    private static List<Ability> CreateAbilitiesForClass(Guid unitId, UnitClass unitClass)
    {
        var abilities = new List<Ability>
        {
            new()
            {
                Id = Guid.NewGuid(), UnitId = unitId, Name = "Basic Attack",
                Type = AbilityType.BasicAttack, Damage = 100, CooldownTurns = 0,
                Description = "A standard attack.", TargetsAllies = false, IsAoE = false
            }
        };

        switch (unitClass)
        {
            case UnitClass.Warrior:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Cleave",
                    Type = AbilityType.ClassAbility, Damage = 75, CooldownTurns = 3,
                    Description = "Hits all enemies for 75% damage.", TargetsAllies = false, IsAoE = true
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Berserker Rage",
                    Type = AbilityType.Ultimate, Damage = 200, CooldownTurns = 5,
                    Description = "200% single-target damage.", TargetsAllies = false, IsAoE = false
                });
                break;
            case UnitClass.Mage:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Fireball",
                    Type = AbilityType.ClassAbility, Damage = 120, CooldownTurns = 3,
                    Description = "120% single-target damage.", TargetsAllies = false, IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Meteor Storm",
                    Type = AbilityType.Ultimate, Damage = 90, CooldownTurns = 5,
                    Description = "90% damage to all enemies.", TargetsAllies = false, IsAoE = true
                });
                break;
            case UnitClass.Ranger:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Aimed Shot",
                    Type = AbilityType.ClassAbility, Damage = 150, CooldownTurns = 3,
                    Description = "150% single-target damage.", TargetsAllies = false, IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Arrow Rain",
                    Type = AbilityType.Ultimate, Damage = 80, CooldownTurns = 5,
                    Description = "80% damage to all enemies.", TargetsAllies = false, IsAoE = true
                });
                break;
            case UnitClass.Healer:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Heal",
                    Type = AbilityType.ClassAbility, Healing = 150, CooldownTurns = 3,
                    Description = "Heals lowest ally for 150% of Attack.", TargetsAllies = true, IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Divine Light",
                    Type = AbilityType.Ultimate, Healing = 100, CooldownTurns = 5,
                    Description = "Heals all allies for 100% of Attack.", TargetsAllies = true, IsAoE = true
                });
                break;
            case UnitClass.Tank:
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Shield Bash",
                    Type = AbilityType.ClassAbility, Damage = 80, CooldownTurns = 3,
                    Description = "80% damage, scales with Defense.", TargetsAllies = false, IsAoE = false
                });
                abilities.Add(new Ability
                {
                    Id = Guid.NewGuid(), UnitId = unitId, Name = "Fortify",
                    Type = AbilityType.Ultimate, Healing = 50, CooldownTurns = 5,
                    Description = "Restores 50% of max HP.", TargetsAllies = true, IsAoE = false
                });
                break;
        }

        return abilities;
    }

    // ─────────────────────────────────────────────────
    //  Preset Definitions
    // ─────────────────────────────────────────────────

    private static readonly List<AiPreset> AiPresets = new()
    {
        // ── NOVICE (3 opponents, ~600-800 rating) ──
        new AiPreset
        {
            Id = "novice-1",
            Name = "Training Dummy",
            Description = "A slow-moving practice target. Perfect for learning the basics of team building and strategy configuration.",
            Difficulty = "novice",
            ApproximateRating = 600,
            Strategy = new StrategyConfig { Formation = Formation.balanced, TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp } },
            Units = new()
            {
                new AiUnitTemplate("Recruit Fighter", UnitClass.Warrior, 100, 18, 15, 8),
                new AiUnitTemplate("Recruit Archer", UnitClass.Ranger, 75, 22, 10, 14),
                new AiUnitTemplate("Recruit Mage", UnitClass.Mage, 65, 25, 8, 10)
            }
        },
        new AiPreset
        {
            Id = "novice-2",
            Name = "Village Guard",
            Description = "A small town guard patrol. They fight in defensive formation but lack coordination.",
            Difficulty = "novice",
            ApproximateRating = 700,
            Strategy = new StrategyConfig { Formation = Formation.defensive, TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp } },
            Units = new()
            {
                new AiUnitTemplate("Town Guard", UnitClass.Tank, 130, 16, 25, 5),
                new AiUnitTemplate("Town Militia", UnitClass.Warrior, 110, 20, 16, 9),
                new AiUnitTemplate("Town Herbalist", UnitClass.Healer, 70, 12, 14, 10)
            }
        },
        new AiPreset
        {
            Id = "novice-3",
            Name = "Bandit Scouts",
            Description = "A ragtag group of bandits. Aggressive but disorganized — they'll rush your weakest unit.",
            Difficulty = "novice",
            ApproximateRating = 800,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Cleave"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_3, Target = AbilityTarget.all_enemies }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Bandit Leader", UnitClass.Warrior, 120, 25, 15, 12),
                new AiUnitTemplate("Bandit Rogue", UnitClass.Ranger, 85, 28, 10, 18),
                new AiUnitTemplate("Bandit Thug", UnitClass.Warrior, 110, 22, 14, 10)
            }
        },

        // ── INTERMEDIATE (3 opponents, ~900-1100 rating) ──
        new AiPreset
        {
            Id = "intermediate-1",
            Name = "Arena Challengers",
            Description = "A balanced team of arena combatants. They coordinate abilities and protect their healer.",
            Difficulty = "intermediate",
            ApproximateRating = 900,
            Strategy = new StrategyConfig
            {
                Formation = Formation.balanced,
                TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp },
                    ["Fireball"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Arena Knight", UnitClass.Warrior, 130, 28, 18, 12),
                new AiUnitTemplate("Arena Mage", UnitClass.Mage, 85, 38, 12, 16),
                new AiUnitTemplate("Arena Medic", UnitClass.Healer, 85, 18, 20, 13),
                new AiUnitTemplate("Arena Ranger", UnitClass.Ranger, 95, 32, 16, 22)
            }
        },
        new AiPreset
        {
            Id = "intermediate-2",
            Name = "Iron Vanguard",
            Description = "A heavily armored frontline with ranged support. Their tank absorbs damage while mages deal it.",
            Difficulty = "intermediate",
            ApproximateRating = 1000,
            Strategy = new StrategyConfig
            {
                Formation = Formation.defensive,
                TargetPriority = new List<TargetPriority> { TargetPriority.mages, TargetPriority.healers, TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Fortify"] = new AbilityCondition { When = AbilityWhen.self_hp_below_50, Target = AbilityTarget.self },
                    ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp },
                    ["Meteor Storm"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Iron Bastion", UnitClass.Tank, 170, 24, 35, 6),
                new AiUnitTemplate("Iron Sorcerer", UnitClass.Mage, 90, 40, 10, 18),
                new AiUnitTemplate("Iron Cleric", UnitClass.Healer, 95, 20, 22, 14),
                new AiUnitTemplate("Iron Marksman", UnitClass.Ranger, 100, 35, 14, 25)
            }
        },
        new AiPreset
        {
            Id = "intermediate-3",
            Name = "Shadow Syndicate",
            Description = "Glass-cannon assassins. They hit fast and hard, focusing healers first. Can you survive the burst?",
            Difficulty = "intermediate",
            ApproximateRating = 1100,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.mages, TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Aimed Shot"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Berserker Rage"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Arrow Rain"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_3, Target = AbilityTarget.all_enemies }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Shadow Blade", UnitClass.Warrior, 130, 32, 15, 16),
                new AiUnitTemplate("Shadow Sniper", UnitClass.Ranger, 95, 38, 14, 26),
                new AiUnitTemplate("Shadow Warlock", UnitClass.Mage, 85, 42, 10, 19),
                new AiUnitTemplate("Shadow Striker", UnitClass.Ranger, 90, 35, 12, 24)
            }
        },

        // ── EXPERT (3 opponents, ~1200-1500 rating) ──
        new AiPreset
        {
            Id = "expert-1",
            Name = "Royal Guard",
            Description = "The kingdom's elite. A perfectly balanced 5-unit team with optimized strategies. A true test of skill.",
            Difficulty = "expert",
            ApproximateRating = 1200,
            Strategy = new StrategyConfig
            {
                Formation = Formation.balanced,
                TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.mages, TargetPriority.highest_threat },
                Abilities = new()
                {
                    ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp },
                    ["Divine Light"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_30, Target = AbilityTarget.all_allies },
                    ["Fortify"] = new AbilityCondition { When = AbilityWhen.self_hp_below_50, Target = AbilityTarget.self },
                    ["Meteor Storm"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_3, Target = AbilityTarget.all_enemies },
                    ["Berserker Rage"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Cleave"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Royal Champion", UnitClass.Warrior, 150, 32, 22, 11),
                new AiUnitTemplate("Royal Archmage", UnitClass.Mage, 100, 45, 15, 20),
                new AiUnitTemplate("Royal Guardian", UnitClass.Tank, 180, 26, 38, 7),
                new AiUnitTemplate("Royal Oracle", UnitClass.Healer, 100, 22, 24, 15),
                new AiUnitTemplate("Royal Sharpshooter", UnitClass.Ranger, 105, 38, 17, 23)
            }
        },
        new AiPreset
        {
            Id = "expert-2",
            Name = "Dragon Cult",
            Description = "Fanatical spell-casters backed by a self-healing tank. Their AoE damage is devastating — bring burst or die slowly.",
            Difficulty = "expert",
            ApproximateRating = 1350,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Meteor Storm"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies },
                    ["Fireball"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Fortify"] = new AbilityCondition { When = AbilityWhen.self_hp_below_50, Target = AbilityTarget.self },
                    ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_30, Target = AbilityTarget.lowest_ally_hp },
                    ["Divine Light"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_30, Target = AbilityTarget.all_allies }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Cult Pyromancer", UnitClass.Mage, 95, 42, 13, 17),
                new AiUnitTemplate("Cult Arcanist", UnitClass.Mage, 100, 45, 15, 20),
                new AiUnitTemplate("Cult Warpriest", UnitClass.Healer, 105, 25, 26, 16),
                new AiUnitTemplate("Dragon Scale", UnitClass.Tank, 200, 28, 40, 5),
                new AiUnitTemplate("Cult Storm Ranger", UnitClass.Ranger, 110, 40, 18, 28)
            }
        },
        new AiPreset
        {
            Id = "expert-3",
            Name = "The Undefeated",
            Description = "Legends say no one has beaten this team. Max-stat units, perfect strategy, relentless aggression. Good luck.",
            Difficulty = "expert",
            ApproximateRating = 1500,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.healers, TargetPriority.mages, TargetPriority.highest_threat, TargetPriority.lowest_hp },
                Abilities = new()
                {
                    ["Berserker Rage"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Cleave"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies },
                    ["Aimed Shot"] = new AbilityCondition { When = AbilityWhen.always, Target = AbilityTarget.priority },
                    ["Arrow Rain"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_3, Target = AbilityTarget.all_enemies },
                    ["Meteor Storm"] = new AbilityCondition { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies },
                    ["Heal"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp },
                    ["Divine Light"] = new AbilityCondition { When = AbilityWhen.ally_hp_below_30, Target = AbilityTarget.all_allies },
                    ["Fortify"] = new AbilityCondition { When = AbilityWhen.self_hp_below_50, Target = AbilityTarget.self }
                }
            },
            Units = new()
            {
                new AiUnitTemplate("Gold Warlord", UnitClass.Warrior, 160, 35, 25, 10),
                new AiUnitTemplate("Archmage Supreme", UnitClass.Mage, 100, 45, 15, 20),
                new AiUnitTemplate("Master Archer", UnitClass.Ranger, 110, 40, 18, 28),
                new AiUnitTemplate("Divine Oracle", UnitClass.Healer, 105, 25, 26, 16),
                new AiUnitTemplate("Immovable Fortress", UnitClass.Tank, 200, 28, 40, 5)
            }
        }
    };

    // ─────────────────────────────────────────────────
    //  Internal Types
    // ─────────────────────────────────────────────────

    private class AiPreset
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Difficulty { get; init; } = string.Empty;
        public int ApproximateRating { get; init; }
        public StrategyConfig Strategy { get; init; } = new();
        public List<AiUnitTemplate> Units { get; init; } = new();
    }

    private class AiUnitTemplate
    {
        public string Name { get; }
        public UnitClass Class { get; }
        public int Health { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Speed { get; }

        public AiUnitTemplate(string name, UnitClass unitClass, int hp, int atk, int def, int spd)
        {
            Name = name;
            Class = unitClass;
            Health = hp;
            Attack = atk;
            Defense = def;
            Speed = spd;
        }
    }
}
