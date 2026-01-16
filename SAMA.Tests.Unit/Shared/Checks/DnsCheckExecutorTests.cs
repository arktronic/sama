using System.Net;
using System.Text.Json;
using DnsClient;
using DnsClient.Protocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SAMA.Shared.Checks;
using SAMA.Shared.Constants;

namespace SAMA.Tests.Unit.Shared.Checks;

[TestClass]
public class DnsCheckExecutorTests
{
    private ILookupClient _mockClient = null!;
    private DnsCheckExecutor _executor = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockClient = Substitute.For<ILookupClient>();
        _executor = new DnsCheckExecutor(_mockClient);
    }

    private static IDnsQueryResponse CreateSuccessResponse(params DnsResourceRecord[] records)
    {
        var response = Substitute.For<IDnsQueryResponse>();
        response.HasError.Returns(false);
        response.Answers.Returns([.. records]);
        return response;
    }

    private static IDnsQueryResponse CreateErrorResponse(string errorMessage)
    {
        var response = Substitute.For<IDnsQueryResponse>();
        response.HasError.Returns(true);
        response.ErrorMessage.Returns(errorMessage);
        response.Answers.Returns([]);
        return response;
    }

    private static IDnsQueryResponse CreateEmptyResponse()
    {
        var response = Substitute.For<IDnsQueryResponse>();
        response.HasError.Returns(false);
        response.Answers.Returns([]);
        return response;
    }

    private static ARecord CreateARecord(string ipAddress)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.A, QueryClass.IN, 300, 0);
        return new ARecord(resourceRecordInfo, IPAddress.Parse(ipAddress));
    }

    private static AaaaRecord CreateAaaaRecord(string ipAddress)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.AAAA, QueryClass.IN, 300, 0);
        return new AaaaRecord(resourceRecordInfo, IPAddress.Parse(ipAddress));
    }

    private static CNameRecord CreateCNameRecord(string canonicalName)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.CNAME, QueryClass.IN, 300, 0);
        return new CNameRecord(resourceRecordInfo, DnsString.Parse(canonicalName));
    }

    private static MxRecord CreateMxRecord(string exchange, ushort preference = 10)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.MX, QueryClass.IN, 300, 0);
        return new MxRecord(resourceRecordInfo, preference, DnsString.Parse(exchange));
    }

    private static TxtRecord CreateTxtRecord(params string[] text)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.TXT, QueryClass.IN, 300, 0);
        return new TxtRecord(resourceRecordInfo, text, text);
    }

    private static NsRecord CreateNsRecord(string nameServer)
    {
        var resourceRecordInfo = new ResourceRecordInfo("example.com", ResourceRecordType.NS, QueryClass.IN, 300, 0);
        return new NsRecord(resourceRecordInfo, DnsString.Parse(nameServer));
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenHostnameNotConfigured()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.AreEqual("Hostname not configured", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpWhenARecordResolved()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"),
            CreateARecord("192.0.2.2"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnUpWhenAAAARecordResolved()
    {
        var response = CreateSuccessResponse(
            CreateAaaaRecord("2001:db8::1"),
            CreateAaaaRecord("2001:db8::2"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("AAAA")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenNoRecordsFound()
    {
        var response = CreateEmptyResponse();

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("nonexistent.example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("No A records found", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenQueryHasError()
    {
        var response = CreateErrorResponse("Name server not found");

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.Contains("DNS query failed", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldMatchExpectedValue()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"),
            CreateARecord("192.0.2.2"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "192.0.2.1" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldMatchAnyExpectedValue()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"),
            CreateARecord("192.0.2.2"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "192.0.2.2", "198.51.100.1" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenExpectedValueNotFound()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"),
            CreateARecord("192.0.2.2"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "198.51.100.1" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("do not match expected values", result.ErrorMessage ?? "");
        Assert.Contains("198.51.100.1", result.ErrorMessage ?? "");
        Assert.Contains("192.0.2.1", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleCNAMERecordType()
    {
        var response = CreateSuccessResponse(
            CreateCNameRecord("target.example.com"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("www.example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("CNAME")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNotNull(result.ResponseTimeMs);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleMXRecordType()
    {
        var response = CreateSuccessResponse(
            CreateMxRecord("mail.example.com", 10),
            CreateMxRecord("mail2.example.com", 20));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("MX")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleTXTRecordType()
    {
        var response = CreateSuccessResponse(
            CreateTxtRecord("v=spf1 include:_spf.example.com ~all"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("TXT")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleNSRecordType()
    {
        var response = CreateSuccessResponse(
            CreateNsRecord("ns1.example.com"),
            CreateNsRecord("ns2.example.com"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("NS")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldMatchCNAMEExpectedValue()
    {
        var response = CreateSuccessResponse(
            CreateCNameRecord("target.example.com"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("www.example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("CNAME"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "target.example.com" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldReturnDownWhenCNAMEDoesNotMatch()
    {
        var response = CreateSuccessResponse(
            CreateCNameRecord("actual.example.com"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("www.example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("CNAME"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "expected.example.com" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.Contains("do not match expected values", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleDnsResponseException()
    {
        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DnsResponseException("DNS server error"));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("DNS query failed", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleUnexpectedException()
    {
        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("Unexpected error:", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleCancellation()
    {
        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                await Task.Delay(10000, token);
                return CreateEmptyResponse();
            });

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.Common.TimeoutSeconds] = JsonSerializer.SerializeToElement(30)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _executor.ExecuteAsync(config, cts.Token);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.Contains("timeout", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldUseDefaultRecordType()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com")
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        await _mockClient.Received(1).QueryAsync("example.com", QueryType.A, Arg.Any<QueryClass>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldBeCaseInsensitiveForExpectedValues()
    {
        var response = CreateSuccessResponse(
            CreateCNameRecord("example.net"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("CNAME"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "eXaMpLe.NeT" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldTrimTrailingDotsFromHostnames()
    {
        var response = CreateSuccessResponse(
            CreateCNameRecord("target.example.com."));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("www.example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("CNAME"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "target.example.com" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldTrimWhitespaceFromExpectedValues()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "  192.0.2.1  ", "198.51.100.1" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldHandleAllEmptyExpectedValues()
    {
        var response = CreateSuccessResponse(
            CreateARecord("192.0.2.1"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("A"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "", "   ", "\t" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldNotMatchWrongCaseForTXTRecords()
    {
        var response = CreateSuccessResponse(
            CreateTxtRecord("v=spf1 include:_spf.example.com ~all"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("TXT"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "V=SPF1 include:_spf.example.com ~all" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Down, result.Status);
        Assert.Contains("do not match expected values", result.ErrorMessage ?? "");
    }

    [TestMethod]
    public async Task ExecuteAsyncShouldMatchExactCaseForTXTRecords()
    {
        var response = CreateSuccessResponse(
            CreateTxtRecord("v=spf1 include:_spf.example.com ~all"));

        _mockClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>(), Arg.Any<QueryClass>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.DnsCheck.Hostname] = JsonSerializer.SerializeToElement("example.com"),
            [ConfigurationKeys.DnsCheck.RecordType] = JsonSerializer.SerializeToElement("TXT"),
            [ConfigurationKeys.DnsCheck.ExpectedValues] = JsonSerializer.SerializeToElement(new[] { "v=spf1 include:_spf.example.com ~all" })
        };

        var result = await _executor.ExecuteAsync(config);

        Assert.AreEqual(CheckStatuses.Up, result.Status);
        Assert.IsNull(result.ErrorMessage);
    }
}
