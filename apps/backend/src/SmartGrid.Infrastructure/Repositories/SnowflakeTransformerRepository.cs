using Snowflake.Data.Client;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.Transformers.Models;
using SmartGrid.Application.Transformers.Queries;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Snowflake repository for transformer lookups.
/// <list type="bullet">
/// <item>List / details: DIM_TRANSFORMERS + KPI_TRANSFORMER_PEAK + alert (event) counts from ANOMALY_FLAGS.</item>
/// <item>Readings and hourly load profile: FACT_TRANSFORMER_READINGS.</item>
/// </list>
/// </summary>
public sealed class SnowflakeTransformerRepository : ITransformerReadRepository
{
    private const string AlertCountsCte = $$"""
        WITH alert_counts AS (
            SELECT TRANSFORMER_ID, COUNT(*) AS ALERT_COUNT
            FROM {{SnowflakeQuery.AnomalyEvents}} e
            GROUP BY TRANSFORMER_ID
        )
        """;

    private const string ListFrom = """
        FROM DIM_TRANSFORMERS t
        LEFT JOIN KPI_TRANSFORMER_PEAK p ON t.TRANSFORMER_ID = p.TRANSFORMER_ID
        LEFT JOIN alert_counts c ON t.TRANSFORMER_ID = c.TRANSFORMER_ID
        """;

    private const string ListColumns = """
        SELECT
            t.TRANSFORMER_ID,
            t.DISTRICT,
            t.GOVERNORATE,
            t.FEEDER_ID,
            t.SUBSTATION_ID,
            t.CAPACITY_KW,
            t.LAT,
            t.LON,
            p.PEAK_KW_RECORDED,
            p.PEAK_LOAD_PERCENTAGE,
            COALESCE(c.ALERT_COUNT, 0) AS ALERT_COUNT
        """;

    private readonly string _connectionString;

    public SnowflakeTransformerRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Snowflake connection string must not be empty.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransformerListItemDto>> SearchAsync(
        TransformerSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        long totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            var where = BuildWhere(new SnowflakeQuery(countCmd), criteria);
            countCmd.CommandText = AlertCountsCte + "\nSELECT COUNT(*)\n" + ListFrom + "\n" + where;
            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalCount = scalar is null or DBNull ? 0 : Convert.ToInt64(scalar);
        }

        if (totalCount == 0)
        {
            return PagedResult<TransformerListItemDto>.Empty(criteria.Pagination.Page, criteria.Pagination.PageSize);
        }

        await using var cmd = conn.CreateCommand();
        var whereClause = BuildWhere(new SnowflakeQuery(cmd), criteria);
        cmd.CommandText = AlertCountsCte + "\n" + ListColumns + "\n" + ListFrom
            + "\n" + whereClause
            + "\n" + OrderBy(criteria.SortBy)
            + SnowflakeQuery.LimitOffset(criteria.Pagination.Page, criteria.Pagination.PageSize);

