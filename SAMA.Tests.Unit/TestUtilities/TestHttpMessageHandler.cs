namespace SAMA.Tests.Unit.TestUtilities;

public class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpResponseMessage? ResponseToReturn { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public HttpRequestMessage? RequestReceived { get; private set; }

    public string? RequestContent { get; private set; }

    public TimeSpan? SimulatedDelay { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestReceived = request;

        if (request.Content != null)
        {
            RequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        if (SimulatedDelay.HasValue)
        {
            await Task.Delay(SimulatedDelay.Value, cancellationToken);
        }

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return ResponseToReturn ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}
