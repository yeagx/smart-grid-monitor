# Connecting the API to Snowflake

The API reads the Gold layer (`SMART_GRID_DB.MARTS`) with a read-only user. Nothing is written back.

## 1. Settings (kept out of git)

Run these in `apps/backend/src/SmartGrid.Api`. User secrets live on your machine, not in the repo.

```powershell
dotnet user-secrets set "DataSource:Provider" "Snowflake"
dotnet user-secrets set "ConnectionStrings:Snowflake" "account=<ACCOUNT_IDENTIFIER>;user=WEBAPP_USER;authenticator=snowflake_jwt;private_key_file=C:/path/to/rsa_key.p8;db=SMART_GRID_DB;schema=MARTS;warehouse=SMART_GRID_WH;role=WEBAPP_ROLE"
```

- The account identifier is shared privately by the warehouse owner.
- `rsa_key.p8` is your private key. Never commit it. Only the public key is registered in Snowflake.
- To go back to mock data: `dotnet user-secrets set "DataSource:Provider" "Mock"`.

## 2. Check the connection

```powershell
$env:RUN_SNOWFLAKE_INTEGRATION_TESTS = "true"
$env:SNOWFLAKE_CONNECTION_STRING = "<same connection string as above>"
dotnet test --filter SnowflakeLiveSmokeTests
```

Then `dotnet run --launch-profile http` and open `http://localhost:5091/api/dashboard/summary?from=2026-08-01&to=2026-08-02`.

## 3. Verified schema (answers to API_CONTRACT section 14)

| Item | Live value |
|---|---|
| `demand_date` | `DATE` |
| `capacity_kw`, `lat`, `lon` | `FLOAT` |
| `peak_load_percentage` | `FLOAT` (`ROUND` of a FLOAT stays FLOAT) |
| `kw`, `kwh`, `voltage` | `FLOAT` |
| `reading_ts`, `start_ts`, `end_ts` | `TIMESTAMP_NTZ` |
| `peak_network_kw` NULL? | No. A demand row only exists when readings exist. |
| `dim_transformers` rows | 500 (one per transformer) |
| `anomaly_id` format | `{transformer_id}-{OVL, LND or THF}-{YYYYMMDDHH24MISS}` |

All numbers are FLOAT, so the repositories read them with `Convert.ToDecimal(reader.GetValue(i))`
instead of `GetDecimal`, which works for FLOAT and NUMBER alike.

Bind variables use the numeric style (`:1`, `:2`, parameter names `"1"`, `"2"`), which is the
style the Snowflake .NET driver documents.

## 4. Good to know

- The data covers 2026-08-01 to 2026-08-02. The pages already default to that range.
- Check the provider with `GET http://localhost:5091/api/health` — it should show `"dataProvider": "Snowflake"`.
- The smoke test also runs the alert, transformer and accuracy queries, so one run checks every page's SQL.
- The warehouse sleeps after 60 s and wakes on the first query, so the first request can take a few seconds.
