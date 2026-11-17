# Services

This directory contains service classes that handle business logic for the DriveFinder application.

## Files

- **EmbeddingsService.cs** — Service that splits text into chunks and generates vector embeddings using Ollama.
- **GoogleDriveService.cs** — Service that lists files and folders in Google Drive and reads the text content of supported files.
- **DriveSyncService.cs** — Service that syncs Google Drive with the database: saves files and chunks, removes deleted files, and registers every change.
- **SearchService.cs** — Service that finds the documents closest in meaning to a text query, using pgvector cosine distance.
