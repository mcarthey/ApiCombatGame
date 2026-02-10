using ApiCombatGame.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // Phase 1: Core
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<Ability> Abilities => Set<Ability>();

    // Phase 2: Subscriptions & API Keys
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // Phase 3: Engagement & Anti-Meta
    public DbSet<EnvironmentalModifier> EnvironmentalModifiers => Set<EnvironmentalModifier>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMembership> GuildMemberships => Set<GuildMembership>();
    public DbSet<GuildBoss> GuildBosses => Set<GuildBoss>();
    public DbSet<GuildBossAttempt> GuildBossAttempts => Set<GuildBossAttempt>();
    public DbSet<DailyChallenge> DailyChallenges => Set<DailyChallenge>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyRating> StrategyRatings => Set<StrategyRating>();
    public DbSet<UnitMastery> UnitMasteries => Set<UnitMastery>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<PlayerAchievement> PlayerAchievements => Set<PlayerAchievement>();
    public DbSet<BattleReplay> BattleReplays => Set<BattleReplay>();
    public DbSet<PlayerTitle> PlayerTitles => Set<PlayerTitle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Player ──
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(p => p.Username).IsUnique();
            entity.HasIndex(p => p.Email).IsUnique();

            entity.HasMany(p => p.Roster)
                .WithOne(u => u.Player)
                .HasForeignKey(u => u.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Teams)
                .WithOne(t => t.Player)
                .HasForeignKey(t => t.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Subscription)
                .WithOne(s => s.Player)
                .HasForeignKey<Subscription>(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.ApiKeys)
                .WithOne(k => k.Player)
                .HasForeignKey(k => k.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Phase 3 relationships
            entity.HasOne(p => p.ActiveTitle)
                .WithMany()
                .HasForeignKey(p => p.ActiveTitleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.GuildMembership)
                .WithOne(m => m.Player)
                .HasForeignKey<GuildMembership>(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.DailyChallenges)
                .WithOne(c => c.Player)
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.CreatedStrategies)
                .WithOne(s => s.Creator)
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.UnitMasteries)
                .WithOne(m => m.Player)
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Achievements)
                .WithOne(a => a.Player)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.BossAttempts)
                .WithOne(a => a.Player)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Unit ──
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasMany(u => u.Abilities)
                .WithOne(a => a.Unit)
                .HasForeignKey(a => a.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Battle ──
        modelBuilder.Entity<Battle>(entity =>
        {
            entity.HasOne(b => b.Player1)
                .WithMany()
                .HasForeignKey(b => b.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Player2)
                .WithMany()
                .HasForeignKey(b => b.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Subscription (Phase 2) ──
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(s => s.StripeCustomerId);
            entity.HasIndex(s => s.StripeSubscriptionId);
            entity.Property(s => s.AmountUsd).HasColumnType("decimal(10,2)");
        });

        // ── ApiKey (Phase 2) ──
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(k => k.KeyHash);
            entity.HasIndex(k => new { k.PlayerId, k.IsActive });
        });

        // ── Guild ──
        modelBuilder.Entity<Guild>(entity =>
        {
            entity.HasIndex(g => g.Name).IsUnique();
            entity.HasIndex(g => g.Tag).IsUnique();

            entity.HasOne(g => g.Leader)
                .WithMany()
                .HasForeignKey(g => g.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(g => g.Members)
                .WithOne(m => m.Guild)
                .HasForeignKey(m => m.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Bosses)
                .WithOne(b => b.Guild)
                .HasForeignKey(b => b.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GuildMembership ──
        modelBuilder.Entity<GuildMembership>(entity =>
        {
            entity.HasIndex(m => new { m.GuildId, m.PlayerId }).IsUnique();
        });

        // ── GuildBoss ──
        modelBuilder.Entity<GuildBoss>(entity =>
        {
            entity.HasMany(b => b.Attempts)
                .WithOne(a => a.GuildBoss)
                .HasForeignKey(a => a.GuildBossId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DailyChallenge ──
        modelBuilder.Entity<DailyChallenge>(entity =>
        {
            entity.HasIndex(c => new { c.PlayerId, c.ExpiresAt });
        });

        // ── Strategy ──
        modelBuilder.Entity<Strategy>(entity =>
        {
            entity.HasIndex(s => s.IsPublic);
            entity.HasIndex(s => s.DownloadCount);

            entity.HasMany(s => s.Ratings)
                .WithOne(r => r.Strategy)
                .HasForeignKey(r => r.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StrategyRating ──
        modelBuilder.Entity<StrategyRating>(entity =>
        {
            entity.HasIndex(r => new { r.StrategyId, r.PlayerId }).IsUnique();
        });

        // ── UnitMastery ──
        modelBuilder.Entity<UnitMastery>(entity =>
        {
            entity.HasIndex(m => new { m.PlayerId, m.UnitId }).IsUnique();
        });

        // ── Achievement ──
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasIndex(a => a.Category);

            entity.HasMany(a => a.PlayerAchievements)
                .WithOne(pa => pa.Achievement)
                .HasForeignKey(pa => pa.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerAchievement ──
        modelBuilder.Entity<PlayerAchievement>(entity =>
        {
            entity.HasIndex(pa => new { pa.PlayerId, pa.AchievementId }).IsUnique();
        });

        // ── BattleReplay ──
        modelBuilder.Entity<BattleReplay>(entity =>
        {
            entity.HasIndex(r => r.ShareUrl).IsUnique();
            entity.HasIndex(r => r.BattleId).IsUnique();

            entity.HasOne(r => r.Battle)
                .WithMany()
                .HasForeignKey(r => r.BattleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── EnvironmentalModifier ──
        modelBuilder.Entity<EnvironmentalModifier>(entity =>
        {
            entity.HasIndex(m => new { m.IsActive, m.StartDate, m.EndDate });
        });

        // ── PlayerTitle ──
        modelBuilder.Entity<PlayerTitle>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });
    }
}
