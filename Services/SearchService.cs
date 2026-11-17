using DriveFinder.Data;
using DriveFinder.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace DriveFinder.Services;

public interface ISearchService
{
    Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public class SearchService : ISearchService
{
    private const int MaxResults = 3;

    // Fetch more chunks than results, because several of the closest chunks can belong to the same file
    private const int CandidateChunkCount = 30;

    private readonly IEmbeddingsService _embeddingsService;
    private readonly DriveFinderDbContext _dbContext;

    public SearchService(IEmbeddingsService embeddingsService, DriveFinderDbContext dbContext)
    {
        _embeddingsService = embeddingsService;
        _dbContext = dbContext;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingsService.CreateQueryEmbeddingAsync(query, cancellationToken);

        // Closest chunks first (smallest cosine distance)
        var closestChunks = await _dbContext.DriveFileChunks
            .Where(chunk => chunk.Embedding != null)
            .OrderBy(chunk => chunk.Embedding!.CosineDistance(queryEmbedding))
            .Take(CandidateChunkCount)
            .Select(chunk => new
            {
                chunk.DriveFileId,
                chunk.FileName,
                chunk.FilePath,
                chunk.MimeType,
                chunk.Index,
                chunk.Content,
                chunk.DriveFile!.WebViewLink,
                Distance = chunk.Embedding!.CosineDistance(queryEmbedding)
            })
            .ToListAsync(cancellationToken);

        // Keep only the best chunk of each file. The list is already sorted, so the first chunk of each file is its best one.
        var results = closestChunks
            .GroupBy(chunk => chunk.DriveFileId)
            .Select(fileChunks => fileChunks.First())
            .Take(MaxResults)
            .Select(chunk => new SearchResult
            {
                FileName = chunk.FileName,
                FilePath = chunk.FilePath,
                WebViewLink = chunk.WebViewLink,
                MimeType = chunk.MimeType,
                ChunkIndex = chunk.Index,
                Content = chunk.Content,
                Score = 1 - chunk.Distance
            })
            .ToList();

        return results;
    }
}
