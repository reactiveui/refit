// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Refit.NetFrameworkSmoke;

/// <summary>A test HTTP message handler that serves canned responses for the .NET Framework smoke test.</summary>
internal sealed class NetFrameworkSmokeHandler : HttpMessageHandler
{
    /// <summary>Gets a value indicating whether a POST body containing the expected payload was observed.</summary>
    internal bool SawPostBody { get; private set; }

    /// <summary>Gets a value indicating whether the generated query string matched the expected shape.</summary>
    internal bool SawExpectedQuery { get; private set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri?.AbsolutePath == "/todos")
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            SawPostBody = body.IndexOf("prove net framework", StringComparison.Ordinal) >= 0;

            return Json("{\"id\":42,\"title\":\"prove net framework\"}");
        }

        if (request.RequestUri?.AbsolutePath == "/search")
        {
            SawExpectedQuery = request.RequestUri.PathAndQuery == "/search?q=a%20b&page=3";

            return new(HttpStatusCode.OK) { Content = new StringContent("found", Encoding.UTF8, "text/plain") };
        }

        return request.RequestUri?.AbsolutePath == "/todos/42"
            ? Json("{\"id\":42,\"title\":\"fetched on net framework\"}")
            : new(HttpStatusCode.NotFound);
    }

    /// <summary>Builds an OK response with the given JSON content.</summary>
    /// <param name="content">The JSON content for the response body.</param>
    /// <returns>The constructed JSON response message.</returns>
    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
}
