using Pgvector;

namespace DriveFinder.Models;

public class DriveFile
{
    public Guid Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? WebViewLink { get; set; }
    public DateTimeOffset ModifiedTime { get; set; }
    public DateTimeOffset IndexedAt { get; set; }
    public string? ContentHash { get; set; }
    public string ContentText { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
}
