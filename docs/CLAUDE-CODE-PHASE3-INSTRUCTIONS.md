# API Combat Game - Phase 3 Implementation Instructions

**Version:** 1.0  
**Date:** February 10, 2026  
**Purpose:** Stub out engagement features and anti-meta systems for future implementation

---

## Overview

This is Phase 3 of the API Combat Game implementation. Phase 1 created the core API and battle system. Phase 2 added the web UI and subscription management. Phase 3 adds engagement mechanics, anti-meta systems, and collaborative features.

**Important:** This phase **stubs out** the architecture to allow for future variations and implementations. We're building the foundation, not the complete features yet.

---

## Instructions to Give Claude Code

Copy everything from "START INSTRUCTIONS" to "END INSTRUCTIONS" and paste it into Claude Code.

---

**START INSTRUCTIONS**

I need you to add Phase 3 features to the API Combat Game. This phase focuses on engagement mechanics and anti-meta systems. **IMPORTANT:** Stub out these features with extensible architecture. Implement the database models, API endpoints, and basic services, but leave room for future variations.

## Goals

1. **Prevent meta stagnation:** Weekly modifiers, strategy decay, personalized challenges
2. **Enable collaboration:** Guild boss raids, strategy marketplace, replay sharing
3. **Deepen progression:** Unit mastery, achievements, ranked titles
4. **Maintain extensibility:** Easy to add new modifiers, challenges, boss types, etc.

## Architecture Principles

**Extensibility First:**
- Use strategy pattern for modifiers
- Factory pattern for challenge generation
- Plugin architecture for boss abilities
- Template method for progression systems

**Stub, Don't Fully Implement:**
- Create the interfaces and base classes
- Implement 1-2 concrete examples
- Leave TODOs for future variations
- Document extension points clearly

---

## Project Structure Updates

Add these folders and files to the existing `ApiCombatGame` project:

```
ApiCombatGame/
├── Models/
│   ├── Domain/
│   │   ├── EnvironmentalModifier.cs      # NEW
│   │   ├── GuildBoss.cs                  # NEW
│   │   ├── GuildBossAttempt.cs           # NEW
│   │   ├── Guild.cs                      # NEW
│   │   ├── GuildMembership.cs            # NEW
│   │   ├── DailyChallenge.cs             # NEW
│   │   ├── Strategy.cs                   # NEW
│   │   ├── StrategyRating.cs             # NEW
│   │   ├── UnitMastery.cs                # NEW
│   │   ├── Achievement.cs                # NEW
│   │   ├── PlayerAchievement.cs          # NEW
│   │   ├── BattleReplay.cs               # NEW
│   │   └── PlayerTitle.cs                # NEW
│   └── DTOs/
│       ├── Modifier/
│       │   └── ModifierResponse.cs
│       ├── Guild/
│       │   ├── GuildBossResponse.cs
│       │   ├── BossAttemptRequest.cs
│       │   └── GuildResponse.cs
│       ├── Challenge/
│       │   └── ChallengeResponse.cs
│       ├── Marketplace/
│       │   ├── StrategyUploadRequest.cs
│       │   ├── StrategyResponse.cs
│       │   └── StrategyRatingRequest.cs
│       └── Progression/
│           ├── MasteryResponse.cs
│           └── AchievementResponse.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IModifierService.cs           # NEW
│   │   ├── IGuildBossService.cs          # NEW
│   │   ├── IChallengeService.cs          # NEW
│   │   ├── IStrategyMarketplace.cs       # NEW
│   │   ├── IMasteryService.cs            # NEW
│   │   └── IReplayService.cs             # NEW
│   ├── Modifiers/
│   │   ├── IModifierEffect.cs            # NEW: Interface
│   │   ├── BaseModifierEffect.cs         # NEW: Abstract base
│   │   ├── ArcaneDisruptionModifier.cs   # NEW: Example
│   │   └── HeavyArmorModifier.cs         # NEW: Example
│   ├── Challenges/
│   │   ├── IChallengeGenerator.cs        # NEW: Interface
│   │   ├── BaseChallengeGenerator.cs     # NEW: Abstract base
│   │   ├── TeamCompositionChallenge.cs   # NEW: Example
│   │   └── WinStreakChallenge.cs         # NEW: Example
│   ├── Bosses/
│   │   ├── IBossAbility.cs               # NEW: Interface
│   │   ├── BaseBossAbility.cs            # NEW: Abstract base
│   │   ├── ScalesHardenAbility.cs        # NEW: Example
│   │   └── EnrageAbility.cs              # NEW: Example
│   ├── ModifierService.cs                # NEW
│   ├── GuildBossService.cs               # NEW
│   ├── ChallengeService.cs               # NEW
│   ├── StrategyMarketplaceService.cs     # NEW
│   ├── MasteryService.cs                 # NEW
│   └── ReplayService.cs                  # NEW
├── BackgroundJobs/                       # NEW folder
│   ├── WeeklyModifierRotationJob.cs
│   ├── DailyChallengeGenerationJob.cs
│   ├── StrategyDecayJob.cs
│   └── GuildBossSpawnJob.cs
└── Controllers/
    └── Api/
        ├── ModifierController.cs         # NEW
        ├── GuildBossController.cs        # NEW
        ├── ChallengeController.cs        # NEW
        ├── StrategyController.cs         # NEW
        ├── MasteryController.cs          # NEW
        └── ReplayController.cs           # NEW
```

---

## Domain Models

### 1. Environmental Modifiers

```csharp
public class EnvironmentalModifier
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ModifierType { get; set; } // Class name of the modifier implementation
    public string ConfigJson { get; set; } // Serialized config for the modifier
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

**Design Notes:**
- `ModifierType` allows dynamic loading of modifier implementations
- `ConfigJson` allows each modifier to have custom parameters
- Extensible: Add new modifiers without changing this model

### 2. Guild System

```csharp
public class Guild
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; } // 3-5 character tag, e.g., "APEX"
    public string Description { get; set; }
    
    public Guid LeaderId { get; set; }
    public Player Leader { get; set; }
    
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public int MaxMembers { get; set; } = 20;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public List<GuildMembership> Members { get; set; } = new();
    public List<GuildBoss> Bosses { get; set; } = new();
}

