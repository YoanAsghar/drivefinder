using Pgvector;

namespace DriveFinder.Models;

public class DriveFileChunk
{
    public Guid Id { get; set; }

    public Guid DriveFileId { get; set; }

    public DriveFile? DriveFile { get; set; }

    public int Index { get; set; }

    public string Content { get; set; } = string.Empty;

    public Vector? Embedding { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
