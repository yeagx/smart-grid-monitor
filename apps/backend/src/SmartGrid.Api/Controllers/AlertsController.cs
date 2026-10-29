using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Common;
using SmartGrid.Application.Alerts;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Api.Controllers;

/// <summary>
/// Alert log: detected anomalies from MARTS.ANOMALY_FLAGS.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    }

    /// <summary>
    /// GET /api/alerts?from=&amp;to=&amp;type=&amp;district=&amp;transformerId=&amp;page=&amp;pageSize=
    /// Newest first. from/to are optional (both or neither).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? type,
        [FromQuery] string? district,
        [FromQuery] string? transformerId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/alerts";

        if (!QueryValidation.TryParseOptionalRange(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        if (!string.IsNullOrWhiteSpace(type) && !EventTypes.IsValid(type.Trim()))
        {
            return BadRequest(InvalidType(type, path));
        }

        if (!QueryValidation.TryCreatePage(page, pageSize, path, out var pageRequest, out problem))
        {
            return BadRequest(problem);
        }

        var criteria = new AlertSearchCriteria
        {
            Period = period,
            EventType = QueryValidation.Clean(type),
            District = QueryValidation.Clean(district),
            TransformerId = QueryValidation.Clean(transformerId),
            Pagination = pageRequest
        };

        return Ok(await _alertService.SearchAsync(criteria, cancellationToken));
    }

    /// <summary>GET /api/alerts/summary?from=&amp;to= — alert count and affected transformers per type.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertSummaryByTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/alerts/summary";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        return Ok(await _alertService.GetSummaryByTypeAsync(period, cancellationToken));
    }

    /// <summary>GET /api/alerts/{anomalyId}</summary>
    [HttpGet("{anomalyId}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string anomalyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(anomalyId))
        {
            return NotFound();
        }

        var alert = await _alertService.GetByIdAsync(anomalyId.Trim(), cancellationToken);
        return alert is null ? NotFound() : Ok(alert);
    }

    private static ProblemDetails InvalidType(string type, string path) => QueryValidation.Problem(
        "Invalid Threat Type",
        $"Unsupported threat type '{type}'. Supported types are: {string.Join(", ", EventTypes.All)}.",
        path);
}
