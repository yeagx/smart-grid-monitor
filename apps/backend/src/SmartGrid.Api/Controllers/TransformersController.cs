using Microsoft.AspNetCore.Mvc;
using SmartGrid.Api.Common;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;

namespace SmartGrid.Api.Controllers;

/// <summary>
/// Transformer directory, details and load profile.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransformersController : ControllerBase
{
    private readonly ITransformerService _transformerService;

    public TransformersController(ITransformerService transformerService)
    {
        _transformerService = transformerService ?? throw new ArgumentNullException(nameof(transformerService));
    }

    /// <summary>
    /// GET /api/transformers?search=&amp;district=&amp;governorate=&amp;onlyWithAlerts=&amp;sort=id|alerts|load&amp;page=&amp;pageSize=
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransformerListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? district,
        [FromQuery] string? governorate,
        [FromQuery] string? feederId,
        [FromQuery] string? substationId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool onlyWithAlerts = false,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/transformers";

        var sortBy = QueryValidation.Clean(sort)?.ToLowerInvariant() ?? TransformerSortOptions.Id;
        if (!TransformerSortOptions.IsValid(sortBy))
        {
            return BadRequest(QueryValidation.Problem(
                "Invalid Sort",
                $"Unsupported sort '{sort}'. Supported values are: {string.Join(", ", TransformerSortOptions.All)}.",
                path));
        }

        if (!QueryValidation.TryCreatePage(page, pageSize, path, out var pageRequest, out var problem))
        {
            return BadRequest(problem);
        }

        var criteria = new TransformerSearchCriteria
        {
            Search = QueryValidation.Clean(search),
            District = QueryValidation.Clean(district),
            Governorate = QueryValidation.Clean(governorate),
            FeederId = QueryValidation.Clean(feederId),
            SubstationId = QueryValidation.Clean(substationId),
            OnlyWithAlerts = onlyWithAlerts,
            SortBy = sortBy,
            Pagination = pageRequest
        };

        return Ok(await _transformerService.SearchAsync(criteria, cancellationToken));
    }

    /// <summary>GET /api/transformers/peaks?district=&amp;page=&amp;pageSize= — most loaded first.</summary>
    [HttpGet("peaks")]
    [ProducesResponseType(typeof(PagedResult<TransformerPeakDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPeaks(
        [FromQuery] string? district,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? "/api/transformers/peaks";

        if (!QueryValidation.TryCreatePage(page, pageSize, path, out var pageRequest, out var problem))
        {
            return BadRequest(problem);
        }

        var criteria = new TransformerPeakCriteria
        {
            District = QueryValidation.Clean(district),
            Pagination = pageRequest
        };

        return Ok(await _transformerService.GetPeaksAsync(criteria, cancellationToken));
    }

    /// <summary>GET /api/transformers/{transformerId}</summary>
    [HttpGet("{transformerId}")]
    [ProducesResponseType(typeof(TransformerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string transformerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transformerId))
        {
            return NotFound();
        }

        var details = await _transformerService.GetByIdAsync(transformerId.Trim(), cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    /// <summary>
    /// GET /api/transformers/{transformerId}/load-profile?from=&amp;to=
    /// Hourly kW / kWh / voltage for charting (max 31 days).
    /// </summary>
    [HttpGet("{transformerId}/load-profile")]
    [ProducesResponseType(typeof(IReadOnlyList<TransformerLoadPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLoadProfile(
        string transformerId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? $"/api/transformers/{transformerId}/load-profile";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        if (period.DayCount > TransformerService.MaxLoadProfileDays)
        {
            return BadRequest(QueryValidation.Problem(
                "Invalid Date Range",
                $"The load profile supports at most {TransformerService.MaxLoadProfileDays} days; {period.DayCount} were requested.",
                path));
        }

        return Ok(await _transformerService.GetLoadProfileAsync(transformerId, period, cancellationToken));
    }

    /// <summary>
    /// GET /api/transformers/{transformerId}/readings?from=&amp;to=&amp;page=&amp;pageSize=
    /// Raw 15-minute readings, oldest first.
    /// </summary>
    [HttpGet("{transformerId}/readings")]
    [ProducesResponseType(typeof(PagedResult<TransformerReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReadings(
        string transformerId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = HttpContext?.Request?.Path.Value ?? $"/api/transformers/{transformerId}/readings";

        if (!DateRangeQueryParser.TryParse(from, to, path, out var period, out var problem))
        {
            return BadRequest(problem);
        }

        if (!QueryValidation.TryCreatePage(page, pageSize, path, out var pageRequest, out problem))
        {
            return BadRequest(problem);
        }

        var criteria = new TransformerReadingCriteria
        {
            TransformerId = transformerId.Trim(),
            Period = period,
            Pagination = pageRequest
        };

        return Ok(await _transformerService.GetReadingsAsync(criteria, cancellationToken));
    }
}
