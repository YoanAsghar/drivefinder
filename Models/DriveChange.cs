namespace DriveFinder.Models;

public class DriveChange
{
    public long Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DriveChangeType ChangeType { get; set; }
    public string? ChangeToken { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public ChangeStatus Status { get; set; } = ChangeStatus.Pending;
}