public class GuildMembership
{
    public Guid Id { get; set; }
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; }
    
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTime JoinedAt { get; set; }
    public int ContributionPoints { get; set; } = 0;
}

public enum GuildRole
{
    Member,
    Officer,
    Leader
}
```

### 3. Guild Boss System

```csharp
public class GuildBoss
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string BossType { get; set; } // Class name of boss implementation
    public string AbilitiesJson { get; set; } // Serialized boss abilities
    
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; }
    
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    
    public DateTime SpawnedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsDefeated { get; set; } = false;
    public DateTime? DefeatedAt { get; set; }
    
    // Rewards
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }
    
    // Navigation
    public List<GuildBossAttempt> Attempts { get; set; } = new();
}

public class GuildBossAttempt
{
    public Guid Id { get; set; }
    public Guid GuildBossId { get; set; }
    public GuildBoss GuildBoss { get; set; }
    
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public int DamageDealt { get; set; }
    public bool WasKillingBlow { get; set; } = false;
    public string BattleLogJson { get; set; } // Full battle log
    
    public DateTime AttemptedAt { get; set; }
}
```

**Design Notes:**
- `BossType` allows different boss implementations
- `AbilitiesJson` allows bosses to have unique mechanics
- Extensible: Create new boss types without schema changes

### 4. Daily Challenges

```csharp
public class DailyChallenge
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public string ChallengeType { get; set; } // Class name of challenge generator
    public string Name { get; set; }
    public string Description { get; set; }
    public string RequirementsJson { get; set; } // Serialized requirements
    
    public int Progress { get; set; } = 0;
    public int RequiredProgress { get; set; }
    public bool IsCompleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Rewards
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }
}
```

### 5. Strategy Marketplace

```csharp
public class Strategy
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public Player Creator { get; set; }
    
    public string Name { get; set; }
    public string Description { get; set; }
    public string StrategyJson { get; set; } // The actual strategy config
    
    public int Price { get; set; } = 0; // 0 = free
    public bool IsPublic { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Metadata
    public int DownloadCount { get; set; } = 0;
    public int WinCount { get; set; } = 0;
    public int LossCount { get; set; } = 0;
    public double AverageRating { get; set; } = 0.0;
    
    // Decay system
    public double EffectivenessMultiplier { get; set; } = 1.0;
    public DateTime LastDecayUpdate { get; set; }
    
    // Navigation
    public List<StrategyRating> Ratings { get; set; } = new();
}

