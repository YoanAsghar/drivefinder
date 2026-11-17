# SearchController

## Purpose

The `SearchController` exposes the search endpoint. It takes a text query and returns the documents whose content is closest in meaning, with the section of text where the match was found. There is no AI-generated answer: results come straight from the database.

## Endpoints

### POST `/api/v1/Search`

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Query` | `string` | Yes | The text to search for. |

**Response:** `200 OK` — Returns a JSON array of up to 3 `SearchResult` objects, best match first. Each result is a different file.

| Field | Type | Description |
|-------|------|-------------|
| `fileName` | `string` | Name of the file. |
| `filePath` | `string` | Path of the file in Google Drive, for example `/Finance/Budget 2026.pdf`. |
| `webViewLink` | `string?` | Link to open the file in Google Drive. |
| `mimeType` | `string` | MIME type of the file. |
| `chunkIndex` | `int` | Zero-based index of the matching section inside the file. |
| `content` | `string` | The section of text that matched the query. |
| `score` | `double` | Similarity between the query and the section (cosine similarity, higher is closer). |

The array is empty when no documents are indexed.

**Error Responses:**

| Status Code | Condition |
|-------------|-----------|
| `400 Bad Request` | `Query` is empty or whitespace. |

## Dependencies

- **ISearchService** — Runs the search.
- **DriveFinder.Models.SearchResult** — Returned in the response.

## Request Model

`SearchRequest` is a DTO with a single `Query` field.
