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
        CancellationToken cancellationToken)
    {
        if (request.RequestUri?.AbsolutePath == "/echo-customer")
        {
            var customerIdHeader = request.Headers.TryGetValues("CustomerId", out var values)
                ? values.FirstOrDefault()
                : null;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new CustomerEchoResponse { CustomerIdHeader = customerIdHeader }),
                        Encoding.UTF8,
                        "application/json")
                });
        }

        if (request.RequestUri?.AbsolutePath == "/large-payload")
        {
            var query = request.RequestUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
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

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new AsyncOnlyJsonHttpContent(payload) });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
