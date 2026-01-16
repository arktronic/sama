using System.Net;
using System.Net.NetworkInformation;

namespace SAMA.Shared.Models;

/// <summary>
/// Represents the result of a ping operation.
/// Replacement for <see cref="PingReply"/> to enable testability.
/// </summary>
public class PingResult
{
    public IPStatus Status { get; init; }

    public IPAddress? Address { get; init; }

    public long RoundtripTime { get; init; }

    public PingOptions? Options { get; init; }

    public byte[]? Buffer { get; init; }

    public static PingResult FromPingReply(PingReply reply)
    {
        return new PingResult
        {
            Status = reply.Status,
            Address = reply.Address,
            RoundtripTime = reply.RoundtripTime,
            Options = reply.Options,
            Buffer = reply.Buffer
        };
    }
}
