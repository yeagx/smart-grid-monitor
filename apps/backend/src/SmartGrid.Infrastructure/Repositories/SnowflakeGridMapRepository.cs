using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Snowflake.Data.Client;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.GridMap.Models;
using SmartGrid.Application.GridMap.Queries;
using SmartGrid.Infrastructure.Data;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Snowflake repository implementation for querying grid transformer markers,
/// geographic metadata, historical peak loading, and period-specific alert counts.
/// </summary>
public sealed class SnowflakeGridMapRepository : IGridMapReadRepository
{
    private readonly string _connectionString;

    public SnowflakeGridMapRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Snowflake connection string must not be empty.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GridMapCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var sqlBuilder = new StringBuilder($$"""
            WITH filtered_alerts AS (
                SELECT
                    TRANSFORMER_ID,
                    EVENT_TYPE,
                    END_TS
                FROM {{SnowflakeQuery.AnomalyEvents}} e
                WHERE START_TS < :2 AND END_TS >= :1
            ),
            alert_agg AS (
                SELECT
                    TRANSFORMER_ID,
                    COUNT(CASE WHEN EVENT_TYPE = 'theft' THEN 1 END) AS THEFT_COUNT,
                    COUNT(CASE WHEN EVENT_TYPE = 'overload' THEN 1 END) AS OVERLOAD_COUNT,
                    COUNT(CASE WHEN EVENT_TYPE = 'line_drop' THEN 1 END) AS LINEDROP_COUNT,
                    COUNT(*) AS TOTAL_ALERT_COUNT,
                    MAX(END_TS) AS LATEST_ALERT_AT
                FROM filtered_alerts
                GROUP BY TRANSFORMER_ID
            )
            SELECT
                t.TRANSFORMER_ID,
                t.DISTRICT,
                t.GOVERNORATE,
                t.FEEDER_ID,
                t.SUBSTATION_ID,
                t.LAT,
                t.LON,
                t.CAPACITY_KW,
                p.PEAK_KW_RECORDED,
                p.PEAK_LOAD_PERCENTAGE,
                COALESCE(a.THEFT_COUNT, 0) AS THEFT_ALERT_COUNT,
                COALESCE(a.OVERLOAD_COUNT, 0) AS OVERLOAD_ALERT_COUNT,
                COALESCE(a.LINEDROP_COUNT, 0) AS LINEDROP_ALERT_COUNT,
                COALESCE(a.TOTAL_ALERT_COUNT, 0) AS TOTAL_ALERT_COUNT,
                a.LATEST_ALERT_AT
            FROM DIM_TRANSFORMERS t
            LEFT JOIN alert_agg a ON t.TRANSFORMER_ID = a.TRANSFORMER_ID
            LEFT JOIN KPI_TRANSFORMER_PEAK p ON t.TRANSFORMER_ID = p.TRANSFORMER_ID
            WHERE (t.LAT IS NOT NULL AND t.LON IS NOT NULL AND NOT (t.LAT = 0 AND t.LON = 0))
            """);

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            sqlBuilder.Append(" AND UPPER(t.DISTRICT) = UPPER(:3)");
        }

        if (criteria.OnlyWithAlerts)
        {
            if (string.Equals(criteria.EventType, EventTypes.Theft, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND COALESCE(a.THEFT_COUNT, 0) > 0");
            }
            else if (string.Equals(criteria.EventType, EventTypes.Overload, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND COALESCE(a.OVERLOAD_COUNT, 0) > 0");
            }
            else if (string.Equals(criteria.EventType, EventTypes.LineDrop, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND COALESCE(a.LINEDROP_COUNT, 0) > 0");
            }
            else
            {
                sqlBuilder.Append(" AND COALESCE(a.TOTAL_ALERT_COUNT, 0) > 0");
            }
        }
        else if (criteria.EventType is not null)
        {
            if (string.Equals(criteria.EventType, EventTypes.Theft, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND (COALESCE(a.TOTAL_ALERT_COUNT, 0) = 0 OR a.THEFT_COUNT > 0)");
            }
            else if (string.Equals(criteria.EventType, EventTypes.Overload, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND (COALESCE(a.TOTAL_ALERT_COUNT, 0) = 0 OR a.OVERLOAD_COUNT > 0)");
            }
            else if (string.Equals(criteria.EventType, EventTypes.LineDrop, StringComparison.OrdinalIgnoreCase))
            {
                sqlBuilder.Append(" AND (COALESCE(a.TOTAL_ALERT_COUNT, 0) = 0 OR a.LINEDROP_COUNT > 0)");
            }
        }

        sqlBuilder.Append(" ORDER BY t.TRANSFORMER_ID ASC");

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sqlBuilder.ToString();

        // Parameterized date boundaries: inclusive start, exclusive end of day
        AddDateTimeParameter(cmd, "1", criteria.Period.Start.ToDateTime(TimeOnly.MinValue));
        AddDateTimeParameter(cmd, "2", criteria.Period.End.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            AddStringParameter(cmd, "3", criteria.District.Trim());
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var markers = new List<MapMarkerDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var transformerId = reader.GetString(0);
            var district = reader.GetString(1);
            var governorate = reader.GetString(2);
            var feederId = reader.GetString(3);
            var substationId = reader.GetString(4);
            var latitude = Convert.ToDouble(reader[5]);
            var longitude = Convert.ToDouble(reader[6]);

            // Validate coordinates
            if (!MockGridMapData.IsValidCoordinate(latitude, longitude))
            {
                continue;
            }

            var capacityKw = ReadDecimal(reader, 7);
            var peakKwRecorded = reader.IsDBNull(8) ? (decimal?)null : ReadDecimal(reader, 8);
            var peakLoadPercentage = reader.IsDBNull(9) ? (decimal?)null : ReadDecimal(reader, 9);
            var theftCount = reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader[10]);
            var overloadCount = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader[11]);
            var lineDropCount = reader.IsDBNull(12) ? 0 : Convert.ToInt32(reader[12]);
            var totalCount = reader.IsDBNull(13) ? 0 : Convert.ToInt32(reader[13]);

            DateTimeOffset? latestAlertAt = reader.IsDBNull(14)
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc));

            markers.Add(new MapMarkerDto
            {
                TransformerId = transformerId,
                District = district,
                Governorate = governorate,
                FeederId = feederId,
                SubstationId = substationId,
                Latitude = latitude,
                Longitude = longitude,
                CapacityKw = capacityKw,
                PeakKwRecorded = peakKwRecorded,
                PeakLoadPercentage = peakLoadPercentage,
                TheftAlertCount = theftCount,
                OverloadAlertCount = overloadCount,
                LineDropAlertCount = lineDropCount,
                TotalAlertCount = totalCount,
                LatestAlertAt = latestAlertAt
            });
        }

        return markers;
    }

    private static void AddDateTimeParameter(DbCommand cmd, string name, DateTime value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.DbType = DbType.DateTime;
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    private static void AddStringParameter(DbCommand cmd, string name, string value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.DbType = DbType.String;
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    // Our MARTS numbers are FLOAT (double) in Snowflake. Converting the value works for
    // FLOAT and NUMBER alike, while GetDecimal can fail on a FLOAT column.
    private static decimal ReadDecimal(DbDataReader reader, int ordinal)
        => Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
}