        var items = new List<TransformerListItemDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TransformerListItemDto
            {
                TransformerId = SnowflakeQuery.ReadString(reader, 0),
                District = SnowflakeQuery.ReadString(reader, 1, "Unknown"),
                Governorate = SnowflakeQuery.ReadString(reader, 2, "Unknown"),
                FeederId = SnowflakeQuery.ReadString(reader, 3),
                SubstationId = SnowflakeQuery.ReadString(reader, 4),
                CapacityKw = SnowflakeQuery.ReadNullableDecimal(reader, 5) ?? 0m,
                Latitude = SnowflakeQuery.ReadDouble(reader, 6),
                Longitude = SnowflakeQuery.ReadDouble(reader, 7),
                PeakKwRecorded = SnowflakeQuery.ReadNullableDecimal(reader, 8),
                PeakLoadPercentage = SnowflakeQuery.ReadNullableDecimal(reader, 9),
                AlertCount = SnowflakeQuery.ReadInt(reader, 10),
            });
        }

        return new PagedResult<TransformerListItemDto>(items, criteria.Pagination.Page, criteria.Pagination.PageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<TransformerDetailsDto?> GetByIdAsync(string transformerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transformerId);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        var q = new SnowflakeQuery(cmd);
        cmd.CommandText = $"""
            SELECT TRANSFORMER_ID, DISTRICT, GOVERNORATE, FEEDER_ID, SUBSTATION_ID, CAPACITY_KW, LAT, LON
            FROM DIM_TRANSFORMERS
            WHERE UPPER(TRANSFORMER_ID) = UPPER({q.BindString(transformerId.Trim())})
            LIMIT 1
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TransformerDetailsDto
        {
            TransformerId = SnowflakeQuery.ReadString(reader, 0),
            District = SnowflakeQuery.ReadString(reader, 1, "Unknown"),
            Governorate = SnowflakeQuery.ReadString(reader, 2, "Unknown"),
            FeederId = SnowflakeQuery.ReadString(reader, 3),
            SubstationId = SnowflakeQuery.ReadString(reader, 4),
            CapacityKw = SnowflakeQuery.ReadNullableDecimal(reader, 5) ?? 0m,
            Latitude = SnowflakeQuery.ReadDouble(reader, 6),
            Longitude = SnowflakeQuery.ReadDouble(reader, 7),
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransformerPeakDto>> GetPeaksAsync(
        TransformerPeakCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        long totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM KPI_TRANSFORMER_PEAK p" + PeakWhere(new SnowflakeQuery(countCmd), criteria);
            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalCount = scalar is null or DBNull ? 0 : Convert.ToInt64(scalar);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT p.TRANSFORMER_ID, p.DISTRICT, p.CAPACITY_KW, p.PEAK_KW_RECORDED, p.PEAK_LOAD_PERCENTAGE
            FROM KPI_TRANSFORMER_PEAK p
            """
            + PeakWhere(new SnowflakeQuery(cmd), criteria)
            + "\nORDER BY p.PEAK_LOAD_PERCENTAGE DESC NULLS LAST, p.TRANSFORMER_ID ASC"
            + SnowflakeQuery.LimitOffset(criteria.Pagination.Page, criteria.Pagination.PageSize);

        var items = new List<TransformerPeakDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TransformerPeakDto(
                SnowflakeQuery.ReadString(reader, 0),
                SnowflakeQuery.ReadString(reader, 1, "Unknown"),
                SnowflakeQuery.ReadNullableDecimal(reader, 2) ?? 0m,
                SnowflakeQuery.ReadNullableDecimal(reader, 3) ?? 0m,
                SnowflakeQuery.ReadNullableDecimal(reader, 4) ?? 0m));
        }

        return new PagedResult<TransformerPeakDto>(items, criteria.Pagination.Page, criteria.Pagination.PageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransformerReadingDto>> GetReadingsAsync(
        TransformerReadingCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        long totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM FACT_TRANSFORMER_READINGS" + ReadingWhere(new SnowflakeQuery(countCmd), criteria.TransformerId, criteria.Period);
            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalCount = scalar is null or DBNull ? 0 : Convert.ToInt64(scalar);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TRANSFORMER_ID, READING_TS, KW, KWH, VOLTAGE FROM FACT_TRANSFORMER_READINGS"
            + ReadingWhere(new SnowflakeQuery(cmd), criteria.TransformerId, criteria.Period)
            + "\nORDER BY READING_TS ASC"
            + SnowflakeQuery.LimitOffset(criteria.Pagination.Page, criteria.Pagination.PageSize);

        var items = new List<TransformerReadingDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TransformerReadingDto(
                SnowflakeQuery.ReadString(reader, 0),
                SnowflakeQuery.ReadUtc(reader, 1),
                SnowflakeQuery.ReadNullableDecimal(reader, 2) ?? 0m,
                SnowflakeQuery.ReadNullableDecimal(reader, 3) ?? 0m,
                SnowflakeQuery.ReadNullableDecimal(reader, 4) ?? 0m));
        }

        return new PagedResult<TransformerReadingDto>(items, criteria.Pagination.Page, criteria.Pagination.PageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransformerLoadPointDto>> GetLoadProfileAsync(
        string transformerId,
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transformerId);
        ArgumentNullException.ThrowIfNull(period);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                DATE_TRUNC('HOUR', READING_TS) AS HOUR_TS,
                AVG(KW) AS AVG_KW,
                MAX(KW) AS MAX_KW,
                SUM(KWH) AS ENERGY_KWH,
                AVG(VOLTAGE) AS AVG_VOLTAGE,
                MIN(VOLTAGE) AS MIN_VOLTAGE,
                COUNT(*) AS READING_COUNT
            FROM FACT_TRANSFORMER_READINGS
            """
            + ReadingWhere(new SnowflakeQuery(cmd), transformerId, period)
            + "\nGROUP BY HOUR_TS\nORDER BY HOUR_TS ASC";

        var points = new List<TransformerLoadPointDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            points.Add(new TransformerLoadPointDto(
                SnowflakeQuery.ReadUtc(reader, 0),
                Math.Round(SnowflakeQuery.ReadNullableDecimal(reader, 1) ?? 0m, 2),
                Math.Round(SnowflakeQuery.ReadNullableDecimal(reader, 2) ?? 0m, 2),
                Math.Round(SnowflakeQuery.ReadNullableDecimal(reader, 3) ?? 0m, 2),
                Math.Round(SnowflakeQuery.ReadNullableDecimal(reader, 4) ?? 0m, 2),
                Math.Round(SnowflakeQuery.ReadNullableDecimal(reader, 5) ?? 0m, 2),
                SnowflakeQuery.ReadInt(reader, 6)));
        }

        return points;
    }

    private static string BuildWhere(SnowflakeQuery q, TransformerSearchCriteria criteria)
    {
        // Same coordinate rule as the map, so both pages list the same transformers.
        var conditions = new List<string> { "t.LAT IS NOT NULL AND t.LON IS NOT NULL AND NOT (t.LAT = 0 AND t.LON = 0)" };

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            conditions.Add($"UPPER(t.DISTRICT) = UPPER({q.BindString(criteria.District.Trim())})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.Governorate))
        {
            conditions.Add($"UPPER(t.GOVERNORATE) = UPPER({q.BindString(criteria.Governorate.Trim())})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.FeederId))
        {
            conditions.Add($"UPPER(t.FEEDER_ID) = UPPER({q.BindString(criteria.FeederId.Trim())})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.SubstationId))
        {
            conditions.Add($"UPPER(t.SUBSTATION_ID) = UPPER({q.BindString(criteria.SubstationId.Trim())})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            conditions.Add($"t.TRANSFORMER_ID ILIKE {q.BindString("%" + SnowflakeAlertRepository.EscapeLike(criteria.Search.Trim()) + "%")} ESCAPE '\\\\'");
        }

        if (criteria.OnlyWithAlerts)
        {
            conditions.Add("COALESCE(c.ALERT_COUNT, 0) > 0");
        }

        return "WHERE " + string.Join("\n  AND ", conditions);
    }

    // Fixed ORDER BY per whitelisted sort key — user input never reaches the SQL text.
    private static string OrderBy(string? sortBy) => sortBy switch
    {
        TransformerSortOptions.Alerts => "ORDER BY ALERT_COUNT DESC, t.TRANSFORMER_ID ASC",
        TransformerSortOptions.Load => "ORDER BY p.PEAK_LOAD_PERCENTAGE DESC NULLS LAST, t.TRANSFORMER_ID ASC",
        _ => "ORDER BY t.TRANSFORMER_ID ASC",
    };

    private static string PeakWhere(SnowflakeQuery q, TransformerPeakCriteria criteria)
        => string.IsNullOrWhiteSpace(criteria.District)
            ? string.Empty
            : $"\nWHERE UPPER(p.DISTRICT) = UPPER({q.BindString(criteria.District.Trim())})";

    // TRANSFORMER_ID is compared as-is (IDs are upper case) so Snowflake can prune micro-partitions.
    private static string ReadingWhere(SnowflakeQuery q, string transformerId, DateRange period)
    {
        var id = q.BindString(transformerId.Trim());
        var start = q.BindDateTime(SnowflakeQuery.StartOf(period.Start));
        var end = q.BindDateTime(SnowflakeQuery.EndExclusiveOf(period.End));
        return $"\nWHERE TRANSFORMER_ID = UPPER({id}) AND READING_TS >= {start} AND READING_TS < {end}";
    }
}
