// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Refit.NativeAotSmoke;

/// <summary>A test HTTP message handler that serves canned responses for the native AOT smoke test.</summary>
internal sealed class NativeAotSmokeHandler : HttpMessageHandler
{
    /// <summary>Gets a value indicating whether a POST body containing the expected payload was observed.</summary>
    public bool SawPostBody { get; private set; }

    /// <summary>Gets a value indicating whether a URL-encoded form body was observed.</summary>
    public bool SawFormBody { get; private set; }

    /// <summary>Gets a value indicating whether the generated query string matched the expected shape.</summary>
    public bool SawExpectedQuery { get; private set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri?.AbsolutePath == "/todos")
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            SawPostBody = body.Contains("prove native aot", StringComparison.Ordinal);
            return Json("""{"id":42,"title":"prove native aot"}""");
        }

        if (request.RequestUri?.AbsolutePath == "/echo")
        {
            return Json("""{"id":42,"title":"generic inline"}""");
        }

        if (request.RequestUri?.AbsolutePath == "/forms")
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            SawFormBody = body.Contains("Name=Ada", StringComparison.Ordinal)
                && body.Contains("Count=2", StringComparison.Ordinal);
            return new(HttpStatusCode.OK) { Content = new StringContent("accepted", Encoding.UTF8, "text/plain") };
        }

        return HandleGetRequest(request);
    }

    /// <summary>Builds an OK response with the given JSON content.</summary>
    /// <param name="content">The JSON content for the response body.</param>
    /// <returns>The constructed JSON response message.</returns>
    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    /// <summary>Serves the bodyless GET endpoints.</summary>
    /// <param name="request">The request to serve.</param>
    /// <returns>The canned response.</returns>
    private HttpResponseMessage HandleGetRequest(HttpRequestMessage request)
    {
        if (request.RequestUri?.AbsolutePath == "/search")
        {
            SawExpectedQuery = request.RequestUri.PathAndQuery
                == "/search?q=a%20b&page=3&ids=1&ids=2&sort=date-desc&ready&cursor=x%2Fy";
            return Json("""{"name":"native-aot"}""");
        }

        return request.RequestUri?.AbsolutePath == "/status"
            ? Json("""{"name":"native-aot"}""")
            : new(HttpStatusCode.NotFound);
    }
}
