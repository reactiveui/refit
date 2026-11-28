// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Refit.Testing.Tests;

/// <summary>Checks typed JSON replies through the HTTP handler.</summary>
public class ReplyTests
{
    /// <summary>The resource requested from the stub.</summary>
    private const string UserPath = "/users/1";

    /// <summary>Each match writes a fresh JSON body with the caller's metadata and status.</summary>
    /// <param name="cancellationToken">Cancels sending and reading the replies.</param>
    /// <returns>Completion after both responses have been read and disposed.</returns>
    [Test]
    public async Task With_ExplicitMetadata_WritesEachResponse(CancellationToken cancellationToken)
    {
        var user = new User(1, "Ada");
        using var stub = new StubHttp
        {
            { Route.Get(UserPath), Reply.With(user, ReplyJsonContext.Default.User) },
            { Route.Fallback(), Reply.With(user, ReplyJsonContext.Default.User, HttpStatusCode.Created) },
        };
        using var client = HttpClientTestFactory.Create(stub, new("https://api.test"));
        var uri = new Uri(UserPath, UriKind.Relative);
        foreach (var expected in new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Created })
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            await Assert.That(response.StatusCode).IsEqualTo(expected);
            await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
            await Assert.That(await response.Content.ReadAsStringAsync(cancellationToken)).IsEqualTo("""{"ID":1,"LOGIN":"Ada"}""");
        }
    }

    /// <summary>Missing metadata fails when the route is declared.</summary>
    /// <returns>Completion after the exception has been checked.</returns>
    [Test]
    public async Task With_NullMetadata_ThrowsAtSetup() =>
        await Assert.That(static () => Reply.With(new User(1, "Ada"), null!)).Throws<ArgumentNullException>();

    /// <summary>Writes a JSON object whose keys are supplied by the test.</summary>
    /// <param name="cancellationToken">Cancels the request and body read.</param>
    /// <returns>Completion after checking the JSON sent on the wire.</returns>
    [Test]
    public async Task With_JsonObject_PreservesDynamicKeys(CancellationToken cancellationToken)
    {
        JsonObject body = new() { ["user-name"] = "Ada", ["active"] = true };
        using var stub = new StubHttp { { Route.Fallback(), Reply.With(body, ReplyJsonContext.Default.JsonObject) } };
        using var client = HttpClientTestFactory.Create(stub, new("https://api.test"));
        var json = await client.GetStringAsync(new Uri(UserPath, UriKind.Relative), cancellationToken);
        await Assert.That(JsonNode.DeepEquals(body, JsonNode.Parse(json))).IsTrue();
    }

    /// <summary>A default literal still selects the existing status-code overload.</summary>
    /// <returns>Completion after the explicit default status has been checked.</returns>
    [Test]
    public async Task With_DefaultLiteral_UsesStatusCodeOverload() =>
        await Assert.That(Reply.With(new User(1, "Ada"), default).Status).IsEqualTo(default(HttpStatusCode));
}
