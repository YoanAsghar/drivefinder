using DriveFinder.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using Pgvector;

namespace DriveFinder.Services;

public interface IEmbeddingsService
{
    Task<List<DriveFileChunk>> CreateFileChunksAsync(DriveFile file, string filePath, CancellationToken cancellationToken = default);

    Task<Vector> CreateQueryEmbeddingAsync(string query, CancellationToken cancellationToken = default);
}

public class EmbeddingsService : IEmbeddingsService
{
    // nomic-embed-text expects these prefixes to tell stored text apart from search queries
    private const string DocumentPrefix = "search_document: ";
    private const string QueryPrefix = "search_query: ";

    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly int _maxChunkSize;

    public EmbeddingsService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IConfiguration configuration)
    {
        _embeddingGenerator = embeddingGenerator;
        _maxChunkSize = configuration.GetValue<int>("Embeddings:MaxChunkSize", 1000);
    }

    public async Task<List<DriveFileChunk>> CreateFileChunksAsync(DriveFile file, string filePath, CancellationToken cancellationToken = default)
    {
        var textChunks = SplitTextIntoChunks(file.ContentText, _maxChunkSize);

        var fileChunks = new List<DriveFileChunk>();

        for (int i = 0; i < textChunks.Count; i++)
        {
            var text = textChunks[i];

            // Generate embedding for the chunk via Ollama
            var embedding = await _embeddingGenerator.GenerateVectorAsync(DocumentPrefix + text, cancellationToken: cancellationToken);

            fileChunks.Add(new DriveFileChunk
            {
                DriveFileId = file.Id,
                Index = i,
                Content = text,
                Embedding = new Vector(embedding.ToArray()),
                FilePath = filePath,
                FileName = file.Name,
                MimeType = file.MimeType,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return fileChunks;
    }

    public async Task<Vector> CreateQueryEmbeddingAsync(string query, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateVectorAsync(QueryPrefix + query, cancellationToken: cancellationToken);

        return new Vector(embedding.ToArray());
    }

    private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        var trimmed = text.Trim();

        if (trimmed.Length <= maxChunkSize)
        {
            chunks.Add(trimmed);
            return chunks;
        }

        int start = 0;

        while (start < trimmed.Length)
        {
            int end = Math.Min(start + maxChunkSize, trimmed.Length);

            // Try to break at a sentence boundary within the window
            if (end < trimmed.Length)
            {
                // Search backward from the end of the window to its start for a sentence break
                int lastSentenceBreak = trimmed.LastIndexOfAny(
                    new[] { '.', '!', '?', '\n' },
                    end - 1,
                    end - start);

                if (lastSentenceBreak > start + (maxChunkSize / 4))
                {
                    end = lastSentenceBreak + 1;
                }
            }

            chunks.Add(trimmed.Substring(start, end - start).Trim());
            start = end;
        }

        return chunks;
    }
}
