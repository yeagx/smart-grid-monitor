namespace SmartGrid.Application.Accuracy.Models;

/// <summary>
/// DETECTION_STATUS values written by the dbt accuracy model.
/// </summary>
public static class DetectionStatuses
{
    public const string TruePositive = "True Positive";
    public const string FalsePositive = "False Positive";
    public const string FalseNegative = "False Negative";

    public static readonly IReadOnlyList<string> All = [TruePositive, FalsePositive, FalseNegative];

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value);
}
