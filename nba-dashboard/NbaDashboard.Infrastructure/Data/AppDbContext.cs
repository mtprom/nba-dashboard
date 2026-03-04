using Microsoft.EntityFrameworkCore;
using NbaDashboard.Core.Entities;

namespace NbaDashboard.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<PlayerGameStats> PlayerGameStats => Set<PlayerGameStats>();
    public DbSet<PlayerGameAdvanced> PlayerGameAdvanced => Set<PlayerGameAdvanced>();
    public DbSet<PlayerSeasonStats> PlayerSeasonStats => Set<PlayerSeasonStats>();
    public DbSet<PlayerHeat> PlayerHeat => Set<PlayerHeat>();
    public DbSet<StandingsSnapshot> StandingsSnapshots => Set<StandingsSnapshot>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Season
        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Year).IsUnique();
        });

        // Team
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Abbreviation).HasMaxLength(10);
            entity.Property(t => t.City).HasMaxLength(100);
            entity.HasMany(t => t.Players)
                  .WithOne(p => p.Team)
                  .HasForeignKey(p => p.TeamId);
        });

        // Player
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FirstName).HasMaxLength(100);
            entity.Property(p => p.LastName).HasMaxLength(100);
            entity.Property(p => p.Position).HasMaxLength(10);
            entity.Property(p => p.Height).HasMaxLength(10);
            entity.Property(p => p.Weight).HasMaxLength(10);
            entity.Property(p => p.JerseyNumber).HasMaxLength(5);
        });

        // Game
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasOne(g => g.Season)
                  .WithMany(s => s.Games)
                  .HasForeignKey(g => g.SeasonId);
            entity.HasOne(g => g.HomeTeam)
                  .WithMany()
                  .HasForeignKey(g => g.HomeTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(g => g.VisitorTeam)
                  .WithMany()
                  .HasForeignKey(g => g.VisitorTeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(g => g.Date);
            entity.HasIndex(g => g.SeasonId);
        });

        // PlayerGameStats
        modelBuilder.Entity<PlayerGameStats>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Game)
                  .WithMany(g => g.PlayerGameStats)
                  .HasForeignKey(s => s.GameId);
            entity.HasOne(s => s.Player)
                  .WithMany(p => p.GameStats)
                  .HasForeignKey(s => s.PlayerId);
            entity.HasOne(s => s.Team)
                  .WithMany()
                  .HasForeignKey(s => s.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => s.PlayerId);
            entity.HasIndex(s => new { s.GameId, s.PlayerId }).IsUnique();
        });

        // PlayerGameAdvanced
        modelBuilder.Entity<PlayerGameAdvanced>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasOne(a => a.Game)
                  .WithMany(g => g.PlayerGameAdvanced)
                  .HasForeignKey(a => a.GameId);
            entity.HasOne(a => a.Player)
                  .WithMany(p => p.GameAdvanced)
                  .HasForeignKey(a => a.PlayerId);
            entity.HasOne(a => a.Team)
                  .WithMany()
                  .HasForeignKey(a => a.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => a.PlayerId);
            entity.HasIndex(a => new { a.GameId, a.PlayerId }).IsUnique();
        });

        // PlayerSeasonStats
        modelBuilder.Entity<PlayerSeasonStats>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Player)
                  .WithMany(p => p.SeasonStats)
                  .HasForeignKey(s => s.PlayerId);
            entity.HasOne(s => s.Season)
                  .WithMany()
                  .HasForeignKey(s => s.SeasonId);
            entity.HasOne(s => s.Team)
                  .WithMany()
                  .HasForeignKey(s => s.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => new { s.PlayerId, s.SeasonId, s.TeamId }).IsUnique();
        });

        // PlayerHeat
        modelBuilder.Entity<PlayerHeat>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasOne(h => h.Player)
                  .WithMany(p => p.HeatScores)
                  .HasForeignKey(h => h.PlayerId);
            entity.HasIndex(h => new { h.ComputedDate, h.PlayerId });
            entity.HasIndex(h => new { h.PlayerId, h.ComputedDate }).IsUnique();
        });

        // StandingsSnapshot
        modelBuilder.Entity<StandingsSnapshot>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Team)
                  .WithMany()
                  .HasForeignKey(s => s.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Season)
                  .WithMany()
                  .HasForeignKey(s => s.SeasonId);
            entity.HasIndex(s => new { s.TeamId, s.SeasonId, s.SnapshotDate }).IsUnique();
        });

        // SyncState
        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.HasKey(s => s.Key);
        });
    }
}
