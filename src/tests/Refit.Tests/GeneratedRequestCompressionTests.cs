// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;
using System.Net;

namespace Refit.Tests;

/// <summary>End-to-end coverage that a source-generated client applies the content coding its <c>[Body]</c> parameter
/// declares, and falls back to the settings when it declares none.</summary>
public class GeneratedRequestCompressionTests
{
    /// <summary>The base address the generated compression fixtures dispatch against.</summary>
    private const string BaseUrl = "http://nowhere.com";

    /// <summary>The body value every request in this fixture sends.</summary>
    private const string BodyText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    /// <summary>Verifies a declared coding reaches the wire and announces itself.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeclaredCodingIsApplied()
    {
        var sent = await SendAsync(static api => api.GZip(BodyText), new());

        await Assert.That(sent.ContentEncoding).IsEquivalentTo(["gzip"]);
        await Assert.That(sent.Body.Length).IsLessThan(BodyText.Length);
    }

    /// <summary>Verifies a body that declares no coding takes the one the settings name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UndeclaredCodingComesFromTheSettings()
    {
        var settings = new RefitSettings { RequestCompression = RequestCompression.GZip };

        var sent = await SendAsync(static api => api.Inherited(BodyText), settings);

        await Assert.That(sent.ContentEncoding).IsEquivalentTo(["gzip"]);
    }

    /// <summary>Verifies a body declaring no coding is sent uncoded when the settings name none either.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UndeclaredCodingSendsUncodedWhenTheSettingsNameNone()
    {
        var sent = await SendAsync(static api => api.Inherited(BodyText), new());

        await Assert.That(sent.ContentEncoding).IsEmpty();
        await Assert.That(System.Text.Encoding.UTF8.GetString(sent.Body)).IsEqualTo(BodyText);
    }

    /// <summary>Verifies a body declaring <see cref="RequestCompression.None"/> overrides a coding the settings turned on.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeclaredNoneOverridesTheSettingsCoding()
    {
        var settings = new RefitSettings { RequestCompression = RequestCompression.GZip };

        var sent = await SendAsync(static api => api.Uncompressed(BodyText), settings);

        await Assert.That(sent.ContentEncoding).IsEmpty();
        await Assert.That(System.Text.Encoding.UTF8.GetString(sent.Body)).IsEqualTo(BodyText);
    }

    /// <summary>Verifies the coded bytes on the wire decompress back to the declared body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CodedBodyRoundTrips()
    {
        var sent = await SendAsync(static api => api.GZip(BodyText), new());

        await using var source = new MemoryStream(sent.Body);
        await using var decompressor = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressor);

        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo(BodyText);
    }

    /// <summary>Dispatches a generated call and returns what reached the wire.</summary>
    /// <param name="call">The generated call to make.</param>
    /// <param name="settings">The settings the client runs with.</param>
    /// <returns>The captured content coding and body bytes.</returns>
    private static async Task<(string[] ContentEncoding, byte[] Body)> SendAsync(
        Func<ICompressedBodyGeneratedApi, Task<string>> call,
        RefitSettings settings)
    {
        using var handler = new BodyCapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseUrl));

        _ = await call(RestService.For<ICompressedBodyGeneratedApi>(client, settings));

        return (handler.ContentEncoding, handler.Body);
    }

    /// <summary>Captures the coding and the raw bytes of the request body before the request is disposed.</summary>
    private sealed class BodyCapturingHandler : HttpMessageHandler
    {
        /// <summary>Gets the <c>Content-Encoding</c> tokens the request carried.</summary>
        public string[] ContentEncoding { get; private set; } = [];

        /// <summary>Gets the bytes the request body serialized to.</summary>
        public byte[] Body { get; private set; } = [];

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                ContentEncoding = [.. request.Content.Headers.ContentEncoding];
                Body = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }

            return new(HttpStatusCode.OK) { Content = new StringContent("\"ok\"") };
        }
    }
}
