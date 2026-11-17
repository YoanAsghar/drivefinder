using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DriveFinder.Models;

namespace DriveFinder.Data.Configurations;

public class DriveFileChunkConfiguration : IEntityTypeConfiguration<DriveFileChunk>
{
    public void Configure(EntityTypeBuilder<DriveFileChunk> builder)
    {
        builder.ToTable("drive_file_chunks");

        builder.HasKey(chunk => chunk.Id);

        builder.HasIndex(chunk => chunk.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasIndex(chunk => new { chunk.DriveFileId, chunk.Index });

        builder.Property(chunk => chunk.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(chunk => chunk.DriveFileId).IsRequired();
        builder.Property(chunk => chunk.Index).IsRequired();
        builder.Property(chunk => chunk.Content).IsRequired();
        builder.Property(chunk => chunk.FilePath).HasMaxLength(1024).IsRequired();
        builder.Property(chunk => chunk.FileName).HasMaxLength(512).IsRequired();
        builder.Property(chunk => chunk.MimeType).HasMaxLength(255).IsRequired();
        builder.Property(chunk => chunk.CreatedAt).IsRequired();
        builder.Property(chunk => chunk.Embedding).HasColumnType("vector(768)");

        builder.HasOne(chunk => chunk.DriveFile)
            .WithMany()
            .HasForeignKey(chunk => chunk.DriveFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
