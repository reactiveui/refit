using System.Net;

namespace Refit.Benchmarks;

public class StaticValueHttpResponseHandler (string response, HttpStatusCode code) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
            return Task.FromResult(
                new HttpResponseMessage(code)
                {
                    RequestMessage = request,
                    Content = new StringContent(response)
                }
            );
        }
}
