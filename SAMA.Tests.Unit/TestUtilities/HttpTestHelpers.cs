using System.Net;
using NSubstitute;
using SAMA.Shared.Factories;

namespace SAMA.Tests.Unit.TestUtilities;

public static class HttpTestHelpers
{
    public static ConfigurableHttpClientFactory CreateConfigurableHttpClientFactory(HttpMessageHandler handler)
    {
        var factory = Substitute.For<ConfigurableHttpClientFactory>();
        factory.CreateClient(Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>()).Returns(_ => new HttpClient(handler, disposeHandler: false));
        return factory;
    }

    public static TestHttpMessageHandler CreateMockHandler(HttpStatusCode responseStatusCode, string responseContent)
    {
        return new TestHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(responseStatusCode)
            {
                Content = new StringContent(responseContent)
            }
        };
    }

    public static TestHttpMessageHandler CreateMockHandlerWithDelay(HttpStatusCode responseStatusCode, string responseContent, TimeSpan delay)
    {
        return new TestHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(responseStatusCode)
            {
                Content = new StringContent(responseContent)
            },
            SimulatedDelay = delay
        };
    }
}
