# GoogleDriveService

## Purpose

The `GoogleDriveService` is the only class that talks to the Google Drive API. It lists files and folders, reads the text content of files, and gets the start page token used for incremental sync. It does not touch the database.

## Public API

### `IGoogleDriveService`

| Method | Description |
|--------|-------------|
| `Task<string> GetStartPageTokenAsync(CancellationToken)` | Returns the current Drive changes page token. Changes after this token can be read later with the changes API. |
| `Task<List<File>> ListAllFilesAsync(CancellationToken)` | Returns every non-trashed file that is not a folder, going through all result pages. |
| `Task<List<File>> ListAllFoldersAsync(CancellationToken)` | Returns every non-trashed folder. Used to build file paths. |
| `bool IsSupported(string mimeType)` | Returns `true` when text can be read from files of this MIME type. |
| `Task<string> ReadFileTextAsync(File file, CancellationToken)` | Returns the text content of a supported file. |

`File` is `Google.Apis.Drive.v3.Data.File` (aliased as `GoogleFile` in the code). Listed files include the fields `id`, `name`, `mimeType`, `parents`, `webViewLink`, and `modifiedTime`.

## Supported File Types

| MIME type | How the text is read |
|-----------|----------------------|
| `application/vnd.google-apps.document` (Google Docs) | Exported as `text/plain`. |
| `application/vnd.google-apps.presentation` (Google Slides) | Exported as `text/plain`. |
| `application/vnd.google-apps.spreadsheet` (Google Sheets) | Exported as `text/csv`. Google only exports the first sheet. |
| `application/pdf` | Downloaded and read with PdfPig, page by page. |
| `application/vnd.openxmlformats-officedocument.wordprocessingml.document` (.docx) | Downloaded and read with DocumentFormat.OpenXml, paragraph by paragraph. |
| `text/*`, `application/json`, `application/xml` | Downloaded and read as UTF-8. |

## Shared Drive

The service has two modes, chosen by `GoogleDrive:SharedDriveId`:

| Setting | Mode | Use |
|---------|------|-----|
| Set | **Shared drive mode.** Only files from that Google Workspace shared drive are read. | Production. |
| Empty or missing | **Shared folder mode.** Every file the service account can see is read, which means the folders shared with its email. | Testing with a personal Gmail account, which cannot create shared drives. |

In shared drive mode:

- File and folder listing uses `Corpora = "drive"` and `DriveId`, so files from other drives are never returned.
- `SupportsAllDrives` and `IncludeItemsFromAllDrives` are set on listing and on the start page token request. Downloads always set `SupportsAllDrives`, which is harmless in shared folder mode.
- The start page token is requested for the shared drive. The future incremental sync must call the changes API with the same `DriveId` and flags.
- Exports of Google Docs, Sheets, and Slides do not need these flags.

## Dependencies

- **Google.Apis.Drive.v3** — `DriveService`, registered in `Program.cs`.
- **IConfiguration** — Reads `GoogleDrive:SharedDriveId`.
- **PdfPig** — Text extraction from PDF files.
- **DocumentFormat.OpenXml** — Text extraction from .docx files.

## Implementation Details

- Files are listed with a page size of 1000 and the service follows `nextPageToken` until all pages are read.
- Files are loaded fully into memory before text is extracted.
- Google limits exports of Google Docs, Sheets, and Slides to 10 MB.
- A failed download throws an `InvalidOperationException` with the Drive file ID.
- Scanned PDFs (images only) give empty text, because there is no OCR.
