using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Api.Common;

/// <summary>
/// Validates and parses HTTP query parameters 'from' and 'to' into a validated <see cref="DateRange"/>.
/// </summary>
public static class DateRangeQueryParser
{
    /// <summary>
    /// Parses and validates 'from' and 'to' date query parameters.
    /// Returns true when valid; otherwise returns false with a formatted <see cref="ProblemDetails"/>.
    /// </summary>
    public static bool TryParse(
        string? from,
        string? to,
        string? instancePath,
        [NotNullWhen(true)] out DateRange? period,
        [NotNullWhen(false)] out ProblemDetails? problem)
    {
        if (string.IsNullOrWhiteSpace(from))
        {
            period = null;
            problem = CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Query Parameters",
                "The 'from' query parameter is required.",
                instancePath);
            return false;
        }

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
        {
            period = null;
            problem = CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Query Parameters",
                $"The 'from' query parameter value '{from}' is not a valid date. Expected format: YYYY-MM-DD.",
                instancePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            period = null;
            problem = CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Query Parameters",
                "The 'to' query parameter is required.",
                instancePath);
            return false;
        }

        if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            period = null;
            problem = CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Query Parameters",
                $"The 'to' query parameter value '{to}' is not a valid date. Expected format: YYYY-MM-DD.",
                instancePath);
            return false;
        }

        if (!DateRange.TryCreate(fromDate, toDate, out var validatedRange, out var rangeError))
        {
            period = null;
            problem = CreateProblem(
                StatusCodes.Status400BadRequest,
                "Invalid Date Range",
                rangeError ?? "The specified date range is invalid.",
                instancePath);
            return false;
        }

        period = validatedRange;
        problem = null;
        return true;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail, string? instance)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = instance
        };
    }
}
