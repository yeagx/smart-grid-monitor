using System.Data.Common;
using System.Text;
using Snowflake.Data.Client;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Alerts.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Snowflake repository for the alert log.
/// Reads MARTS.ANOMALY_FLAGS merged into events (see <see cref="SnowflakeQuery.AnomalyEvents"/>)
/// and joins DIM_TRANSFORMERS for district and governorate.
/// An alert falls in a period when its window overlaps it (START_TS before the period ends,
/// END_TS after it starts) — the same rule the map uses.
/// </summary>
public sealed class SnowflakeAlertRepository : IAlertReadRepository
{
    private const string SelectColumns = $$"""
        SELECT
            a.ANOMALY_ID,
            a.EVENT_TYPE,
            a.ENTITY_ID,
            a.TRANSFORMER_ID,
            t.DISTRICT,
            t.GOVERNORATE,
            a.START_TS,
            a.END_TS
        FROM {{SnowflakeQuery.AnomalyEvents}} a
        LEFT JOIN DIM_TRANSFORMERS t ON a.TRANSFORMER_ID = t.TRANSFORMER_ID
        """;

    private readonly string _connectionString;

    public SnowflakeAlertRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Snowflake connection string must not be empty.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<PagedResult<AlertDto>> SearchAsync(
        AlertSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        // 1) Total count with the same filters, so the UI can show page numbers.
        long totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            var where = BuildWhere(new SnowflakeQuery(countCmd), criteria);
            countCmd.CommandText = $"""
                SELECT COUNT(*)
                FROM {SnowflakeQuery.AnomalyEvents} a
                LEFT JOIN DIM_TRANSFORMERS t ON a.TRANSFORMER_ID = t.TRANSFORMER_ID
                {where}
                """;
            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalCount = scalar is null or DBNull ? 0 : Convert.ToInt64(scalar);
        }

        if (totalCount == 0)
        {
            return PagedResult<AlertDto>.Empty(criteria.Pagination.Page, criteria.Pagination.PageSize);
        }

        // 2) The requested page, newest first.
        await using var cmd = conn.CreateCommand();
        var whereClause = BuildWhere(new SnowflakeQuery(cmd), criteria);
        cmd.CommandText = SelectColumns
            + "\n" + whereClause
            + "\nORDER BY a.START_TS DESC, a.ANOMALY_ID DESC"
            + SnowflakeQuery.LimitOffset(criteria.Pagination.Page, criteria.Pagination.PageSize);

        var items = new List<AlertDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(Map(reader));
        }

        return new PagedResult<AlertDto>(items, criteria.Pagination.Page, criteria.Pagination.PageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<AlertDto?> GetByIdAsync(string anomalyId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anomalyId);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        var q = new SnowflakeQuery(cmd);
        cmd.CommandText = SelectColumns + $"\nWHERE UPPER(a.ANOMALY_ID) = UPPER({q.BindString(anomalyId.Trim())})\nLIMIT 1";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertSummaryByTypeDto>> GetSummaryByTypeAsync(
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        var q = new SnowflakeQuery(cmd);
        var start = q.BindDateTime(SnowflakeQuery.StartOf(period.Start));
        var end = q.BindDateTime(SnowflakeQuery.EndExclusiveOf(period.End));
        cmd.CommandText = $"""
            SELECT
                EVENT_TYPE,
                COUNT(*) AS TOTAL_ALERTS,
                COUNT(DISTINCT TRANSFORMER_ID) AS AFFECTED_TRANSFORMERS
            FROM {SnowflakeQuery.AnomalyEvents} a
            WHERE START_TS < {end} AND END_TS >= {start}
            GROUP BY EVENT_TYPE
            """;

        var counts = new Dictionary<string, AlertSummaryByTypeDto>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = SnowflakeQuery.ReadString(reader, 0);
            counts[type] = new AlertSummaryByTypeDto(type, SnowflakeQuery.ReadInt(reader, 1), SnowflakeQuery.ReadInt(reader, 2));
        }

        // Always return all three types (zeros included) so the UI layout never shifts.
        return EventTypes.All
            .Select(type => counts.TryGetValue(type, out var dto) ? dto with { EventType = type } : new AlertSummaryByTypeDto(type, 0, 0))
            .ToList();
    }

    private static string BuildWhere(SnowflakeQuery q, AlertSearchCriteria criteria)
    {
        var conditions = new List<string>();

        if (criteria.Period is not null)
        {
            var start = q.BindDateTime(SnowflakeQuery.StartOf(criteria.Period.Start));
            var end = q.BindDateTime(SnowflakeQuery.EndExclusiveOf(criteria.Period.End));
            conditions.Add($"a.START_TS < {end} AND a.END_TS >= {start}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            conditions.Add($"a.EVENT_TYPE = {q.BindString(criteria.EventType.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.District))
        {
            conditions.Add($"UPPER(t.DISTRICT) = UPPER({q.BindString(criteria.District.Trim())})");
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransformerId))
        {
            // Partial match so typing "TR-00" narrows the list; LIKE wildcards in input are escaped.
            conditions.Add($"a.TRANSFORMER_ID ILIKE {q.BindString("%" + EscapeLike(criteria.TransformerId.Trim()) + "%")} ESCAPE '\\\\'");
        }

        if (conditions.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder("WHERE ");
        sb.AppendJoin("\n  AND ", conditions);
        return sb.ToString();
    }

    internal static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static AlertDto Map(DbDataReader reader)
    {
        var transformerId = SnowflakeQuery.ReadString(reader, 3);
        return new AlertDto
        {
            AnomalyId = SnowflakeQuery.ReadString(reader, 0),
            EventType = SnowflakeQuery.ReadString(reader, 1),
            EntityId = SnowflakeQuery.ReadString(reader, 2, transformerId),
            TransformerId = transformerId,
            District = SnowflakeQuery.ReadString(reader, 4, "Unknown"),
            Governorate = SnowflakeQuery.ReadString(reader, 5, "Unknown"),
            StartTimestamp = SnowflakeQuery.ReadUtc(reader, 6),
            EndTimestamp = SnowflakeQuery.ReadUtc(reader, 7),
        };
    }
}
