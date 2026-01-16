using System.Net.NetworkInformation;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SAMA.Shared.Checks;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Shared.Wrappers;

namespace SAMA.Tests.Unit.Shared.Checks;

[TestClass]
public class PingCheckExecutorTests
{
    private PingWrapper _mockWrapper = null!;
    private PingFactory _mockFactory = null!;
    private PingCheckExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockWrapper = Substitute.For<PingWrapper>();
        _mockFactory = Substitute.For<PingFactory>();
        _mockFactory.CreatePing().Returns(_mockWrapper);
        _executor = new PingCheckExecutor(_mockFactory);
    }

    private static PingResult CreateSuccessResult(long roundtripTime = 50)
    {
        return new PingResult
        {
            Status = IPStatus.Success,
            RoundtripTime = roundtripTime
        };
    }

    private static PingResult CreateFailureResult(IPStatus status = IPStatus.TimedOut)
    {
        return new PingResult
        {
            Status = status,
            RoundtripTime = 0
        };
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenHostNotConfigured()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Host not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenPacketCountIsZero()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(0)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Packet count must be greater than 0", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenPacketLossThresholdIsInvalid()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(150)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Packet loss threshold must be between 0 and 100", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpWhenAllPingsSucceed()
    {
        var successResult = CreateSuccessResult(50);

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(successResult));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.AreEqual(50, result.ResponseTimeMs.Value);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenAllPingsFail()
    {
        var failResult = CreateFailureResult();

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failResult));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("All 4 packet(s) failed", result.ErrorMessage);
        Assert.Contains("100% packet loss", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnWarnWhenPacketLossExceedsThreshold()
    {
        var successResult = CreateSuccessResult(40);
        var failResult = CreateFailureResult();

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(successResult),
                Task.FromResult(failResult),
                Task.FromResult(failResult),
                Task.FromResult(failResult)
            );

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Warn, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.AreEqual(40, result.ResponseTimeMs.Value);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Packet loss (75%)", result.ErrorMessage);
        Assert.Contains("threshold (50%)", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpWhenPacketLossBelowThreshold()
    {
        var successResult = CreateSuccessResult(30);
        var failResult = CreateFailureResult();

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(successResult),
                Task.FromResult(successResult),
                Task.FromResult(successResult),
                Task.FromResult(failResult)
            );

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(4),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.AreEqual(30, result.ResponseTimeMs.Value);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldCalculateAverageResponseTime()
    {
        var result1 = CreateSuccessResult(10);
        var result2 = CreateSuccessResult(20);
        var result3 = CreateSuccessResult(30);

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(result1),
                Task.FromResult(result2),
                Task.FromResult(result3)
            );

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(3),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.AreEqual(20, result.ResponseTimeMs.Value);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandlePingException()
    {
        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PingException("Network unreachable"));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(2),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("All 2 packet(s) failed", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleUnexpectedException()
    {
        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new System.Net.Sockets.SocketException(10035));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(2),
            [ConfigurationKeys.PingCheck.PacketLossThresholdPercent] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Unexpected error:", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleCancellation()
    {
        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.Delay(10000, callInfo.Arg<CancellationToken>()).ContinueWith<PingResult>(_ => null!));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(1),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(30)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _executor.ExecuteAsync(config, cts.Token);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldUseDefaultValues()
    {
        var successResult = CreateSuccessResult(25);

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(successResult));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        await _mockWrapper.Received(CheckDefaults.PingPacketCount).SendPingAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldRespectTimeoutConfiguration()
    {
        var successResult = CreateSuccessResult(10);

        _mockWrapper.SendPingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(successResult));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.PingCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.PingCheck.PacketCount] = JsonSerializer.SerializeToElement(1),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(15)
        };

        await _executor.ExecuteAsync(config);

        // Per-ping timeout is fixed at 5 seconds, global timeout is 15 seconds
        await _mockWrapper.Received(1).SendPingAsync(
            "example.com",
            5000,
            Arg.Any<CancellationToken>());
    }
}
