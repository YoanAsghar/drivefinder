# DriveFileChunkConfiguration

## Purpose

EF Core fluent API configuration for the `DriveFileChunk` entity. It maps the entity to the `drive_file_chunks` table, defines indexes, property constraints, and the foreign key relationship to `DriveFile`.

## Configuration Details

| Setting | Value | Description |
|---------|-------|-------------|
| Table | `drive_file_chunks` | PostgreSQL table name. |
| Primary Key | `Id` | Auto-generated via `gen_random_uuid()`. |
| Embedding Index | HNSW with `vector_cosine_ops` | Enables fast cosine similarity search on embeddings. |
| Composite Index | `(DriveFileId, Index)` | Fast lookup of chunks by file and position. |
| Foreign Key | `DriveFileId` → `drive_files.Id` | Cascade delete — deleting a file removes its chunks. |

## Property Constraints

| Property | Constraint |
|----------|------------|
| `FilePath` | Max length 1024, required |
| `FileName` | Max length 512, required |
| `MimeType` | Max length 255, required |
| `Content` | Required |
| `Index` | Required |
| `CreatedAt` | Required |
| `Embedding` | Column type `vector(768)` |

## Dependencies

- **Microsoft.EntityFrameworkCore**
- **DriveFinder.Models.DriveFileChunk**