public class StrategyRating
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public Strategy Strategy { get; set; }
    
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public int Rating { get; set; } // 1-5 stars
    public string Comment { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

### 6. Unit Mastery

```csharp
public class UnitMastery
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public Guid UnitId { get; set; }
    // Note: UnitId refers to the unit template, not a specific player's unit
    
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public int BattlesUsed { get; set; } = 0;
    public int WinsWithUnit { get; set; } = 0;
    
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
}
```

### 7. Achievements

```csharp
public class Achievement
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IconUrl { get; set; }
    
    public string Category { get; set; } // "Combat", "Collection", "Social", etc.
    public int Points { get; set; } // Achievement points awarded
    
    public string RequirementsJson { get; set; } // Serialized unlock requirements
    
    public bool IsSecret { get; set; } = false; // Hidden until unlocked
    public DateTime CreatedAt { get; set; }
}

public class PlayerAchievement
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public Guid AchievementId { get; set; }
    public Achievement Achievement { get; set; }
    
    public int Progress { get; set; } = 0;
    public int RequiredProgress { get; set; }
    public bool IsUnlocked { get; set; } = false;
    
    public DateTime? UnlockedAt { get; set; }
}
```

### 8. Battle Replays

```csharp
public class BattleReplay
{
    public Guid Id { get; set; }
    public Guid BattleId { get; set; }
    public Battle Battle { get; set; }
    
    public string ShareUrl { get; set; } // Short URL for sharing
    public int ViewCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? FeaturedAt { get; set; }
}
```

### 9. Player Titles

```csharp
public class PlayerTitle
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ColorHex { get; set; } // Display color, e.g., "#FFD700" for gold
    
    public string UnlockRequirementsJson { get; set; } // How to earn this title
    
    public DateTime CreatedAt { get; set; }
}
```

---

## Update Player Model

Add new relationships and fields:

```csharp
public class Player
{
    // ... existing properties ...
    
    // NEW: Phase 3 additions
    public Guid? ActiveTitleId { get; set; }
    public PlayerTitle ActiveTitle { get; set; }
    
    public int AchievementPoints { get; set; } = 0;
    
    // Navigation properties
    public GuildMembership GuildMembership { get; set; }
    public List<DailyChallenge> DailyChallenges { get; set; } = new();
    public List<Strategy> CreatedStrategies { get; set; } = new();
    public List<UnitMastery> UnitMasteries { get; set; } = new();
    public List<PlayerAchievement> Achievements { get; set; } = new();
    public List<GuildBossAttempt> BossAttempts { get; set; } = new();
}
```

---

## Service Interfaces

### IModifierEffect (Extensibility Interface)

```csharp
public interface IModifierEffect
{
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Apply this modifier's effects to a battle context
    /// </summary>
    void ApplyToBattle(BattleContext context);
    
    /// <summary>
    /// Modify unit stats based on this modifier
    /// </summary>
    void ModifyUnitStats(Unit unit);
}

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
```

**Example Implementations:**

```csharp
public class ArcaneDisruptionModifier : BaseModifierEffect
{
    public override string Name => "Arcane Disruption";
    public override string Description => "Mage abilities cost 2x mana, physical attacks deal +20% damage";
    
    public override void ModifyUnitStats(Unit unit)
    {
        if (unit.Class == UnitClass.Mage)
        {
            // TODO: Implement mana cost modifier (requires mana system)
        }
        else if (unit.Class == UnitClass.Warrior || unit.Class == UnitClass.Ranger)
        {
            unit.Attack = (int)(unit.Attack * 1.2);
        }
    }
}

public class HeavyArmorModifier : BaseModifierEffect
{
    public override string Name => "Heavy Armor";
    public override string Description => "All units gain +50% defense, healer abilities 2x effectiveness";
    
    public override void ModifyUnitStats(Unit unit)
    {
        unit.Defense = (int)(unit.Defense * 1.5);
        
        if (unit.Class == UnitClass.Healer)
        {
            // TODO: Implement healing multiplier (requires healing system)
        }
    }
}
```

### IChallengeGenerator (Extensibility Interface)

```csharp
public interface IChallengeGenerator
{
    string ChallengeType { get; }
    DailyChallenge Generate(Player player);
    bool CheckProgress(DailyChallenge challenge, Battle battle);
}

public abstract class BaseChallengeGenerator : IChallengeGenerator
{
    public abstract string ChallengeType { get; }
    public abstract DailyChallenge Generate(Player player);
    public abstract bool CheckProgress(DailyChallenge challenge, Battle battle);
}
```

**Example Implementations:**

```csharp
public class TeamCompositionChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "TeamComposition";
    
    public override DailyChallenge Generate(Player player)
    {
        // Pick a random unit class
        var requiredClass = PickRandomClass();
        
        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = $"Win with {requiredClass}s",
            Description = $"Win 5 battles using only {requiredClass} units",
            RequirementsJson = JsonSerializer.Serialize(new { Class = requiredClass }),
            Progress = 0,
            RequiredProgress = 5,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RewardCurrency = 500,
            RewardExperience = 100
        };
    }
    
    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        // TODO: Check if winning team used only required class
        // Increment progress if true
        return false; // Stub
    }
    
    private UnitClass PickRandomClass()
    {
        var classes = Enum.GetValues<UnitClass>();
        return classes[Random.Shared.Next(classes.Length)];
    }
}

public class WinStreakChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "WinStreak";
    
    public override DailyChallenge Generate(Player player)
    {
        return new DailyChallenge
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            ChallengeType = ChallengeType,
            Name = "Win Streak",
            Description = "Win 3 battles in a row without losing",
            RequirementsJson = JsonSerializer.Serialize(new { Streak = 3 }),
            Progress = 0,
            RequiredProgress = 3,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RewardCurrency = 750,
            RewardExperience = 150
        };
    }
    
    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        // TODO: Track win streak, reset on loss
        return false; // Stub
    }
}
```

### IBossAbility (Extensibility Interface)

```csharp
public interface IBossAbility
{
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Execute this ability during a boss battle
    /// </summary>
    void Execute(GuildBoss boss, BattleContext context);
    
    /// <summary>
    /// Check if this ability should trigger
    /// </summary>
    bool ShouldTrigger(GuildBoss boss, BattleContext context);
}

public abstract class BaseBossAbility : IBossAbility
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public abstract void Execute(GuildBoss boss, BattleContext context);
    
    public virtual bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        return true; // Default: always trigger
    }
}
```

**Example Implementations:**

```csharp
public class ScalesHardenAbility : BaseBossAbility
{
    public override string Name => "Scales Harden";
    public override string Description => "Defense increases by 10% each turn";
    
    private int _turnsElapsed = 0;
    
    public override void Execute(GuildBoss boss, BattleContext context)
    {
        _turnsElapsed++;
        var defenseMultiplier = 1.0 + (_turnsElapsed * 0.1);
        // TODO: Apply defense multiplier to boss stats
    }
    
    public override bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        // Trigger at the start of each turn
        return true;
    }
}

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
        }
    }
    
    public override bool ShouldTrigger(GuildBoss boss, BattleContext context)
    {
        return !_hasEnraged && (boss.CurrentHp <= boss.MaxHp * 0.25);
    }
}
```

---

## Service Implementations (Stubbed)

### ModifierService

```csharp
public interface IModifierService
{
    Task<EnvironmentalModifier> GetCurrentModifier();
    Task<EnvironmentalModifier> GetUpcomingModifier();
    Task RotateModifier();
    IModifierEffect GetModifierEffect(string modifierType);
}

public class ModifierService : IModifierService
{
    private readonly GameDbContext _context;
    private readonly Dictionary<string, IModifierEffect> _modifiers;
    
    public ModifierService(GameDbContext context)
    {
        _context = context;
        
        // Register available modifiers
        _modifiers = new Dictionary<string, IModifierEffect>
        {
            { "ArcaneDisruption", new ArcaneDisruptionModifier() },
            { "HeavyArmor", new HeavyArmorModifier() },
            // TODO: Add more modifiers as they're implemented
        };
    }
    
    public async Task<EnvironmentalModifier> GetCurrentModifier()
    {
        var now = DateTime.UtcNow;
        return await _context.EnvironmentalModifiers
            .FirstOrDefaultAsync(m => m.IsActive && m.StartDate <= now && m.EndDate >= now);
    }
    
    public async Task<EnvironmentalModifier> GetUpcomingModifier()
    {
        var now = DateTime.UtcNow;
        return await _context.EnvironmentalModifiers
            .Where(m => m.StartDate > now)
            .OrderBy(m => m.StartDate)
            .FirstOrDefaultAsync();
    }
    
    public async Task RotateModifier()
    {
        // TODO: Implement weekly rotation logic
        // 1. Deactivate current modifier
        // 2. Activate next modifier from queue
        // 3. Generate new modifier for next week
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public IModifierEffect GetModifierEffect(string modifierType)
    {
        return _modifiers.ContainsKey(modifierType) 
            ? _modifiers[modifierType] 
            : null;
    }
}
```

### GuildBossService

```csharp
public interface IGuildBossService
{
    Task<GuildBoss> GetActiveGuildBoss(Guid guildId);
    Task<GuildBossAttempt> AttemptBoss(Guid guildBossId, Guid playerId, Guid teamId);
    Task<List<GuildBossAttempt>> GetBossLeaderboard(Guid guildBossId);
    Task SpawnBossForGuild(Guid guildId);
}

public class GuildBossService : IGuildBossService
{
    private readonly GameDbContext _context;
    private readonly IBattleService _battleService;
    
    public GuildBossService(GameDbContext context, IBattleService battleService)
    {
        _context = context;
        _battleService = battleService;
    }
    
    public async Task<GuildBoss> GetActiveGuildBoss(Guid guildId)
    {
        var now = DateTime.UtcNow;
        return await _context.GuildBosses
            .FirstOrDefaultAsync(b => 
                b.GuildId == guildId && 
                !b.IsDefeated && 
                b.ExpiresAt > now);
    }
    
    public async Task<GuildBossAttempt> AttemptBoss(Guid guildBossId, Guid playerId, Guid teamId)
    {
        // TODO: Implement boss battle logic
        // 1. Load boss and player team
        // 2. Run battle simulation
        // 3. Calculate damage dealt
        // 4. Update boss HP
        // 5. Check if boss defeated
        // 6. Award rewards if defeated
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task<List<GuildBossAttempt>> GetBossLeaderboard(Guid guildBossId)
    {
        return await _context.GuildBossAttempts
            .Where(a => a.GuildBossId == guildBossId)
            .OrderByDescending(a => a.DamageDealt)
            .Take(10)
            .Include(a => a.Player)
            .ToListAsync();
    }
    
    public async Task SpawnBossForGuild(Guid guildId)
    {
        // TODO: Implement boss spawning logic
        // 1. Select boss type (random or scheduled)
        // 2. Scale HP based on guild size
        // 3. Set abilities
        // 4. Notify guild members
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
}
```

### ChallengeService

```csharp
public interface IChallengeService
{
    Task<List<DailyChallenge>> GetActiveChallenges(Guid playerId);
    Task GenerateDailyChallenges(Guid playerId);
    Task CheckChallengeProgress(Guid playerId, Battle battle);
    Task ClaimReward(Guid challengeId);
}

public class ChallengeService : IChallengeService
{
    private readonly GameDbContext _context;
    private readonly List<IChallengeGenerator> _generators;
    
    public ChallengeService(GameDbContext context)
    {
        _context = context;
        
        // Register challenge generators
        _generators = new List<IChallengeGenerator>
        {
            new TeamCompositionChallenge(),
            new WinStreakChallenge(),
            // TODO: Add more challenge types
        };
    }
    
    public async Task<List<DailyChallenge>> GetActiveChallenges(Guid playerId)
    {
        var now = DateTime.UtcNow;
        return await _context.DailyChallenges
            .Where(c => c.PlayerId == playerId && c.ExpiresAt > now)
            .ToListAsync();
    }
    
    public async Task GenerateDailyChallenges(Guid playerId)
    {
        // TODO: Implement challenge generation
        // 1. Check if player already has challenges today
        // 2. Pick 3 random challenge types
        // 3. Generate challenges using generators
        // 4. Save to database
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task CheckChallengeProgress(Guid playerId, Battle battle)
    {
        // TODO: Implement progress checking
        // 1. Get active challenges
        // 2. For each challenge, check if battle satisfies requirements
        // 3. Update progress
        // 4. Mark as completed if requirements met
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task ClaimReward(Guid challengeId)
    {
        // TODO: Implement reward claiming
        // 1. Verify challenge is completed
        // 2. Award currency and experience
        // 3. Mark as claimed
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
}
```

### StrategyMarketplaceService

```csharp
public interface IStrategyMarketplaceService
{
    Task<Strategy> UploadStrategy(Guid playerId, string name, string description, string strategyJson, int price);
    Task<List<Strategy>> BrowseStrategies(string sortBy, int limit, int offset);
    Task<Strategy> DownloadStrategy(Guid strategyId, Guid playerId);
    Task RateStrategy(Guid strategyId, Guid playerId, int rating, string comment);
    Task ApplyStrategyDecay();
}

public class StrategyMarketplaceService : IStrategyMarketplaceService
{
    private readonly GameDbContext _context;
    
    public StrategyMarketplaceService(GameDbContext context)
    {
        _context = context;
    }
    
    public async Task<Strategy> UploadStrategy(Guid playerId, string name, string description, string strategyJson, int price)
    {
        var strategy = new Strategy
        {
            Id = Guid.NewGuid(),
            CreatorId = playerId,
            Name = name,
            Description = description,
            StrategyJson = strategyJson,
            Price = price,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastDecayUpdate = DateTime.UtcNow
        };
        
        _context.Strategies.Add(strategy);
        await _context.SaveChangesAsync();
        
        return strategy;
    }
    
    public async Task<List<Strategy>> BrowseStrategies(string sortBy, int limit, int offset)
    {
        var query = _context.Strategies.Where(s => s.IsPublic);
        
        query = sortBy switch
        {
            "popular" => query.OrderByDescending(s => s.DownloadCount),
            "rating" => query.OrderByDescending(s => s.AverageRating),
            "recent" => query.OrderByDescending(s => s.CreatedAt),
            "winrate" => query.OrderByDescending(s => s.WinCount / (double)(s.WinCount + s.LossCount + 1)),
            _ => query.OrderByDescending(s => s.DownloadCount)
        };
        
        return await query
            .Skip(offset)
            .Take(limit)
            .Include(s => s.Creator)
            .ToListAsync();
    }
    
    public async Task<Strategy> DownloadStrategy(Guid strategyId, Guid playerId)
    {
        // TODO: Implement download logic
        // 1. Load strategy
        // 2. Check if player has enough currency (if not free)
        // 3. Deduct currency
        // 4. Increment download count
        // 5. Return strategy
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task RateStrategy(Guid strategyId, Guid playerId, int rating, string comment)
    {
        // TODO: Implement rating logic
        // 1. Check if player already rated
        // 2. Add or update rating
        // 3. Recalculate average rating
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task ApplyStrategyDecay()
    {
        // TODO: Implement decay logic
        // 1. Get all public strategies
        // 2. Calculate age in weeks
        // 3. Apply decay formula: multiplier = 1.0 - (age * 0.05), min 0.5
        // 4. Update EffectivenessMultiplier
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
}
```

### MasteryService

```csharp
public interface IMasteryService
{
    Task<List<UnitMastery>> GetPlayerMastery(Guid playerId);
    Task<UnitMastery> GetUnitMastery(Guid playerId, Guid unitId);
    Task IncrementMastery(Guid playerId, Guid unitId, bool won);
}

public class MasteryService : IMasteryService
{
    private readonly GameDbContext _context;
    
    public MasteryService(GameDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<UnitMastery>> GetPlayerMastery(Guid playerId)
    {
        return await _context.UnitMasteries
            .Where(m => m.PlayerId == playerId)
            .OrderByDescending(m => m.Level)
            .ThenByDescending(m => m.ExperiencePoints)
            .ToListAsync();
    }
    
    public async Task<UnitMastery> GetUnitMastery(Guid playerId, Guid unitId)
    {
        return await _context.UnitMasteries
            .FirstOrDefaultAsync(m => m.PlayerId == playerId && m.UnitId == unitId);
    }
    
    public async Task IncrementMastery(Guid playerId, Guid unitId, bool won)
    {
        // TODO: Implement mastery progression
        // 1. Find or create mastery record
        // 2. Increment battles used
        // 3. If won, increment wins
        // 4. Add experience points
        // 5. Check for level up
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
}
```

### ReplayService

```csharp
public interface IReplayService
{
    Task<BattleReplay> CreateReplay(Guid battleId);
    Task<BattleReplay> GetReplay(string shareUrl);
    Task IncrementViewCount(Guid replayId);
}

public class ReplayService : IReplayService
{
    private readonly GameDbContext _context;
    
    public ReplayService(GameDbContext context)
    {
        _context = context;
    }
    
    public async Task<BattleReplay> CreateReplay(Guid battleId)
    {
        // TODO: Implement replay creation
        // 1. Check if replay already exists
        // 2. Generate short URL (e.g., base62 encode of replay ID)
        // 3. Create replay record
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    public async Task<BattleReplay> GetReplay(string shareUrl)
    {
        return await _context.BattleReplays
            .Include(r => r.Battle)
            .ThenInclude(b => b.Player1)
            .Include(r => r.Battle)
            .ThenInclude(b => b.Player2)
            .FirstOrDefaultAsync(r => r.ShareUrl == shareUrl);
    }
    
    public async Task IncrementViewCount(Guid replayId)
    {
        var replay = await _context.BattleReplays.FindAsync(replayId);
        if (replay != null)
        {
            replay.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }
}
```

---

## API Controllers (Stubbed)

### ModifierController

```csharp
[ApiController]
[Route("api/v1/modifiers")]
public class ModifierController : ControllerBase
{
    private readonly IModifierService _modifierService;
    
    public ModifierController(IModifierService modifierService)
    {
        _modifierService = modifierService;
    }
    
    /// <summary>
    /// Get the current environmental modifier
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ModifierResponse>> GetCurrentModifier()
    {
        var modifier = await _modifierService.GetCurrentModifier();
        if (modifier == null)
        {
            return Ok(new ModifierResponse 
            { 
                Name = "Normal", 
                Description = "No environmental effects this week" 
            });
        }
        
        return Ok(new ModifierResponse
        {
            Name = modifier.Name,
            Description = modifier.Description,
            StartDate = modifier.StartDate,
            EndDate = modifier.EndDate
        });
    }
    
    /// <summary>
    /// Get next week's modifier (preview)
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<ModifierResponse>> GetUpcomingModifier()
    {
        var modifier = await _modifierService.GetUpcomingModifier();
        if (modifier == null)
        {
            return NotFound("No upcoming modifier scheduled");
        }
        
        return Ok(new ModifierResponse
        {
            Name = modifier.Name,
            Description = modifier.Description,
            StartDate = modifier.StartDate,
            EndDate = modifier.EndDate
        });
    }
}
```

### GuildBossController

```csharp
[ApiController]
[Route("api/v1/guild/boss")]
[Authorize]
public class GuildBossController : ControllerBase
{
    private readonly IGuildBossService _guildBossService;
    
    public GuildBossController(IGuildBossService guildBossService)
    {
        _guildBossService = guildBossService;
    }
    
    /// <summary>
    /// Get current guild boss
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<GuildBossResponse>> GetCurrentBoss()
    {
        var playerId = GetPlayerIdFromToken();
        // TODO: Get player's guild ID
        var guildId = Guid.Empty; // Stub
        
        var boss = await _guildBossService.GetActiveGuildBoss(guildId);
        if (boss == null)
        {
            return NotFound("No active boss for your guild");
        }
        
        return Ok(new GuildBossResponse
        {
            BossId = boss.Id,
            Name = boss.Name,
            Description = boss.Description,
            MaxHp = boss.MaxHp,
            CurrentHp = boss.CurrentHp,
            ExpiresAt = boss.ExpiresAt
        });
    }
    
    /// <summary>
    /// Attempt to damage the guild boss
    /// </summary>
    [HttpPost("attempt")]
    public async Task<ActionResult<BossAttemptResponse>> AttemptBoss([FromBody] BossAttemptRequest request)
    {
        // TODO: Implement boss attempt
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    /// <summary>
    /// Get boss damage leaderboard
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<BossAttemptResponse>>> GetLeaderboard([FromQuery] Guid bossId)
    {
        var attempts = await _guildBossService.GetBossLeaderboard(bossId);
        
        return Ok(attempts.Select(a => new BossAttemptResponse
        {
            PlayerName = a.Player.Username,
            DamageDealt = a.DamageDealt,
            AttemptedAt = a.AttemptedAt
        }));
    }
    
    private Guid GetPlayerIdFromToken()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(playerIdClaim.Value);
    }
}
```

### ChallengeController

```csharp
[ApiController]
[Route("api/v1/challenges")]
[Authorize]
public class ChallengeController : ControllerBase
{
    private readonly IChallengeService _challengeService;
    
    public ChallengeController(IChallengeService challengeService)
    {
        _challengeService = challengeService;
    }
    
    /// <summary>
    /// Get player's active daily challenges
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult<List<ChallengeResponse>>> GetDailyChallenges()
    {
        var playerId = GetPlayerIdFromToken();
        var challenges = await _challengeService.GetActiveChallenges(playerId);
        
        return Ok(challenges.Select(c => new ChallengeResponse
        {
            ChallengeId = c.Id,
            Name = c.Name,
            Description = c.Description,
            Progress = c.Progress,
            RequiredProgress = c.RequiredProgress,
            IsCompleted = c.IsCompleted,
            RewardCurrency = c.RewardCurrency,
            ExpiresAt = c.ExpiresAt
        }));
    }
    
    /// <summary>
    /// Claim reward for completed challenge
    /// </summary>
    [HttpPost("claim")]
    public async Task<ActionResult> ClaimReward([FromBody] ClaimRequest request)
    {
        // TODO: Implement reward claiming
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    private Guid GetPlayerIdFromToken()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(playerIdClaim.Value);
    }
}
```

### StrategyController

```csharp
[ApiController]
[Route("api/v1/strategies")]
[Authorize]
public class StrategyController : ControllerBase
{
    private readonly IStrategyMarketplaceService _marketplace;
    
    public StrategyController(IStrategyMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }
    
    /// <summary>
    /// Browse public strategies
    /// </summary>
    [HttpGet("browse")]
    public async Task<ActionResult<List<StrategyResponse>>> Browse(
        [FromQuery] string sortBy = "popular",
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        var strategies = await _marketplace.BrowseStrategies(sortBy, limit, offset);
        
        return Ok(strategies.Select(s => new StrategyResponse
        {
            StrategyId = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatorName = s.Creator.Username,
            Price = s.Price,
            DownloadCount = s.DownloadCount,
            AverageRating = s.AverageRating,
            WinRate = s.WinCount / (double)(s.WinCount + s.LossCount + 1),
            EffectivenessMultiplier = s.EffectivenessMultiplier
        }));
    }
    
    /// <summary>
    /// Upload a new strategy
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<StrategyResponse>> Upload([FromBody] StrategyUploadRequest request)
    {
        var playerId = GetPlayerIdFromToken();
        
        var strategy = await _marketplace.UploadStrategy(
            playerId,
            request.Name,
            request.Description,
            request.StrategyJson,
            request.Price
        );
        
        return CreatedAtAction(nameof(Browse), new StrategyResponse
        {
            StrategyId = strategy.Id,
            Name = strategy.Name,
            Description = strategy.Description,
            Price = strategy.Price
        });
    }
    
    /// <summary>
    /// Download a strategy
    /// </summary>
    [HttpPost("{strategyId}/download")]
    public async Task<ActionResult<StrategyDownloadResponse>> Download(Guid strategyId)
    {
        // TODO: Implement download logic
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    /// <summary>
    /// Rate a strategy
    /// </summary>
    [HttpPost("{strategyId}/rate")]
    public async Task<ActionResult> Rate(Guid strategyId, [FromBody] StrategyRatingRequest request)
    {
        // TODO: Implement rating logic
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    private Guid GetPlayerIdFromToken()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(playerIdClaim.Value);
    }
}
```

### MasteryController

```csharp
[ApiController]
[Route("api/v1/mastery")]
[Authorize]
public class MasteryController : ControllerBase
{
    private readonly IMasteryService _masteryService;
    
    public MasteryController(IMasteryService masteryService)
    {
        _masteryService = masteryService;
    }
    
    /// <summary>
    /// Get all unit mastery levels for current player
    /// </summary>
    [HttpGet("units")]
    public async Task<ActionResult<List<MasteryResponse>>> GetMastery()
    {
        var playerId = GetPlayerIdFromToken();
        var mastery = await _masteryService.GetPlayerMastery(playerId);
        
        return Ok(mastery.Select(m => new MasteryResponse
        {
            UnitId = m.UnitId,
            Level = m.Level,
            ExperiencePoints = m.ExperiencePoints,
            BattlesUsed = m.BattlesUsed,
            WinsWithUnit = m.WinsWithUnit
        }));
    }
    
    /// <summary>
    /// Get mastery for specific unit
    /// </summary>
    [HttpGet("unit/{unitId}")]
    public async Task<ActionResult<MasteryResponse>> GetUnitMastery(Guid unitId)
    {
        var playerId = GetPlayerIdFromToken();
        var mastery = await _masteryService.GetUnitMastery(playerId, unitId);
        
        if (mastery == null)
        {
            return Ok(new MasteryResponse
            {
                UnitId = unitId,
                Level = 1,
                ExperiencePoints = 0,
                BattlesUsed = 0,
                WinsWithUnit = 0
            });
        }
        
        return Ok(new MasteryResponse
        {
            UnitId = mastery.UnitId,
            Level = mastery.Level,
            ExperiencePoints = mastery.ExperiencePoints,
            BattlesUsed = mastery.BattlesUsed,
            WinsWithUnit = mastery.WinsWithUnit
        });
    }
    
    private Guid GetPlayerIdFromToken()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(playerIdClaim.Value);
    }
}
```

### ReplayController

```csharp
[ApiController]
[Route("api/v1/replays")]
public class ReplayController : ControllerBase
{
    private readonly IReplayService _replayService;
    
    public ReplayController(IReplayService replayService)
    {
        _replayService = replayService;
    }
    
    /// <summary>
    /// Create a shareable replay for a battle
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<ReplayResponse>> CreateReplay([FromBody] CreateReplayRequest request)
    {
        // TODO: Implement replay creation
        throw new NotImplementedException("TODO: Phase 3 implementation");
    }
    
    /// <summary>
    /// Get battle replay (public, no auth required)
    /// </summary>
    [HttpGet("{shareUrl}")]
    public async Task<ActionResult<ReplayResponse>> GetReplay(string shareUrl)
    {
        var replay = await _replayService.GetReplay(shareUrl);
        if (replay == null)
        {
            return NotFound("Replay not found");
        }
        
        await _replayService.IncrementViewCount(replay.Id);
        
        return Ok(new ReplayResponse
        {
            BattleId = replay.BattleId,
            ShareUrl = replay.ShareUrl,
            ViewCount = replay.ViewCount,
            CreatedAt = replay.CreatedAt,
            // TODO: Include battle details
        });
    }
}
```

---

## Background Jobs

### WeeklyModifierRotationJob

```csharp
public class WeeklyModifierRotationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyModifierRotationJob> _logger;
    
    public WeeklyModifierRotationJob(
        IServiceProvider serviceProvider,
        ILogger<WeeklyModifierRotationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMonday = GetNextMonday(now);
            var delay = nextMonday - now;
            
            _logger.LogInformation($"Next modifier rotation scheduled for {nextMonday}");
            
            await Task.Delay(delay, stoppingToken);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();
                
                await modifierService.RotateModifier();
                
                _logger.LogInformation("Modifier rotation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating modifier");
            }
        }
    }
    
    private DateTime GetNextMonday(DateTime from)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, next Monday
        
        var nextMonday = from.Date.AddDays(daysUntilMonday);
        return new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
```

### DailyChallengeGenerationJob

```csharp
public class DailyChallengeGenerationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyChallengeGenerationJob> _logger;
    
    public DailyChallengeGenerationJob(
        IServiceProvider serviceProvider,
        ILogger<DailyChallengeGenerationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;
            
            _logger.LogInformation($"Next challenge generation at {nextMidnight}");
            
            await Task.Delay(delay, stoppingToken);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                var challengeService = scope.ServiceProvider.GetRequiredService<IChallengeService>();
                
                // Get all active players (logged in within last 7 days)
                var activePlayers = await context.Players
                    .Where(p => p.LastLoginAt > DateTime.UtcNow.AddDays(-7))
                    .ToListAsync(stoppingToken);
                
                foreach (var player in activePlayers)
                {
                    await challengeService.GenerateDailyChallenges(player.Id);
                }
                
                _logger.LogInformation($"Generated challenges for {activePlayers.Count} players");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily challenges");
            }
        }
    }
}
```

### StrategyDecayJob

```csharp
public class StrategyDecayJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StrategyDecayJob> _logger;
    
    public StrategyDecayJob(
        IServiceProvider serviceProvider,
        ILogger<StrategyDecayJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run once per day at 2 AM UTC
            var now = DateTime.UtcNow;
            var next2AM = now.Date.AddHours(2);
            if (now.Hour >= 2) next2AM = next2AM.AddDays(1);
            
            var delay = next2AM - now;
            
            await Task.Delay(delay, stoppingToken);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var marketplace = scope.ServiceProvider.GetRequiredService<IStrategyMarketplaceService>();
                
                await marketplace.ApplyStrategyDecay();
                
                _logger.LogInformation("Strategy decay applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying strategy decay");
            }
        }
    }
}
```

### GuildBossSpawnJob

```csharp
public class GuildBossSpawnJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GuildBossSpawnJob> _logger;
    
    public GuildBossSpawnJob(
        IServiceProvider serviceProvider,
        ILogger<GuildBossSpawnJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run once per week on Monday at 00:00 UTC
            var now = DateTime.UtcNow;
            var nextMonday = GetNextMonday(now);
            var delay = nextMonday - now;
            
            _logger.LogInformation($"Next boss spawn scheduled for {nextMonday}");
            
            await Task.Delay(delay, stoppingToken);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                var bossService = scope.ServiceProvider.GetRequiredService<IGuildBossService>();
                
                // Get all active guilds
                var guilds = await context.Guilds.ToListAsync(stoppingToken);
                
                foreach (var guild in guilds)
                {
                    await bossService.SpawnBossForGuild(guild.Id);
                }
                
                _logger.LogInformation($"Spawned bosses for {guilds.Count} guilds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error spawning guild bosses");
            }
        }
    }
    
    private DateTime GetNextMonday(DateTime from)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        
        var nextMonday = from.Date.AddDays(daysUntilMonday);
        return new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
```

---

## Program.cs Updates

Add Phase 3 service registrations:

```csharp
// ... existing services ...

// Phase 3: Engagement & Anti-Meta Services
builder.Services.AddScoped<IModifierService, ModifierService>();
builder.Services.AddScoped<IGuildBossService, GuildBossService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IStrategyMarketplaceService, StrategyMarketplaceService>();
builder.Services.AddScoped<IMasteryService, MasteryService>();
builder.Services.AddScoped<IReplayService, ReplayService>();

// Phase 3: Background Jobs
builder.Services.AddHostedService<WeeklyModifierRotationJob>();
builder.Services.AddHostedService<DailyChallengeGenerationJob>();
builder.Services.AddHostedService<StrategyDecayJob>();
builder.Services.AddHostedService<GuildBossSpawnJob>();
```

---

## Database Migration

Create migration for Phase 3 tables:

```bash
dotnet ef migrations add Phase3_EngagementFeatures --project ApiCombatGame
dotnet ef database update --project ApiCombatGame
```

---

## Seed Data (Example Modifiers)

Add initial environmental modifiers to seed data:

```csharp
public static class Phase3SeedData
{
    public static async Task SeedModifiers(GameDbContext context)
    {
        if (await context.EnvironmentalModifiers.AnyAsync())
            return; // Already seeded
        
        var modifiers = new List<EnvironmentalModifier>
        {
            new EnvironmentalModifier
            {
                Id = Guid.NewGuid(),
                Name = "Arcane Disruption",
                Description = "Mage abilities cost 2x mana, physical attacks deal +20% damage",
                ModifierType = "ArcaneDisruption",
                ConfigJson = "{}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new EnvironmentalModifier
            {
                Id = Guid.NewGuid(),
                Name = "Heavy Armor",
                Description = "All units gain +50% defense, healer abilities 2x effectiveness",
                ModifierType = "HeavyArmor",
                ConfigJson = "{}",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(14),
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        context.EnvironmentalModifiers.AddRange(modifiers);
        await context.SaveChangesAsync();
    }
}
```

---

## Extension Points (Documentation)

Document how future developers can extend the system:

### Adding New Modifiers

```csharp
// 1. Create new class implementing IModifierEffect
public class MyCustomModifier : BaseModifierEffect
{
    public override string Name => "My Modifier";
    public override string Description => "Does something cool";
    
    public override void ModifyUnitStats(Unit unit)
    {
        // Implement your logic
    }
}

// 2. Register in ModifierService constructor
_modifiers.Add("MyCustomModifier", new MyCustomModifier());

// 3. Create database entry
context.EnvironmentalModifiers.Add(new EnvironmentalModifier
{
    Name = "My Modifier",
    ModifierType = "MyCustomModifier",
    // ...
});
```

### Adding New Challenge Types

```csharp
// 1. Create new class implementing IChallengeGenerator
public class MyCustomChallenge : BaseChallengeGenerator
{
    public override string ChallengeType => "MyChallenge";
    
    public override DailyChallenge Generate(Player player)
    {
        // Generate challenge
    }
    
    public override bool CheckProgress(DailyChallenge challenge, Battle battle)
    {
        // Check if battle satisfies requirements
    }
}

// 2. Register in ChallengeService constructor
_generators.Add(new MyCustomChallenge());
```

### Adding New Boss Abilities

```csharp
// 1. Create new class implementing IBossAbility
public class MyBossAbility : BaseBossAbility
{
    public override string Name => "My Ability";
    public override string Description => "Boss does something";
    
    public override void Execute(GuildBoss boss, BattleContext context)
    {
        // Implement ability
    }
}

// 2. Serialize into boss's AbilitiesJson when creating boss
var boss = new GuildBoss
{
    AbilitiesJson = JsonSerializer.Serialize(new[]
    {
        new { Type = "MyBossAbility", Config = new { /* params */ } }
    })
};
```

---

## Testing Checklist

After implementation, test:

**Modifiers:**
- [ ] Can retrieve current modifier via `/api/v1/modifiers/current`
- [ ] Can retrieve upcoming modifier via `/api/v1/modifiers/upcoming`
- [ ] Modifier affects battle stats correctly
- [ ] Weekly rotation job runs and switches modifiers

**Guild Bosses:**
- [ ] Can retrieve active boss via `/api/v1/guild/boss/current`
- [ ] Can attempt boss damage via `/api/v1/guild/boss/attempt`
- [ ] Boss HP decreases correctly
- [ ] Leaderboard shows top damage dealers
- [ ] Boss spawns weekly via job

**Daily Challenges:**
- [ ] Can retrieve active challenges via `/api/v1/challenges/daily`
- [ ] Challenges generate daily via job
- [ ] Progress updates after battles
- [ ] Can claim rewards via `/api/v1/challenges/claim`

**Strategy Marketplace:**
- [ ] Can upload strategy via `/api/v1/strategies/upload`
- [ ] Can browse strategies via `/api/v1/strategies/browse`
- [ ] Can download strategy via `/api/v1/strategies/{id}/download`
- [ ] Can rate strategy via `/api/v1/strategies/{id}/rate`
- [ ] Strategy effectiveness decays over time

**Unit Mastery:**
- [ ] Can retrieve mastery via `/api/v1/mastery/units`
- [ ] Mastery increases after battles
- [ ] Levels up correctly

**Replays:**
- [ ] Can create replay via `/api/v1/replays/create`
- [ ] Can view replay via `/api/v1/replays/{shareUrl}`
- [ ] View count increments

---

## Future Implementation Notes

### Phase 3A (Next Steps)
- Implement full battle integration with modifiers
- Complete boss attempt logic with damage calculation
- Implement challenge progress tracking
- Add notification system for events

### Phase 3B (After 3A)
- Add guild management endpoints (create, join, leave)
- Implement guild chat/communication
- Add tournament system with unit bans
- Create admin dashboard for balance monitoring

### Phase 3C (Polish)
- Add achievement unlock notifications
- Implement title system with visual badges
- Create featured replay system
- Add social features (friends, followers)

---

## Questions to Answer for Me

After you generate all the code, please provide:

1. **Files Created:** Complete list of new files
2. **Database Schema:** New tables and relationships
3. **Extension Points:** Summary of how to add new modifiers/challenges/bosses
4. **Testing Guide:** Step-by-step to verify each feature works
5. **TODO Summary:** What needs implementation in future phases
6. **Known Limitations:** Features stubbed but not fully working

**END INSTRUCTIONS**

---

## What to Expect from Claude Code

After pasting these instructions, Claude Code should:

1. **Create all domain models** with proper relationships
2. **Implement service interfaces** with base classes for extensibility
3. **Stub out service implementations** with TODOs for future work
4. **Create all API controllers** with documented endpoints
5. **Implement background jobs** with scheduling logic
6. **Update Program.cs** with service registrations
7. **Create database migration** for new tables
8. **Provide documentation** on extension points

---

## After Claude Code Generates Everything

**Step 1: Run migration**
```bash
dotnet ef migrations add Phase3_EngagementFeatures --project ApiCombatGame
dotnet ef database update --project ApiCombatGame
```

**Step 2: Test new endpoints**
```bash
# Get current modifier
curl https://localhost:7000/api/v1/modifiers/current

# Get daily challenges (requires auth)
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://localhost:7000/api/v1/challenges/daily

# Browse strategies
curl https://localhost:7000/api/v1/strategies/browse?sortBy=popular&limit=10
```

**Step 3: Verify background jobs**
- Check logs for job startup messages
- Wait for scheduled times or trigger manually for testing

**Step 4: Implement TODOs**
- Search for "TODO: Phase 3 implementation" in code
- Prioritize based on feature importance
- Test each implementation individually

---

## Success Criteria

After Phase 3 implementation, you should have:

- [x] Extensible modifier system (easy to add new modifiers)
- [x] Pluggable challenge system (easy to add new challenge types)
- [x] Guild boss framework (ready for boss battles)
- [x] Strategy marketplace foundation (upload/download/rate)
- [x] Unit mastery tracking (progression system)
- [x] Battle replay sharing (social features)
- [x] Background jobs (automated content rotation)
- [x] Clear extension points documented
- [x] Testable via API endpoints
- [x] Database schema supports all features

**Most importantly:** Future features can be added without changing core architecture.

---

**Good luck implementing Phase 3! This creates the foundation for endless content variations. 🎮**

---

*Document Version: 1.0*  
*Last Updated: February 10, 2026*  
*Prepared by: Mark @ Learned Geek Consulting*
