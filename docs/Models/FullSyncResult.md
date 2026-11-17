# FullSyncResult

## Purpose

`FullSyncResult` is the summary of a full Google Drive sync. It is returned by `IDriveSyncService.RunFullSyncAsync` and by the `POST /api/v1/Drive/full-sync` endpoint. It is not stored in the database.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `FilesFound` | `int` | Number of files (not folders) found in Google Drive. |
| `Created` | `int` | Files that were new and were indexed. |
| `Updated` | `int` | Files that changed and were indexed again. |
| `Unchanged` | `int` | Files that did not change since the last sync. |
| `Deleted` | `int` | Files removed from the database because they are no longer in Drive. |
| `Skipped` | `int` | Files with an unsupported type. |
| `Failed` | `int` | Files that could not be synced. |
