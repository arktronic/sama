using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Services;

namespace SAMA.Web.Pages.Admin;

[Authorize(Roles = AuthConstants.AdminRole)]
public class LogsModel(InMemoryLogSink _logSink) : PageModel
{
    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? Exception { get; set; }

        public string? SourceContext { get; set; }
    }

    public List<LogEntryViewModel> LogEntries { get; set; } = [];

    public void OnGet()
    {
        var logEvents = _logSink.GetLogEvents()
            .OrderByDescending(e => e.Timestamp)
            .Take(500)
            .ToList();

        LogEntries = logEvents.Select(e => new LogEntryViewModel
        {
            Timestamp = e.Timestamp.UtcDateTime,
            Level = e.Level.ToString(),
            Message = e.RenderMessage(),
            Exception = e.Exception?.ToString(),
            SourceContext = e.Properties.TryGetValue("SourceContext", out var sourceContext)
                ? sourceContext.ToString().Trim('"')
                : null
        }).ToList();
    }
}
