using SmartGrid.Application.Accuracy;
using SmartGrid.Application.Alerts;
using SmartGrid.Application.Dashboard;
using SmartGrid.Application.GridMap;
using SmartGrid.Application.Transformers;
using SmartGrid.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS for local frontend development origins
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IGridMapService, GridMapService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ITransformerService, TransformerService>();
builder.Services.AddScoped<IAccuracyService, AccuracyService>();

// Infrastructure services and data provider selection (Mock / Snowflake)
builder.Services.AddSmartGridInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
