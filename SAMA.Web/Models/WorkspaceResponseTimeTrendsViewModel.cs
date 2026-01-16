namespace SAMA.Web.Models;

public class WorkspaceResponseTimeTrendsViewModel
{
    public class CheckResponseTimeSeries
    {
        public Guid CheckId { get; set; }

        public string CheckName { get; set; } = string.Empty;

        public List<ResponseTimeDataPoint> DataPoints { get; set; } = [];

        public double? AverageResponseTimeMs { get; set; }
    }

    public class ResponseTimeDataPoint
    {
        public DateTimeOffset Timestamp { get; set; }

        public int? ResponseTimeMs { get; set; }
    }

    public List<CheckResponseTimeSeries> Series { get; set; } = [];

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int Hours { get; set; }
}
