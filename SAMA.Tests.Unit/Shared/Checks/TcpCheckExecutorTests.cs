using System.Net.Sockets;
using System.Text.Json;
using NSubstitute;
using SAMA.Shared.Checks;
using SAMA.Shared.Constants;
using SAMA.Shared.Wrappers;

namespace SAMA.Tests.Unit.Shared.Checks;

[TestClass]
public class TcpCheckExecutorTests
{
    private TcpClientWrapper _mockWrapper = null!;
    private TcpClientFactory _mockFactory = null!;
    private TcpCheckExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockWrapper = Substitute.For<TcpClientWrapper>();
        _mockFactory = Substitute.For<TcpClientFactory>();
        _mockFactory.CreateClient().Returns(_mockWrapper);
        _executor = new TcpCheckExecutor(_mockFactory);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenHostNotConfigured()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Host not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenPortNotConfigured()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Valid port (1-65535) not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenPortInvalid()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(0)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Valid port (1-65535) not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenPortTooHigh()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(70000)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Valid port (1-65535) not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpForSuccessfulConnection()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.Delay(50));
        _mockWrapper.Connected.Returns(true);

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(10)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsGreaterThanOrEqualTo(0, result.ResponseTimeMs.Value);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownForFailedConnection()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new SocketException((int)SocketError.ConnectionRefused));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(5)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Connection failed", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnWarnWhenConnectionTimeExceedsThreshold()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.Delay(100));
        _mockWrapper.Connected.Returns(true);

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.TcpCheck.ConnectionTimeWarnThresholdMs] = JsonSerializer.SerializeToElement(50),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(10)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Warn, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsGreaterThanOrEqualTo(50, result.ResponseTimeMs.Value);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Connection time", result.ErrorMessage);
        Assert.Contains("exceeded threshold", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpWhenConnectionTimeWithinThreshold()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.Delay(50));
        _mockWrapper.Connected.Returns(true);

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.TcpCheck.ConnectionTimeWarnThresholdMs] = JsonSerializer.SerializeToElement(5000),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(10)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsLessThan(5000, result.ResponseTimeMs.Value);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleTimeout()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.Delay(10000, callInfo.Arg<CancellationToken>()));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(1)
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("timeout", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleCancellation()
    {
        _mockWrapper.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.Delay(10000, callInfo.Arg<CancellationToken>()));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.TcpCheck.Host] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.TcpCheck.Port] = JsonSerializer.SerializeToElement(80),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(30)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _executor.ExecuteAsync(config, cts.Token);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
    }
}
