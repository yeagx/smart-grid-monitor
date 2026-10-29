using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Common;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.GridMap;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Api.Controllers;

/// <summary>
/// Exposes grid infrastructure and geospatial telemetry endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GridController : ControllerBase
{
    private readonly IGridMapService _gridMapService;

    public GridController(IGridMapService gridMapService)
    {
        _gridMapService = gridMapService ?? throw new ArgumentNullException(nameof(gridMapService));
    }

    /// <summary>
    /// Retrieves geographic transformer map markers enriched with metadata,
    /// historical recorded peak metrics, and period-specific alert counts.
    /// </summary>
    /// <param name="from">Inclusive start date (format: YYYY-MM-DD).</param>
    /// <param name="to">Inclusive end date (format: YYYY-MM-DD).</param>
    /// <param name="district">Optional district filter (e.g. "Dokki", "Nasr City").</param>
    /// <param name="type">Optional threat type filter ('theft', 'overload', 'line_drop').</param>
    /// <param name="onlyWithAlerts">When true, returns only transformers with matching alerts in the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of matching transformer map markers.</response>
    /// <response code="400">If date parameters are missing, malformed, start &gt; end, range &gt; 366 days, or if an unsupported threat type is provided.</response>
    [HttpGet("map-markers")]
    [ProducesResponseType(typeof(IReadOnlyList<MapMarkerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMapMarkers(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? district,
        [FromQuery] string? type,
        [FromQuery] bool onlyWithAlerts = false,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/grid/map-markers";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        if (!string.IsNullOrWhiteSpace(type) && !EventTypes.IsValid(type))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Threat Type",
                Detail = $"Unsupported threat type '{type}'. Supported types are: {string.Join(", ", EventTypes.All)}.",
                Instance = path
            });
        }

        var criteria = new GridMapCriteria
        {
            Period = period,
            District = string.IsNullOrWhiteSpace(district) ? null : district.Trim(),
            EventType = string.IsNullOrWhiteSpace(type) ? null : type.Trim(),
            OnlyWithAlerts = onlyWithAlerts
        };

        var markers = await _gridMapService.GetMapMarkersAsync(criteria, cancellationToken);
        return Ok(markers);
    }
}
