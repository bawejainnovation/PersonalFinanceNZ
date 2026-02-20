using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController(ISyncService syncService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SyncResponse>> SyncAsync(
        [FromBody] SyncRequest request,
        CancellationToken cancellationToken)
    {
        var response = await syncService.SyncAsync(request, cancellationToken);
        return Ok(response);
    }
}
