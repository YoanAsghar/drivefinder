using Microsoft.EntityFrameworkCore;
using DriveFinder.Data.Configurations;
using DriveFinder.Models;

namespace DriveFinder.Data;

public class DriveFinderDbContext(DbContextOptions<DriveFinderDbContext> options) : DbContext(options)
{
    public DbSet<DriveFile> DriveFiles => Set<DriveFile>();

    public DbSet<DriveChange> DriveChanges => Set<DriveChange>();

    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DriveFinderDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

