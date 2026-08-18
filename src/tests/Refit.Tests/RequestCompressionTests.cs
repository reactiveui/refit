// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;

namespace Refit.Tests;

/// <summary>Applying a content coding to a request body: what reaches the wire, what <c>Content-Encoding</c> says, and
/// which codings a given target framework can produce.</summary>
public class RequestCompressionTests
{
    /// <summary>The media type the coded content must carry through from the inner content.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>A body long enough that every coding makes it shorter.</summary>
    private const string BodyText =
        """{"name":"refit","description":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}""";

    /// <summary>Verifies each supported coding compresses the body and announces itself.</summary>
    /// <param name="compression">The coding under test.</param>
    /// <param name="expectedToken">The <c>Content-Encoding</c> token it must send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(RequestCompression.GZip, "gzip")]
#if NET8_0_OR_GREATER
    [Arguments(RequestCompression.Brotli, "br")]
#endif
#if NET11_0_OR_GREATER
    [Arguments(RequestCompression.Zstandard, "zstd")]
#endif
    public async Task SupportedCodingCompressesTheBodyAndAnnouncesItself(
        RequestCompression compression,
        string expectedToken)
    {
        using var content = new StringContent(BodyText, System.Text.Encoding.UTF8, JsonMediaType);
        using var compressed = GeneratedRequestRunner.CompressBodyContent(
            content,
            new(),
            compression,
            CompressionLevel.Optimal);

        var bytes = await compressed.ReadAsByteArrayAsync();

        await Assert.That(compressed.Headers.ContentEncoding).IsEquivalentTo([expectedToken]);
        await Assert.That(compressed.Headers.ContentType?.MediaType).IsEqualTo(JsonMediaType);
        await Assert.That(bytes.Length).IsLessThan(BodyText.Length);
    }

#if !NET11_0_OR_GREATER
    /// <summary>Verifies a coding this framework cannot produce fails loudly instead of sending the body uncompressed.
    /// Every coding exists on .NET 11.0 and later, so there is nothing left to reject there.</summary>
    /// <param name="compression">The coding under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(RequestCompression.Zstandard)]
#if !NET8_0_OR_GREATER
    [Arguments(RequestCompression.Brotli)]
#endif
    public async Task UnsupportedCodingThrows(RequestCompression compression)
    {
        using var content = new StringContent(BodyText);

        await Assert.That(() => GeneratedRequestRunner.CompressBodyContent(
                content,
                new(),
                compression,
                CompressionLevel.Optimal))
            .Throws<PlatformNotSupportedException>();
    }
#endif

    /// <summary>Verifies an unset coding leaves the content alone when the settings ask for none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultCodingLeavesTheContentAloneWhenTheSettingsAskForNone()
    {
        using var content = new StringContent(BodyText);

        var result = GeneratedRequestRunner.CompressBodyContent(
            content,
            new(),
            RequestCompression.Default,
            CompressionLevel.Optimal);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies an unset coding falls back to the one the settings name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultCodingFallsBackToTheSettings()
    {
        using var content = new StringContent(BodyText);
        var settings = new RefitSettings { RequestCompression = RequestCompression.GZip, RequestCompressionLevel = CompressionLevel.SmallestSize };

        using var result = GeneratedRequestRunner.CompressBodyContent(
            content,
            settings,
            RequestCompression.Default,
            CompressionLevel.Optimal);

        await Assert.That(result).IsNotSameReferenceAs(content);
        await Assert.That(result.Headers.ContentEncoding).IsEquivalentTo(["gzip"]);
    }

    /// <summary>Verifies an explicit <see cref="RequestCompression.None"/> opts out of a coding the settings turned on.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ExplicitNoneOptsOutOfTheSettingsCoding()
    {
        using var content = new StringContent(BodyText);
        var settings = new RefitSettings { RequestCompression = RequestCompression.GZip };

        var result = GeneratedRequestRunner.CompressBodyContent(
            content,
            settings,
            RequestCompression.None,
            CompressionLevel.Optimal);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies the compressed bytes decompress back to the original body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CompressedBodyRoundTrips()
    {
        using var content = new StringContent(BodyText, System.Text.Encoding.UTF8, JsonMediaType);
        using var compressed = GeneratedRequestRunner.CompressBodyContent(
            content,
            new(),
            RequestCompression.GZip,
            CompressionLevel.Optimal);

        await using var source = new MemoryStream(await compressed.ReadAsByteArrayAsync());
        await using var decompressor = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressor);

        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo(BodyText);
    }

#if NET9_0_OR_GREATER
    /// <summary>Verifies per-coding options replace the level for the coding they name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PerCodingOptionsReplaceTheLevel()
    {
        // Zero is the zlib "store, do not compress" level, and the declared level asks for the smallest output.
        var settings = new RefitSettings { RequestCompressionOptions = new() { GZip = new() { CompressionLevel = 0 } } };

        var byOptions = await CompressAsync(settings, RequestCompression.GZip, CompressionLevel.SmallestSize);
        var byLevel = await CompressAsync(new(), RequestCompression.GZip, CompressionLevel.SmallestSize);

        await Assert.That(byOptions.Length).IsGreaterThan(byLevel.Length);
    }

    /// <summary>Verifies a coding the options left unset still compresses by level.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CodingWithoutOptionsStillCompressesByLevel()
    {
        var settings = new RefitSettings { RequestCompressionOptions = new() { Brotli = new() { Quality = 0 } } };

        var bytes = await CompressAsync(settings, RequestCompression.GZip, CompressionLevel.SmallestSize);

        await Assert.That(bytes.Length).IsLessThan(BodyText.Length);
    }
#endif

    /// <summary>Verifies the compressed content reports no length, so the request is sent chunked.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CompressedBodyReportsNoContentLength()
    {
        using var content = new StringContent(BodyText);
        using var compressed = GeneratedRequestRunner.CompressBodyContent(
            content,
            new(),
            RequestCompression.GZip,
            CompressionLevel.Optimal);

        await Assert.That(compressed.Headers.ContentLength).IsNull();
    }

#if NET9_0_OR_GREATER
    /// <summary>Compresses the shared body and returns the bytes that would reach the wire.</summary>
    /// <param name="settings">The settings supplying any per-coding options.</param>
    /// <param name="compression">The coding to apply.</param>
    /// <param name="level">The level to apply where the options do not override it.</param>
    /// <returns>The compressed bytes.</returns>
    private static async Task<byte[]> CompressAsync(
        RefitSettings settings,
        RequestCompression compression,
        CompressionLevel level)
    {
        using var content = new StringContent(BodyText);
        using var compressed = GeneratedRequestRunner.CompressBodyContent(content, settings, compression, level);

        return await compressed.ReadAsByteArrayAsync();
    }
#endif
}
