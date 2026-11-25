using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GZCTF.Services.HealthCheck;

public class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var time = DateTime.UtcNow;
            var appliedMigrations =
                (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet();
            var requiredMigrations = dbContext.Database.GetMigrations().ToHashSet();

            // The database may carry migrations from a newer build (additive history is
            // backward compatible), so only missing required migrations are unhealthy.
            return DateTime.UtcNow - time > TimeSpan.FromSeconds(1)
                ? HealthCheckResult.Degraded()
                : requiredMigrations.Except(appliedMigrations).Any()
                    ? HealthCheckResult.Unhealthy()
                    : appliedMigrations.Count > requiredMigrations.Count
                        ? HealthCheckResult.Degraded("database contains migrations from a newer build")
                        : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
