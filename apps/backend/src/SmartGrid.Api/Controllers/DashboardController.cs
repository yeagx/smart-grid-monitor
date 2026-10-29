using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Common;
using SmartGrid.Application.Dashboard;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Api.Controllers;

/// <summary>
/// Exposes dashboard-level analytics endpoints for energy consumption, peak network load, and grid alerts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
    }

    /// <summary>
    /// Retrieves dashboard overview metrics for the specified inclusive date range.
    /// </summary>
    /// <param name="from">Inclusive start date (format: YYYY-MM-DD).</param>
    /// <param name="to">Inclusive end date (format: YYYY-MM-DD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the aggregated dashboard overview metrics for the requested period.</response>
    /// <response code="400">If the date parameters are missing, malformed, start &gt; end, or range &gt; 366 days.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/dashboard/summary";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        var result = await _dashboardService.GetOverviewAsync(period, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves daily demand points (total energy and peak power) for the specified inclusive date range.
    /// </summary>
    /// <param name="from">Inclusive start date (format: YYYY-MM-DD).</param>
    /// <param name="to">Inclusive end date (format: YYYY-MM-DD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns daily demand points ordered by date ascending.</response>
    /// <response code="400">If the date parameters are missing, malformed, start &gt; end, or range &gt; 366 days.</response>
    [HttpGet("demand")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyDemandPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDemand(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/dashboard/demand";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        var result = await _dashboardService.GetDailyDemandAsync(period, cancellationToken);
        return Ok(result);
    }
}
