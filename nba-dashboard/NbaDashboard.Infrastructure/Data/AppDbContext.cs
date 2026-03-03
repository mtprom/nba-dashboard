using Microsoft.EntityFrameworkCore;
using NbaDashboard.Core.Entities;

namespace NbaDashboard.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<BoxScore> BoxScores => Set<BoxScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Abbreviation).IsRequired().HasMaxLength(3);
            entity.HasMany(t => t.Players)
                  .WithOne(p => p.Team)
                  .HasForeignKey(p => p.TeamId);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasOne(g => g.HomeTeam)
                  .WithMany()
                  .HasForeignKey(g => g.HomeTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(g => g.AwayTeam)
                  .WithMany()
                  .HasForeignKey(g => g.AwayTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BoxScore>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.Player)
                  .WithMany(p => p.BoxScores)
                  .HasForeignKey(b => b.PlayerId);
            entity.HasOne(b => b.Game)
                  .WithMany(g => g.BoxScores)
                  .HasForeignKey(b => b.GameId);
        });
    }
}
