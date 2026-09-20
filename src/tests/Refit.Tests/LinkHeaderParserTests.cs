// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the reading of <c>Link</c> response headers, as defined by RFC 8288.</summary>
public class LinkHeaderParserTests
{
    /// <summary>The link target most cases expect.</summary>
    private const string ExampleTarget = "https://a.example/x";

    /// <summary>The next relation.</summary>
    private const string Next = "next";

    /// <summary>The separator between header lines in a case.</summary>
    private const char LineSeparator = '\n';

    /// <summary>Gets header values with the relation to find and the link expected, or <see langword="null"/> when there is none.</summary>
    /// <returns>The header lines separated by newlines, the relation and the expected target.</returns>
    public static IEnumerable<(string Lines, string Relation, string? Expected)> Cases() =>
    [
        ("<https://a.example/x?page=2>; rel=\"next\", <https://a.example/x?page=5>; rel=\"last\"", Next, "https://a.example/x?page=2"),
        ("<https://a.example/x?page=2>; rel=\"next\", <https://a.example/x?page=5>; rel=\"last\"", "last", "https://a.example/x?page=5"),
        ("<https://a.example/x>; rel=next", Next, ExampleTarget),
        ("<https://a.example/x>;rel=\"next\"", Next, ExampleTarget),
        ("<https://a.example/x>; REL=\"NEXT\"", Next, ExampleTarget),
        ("<https://a.example/x>; rel=\"prev next\"", Next, ExampleTarget),
        ("<https://a.example/x>; title=\"a, b\"; rel=\"next\"", Next, ExampleTarget),
        ("<https://a.example/x?ids=1,2>; rel=\"next\"", Next, "https://a.example/x?ids=1,2"),
        ("<https://a.example/x>; title=\"say \\\"hi\\\"\"; rel=\"next\"", Next, ExampleTarget),
        ("</relative?page=2>; rel=\"next\"", Next, "/relative?page=2"),
        ("<https://a.example/1>; rel=\"prev\"\n<https://a.example/2>; rel=\"next\"", Next, "https://a.example/2"),
        ("<https://a.example/x>; rel=\"last\"", Next, null),
        ("garbage", Next, null),
        ("<https://a.example/x; rel=\"next\"", Next, null),
        ("<https://a.example/x>; nofollow; rel=\"next\"", Next, ExampleTarget),
        (" , <https://a.example/x>; rel=\"next\"", Next, ExampleTarget),
        ("<http://>; rel=\"next\"", Next, null),
        ("<https://a.example/x>; rel", Next, null),
        ("<https://a.example/x>; rel=\"next", Next, ExampleTarget),
        ("<https://a.example/x>; rel=\"\"", Next, null),
        ("<https://a.example/x>; rel*=UTF-8''next", Next, null),
        ("<https://a.example/x>; title=\"abc\\", Next, null),
        ("<https://a.example/x>; rel=next ; foo", Next, ExampleTarget),
        ("<https://a.example/x>; rel=next; title=\"t\"", Next, ExampleTarget),
        ("<https://a.example/y>; rel=prev, <https://a.example/x>; rel=next", Next, ExampleTarget),
        (string.Empty, Next, null),
    ];

    /// <summary>Verifies each header is read for the link that carries the relation.</summary>
    /// <param name="lines">The values of the <c>Link</c> header lines, separated by newlines.</param>
    /// <param name="relation">The relation to find.</param>
    /// <param name="expected">The expected target, or <see langword="null"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Find_ReturnsTheTargetOfTheLinkCarryingTheRelation(string lines, string relation, string? expected)
    {
        var link = LinkHeaderParser.Find(lines.Split(LineSeparator, StringSplitOptions.RemoveEmptyEntries), relation);

        await Assert.That(link?.OriginalString).IsEqualTo(expected);
    }

    /// <summary>Verifies the response extension reads the <c>Link</c> header.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GetLink_ReadsTheLinkHeaderOfAResponse()
    {
        using var response = CreateResponse("Link", "<https://a.example/x?page=2>; rel=\"next\"");

        var link = ((IApiResponse)response).GetLink(Next);

        await Assert.That(link?.AbsoluteUri).IsEqualTo("https://a.example/x?page=2");
    }

    /// <summary>Verifies a response with no <c>Link</c> header has no link.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GetLink_ResponseWithoutTheHeader_ReturnsNull()
    {
        using var response = CreateResponse("X-Other", "value");

        await Assert.That(((IApiResponse)response).GetLink(Next)).IsNull();
    }

    /// <summary>Verifies a response that carries no headers, because the request failed, has no link.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GetLink_ResponseWithoutHeaders_ReturnsNull()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, ExampleTarget);
        using var response = new ApiResponse<string>(request, null, null, new());

        await Assert.That(((IApiResponse)response).GetLink(Next)).IsNull();
    }

    /// <summary>Verifies the response extension rejects a missing argument.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GetLink_NullArguments_Throw()
    {
        using var response = CreateResponse("Link", "<https://a.example/x>; rel=next");
        IApiResponse noResponse = null!;

        using (Assert.Multiple())
        {
            await Assert.That(() => noResponse.GetLink(Next)).Throws<ArgumentNullException>();
            await Assert.That(() => ((IApiResponse)response).GetLink(null!)).Throws<ArgumentNullException>();
        }
    }

    /// <summary>Creates a response carrying one header.</summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The response.</returns>
    internal static ApiResponse<string> CreateResponse(string name, string value)
    {
        var httpResponse = new HttpResponseMessage { RequestMessage = new(HttpMethod.Get, ExampleTarget) };
        _ = httpResponse.Headers.TryAddWithoutValidation(name, value);
        return new(httpResponse, "body", new());
    }
}
