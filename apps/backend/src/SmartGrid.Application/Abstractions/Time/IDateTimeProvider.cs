namespace SmartGrid.Application.Abstractions.Time;

/// <summary>
/// Abstraction over the system clock, enabling deterministic testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Returns the current UTC date and time.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Returns the current UTC date.</summary>
    DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
