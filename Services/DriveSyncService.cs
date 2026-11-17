using System.Security.Cryptography;
using System.Text;
using DriveFinder.Data;
using DriveFinder.Models;
using Microsoft.EntityFrameworkCore;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace DriveFinder.Services;

public interface IDriveSyncService
{
    Task<FullSyncResult> RunFullSyncAsync(CancellationToken cancellationToken = default);
}

public class DriveSyncService : IDriveSyncService
{
    private const int SyncStateId = 1;
    private const int MaxFolderDepth = 100;

    private readonly IGoogleDriveService _googleDriveService;
    private readonly IEmbeddingsService _embeddingsService;
    private readonly DriveFinderDbContext _dbContext;
    private readonly ILogger<DriveSyncService> _logger;

    public DriveSyncService(
        IGoogleDriveService googleDriveService,
        IEmbeddingsService embeddingsService,
        DriveFinderDbContext dbContext,
        ILogger<DriveSyncService> logger)
    {
        _googleDriveService = googleDriveService;
        _embeddingsService = embeddingsService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<FullSyncResult> RunFullSyncAsync(CancellationToken cancellationToken = default)
    {
        var result = new FullSyncResult();

        // Get the token before listing, so changes made during this sync are picked up by the next incremental sync
        var startPageToken = await _googleDriveService.GetStartPageTokenAsync(cancellationToken);

        var folders = await _googleDriveService.ListAllFoldersAsync(cancellationToken);
        var folderById = folders.ToDictionary(folder => folder.Id);

        var googleFiles = await _googleDriveService.ListAllFilesAsync(cancellationToken);
        result.FilesFound = googleFiles.Count;

        foreach (var googleFile in googleFiles)
        {
            if (!_googleDriveService.IsSupported(googleFile.MimeType))
            {
                result.Skipped++;
                continue;
            }

            try
            {
                var changeType = await SyncFileAsync(googleFile, folderById, cancellationToken);

                if (changeType == DriveChangeType.Created)
                {
                    result.Created++;
                }
                else if (changeType == DriveChangeType.Updated)
                {
                    result.Updated++;
                }
                else
                {
                    result.Unchanged++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Failed to sync Drive file {FileId}", googleFile.Id);
                await RegisterFailedChangeAsync(googleFile, cancellationToken);
                result.Failed++;
            }
        }

        var driveFileIds = googleFiles.Select(googleFile => googleFile.Id).ToList();
        result.Deleted = await DeleteFilesMissingFromDriveAsync(driveFileIds, cancellationToken);

        await SaveSyncStateAsync(startPageToken, cancellationToken);

        return result;
    }

    /// <summary>
    /// Saves one Google Drive file and its chunks to the database.
    /// Returns Created or Updated, or null when the file did not change.
    /// </summary>
    private async Task<DriveChangeType?> SyncFileAsync(
        GoogleFile googleFile,
        Dictionary<string, GoogleFile> folderById,
        CancellationToken cancellationToken)
    {
        var existingFile = await _dbContext.DriveFiles
            .FirstOrDefaultAsync(file => file.FileId == googleFile.Id, cancellationToken);

        var modifiedTime = googleFile.ModifiedTimeDateTimeOffset?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var parentId = googleFile.Parents?.FirstOrDefault();

        if (existingFile != null
            && existingFile.ModifiedTime == modifiedTime
            && existingFile.Name == googleFile.Name
            && existingFile.ParentId == parentId)
        {
            return null;
        }

        var contentText = await _googleDriveService.ReadFileTextAsync(googleFile, cancellationToken);

        // PostgreSQL text columns cannot store null characters, which PDF extraction sometimes produces
        contentText = contentText.Replace("\0", string.Empty);

        var isNewFile = existingFile == null;

        var file = existingFile ?? new DriveFile { Id = Guid.NewGuid(), FileId = googleFile.Id };
        file.Name = googleFile.Name;
        file.MimeType = googleFile.MimeType;
        file.ParentId = parentId;
        file.WebViewLink = googleFile.WebViewLink;
        file.ModifiedTime = modifiedTime;
        file.IndexedAt = DateTimeOffset.UtcNow;
        file.ContentHash = ComputeHash(contentText);
        file.ContentText = contentText;

        // Create the embeddings before touching the database, so a failure here leaves the old data intact
        var filePath = BuildFilePath(googleFile, folderById);
        var chunks = await _embeddingsService.CreateFileChunksAsync(file, filePath, cancellationToken);

        var changeType = isNewFile ? DriveChangeType.Created : DriveChangeType.Updated;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (isNewFile)
        {
            _dbContext.DriveFiles.Add(file);
        }
        else
        {
            await _dbContext.DriveFileChunks
                .Where(chunk => chunk.DriveFileId == file.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _dbContext.DriveFileChunks.AddRange(chunks);
        _dbContext.DriveChanges.Add(CreateChange(googleFile.Id, googleFile.Name, changeType, ChangeStatus.Processed));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return changeType;
    }

    private async Task RegisterFailedChangeAsync(GoogleFile googleFile, CancellationToken cancellationToken)
    {
        // Drop any half-done changes from the failed file so they are not saved with the failure record
        _dbContext.ChangeTracker.Clear();

        var alreadyIndexed = await _dbContext.DriveFiles
            .AnyAsync(file => file.FileId == googleFile.Id, cancellationToken);

        var changeType = alreadyIndexed ? DriveChangeType.Updated : DriveChangeType.Created;

        _dbContext.DriveChanges.Add(CreateChange(googleFile.Id, googleFile.Name, changeType, ChangeStatus.Failed));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> DeleteFilesMissingFromDriveAsync(List<string> driveFileIds, CancellationToken cancellationToken)
    {
        var filesToDelete = await _dbContext.DriveFiles
            .Where(file => !driveFileIds.Contains(file.FileId))
            .ToListAsync(cancellationToken);

        foreach (var file in filesToDelete)
        {
            // The file's chunks are removed by the cascade delete
            _dbContext.DriveFiles.Remove(file);
            _dbContext.DriveChanges.Add(CreateChange(file.FileId, file.Name, DriveChangeType.Deleted, ChangeStatus.Processed));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return filesToDelete.Count;
    }

    private async Task SaveSyncStateAsync(string startPageToken, CancellationToken cancellationToken)
    {
        var syncState = await _dbContext.SyncStates.FindAsync([SyncStateId], cancellationToken);

        if (syncState == null)
        {
            syncState = new SyncState { Id = SyncStateId };
            _dbContext.SyncStates.Add(syncState);
        }

        syncState.PageToken = startPageToken;
        syncState.LastFullSyncAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DriveChange CreateChange(string fileId, string? fileName, DriveChangeType changeType, ChangeStatus status)
    {
        var now = DateTimeOffset.UtcNow;

        return new DriveChange
        {
            FileId = fileId,
            FileName = fileName,
            ChangeType = changeType,
            DetectedAt = now,
            ProcessedAt = status == ChangeStatus.Processed ? now : null,
            Status = status
        };
    }

    /// <summary>
    /// Builds a readable path such as "/Projects/2026/notes.txt" by walking up the parent folders.
    /// </summary>
    private static string BuildFilePath(GoogleFile googleFile, Dictionary<string, GoogleFile> folderById)
    {
        var pathParts = new List<string> { googleFile.Name };
        var parentId = googleFile.Parents?.FirstOrDefault();
        var depth = 0;

        // Stop at the Drive root (not in the folder list) or at a maximum depth to avoid endless loops
        while (parentId != null && folderById.TryGetValue(parentId, out var folder) && depth < MaxFolderDepth)
        {
            pathParts.Insert(0, folder.Name);
            parentId = folder.Parents?.FirstOrDefault();
            depth++;
        }

        return "/" + string.Join("/", pathParts);
    }

    private static string ComputeHash(string text)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes);
    }
}
