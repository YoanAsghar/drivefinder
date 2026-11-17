using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DriveFinder.Models;

namespace DriveFinder.Data.Configurations;

public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("sync_state");

        builder.HasKey(state => state.Id);
        builder.HasData(new SyncState { Id = 1 });
    }
}
