using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace SAMA.Web.Services;

public class InMemoryLogSink(int _maxLogEvents = 1000) : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _logEvents = new();

    public void Emit(LogEvent logEvent)
    {
        _logEvents.Enqueue(logEvent);

        while (_logEvents.Count > _maxLogEvents)
        {
            _logEvents.TryDequeue(out _);
        }
    }

    public IEnumerable<LogEvent> GetLogEvents()
    {
        return _logEvents.ToArray();
    }
}
