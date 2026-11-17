using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DriveFinder.Models;

namespace DriveFinder.Data.Configurations;

public class DriveChangeConfiguration : IEntityTypeConfiguration<DriveChange>
{
    public void Configure(EntityTypeBuilder<DriveChange> builder)
    {
        builder.ToTable("drive_changes");

        builder.HasKey(change => change.Id);
        builder.HasIndex(change => change.FileId);
        builder.HasIndex(change => change.DetectedAt);

        builder.Property(change => change.FileId).HasMaxLength(255).IsRequired();
        builder.Property(change => change.ChangeType).HasConversion<string>().IsRequired();
        builder.Property(change => change.ChangeToken).HasMaxLength(255);
        builder.Property(change => change.Status).HasConversion<string>().IsRequired();
    }
}
