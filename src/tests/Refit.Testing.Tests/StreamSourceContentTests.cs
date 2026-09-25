// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Refit.Testing.Tests;

/// <summary>Unit tests for <see cref="StreamSourceContent"/> and its nested read stream, covering the <see cref="System.IO.Stream"/> contract directly.</summary>
[SuppressMessage("Performance", "PSH1313:A synchronous call where an async overload fits", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "PSH1314:Use the Memory-based ReadAsync overload", Justification = "These tests intentionally exercise the byte-array Stream overrides.")]
[SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "CA1835:Prefer the memory-based Stream overloads", Justification = "These tests intentionally exercise the byte-array Stream overrides.")]
public sealed class StreamSourceContentTests
{
    /// <summary>The size of the read buffer used by the whole-chunk read tests.</summary>
    private const int ReadBufferSize = 16;

    /// <summary>The maximum number of one-byte reads performed while draining the chunk-boundary test, guarding against an infinite loop if the stream never ends.</summary>
    private const int MaxDrainIterations = 16;

    /// <summary>The serializer used to bind sources under test.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new();

    /// <summary>Verifies the content type header is set from the source's media type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ContentTypeHeaderMatchesSourceMediaType()
    {
        var source = new StreamSource(StreamingContentFormat.ServerSentEvents);

        using var content = source.CreateContent(Serializer);

        await Assert.That(content.Headers.ContentType?.MediaType).IsEqualTo("text/event-stream");
    }

    /// <summary>Verifies buffering the content (e.g. via <c>ReadAsStringAsync</c>) reads the whole released body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferingReadsWholeReleasedBody()
    {
        var source = new StreamSource();
        using var content = source.CreateContent(Serializer);
        source.ReleaseText("a\n");
        source.ReleaseText("b\n");
        source.Complete();

        var text = await content.ReadAsStringAsync();

        await Assert.That(text).IsEqualTo("a\nb\n");
    }

    /// <summary>Verifies <c>TryComputeLength</c> always reports no known length.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NeverReportsContentLength()
    {
        var source = new StreamSource();
        using var content = source.CreateContent(Serializer);
        source.Complete();

        await Assert.That(content.Headers.ContentLength).IsNull();
    }

    /// <summary>Verifies disposing the content closes the source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisposingContentClosesSource()
    {
        var source = new StreamSource();
        var content = source.CreateContent(Serializer);

        content.Dispose();

        await Assert.That(source.IsClosed).IsTrue();
    }

    /// <summary>Verifies the nested read stream reports the expected capability flags and throws for unsupported members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadStreamReportsCapabilitiesAndThrowsForUnsupportedMembers()
    {
        var source = new StreamSource();
        await using var stream = new StreamSourceContent.StreamSourceReadStream(source);

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
        await Assert.That(stream.CanWrite).IsFalse();
        await Assert.That(() => stream.Length).Throws<NotSupportedException>();
        await Assert.That(() => stream.Position).Throws<NotSupportedException>();
        await Assert.That(() => stream.Position = 0).Throws<NotSupportedException>();
        await Assert.That(() => stream.Seek(0, SeekOrigin.Begin)).Throws<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
        await Assert.That(() => stream.Write([1], 0, 1)).Throws<NotSupportedException>();

        // Flush is a no-op; verifying it does not throw exercises the branch.
        stream.Flush();
    }

    /// <summary>Verifies the synchronous <c>Read</c> override copies from a released chunk.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncReadCopiesReleasedChunk()
    {
        var source = new StreamSource();
        await using var stream = new StreamSourceContent.StreamSourceReadStream(source);
        source.ReleaseBytes("hello"u8.ToArray());
        source.Complete();

        var buffer = new byte[ReadBufferSize];
        var read = stream.Read(buffer, 0, buffer.Length);

        await Assert.That(Encoding.UTF8.GetString(buffer, 0, read)).IsEqualTo("hello");
    }

    /// <summary>Verifies reading past the end of the body returns 0 repeatedly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadingPastEndReturnsZeroRepeatedly()
    {
        var source = new StreamSource();
        await using var stream = new StreamSourceContent.StreamSourceReadStream(source);
        source.Complete();

        var buffer = new byte[4];
        var first = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
        var second = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        await Assert.That(first).IsEqualTo(0);
        await Assert.That(second).IsEqualTo(0);
    }

    /// <summary>Verifies a read is satisfied across a chunk boundary: a small buffer drains one chunk, then the next read pulls the next chunk.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadDrainsChunkThenAdvancesToNext()
    {
        var source = new StreamSource();
        await using var stream = new StreamSourceContent.StreamSourceReadStream(source);
        source.ReleaseBytes("ab"u8.ToArray());
        source.ReleaseBytes("cd"u8.ToArray());
        source.Complete();

        var buffer = new byte[1];
        var collected = new StringBuilder();
        int read;
        var iterations = 0;
        while ((read = await stream.ReadAsync(buffer, 0, 1, CancellationToken.None)) != 0)
        {
            _ = collected.Append((char)buffer[0]);
            iterations++;
            if (iterations > MaxDrainIterations)
            {
                break;
            }
        }

        await Assert.That(collected.ToString()).IsEqualTo("abcd");
    }

#if NET5_0_OR_GREATER
    /// <summary>Verifies the synchronous <c>CreateContentReadStream</c> override returns the same stream used for buffering.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncCreateContentReadStreamReturnsStream()
    {
        var source = new StreamSource();
        using var content = source.CreateContent(Serializer);
        source.ReleaseText("x");
        source.Complete();

        await using var stream = content.ReadAsStream();
        var buffer = new byte[ReadBufferSize];
        var read = stream.Read(buffer, 0, buffer.Length);

        await Assert.That(Encoding.UTF8.GetString(buffer, 0, read)).IsEqualTo("x");
    }
#endif
}
