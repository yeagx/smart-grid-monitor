using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Api.Common;

/// <summary>
/// Small helpers that turn bad query values into 400 ProblemDetails
/// (same shape as <see cref="DateRangeQueryParser"/>), instead of letting exceptions become 500s.
/// </summary>
public static class QueryValidation
{
    public const int DefaultPageSize = 25;

    public static bool TryCreatePage(
        int? page,
        int? pageSize,
        string? instancePath,
        [NotNullWhen(true)] out PageRequest? pageRequest,
        [NotNullWhen(false)] out ProblemDetails? problem)
    {
        var p = page ?? 1;
        var size = pageSize ?? DefaultPageSize;

        if (p < 1 || size < 1 || size > PageRequest.MaxPageSize)
        {
            pageRequest = null;
            problem = Problem(
                "Invalid Pagination",
                $"'page' must be at least 1 and 'pageSize' must be between 1 and {PageRequest.MaxPageSize}.",
                instancePath);
            return false;
        }

        pageRequest = new PageRequest(p, size);
        problem = null;
        return true;
    }

    /// <summary>
    /// For endpoints where the date range is optional: both empty means "no date filter";
    /// otherwise both must be valid.
    /// </summary>
    public static bool TryParseOptionalRange(
        string? from,
        string? to,
        string? instancePath,
        out DateRange? period,
        [NotNullWhen(false)] out ProblemDetails? problem)
    {
        if (string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(to))
        {
            period = null;
            problem = null;
            return true;
        }

        if (DateRangeQueryParser.TryParse(from, to, instancePath, out var parsed, out problem))
        {
            period = parsed;
            return true;
        }

        period = null;
        return false;
    }

    public static ProblemDetails Problem(string title, string detail, string? instancePath) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = title,
        Detail = detail,
        Instance = instancePath
    };

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
