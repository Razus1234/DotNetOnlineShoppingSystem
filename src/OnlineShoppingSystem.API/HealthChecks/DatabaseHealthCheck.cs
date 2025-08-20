using Microsoft.Extensions.Diagnostics.HealthChecks;
using OnlineShoppingSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OnlineShoppingSystem.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IApplicationDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity by executing a simple query
            var canConnect = await ((DbContext)_dbContext).Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                _logger.LogDebug("Database health check passed");
                return HealthCheckResult.Healthy("Database is accessible and responsive");
            }
            else
            {
                _logger.LogWarning("Database health check failed - cannot connect to database");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}");
        }
    }
}