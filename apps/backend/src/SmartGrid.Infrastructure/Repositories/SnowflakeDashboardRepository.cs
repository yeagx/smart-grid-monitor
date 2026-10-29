using System.Data;
using System.Data.Common;
using System.Globalization;
using Snowflake.Data.Client;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Dashboard.Models;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Snowflake repository implementation for dashboard-level analytical queries.
/// Queries KPI_DAILY_DEMAND, ANOMALY_FLAGS (merged into events), and FACT_TRANSFORMER_READINGS.
/// </summary>
public sealed class SnowflakeDashboardRepository : IDashboardReadRepository
{
    private readonly string _connectionString;

    public SnowflakeDashboardRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Snowflake connection string must not be empty.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<DashboardOverviewDto> GetOverviewAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        const string sql = $$"""
            WITH demand AS (
                SELECT
                    SUM(TOTAL_DAILY_KWH) AS TOTAL_ENERGY_KWH,
                    MAX(PEAK_NETWORK_KW) AS PEAK_NETWORK_KW,
                    COUNT(*) AS DEMAND_ROW_COUNT
                FROM KPI_DAILY_DEMAND
                WHERE DEMAND_DATE >= :1 AND DEMAND_DATE <= :2
            ),
            alerts AS (
                SELECT
                    COUNT(*) AS TOTAL_ALERTS,
                    COUNT(DISTINCT CASE WHEN EVENT_TYPE = 'theft' THEN TRANSFORMER_ID END) AS THEFT_TRANSFORMERS,
                    COUNT(DISTINCT CASE WHEN EVENT_TYPE = 'overload' THEN TRANSFORMER_ID END) AS OVERLOAD_TRANSFORMERS,
                    COUNT(DISTINCT CASE WHEN EVENT_TYPE = 'line_drop' THEN TRANSFORMER_ID END) AS LINEDROP_TRANSFORMERS,
                    COUNT(DISTINCT TRANSFORMER_ID) AS AFFECTED_TRANSFORMERS
                FROM {{SnowflakeQuery.AnomalyEvents}} e
                WHERE DATE(END_TS) >= :1 AND DATE(END_TS) <= :2
            ),
            freshness AS (
                SELECT
                    MAX(READING_TS) AS LATEST_READING_TS
                FROM FACT_TRANSFORMER_READINGS
                WHERE DATE(READING_TS) >= :1 AND DATE(READING_TS) <= :2
            )
            SELECT
                d.TOTAL_ENERGY_KWH,
                d.PEAK_NETWORK_KW,
                d.DEMAND_ROW_COUNT,
                a.TOTAL_ALERTS,
                a.THEFT_TRANSFORMERS,
                a.OVERLOAD_TRANSFORMERS,
                a.LINEDROP_TRANSFORMERS,
                a.AFFECTED_TRANSFORMERS,
                f.LATEST_READING_TS
            FROM demand d
            CROSS JOIN alerts a
            CROSS JOIN freshness f
            """;

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        AddDateParameter(cmd, "1", period.Start);
        AddDateParameter(cmd, "2", period.End);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var demandRowCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader[2]);
            var totalAlerts = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader[3]);
            DateTimeOffset? latestReading = reader.IsDBNull(8)
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc));

            // If no demand rows, no alerts, and no readings in range, return null aggregates
            if (demandRowCount == 0 && totalAlerts == 0 && latestReading == null)
            {
                return new DashboardOverviewDto
                {
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    TotalEnergyKwh = null,
                    PeakNetworkKw = null,
                    TotalAlerts = null,
                    TheftTransformers = null,
                    OverloadTransformers = null,
                    LinedropTransformers = null,
                    AffectedTransformers = null,
                    LatestReadingTimestamp = null
                };
            }

            var totalEnergy = reader.IsDBNull(0) ? (decimal?)null : ReadDecimal(reader, 0);
            var peakNetwork = reader.IsDBNull(1) ? (decimal?)null : ReadDecimal(reader, 1);
            var theftTransformers = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader[4]);
            var overloadTransformers = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader[5]);
            var linedropTransformers = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader[6]);
            var affectedTransformers = reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader[7]);

            return new DashboardOverviewDto
            {
                PeriodStart = period.Start,
                PeriodEnd = period.End,
                TotalEnergyKwh = totalEnergy,
                PeakNetworkKw = peakNetwork,
                TotalAlerts = totalAlerts,
                TheftTransformers = theftTransformers,
                OverloadTransformers = overloadTransformers,
                LinedropTransformers = linedropTransformers,
                AffectedTransformers = affectedTransformers,
                LatestReadingTimestamp = latestReading
            };
        }

        return new DashboardOverviewDto
        {
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            TotalEnergyKwh = null,
            PeakNetworkKw = null,
            TotalAlerts = null,
            TheftTransformers = null,
            OverloadTransformers = null,
            LinedropTransformers = null,
            AffectedTransformers = null,
            LatestReadingTimestamp = null
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyDemandPointDto>> GetDailyDemandAsync(DateRange period, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        const string sql = """
            SELECT
                DEMAND_DATE,
                TOTAL_DAILY_KWH,
                PEAK_NETWORK_KW
            FROM KPI_DAILY_DEMAND
            WHERE DEMAND_DATE >= :1 AND DEMAND_DATE <= :2
            ORDER BY DEMAND_DATE ASC
            """;

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        AddDateParameter(cmd, "1", period.Start);
        AddDateParameter(cmd, "2", period.End);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var points = new List<DailyDemandPointDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            DateOnly demandDate;
            if (reader.GetFieldType(0) == typeof(DateTime))
            {
                demandDate = DateOnly.FromDateTime(reader.GetDateTime(0));
            }
            else
            {
                demandDate = DateOnly.Parse(reader.GetString(0));
            }

            var totalKwh = ReadDecimal(reader, 1);
            var peakKw = ReadDecimal(reader, 2);

            points.Add(new DailyDemandPointDto(demandDate, totalKwh, peakKw));
        }

        return points;
    }

    private static void AddDateParameter(DbCommand cmd, string name, DateOnly date)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.DbType = DbType.Date;
        param.Value = date.ToDateTime(TimeOnly.MinValue);
        cmd.Parameters.Add(param);
    }

    // Our MARTS numbers are FLOAT (double) in Snowflake. Converting the value works for
    // FLOAT and NUMBER alike, while GetDecimal can fail on a FLOAT column.
    private static decimal ReadDecimal(DbDataReader reader, int ordinal)
        => Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
}
