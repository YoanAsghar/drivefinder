namespace DriveFinder.Models;

public class SearchResult
{
    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? WebViewLink { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public double Score { get; set; }
}
