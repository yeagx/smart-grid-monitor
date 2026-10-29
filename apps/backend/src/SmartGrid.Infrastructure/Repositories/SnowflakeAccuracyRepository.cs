using Snowflake.Data.Client;
using SmartGrid.Application.Abstractions.Persistence;
using SmartGrid.Application.Accuracy;
using SmartGrid.Application.Accuracy.Models;
using SmartGrid.Application.Accuracy.Queries;
using SmartGrid.Application.Common.Models;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Snowflake repository for detection accuracy.
/// <list type="bullet">
/// <item>Report: MARTS.ACCURACY_SUMMARY (TP / FP / FN per event type). Precision and recall are
/// recomputed from the counts in <see cref="AccuracyService.BuildReport"/>.</item>
/// <item>Details: MARTS.ACCURACY_METRICS (one row per truth window or detection).</item>
/// </list>
/// </summary>
public sealed class SnowflakeAccuracyRepository : IAccuracyReadRepository
{
    private readonly string _connectionString;

    public SnowflakeAccuracyRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Snowflake connection string must not be empty.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<AccuracyReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT EVENT_TYPE, TRUE_POSITIVES, FALSE_POSITIVES, FALSE_NEGATIVES
            FROM ACCURACY_SUMMARY
            """;

        var rows = new List<(string, int, int, int)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((
                SnowflakeQuery.ReadString(reader, 0).ToLowerInvariant(),
                SnowflakeQuery.ReadInt(reader, 1),
                SnowflakeQuery.ReadInt(reader, 2),
                SnowflakeQuery.ReadInt(reader, 3)));
        }

        return AccuracyService.BuildReport(rows);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AccuracyDetailDto>> GetDetailsAsync(
        AccuracyDetailCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var conn = new SnowflakeDbConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        long totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM ACCURACY_METRICS" + BuildWhere(new SnowflakeQuery(countCmd), criteria);
            var scalar = await countCmd.ExecuteScalarAsync(cancellationToken);
            totalCount = scalar is null or DBNull ? 0 : Convert.ToInt64(scalar);
        }

        if (totalCount == 0)
        {
            return PagedResult<AccuracyDetailDto>.Empty(criteria.Pagination.Page, criteria.Pagination.PageSize);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                TRANSFORMER_ID,
                EVENT_TYPE,
                DETECTION_STATUS,
                TRUTH_START_TS,
                TRUTH_END_TS,
                DETECTED_START_TS,
                DETECTED_END_TS,
                FLAG_ROWS
            FROM ACCURACY_METRICS
            """
            + BuildWhere(new SnowflakeQuery(cmd), criteria)
            + "\nORDER BY COALESCE(TRUTH_START_TS, DETECTED_START_TS) ASC, TRANSFORMER_ID ASC"
            + SnowflakeQuery.LimitOffset(criteria.Pagination.Page, criteria.Pagination.PageSize);

        var items = new List<AccuracyDetailDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AccuracyDetailDto
            {
                TransformerId = SnowflakeQuery.ReadString(reader, 0),
                EventType = SnowflakeQuery.ReadString(reader, 1).ToLowerInvariant(),
                DetectionStatus = SnowflakeQuery.ReadString(reader, 2),
                TruthStartTimestamp = SnowflakeQuery.ReadNullableUtc(reader, 3),
                TruthEndTimestamp = SnowflakeQuery.ReadNullableUtc(reader, 4),
                DetectedStartTimestamp = SnowflakeQuery.ReadNullableUtc(reader, 5),
                DetectedEndTimestamp = SnowflakeQuery.ReadNullableUtc(reader, 6),
                FlagRows = SnowflakeQuery.ReadNullableInt(reader, 7),
            });
        }

        return new PagedResult<AccuracyDetailDto>(items, criteria.Pagination.Page, criteria.Pagination.PageSize, totalCount);
    }

    private static string BuildWhere(SnowflakeQuery q, AccuracyDetailCriteria criteria)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            conditions.Add($"LOWER(EVENT_TYPE) = {q.BindString(criteria.EventType.Trim().ToLowerInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.DetectionStatus))
        {
            conditions.Add($"DETECTION_STATUS = {q.BindString(criteria.DetectionStatus.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransformerId))
        {
            conditions.Add($"TRANSFORMER_ID ILIKE {q.BindString("%" + SnowflakeAlertRepository.EscapeLike(criteria.TransformerId.Trim()) + "%")} ESCAPE '\\\\'");
        }

        return conditions.Count == 0 ? string.Empty : "\nWHERE " + string.Join("\n  AND ", conditions);
    }
}
