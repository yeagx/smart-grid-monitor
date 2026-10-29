using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Common;
using SmartGrid.Application.Accuracy;
using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Api.Controllers;

/// <summary>
/// Detection accuracy: detected anomalies compared with the injected ground truth.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccuracyController : ControllerBase
{
    private readonly IAccuracyService _accuracyService;

    public AccuracyController(IAccuracyService accuracyService)
    {
        _accuracyService = accuracyService ?? throw new ArgumentNullException(nameof(accuracyService));
    }

    /// <summary>GET /api/accuracy — precision / recall per event type and overall.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AccuracyReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(CancellationToken cancellationToken = default)
        => Ok(await _accuracyService.GetReportAsync(cancellationToken));

    /// <summary>
    /// GET /api/accuracy/details?type=&amp;status=&amp;transformerId=&amp;page=&amp;pageSize=
    /// status is one of "True Positive", "False Positive", "False Negative".
    /// </summary>
    [HttpGet("details")]
    [ProducesResponseType(typeof(PagedResult<AccuracyDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDetails(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] string? transformerId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/accuracy/details";

        if (!string.IsNullOrWhiteSpace(type) && !EventTypes.IsValid(type.Trim()))
        {
            return BadRequest(QueryValidation.Problem(
                "Invalid Threat Type",
                $"Unsupported threat type '{type}'. Supported types are: {string.Join(", ", EventTypes.All)}.",
                path));
        }

        if (!string.IsNullOrWhiteSpace(status) && !DetectionStatuses.IsValid(status.Trim()))
        {
            return BadRequest(QueryValidation.Problem(
                "Invalid Detection Status",
                $"Unsupported status '{status}'. Supported values are: {string.Join(", ", DetectionStatuses.All)}.",
                path));
        }

        if (!QueryValidation.TryCreatePage(page, pageSize, path, out var pageRequest, out var problem))
        {
            return BadRequest(problem);
        }

        var criteria = new AccuracyDetailCriteria
        {
            EventType = QueryValidation.Clean(type),
            DetectionStatus = QueryValidation.Clean(status),
            TransformerId = QueryValidation.Clean(transformerId),
            Pagination = pageRequest
        };

        return Ok(await _accuracyService.GetDetailsAsync(criteria, cancellationToken));
    }
}
