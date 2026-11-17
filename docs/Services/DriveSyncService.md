# DriveSyncService

## Purpose

The `DriveSyncService` keeps the database in line with Google Drive. It reads files through `IGoogleDriveService`, creates chunks with embeddings through `IEmbeddingsService`, and saves files, chunks, changes, and sync state with `DriveFinderDbContext`.

## Public API

### `IDriveSyncService`

| Method | Description |
|--------|-------------|
| `Task<FullSyncResult> RunFullSyncAsync(CancellationToken cancellationToken = default)` | Syncs the whole Drive with the database and returns a summary of what happened. |

## Dependencies

- **IGoogleDriveService** — Lists files and folders and reads file text.
- **IEmbeddingsService** — Splits text into chunks and creates embeddings.
- **DriveFinderDbContext** — Stores `DriveFile`, `DriveFileChunk`, `DriveChange`, and `SyncState` rows.
- **ILogger<DriveSyncService>** — Logs files that fail to sync.

## Full Sync Steps

1. Get the Drive start page token first, so changes made during the sync are picked up by the next incremental sync.
2. List all folders and build a folder map used for file paths such as `/Projects/2026/notes.txt`.
3. List all files. For each file:
   - Unsupported type: counted as `Skipped`.
   - Already in the database with the same `ModifiedTime`, `Name`, and `ParentId`: counted as `Unchanged`. Nothing is downloaded.
   - Otherwise: read the text, compute a SHA-256 `ContentHash`, create chunks with embeddings, then save the file, its chunks, and a `DriveChange` row (`Created` or `Updated`) in one transaction. Old chunks are deleted in the same transaction.
4. Delete database files whose Drive file ID was not found in Drive. Their chunks are removed by the cascade delete, and a `Deleted` change is registered for each one.
5. Save the page token and `LastFullSyncAt` in the `SyncState` row with ID 1.

## Error Handling

- If one file fails, the error is logged, a `DriveChange` with status `Failed` is registered, and the sync continues with the next file.
- Embeddings are created before the database is changed, and the database work runs in a transaction. A failed file keeps its previous data, and it is retried on the next sync because its `ModifiedTime` was not updated.
- Cancellation (`OperationCanceledException`) stops the whole sync.
- Errors from Google while listing files stop the whole sync, so files are never deleted from the database because of a failed listing.

## Implementation Details

- Null characters (`\0`) are removed from the text, because PostgreSQL text columns cannot store them.
- `DriveFile.Embedding` is not filled. Search uses the chunk embeddings.
- When a file has more than one parent folder, only the first parent is used for its path.
