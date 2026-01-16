using Microsoft.EntityFrameworkCore;
using Quartz;

namespace SAMA.Web.Services;

public class CheckSchedulerService(ISchedulerFactory _schedulerFactory, ILogger<CheckSchedulerService> _logger)
{
    public virtual async Task ScheduleCheckAsync(Guid checkId, int intervalSeconds, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var jobKey = new JobKey($"check-{checkId:N}", "checks");
        var triggerKey = new TriggerKey($"check-{checkId:N}-trigger", "checks");

        await scheduler.DeleteJob(jobKey, cancellationToken);

        var job = JobBuilder.Create<CheckExecutorJob>()
            .WithIdentity(jobKey)
            .UsingJobData("CheckId", checkId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(5))
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(intervalSeconds)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation("Scheduled check {CheckId} with interval {IntervalSeconds}s", checkId, intervalSeconds);
    }

    public virtual async Task UnscheduleCheckAsync(Guid checkId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"check-{checkId:N}", "checks");

        await scheduler.DeleteJob(jobKey, cancellationToken);

        _logger.LogInformation("Unscheduled check {CheckId}", checkId);
    }

    public virtual async Task TriggerImmediateCheckAsync(Guid checkId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"check-{checkId:N}", "checks");

        var jobExists = await scheduler.CheckExists(jobKey, cancellationToken);

        if (!jobExists)
        {
            _logger.LogWarning("Cannot trigger immediate check for {CheckId} - job not scheduled", checkId);
            return;
        }

        await scheduler.TriggerJob(jobKey, cancellationToken);
        _logger.LogInformation("Triggered immediate execution for check {CheckId}", checkId);
    }
}
