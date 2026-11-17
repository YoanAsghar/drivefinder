# SearchService

## Purpose

The `SearchService` finds the indexed documents closest in meaning to a text query. It compares the query embedding with the chunk embeddings stored in PostgreSQL (pgvector).

## Public API

### `ISearchService`

| Method | Description |
|--------|-------------|
| `Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)` | Returns up to 3 results, best first. Each result is a different file, with its best-matching chunk. |

## Dependencies

- **IEmbeddingsService** — Creates the query embedding with Ollama (`CreateQueryEmbeddingAsync`).
- **DriveFinderDbContext** — Reads `DriveFileChunk` rows and the `WebViewLink` of their `DriveFile`.
- **Pgvector.EntityFrameworkCore** — Provides `CosineDistance`, translated to the pgvector `<=>` operator.

## Implementation Details

1. The query is turned into a vector with the `search_query: ` prefix (see `EmbeddingsService`).
2. The 30 closest chunks are read from the database, sorted by cosine distance. The HNSW index on `drive_file_chunks.Embedding` (`vector_cosine_ops`) makes this fast.
3. Only the first (best) chunk of each file is kept, and the first 3 files are returned.
4. `Score` is `1 - cosine distance`, so higher means closer.

More chunks than results are read because several of the closest chunks can belong to the same file. If the 30 closest chunks come from fewer than 3 files, fewer than 3 results are returned.

| Constant | Value | Description |
|----------|-------|-------------|
| `MaxResults` | `3` | Number of files returned. |
| `CandidateChunkCount` | `30` | Number of closest chunks read before keeping one per file. |
