using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace SAMA.Web.HealthChecks;

public class QuartzSchedulerHealthCheck(ISchedulerFactory _schedulerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            if (scheduler.IsShutdown)
            {
                return HealthCheckResult.Unhealthy("Quartz scheduler is shut down");
            }

            if (scheduler.InStandbyMode)
            {
                return HealthCheckResult.Degraded("Quartz scheduler is in standby mode");
            }

            if (!scheduler.IsStarted)
            {
                return HealthCheckResult.Unhealthy("Quartz scheduler is not started");
            }

            var metadata = await scheduler.GetMetaData(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "schedulerName", metadata.SchedulerName },
                { "numberOfJobsExecuted", metadata.NumberOfJobsExecuted },
                { "runningSince", metadata.RunningSince?.ToString("o") ?? "not running" }
            };

            return HealthCheckResult.Healthy("Quartz scheduler is running", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check Quartz scheduler status", ex);
        }
    }
}
