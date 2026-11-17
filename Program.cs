using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using DriveFinder.Data;
using DriveFinder.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using OllamaSharp;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<DriveFinderDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector()));

// Register Ollama embedding generator
var ollamaEndpoint = builder.Configuration.GetValue<string>("Ollama:Endpoint")
    ?? "http://localhost:11434";
var ollamaModel = builder.Configuration.GetValue<string>("Ollama:Model")
    ?? "nomic-embed-text";

var ollamaClient = new OllamaApiClient(new Uri(ollamaEndpoint), ollamaModel);
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(ollamaClient);

// Register Embeddings Service
builder.Services.AddScoped<IEmbeddingsService, EmbeddingsService>();

// Register Google Drive client, authenticated as a service account.
// The service account only sees the folders shared with its email.
var googleCredentialsPath = builder.Configuration.GetValue<string>("GoogleDrive:CredentialsPath")
    ?? throw new InvalidOperationException("GoogleDrive:CredentialsPath is not configured.");

var googleCredential = CredentialFactory
    .FromFile<ServiceAccountCredential>(googleCredentialsPath)
    .ToGoogleCredential()
    .CreateScoped(DriveService.Scope.DriveReadonly);

var driveService = new DriveService(new BaseClientService.Initializer
{
    HttpClientInitializer = googleCredential,
    ApplicationName = "DriveFinder"
});
builder.Services.AddSingleton(driveService);

// Register Drive services
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IDriveSyncService, DriveSyncService>();

// Register Search Service
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DriveFinder API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
