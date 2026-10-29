using SmartGrid.Application.Accuracy;

namespace SmartGrid.Application.Tests.Common;

public class AccuracyReportTests
{
    [Fact]
    public void BuildReport_ComputesRatiosAndOrdersByType()
    {
        var report = AccuracyService.BuildReport(
        [
            ("line_drop", 3, 1, 0),
            ("theft", 8, 2, 2),
            ("overload", 0, 0, 0),
        ]);

        Assert.Equal(["theft", "overload", "line_drop"], report.ByType.Select(t => t.EventType));
        Assert.Equal(0.8m, report.ByType[0].Precision);
        Assert.Equal(0.8m, report.ByType[0].Recall);
        Assert.Null(report.ByType[1].Precision);
        Assert.Equal(11, report.TotalTruePositives);
        Assert.Equal(0.7857m, report.OverallPrecision);
        Assert.Equal(0.8462m, report.OverallRecall);
    }
}
