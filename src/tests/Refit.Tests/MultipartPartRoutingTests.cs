// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies how a multipart method routes each argument between a multipart part and the query string.</summary>
public sealed class MultipartPartRoutingTests
{
    /// <summary>The multipart part body shared by the routing fixtures.</summary>
    private const string PartContent = "content";

    /// <summary>A plain multipart argument is added as a multipart part.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PlainArgumentIsAddedAsMultipartPart()
    {
        var handler = await SendAsync(nameof(IMultipartPartRoutingApi.UploadPlainPart), [PartContent]);

        await Assert.That(handler.Parts).HasSingleItem();
        await Assert.That(handler.Parts[0].Name).IsEqualTo("file");
        await Assert.That(handler.RequestUri!.Query).IsEqualTo(string.Empty);
    }

    /// <summary>A query-attributed argument on a multipart method is routed to the query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryAttributedArgumentIsRoutedToQuery()
    {
        var handler = await SendAsync(nameof(IMultipartPartRoutingApi.UploadWithQueryParam), ["mytag", PartContent]);

        await Assert.That(handler.Parts).HasSingleItem();
        await Assert.That(handler.Parts[0].Name).IsEqualTo("file");
        await Assert.That(handler.RequestUri!.Query).Contains("tag=mytag");
    }

    /// <summary>An object-property argument bound to the path is routed to the query string, not a multipart part.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PathBoundObjectPropertyArgumentIsRoutedToQuery()
    {
        var handler = await SendAsync(
            nameof(IMultipartPartRoutingApi.UploadWithPathBoundObject),
            [new MultipartRoutingRequest { Id = "abc" }, PartContent]);

        await Assert.That(handler.Parts).HasSingleItem();
        await Assert.That(handler.Parts[0].Name).IsEqualTo("file");
        await Assert.That(handler.RequestUri!.AbsolutePath).Contains("/object/abc");
    }

    /// <summary>Sends a request through a capturing handler and returns the handler that observed it.</summary>
    /// <param name="method">The interface method to invoke.</param>
    /// <param name="args">The argument values for the call.</param>
    /// <returns>The handler that captured the request.</returns>
    private static async Task<MultipartCapturingHttpMessageHandler> SendAsync(string method, object[] args)
    {
        var fixture = new RequestBuilderImplementation<IMultipartPartRoutingApi>();
        var func = fixture.BuildRestResultFuncForMethod(method);
        var handler = new MultipartCapturingHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new("http://api/") };
        await (Task)func(client, args)!;
        return handler;
    }
}
