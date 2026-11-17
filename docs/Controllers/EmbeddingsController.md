# EmbeddingsController

## Purpose

The `EmbeddingsController` exposes an HTTP endpoint for generating text chunks with embeddings from a DriveFile. It receives file content and metadata via a POST request, delegates chunking and embedding generation to the `IEmbeddingsService`, and returns the resulting list of `DriveFileChunk` entities.

## Endpoints

### POST `/api/v1/Embeddings/file-chunks`

Splits the content of a DriveFile into chunks, generates embeddings, and returns `DriveFileChunk` entities with file metadata.

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `DriveFileId` | `Guid` | Yes | The ID of the source DriveFile. |
| `FileName` | `string` | Yes | The name of the source file. |
| `MimeType` | `string` | No | The MIME type of the file (defaults to `text/plain`). |
| `FilePath` | `string` | Yes | The path/route of the file in Google Drive. |
| `ContentText` | `string` | Yes | The full text content of the file to split into chunks. |

**Response:** `200 OK` — Returns a JSON array of `FileChunkResponse` objects. The embedding vector is not included in the response to keep the payload small.

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Unique identifier for the chunk. |
| `driveFileId` | `Guid` | Foreign key to the source DriveFile. |
| `index` | `int` | Zero-based index of the chunk. |
| `content` | `string` | The text content of the chunk. |
| `filePath` | `string` | The path/route of the source file in Google Drive. |
| `fileName` | `string` | The name of the source file. |
| `mimeType` | `string` | The MIME type of the source file. |
| `createdAt` | `DateTimeOffset` | When the chunk was created. |

**Error Responses:**

| Status Code | Condition |
|-------------|-----------|
| `400 Bad Request` | The `ContentText` or `FilePath` field is empty or whitespace. |

## Dependencies

- **IEmbeddingsService** — Handles the actual text splitting and embedding generation.
- **DriveFinder.Models.DriveFileChunk** — Entity produced by the service and mapped to `FileChunkResponse`.

## Request Model

`EmbedFileRequest` is a DTO that carries the file metadata (`DriveFileId`, `FileName`, `MimeType`, `FilePath`) and the full `ContentText` to be chunked.

## Response Model

`FileChunkResponse` is a DTO with the same fields as `DriveFileChunk`, except `Embedding` and the `DriveFile` navigation property.
