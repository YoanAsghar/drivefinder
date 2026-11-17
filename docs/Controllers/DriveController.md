# DriveController

## Purpose

The `DriveController` exposes the HTTP endpoints that sync Google Drive with the database. For now it has one endpoint that runs a full sync of the whole Drive.

## Endpoints

### POST `/api/v1/Drive/full-sync`

Reads every supported file in Google Drive, saves its text and chunks with embeddings to the database, and removes files that no longer exist in Drive. The sync runs inside the HTTP request, so a large Drive can take several minutes.

**Request Body:** none.

**Response:** `200 OK` — Returns a `FullSyncResult` object.

| Field | Type | Description |
|-------|------|-------------|
| `filesFound` | `int` | Number of files (not folders) found in Google Drive. |
| `created` | `int` | Files that were new and were indexed. |
| `updated` | `int` | Files that changed and were indexed again. |
| `unchanged` | `int` | Files that did not change since the last sync. |
| `deleted` | `int` | Files removed from the database because they are no longer in Drive. |
| `skipped` | `int` | Files with an unsupported type (no text could be read). |
| `failed` | `int` | Files that could not be synced. Each one is recorded as a `Failed` row in `drive_changes`. |

**Error Responses:**

| Status Code | Condition |
|-------------|-----------|
| `502 Bad Gateway` | Google Drive rejected the request (for example missing authentication) or could not be reached. |

## Dependencies

- **IDriveSyncService** — Runs the full sync.
- **DriveFinder.Models.FullSyncResult** — Summary returned in the response.

## Important Notes

- The app reads one Google Workspace shared drive as a service account (see `docs/Program.md`). Only that shared drive is synced.
