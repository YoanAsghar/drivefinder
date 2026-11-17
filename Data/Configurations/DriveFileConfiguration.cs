using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DriveFinder.Models;

namespace DriveFinder.Data.Configurations;

public class DriveFileConfiguration : IEntityTypeConfiguration<DriveFile>
{
    public void Configure(EntityTypeBuilder<DriveFile> builder)
    {
        builder.ToTable("drive_files");

        builder.HasKey(file => file.Id);
        builder.HasIndex(file => file.FileId).IsUnique();
        builder.HasIndex(file => file.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.Property(file => file.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(file => file.FileId).HasMaxLength(255).IsRequired();
        builder.Property(file => file.Name).IsRequired();
        builder.Property(file => file.MimeType).IsRequired();
        builder.Property(file => file.ParentId).HasMaxLength(255);
        builder.Property(file => file.ContentHash).HasMaxLength(128);
        builder.Property(file => file.ContentText).IsRequired();
        builder.Property(file => file.Embedding).HasColumnType("vector(768)");
    }
}
