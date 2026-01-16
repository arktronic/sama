namespace SAMA.Web.Models;

public class WorkspaceIncidentTimelineViewModel
{
    public class TimeIncrement
    {
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public int UpCount { get; set; }

        public int WarnCount { get; set; }

        public int DownCount { get; set; }

        public int TotalChecks { get; set; }

        public List<CheckStatusInfo> ChecksInWarn { get; set; } = [];

        public List<CheckStatusInfo> ChecksInDown { get; set; } = [];
    }

    public class CheckStatusInfo
    {
        public Guid CheckId { get; set; }

        public string CheckName { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    public List<TimeIncrement> Increments { get; set; } = [];

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int Hours { get; set; }

    public int IncrementMinutes { get; set; }
}
