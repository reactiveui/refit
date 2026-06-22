// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Meow;

/// <summary>In-memory backend handler that serves responses for the issue demo endpoints.</summary>
public sealed class DemoBackendHandler : HttpMessageHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            request.RequestUri?.AbsolutePath switch
            {
                "/echo-customer" => EchoCustomer(request),
                "/large-payload" => LargePayload(request),
                _ => new(HttpStatusCode.NotFound),
            });

    /// <summary>Builds the echo response that reflects the CustomerId request header back to the caller.</summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>The echo response.</returns>
    private static HttpResponseMessage EchoCustomer(HttpRequestMessage request)
    {
        string? customerIdHeader = null;
        if (request.Headers.TryGetValues("CustomerId", out var values))
        {
            using var enumerator = values.GetEnumerator();
            if (enumerator.MoveNext())
            {
                customerIdHeader = enumerator.Current;
            }
        }

        return new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(new CustomerEchoResponse { CustomerIdHeader = customerIdHeader }),
                Encoding.UTF8,
                "application/json")
        };
    }

    /// <summary>Builds a large payload response sized by the optional <c>size</c> query parameter.</summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>The large payload response.</returns>
    private static HttpResponseMessage LargePayload(HttpRequestMessage request)
    {
        var query = request.RequestUri!.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var size = 100;
        foreach (var part in query)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "size" && int.TryParse(Uri.UnescapeDataString(kv[1]), out var parsed))
            {
                size = parsed;
                break;
            }
        }

        var response = new LargePayloadResponse();
        response.Items.AddRange(Enumerable.Range(1, size));
        var payload = JsonConvert.SerializeObject(response);

        return new(HttpStatusCode.OK) { Content = new AsyncOnlyJsonHttpContent(payload) };
    }
}
