namespace SAMA.Web.Models;

public class CheckUptimeViewModel
{
    public double UptimePercentage { get; set; }

    public int TotalChecks { get; set; }

    public int UpCount { get; set; }

    public int WarnCount { get; set; }

    public int DownCount { get; set; }

    public int AvailableCount => UpCount + WarnCount;
}
