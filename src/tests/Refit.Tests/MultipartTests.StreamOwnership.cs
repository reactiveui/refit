// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>
/// Tests that Refit does not dispose caller-supplied request streams, while still disposing streams it opens itself.
/// Refit disposes the request after every send (which disposes its content and any stream that content holds), so a
/// caller-owned stream must survive that disposal and a Refit-opened stream must not.
/// </summary>
public partial class MultipartTests
{
    /// <summary>Sample bytes uploaded by the stream-ownership tests.</summary>
    private static readonly byte[] _ownershipStreamBytes = [1, 2, 3, 4, 5];

    /// <summary>A raw stream multipart part uploaded through the generated builder leaves the caller's stream open.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartRawStreamDoesNotDisposeCallerStreamThroughGeneratedBuilder()
    {
        await using var callerStream = new MemoryStream(_ownershipStreamBytes);

        var handler = await UploadThroughGeneratedBuilderAsync(api => api.UploadStream(callerStream));
        handler.RequestMessage!.Dispose();

        await Assert.That(callerStream.CanRead).IsTrue();
    }

    /// <summary>A raw stream multipart part uploaded through the reflection builder leaves the caller's stream open.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartRawStreamDoesNotDisposeCallerStreamThroughReflectionBuilder()
    {
        await using var callerStream = new MemoryStream(_ownershipStreamBytes);

        var fixture = new RequestBuilderImplementation<IRunscopeApi>();
        var handler = await fixture.RunRequest(nameof(IRunscopeApi.UploadStream))([callerStream]);
        handler.RequestMessage!.Dispose();

        await Assert.That(callerStream.CanRead).IsTrue();
    }

    /// <summary>A <see cref="StreamPart"/> uploaded through the generated builder leaves the caller's stream open.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartStreamPartDoesNotDisposeCallerStreamThroughGeneratedBuilder()
    {
        await using var callerStream = new MemoryStream(_ownershipStreamBytes);

        var handler = await UploadThroughGeneratedBuilderAsync(
            api => api.UploadStreamPart(new StreamPart(callerStream, StreamPartFileName, PdfMediaType)));
        handler.RequestMessage!.Dispose();

        await Assert.That(callerStream.CanRead).IsTrue();
    }

    /// <summary>Disposing a <see cref="StreamPart"/>'s content does not dispose the caller-supplied stream it wraps.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <remarks>Both request builders funnel a <see cref="StreamPart"/> through <see cref="MultipartItem.ToContent"/>,
    /// so the ownership contract this asserts holds for the generated and reflection paths alike.</remarks>
    [Test]
    public async Task StreamPartLeavesCallerStreamOpenWhenContentDisposed()
    {
        await using var callerStream = new MemoryStream(_ownershipStreamBytes);

        var content = new StreamPart(callerStream, StreamPartFileName, PdfMediaType).ToContent();
        content.Dispose();

        await Assert.That(callerStream.CanRead).IsTrue();
    }

    /// <summary>Disposing a <see cref="FileInfoPart"/>'s content disposes the stream Refit opened for the file.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FileInfoPartDisposesRefitOpenedStreamWhenContentDisposed()
    {
        var path = CreateTempFile();
        try
        {
            await File.WriteAllBytesAsync(path, _ownershipStreamBytes);

            var content = new FileInfoPart(new FileInfo(path), StreamPartFileName, PdfMediaType).ToContent();

            // StreamContent hands back the exact stream it wraps, which for FileInfoPart is the file stream Refit opened.
            var refitOpenedStream = await content.ReadAsStreamAsync();
            await Assert.That(refitOpenedStream.CanRead).IsTrue();

            content.Dispose();

            await Assert.That(refitOpenedStream.CanRead).IsFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Uploads through the generated request builder against a capturing handler and returns that handler.</summary>
    /// <param name="upload">The upload call to invoke on the generated client.</param>
    /// <returns>The handler that observed the request.</returns>
    private static async Task<TestHttpMessageHandler> UploadThroughGeneratedBuilderAsync(
        Func<IRunscopeApi, Task<HttpResponseMessage>> upload)
    {
        var handler = new TestHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };
        var api = RestService.For<IRunscopeApi>(BaseAddress, settings);

        (await upload(api)).Dispose();

        return handler;
    }
}
