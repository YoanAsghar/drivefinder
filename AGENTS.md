# AGENTS.md

## Project: DriveFinder

Backend service that connects to a Google Drive, indexes every document into a PostgreSQL + pgvector database using Ollama embeddings, and exposes a search API for an AI agent. Changes in Google Drive (create, update, delete) must be detected and reflected in the database so it stays up to date.

## Rules

The agent will only apply text afte its aproved by the user, the code should be as readable as possible

- Present a plan and wait for explicit approval before writing or modifying any code.
- Keep code simple, explicit, well-named, and easy to read. Avoid clever abstractions.
- Never commit, push, or create pull requests unless explicitly asked.
- Never log or expose secrets, credentials, or tokens.

## Architecture

- **Framework**: ASP.NET Core (.NET 10), Web API with controllers.
- **Database**: PostgreSQL with the pgvector extension.
- **Embeddings**: Ollama (via OllamaSharp), called through an embeddings service.
- **Google Drive**: Google.Apis.Drive.v3 + Google.Apis.Auth, full sync on startup and incremental sync via Drive changes API.
- **API docs**: OpenAPI/Swagger UI.
- **Sync log**: every Drive change (created, updated, deleted) must be registered in a database table.
