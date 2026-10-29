namespace SmartGrid.Application.Transformers.Queries;

/// <summary>
/// Allowed sort keys for the transformer list. Repositories map each key to a fixed
/// ORDER BY clause, so user input never reaches SQL directly.
/// </summary>
public static class TransformerSortOptions
{
    public const string Id = "id";
    public const string Alerts = "alerts";
    public const string Load = "load";

    public static readonly IReadOnlyList<string> All = [Id, Alerts, Load];

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value);
}
