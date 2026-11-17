namespace DriveFinder.Models;

public class FullSyncResult
{
    public int FilesFound { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    public int Unchanged { get; set; }

    public int Deleted { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }
}
