using DriveFinder.Models;
using DriveFinder.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFinder.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost]
    public async Task<ActionResult<List<SearchResult>>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required." });
        }

        var results = await _searchService.SearchAsync(request.Query, cancellationToken);

        return Ok(results);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
}
