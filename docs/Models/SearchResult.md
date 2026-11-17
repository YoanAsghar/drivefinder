# SearchResult

## Purpose

`SearchResult` is one search match: a file and the section of its text that best matched the query. It is returned by `ISearchService.SearchAsync` and by `POST /api/v1/Search`. It is not stored in the database.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `FileName` | `string` | Name of the file. |
| `FilePath` | `string` | Path of the file in Google Drive. |
| `WebViewLink` | `string?` | Link to open the file in Google Drive. |
| `MimeType` | `string` | MIME type of the file. |
| `ChunkIndex` | `int` | Zero-based index of the matching chunk inside the file. |
| `Content` | `string` | Text of the matching chunk. |
| `Score` | `double` | Cosine similarity between the query and the chunk (`1 - cosine distance`). Higher is closer. |
