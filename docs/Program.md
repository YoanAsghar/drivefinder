# Program

## Purpose

`Program.cs` is the entry point of the application. It registers all services, configures the HTTP pipeline, and starts the web server.

## Registered Services

| Service | Lifetime | Description |
|---------|----------|-------------|
| `DriveFinderDbContext` | Scoped | PostgreSQL database with pgvector support, using the `DefaultConnection` connection string. |
| `IEmbeddingGenerator<string, Embedding<float>>` | Singleton | Ollama client used to create embeddings. |
| `IEmbeddingsService` | Scoped | Splits text into chunks and creates embeddings. |
| `DriveService` | Singleton | Google Drive API client, authenticated as a service account with read-only access. |
| `IGoogleDriveService` | Scoped | Reads files and folders from Google Drive. |
| `IDriveSyncService` | Scoped | Syncs Google Drive with the database. |
| `ISearchService` | Scoped | Searches indexed documents by meaning. |

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:DefaultConnection` | none (required) | PostgreSQL connection string. |
| `Ollama:Endpoint` | `http://localhost:11434` | Ollama server URL. |
| `Ollama:Model` | `nomic-embed-text` | Ollama embedding model. |
| `GoogleDrive:CredentialsPath` | none (required) | Path to the service account JSON key file. Set to `google-credentials.json` in `appsettings.Development.json`. |
| `GoogleDrive:SharedDriveId` | empty | ID of the Google Workspace shared drive to index. When empty, every folder shared with the service account is indexed (for testing with a personal account). Read by `GoogleDriveService`. |

## HTTP Pipeline

- In Development: OpenAPI document, Swagger, and Swagger UI.
- HTTPS redirection, authorization, and controller routes.

## Important Notes

## Google Drive Authentication

The app connects to Google Drive as a **service account**. A service account is a Google account that belongs to the application, not to a person. It can only see the files and folders that are shared with its email address.

The app indexes one Google Workspace **shared drive** (`GoogleDrive:SharedDriveId`). The service account is added to that shared drive as a member. Domain-wide delegation is not used, so the service account cannot act as any user in the organization.

- The JSON key file is loaded from `GoogleDrive:CredentialsPath`. If the setting is missing, the app stops at startup.
- The scope is `DriveService.Scope.DriveReadonly`, so the app can read Drive but never change or delete anything in it.
- The key file must never be committed. `.gitignore` excludes `*credentials*.json`.

### One-time setup

1. In https://console.cloud.google.com, create a project.
2. In **APIs & Services → Library**, enable **Google Drive API**.
3. In **IAM & Admin → Service Accounts**, create a service account (no roles needed).
4. Open the service account, then **Keys → Add key → Create new key → JSON**, and download the file.
5. Save it as `google-credentials.json` in the project root.
6. In Google Drive, open the shared drive, choose **Manage members**, and add the service account email (`client_email` in the JSON file) as **Viewer**.
7. Copy the shared drive ID from the browser URL (`https://drive.google.com/drive/folders/<SharedDriveId>`) into `GoogleDrive:SharedDriveId`.

Testing with a personal Gmail account (no shared drives): skip steps 6 and 7. Instead, share a normal folder with the service account email as **Viewer** and leave `GoogleDrive:SharedDriveId` empty.

Notes:

- The service account email is outside the organization's domain. The shared drive must allow members from outside the organization, or a Workspace admin must allow it.
- Some organizations block creating service account JSON keys (policy `iam.disableServiceAccountKeyCreation`). If step 4 fails, IT must allow it.
