# EmbeddingsService

## Purpose

The `EmbeddingsService` is responsible for splitting the content of a `DriveFile` into smaller chunks and generating vector embeddings for each chunk using Ollama. It returns `DriveFileChunk` entities ready for database persistence.

## Public API

### `IEmbeddingsService`

| Method | Description |
|--------|-------------|
| `Task<List<DriveFileChunk>> CreateFileChunksAsync(DriveFile file, string filePath, CancellationToken cancellationToken = default)` | Splits the content of a `DriveFile` into chunks, generates embeddings, and returns `DriveFileChunk` entities with file metadata (filePath, fileName, mimeType) ready for database persistence. |
| `Task<Vector> CreateQueryEmbeddingAsync(string query, CancellationToken cancellationToken = default)` | Generates the embedding of a search query. Used by `SearchService`. |

## Dependencies

- **OllamaSharp** — Provides `IEmbeddingGenerator<string, Embedding<float>>` to call Ollama's embedding API.
- **IConfiguration** — Reads `Embeddings:MaxChunkSize` from configuration (defaults to 1000 characters).
- **DriveFinder.Models.DriveFileChunk** — Entity for persistent storage of chunks with file metadata.
- **DriveFinder.Models.DriveFile** — Source file entity used by `CreateFileChunksAsync`.

## Configuration

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Embeddings:MaxChunkSize` | `int` | `1000` | Maximum number of characters per chunk. |
| `Ollama:Endpoint` | `string` | `http://localhost:11434` | Ollama server URL. |
| `Ollama:Model` | `string` | `nomic-embed-text` | Ollama embedding model name. |

## Implementation Details

- The service splits text by attempting to break at sentence boundaries (`.`, `!`, `?`, newline) to keep chunks semantically meaningful.
- For each window `[start, end)`, it searches backward from `end - 1` over the whole window (`LastIndexOfAny(chars, end - 1, end - start)`) for the last sentence boundary.
- If no sentence boundary is found in the last three quarters of the window, it falls back to a hard character limit.
- `nomic-embed-text` expects a task prefix. Chunks are embedded as `search_document: <text>` and queries as `search_query: <query>`. The prefix is only used for the embedding; the stored `Content` has no prefix. If the prefixes change, every chunk must be embedded again.
- `CreateFileChunksAsync` populates all metadata fields (`FilePath`, `FileName`, `MimeType`, `CreatedAt`) on each `DriveFileChunk`.
- Empty or whitespace-only input returns an empty list.
