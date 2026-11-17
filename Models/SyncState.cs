namespace DriveFinder.Models;

public class SyncState
{
    public int Id { get; set; }
    public string? PageToken { get; set; }
    public DateTimeOffset? LastFullSyncAt { get; set; }
    public DateTimeOffset? LastIncrementalSyncAt { get; set; }
}
