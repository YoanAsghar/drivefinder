using DriveFinder.Models;
using DriveFinder.Services;
using Google;
using Microsoft.AspNetCore.Mvc;

namespace DriveFinder.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DriveController : ControllerBase
{
    private readonly IDriveSyncService _driveSyncService;

    public DriveController(IDriveSyncService driveSyncService)
    {
        _driveSyncService = driveSyncService;
    }

    [HttpPost("full-sync")]
    public async Task<ActionResult<FullSyncResult>> RunFullSync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _driveSyncService.RunFullSyncAsync(cancellationToken);
            return Ok(result);
        }
        catch (GoogleApiException exception)
        {
            // Google Drive rejected the request (for example missing authentication)
            return StatusCode(StatusCodes.Status502BadGateway, new { error = exception.Message });
        }
        catch (HttpRequestException)
        {
            // Google Drive could not be reached (network or SSL problem)
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Could not reach Google Drive." });
        }
    }
}
