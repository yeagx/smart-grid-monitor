using Microsoft.AspNetCore.Mvc;
using SmartGrid.Infrastructure;

namespace SmartGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Liveness check. Also reports which data provider is active (Mock or Snowflake),
    /// so it is easy to confirm the app is reading the warehouse. No secrets are returned.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var provider = _configuration[DependencyInjection.ProviderConfigKey];

        return Ok(new
        {
            Status = "healthy",
            Timestamp = DateTimeOffset.UtcNow,
            Service = "SmartGrid.Api",
            DataProvider = string.IsNullOrWhiteSpace(provider) ? DependencyInjection.DefaultProvider : provider
        });
    }
}
