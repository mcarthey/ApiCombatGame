using ApiCombatGame.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<Ability> Abilities => Set<Ability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasMany(u => u.Abilities)
                .WithOne(a => a.Unit)
                .HasForeignKey(a => a.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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
    }
}
