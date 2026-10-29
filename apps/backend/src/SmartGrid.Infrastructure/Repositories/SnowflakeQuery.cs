using System.Data;
using System.Data.Common;
using System.Globalization;

namespace SmartGrid.Infrastructure.Repositories;

/// <summary>
/// Shared helpers for the Snowflake repositories.
/// <list type="bullet">
/// <item>Binds use numbered placeholders (<c>:1</c>, <c>:2</c> …), the form the Snowflake .NET
/// connector documents, so dynamic WHERE clauses stay parameterised.</item>
/// <item>MARTS numbers are FLOAT, so values are converted instead of read with GetDecimal.</item>
/// <item>MARTS timestamps are TIMESTAMP_NTZ in UTC, so they are tagged as UTC.</item>
/// </list>
/// </summary>
internal sealed class SnowflakeQuery
{
    /// <summary>
    /// ANOMALY_FLAGS has one row per flagged 15-minute reading, each with a rolling window
    /// (e.g. 00:00–03:45, 00:15–04:00, …), so one real problem appears as many rows.
    /// This derived table merges overlapping or touching windows of the same transformer and
    /// event type into ONE event — the unit the dashboard, map and alert log count.
    /// Columns: ANOMALY_ID (first row's ID), EVENT_TYPE, ENTITY_ID, TRANSFORMER_ID,
    /// START_TS (first start), END_TS (last end), FLAG_ROWS (rows merged).
    /// Use as <c>FROM {AnomalyEvents} a</c>.
    /// </summary>
    public const string AnomalyEvents = """
        (
            SELECT
                MIN_BY(ANOMALY_ID, START_TS) AS ANOMALY_ID,
                EVENT_TYPE,
                MIN_BY(ENTITY_ID, START_TS) AS ENTITY_ID,
                TRANSFORMER_ID,
                MIN(START_TS) AS START_TS,
                MAX(END_TS) AS END_TS,
                COUNT(*) AS FLAG_ROWS
            FROM (
                SELECT
                    s.*,
                    SUM(CASE WHEN s.PREV_END IS NULL OR s.START_TS > s.PREV_END THEN 1 ELSE 0 END)
                        OVER (PARTITION BY s.TRANSFORMER_ID, s.EVENT_TYPE ORDER BY s.START_TS, s.ANOMALY_ID
                              ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS EVENT_NO
                FROM (
                    SELECT
                        r.*,
                        LAG(r.RUN_END) OVER (PARTITION BY r.TRANSFORMER_ID, r.EVENT_TYPE ORDER BY r.START_TS, r.ANOMALY_ID) AS PREV_END
                    FROM (
                        SELECT
                            ANOMALY_ID, EVENT_TYPE, ENTITY_ID, TRANSFORMER_ID, START_TS, END_TS,
                            MAX(END_TS) OVER (PARTITION BY TRANSFORMER_ID, EVENT_TYPE ORDER BY START_TS, ANOMALY_ID
                                              ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RUN_END
                        FROM ANOMALY_FLAGS
                    ) r
                ) s
            ) g
            GROUP BY TRANSFORMER_ID, EVENT_TYPE, EVENT_NO
        )
        """;

    private readonly DbCommand _command;
    private int _nextIndex = 1;

    public SnowflakeQuery(DbCommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    /// <summary>Adds a bind value and returns its placeholder (e.g. <c>:3</c>) for the SQL text.</summary>
    public string Bind(object value, DbType dbType)
    {
        var name = _nextIndex.ToString(CultureInfo.InvariantCulture);
        _nextIndex++;

        var param = _command.CreateParameter();
        param.ParameterName = name;
        param.DbType = dbType;
        param.Value = value;
        _command.Parameters.Add(param);

        return ":" + name;
    }

    public string BindString(string value) => Bind(value, DbType.String);

    public string BindDateTime(DateTime value) => Bind(value, DbType.DateTime);

    public static decimal ReadDecimal(DbDataReader reader, int ordinal)
        => Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static decimal? ReadNullableDecimal(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : ReadDecimal(reader, ordinal);

    public static int ReadInt(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static int? ReadNullableInt(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static string ReadString(DbDataReader reader, int ordinal, string fallback = "")
        => reader.IsDBNull(ordinal) ? fallback : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? fallback;

    public static double ReadDouble(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? double.NaN : Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static DateTimeOffset ReadUtc(DbDataReader reader, int ordinal)
        => new(DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc));

    public static DateTimeOffset? ReadNullableUtc(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : ReadUtc(reader, ordinal);

    /// <summary>Inclusive start of the first day, bound the same way as the existing map query (TIMESTAMP_NTZ).</summary>
    public static DateTime StartOf(DateOnly day) => day.ToDateTime(TimeOnly.MinValue);

    /// <summary>Exclusive end: midnight after the last day.</summary>
    public static DateTime EndExclusiveOf(DateOnly day) => day.AddDays(1).ToDateTime(TimeOnly.MinValue);

    /// <summary>LIMIT/OFFSET text. Both come from a validated PageRequest (ints), so inlining is safe.</summary>
    public static string LimitOffset(int page, int pageSize)
        => string.Create(CultureInfo.InvariantCulture, $" LIMIT {pageSize} OFFSET {(page - 1) * pageSize}");
}
