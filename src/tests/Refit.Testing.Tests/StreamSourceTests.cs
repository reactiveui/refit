// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Refit.Testing.Tests;

/// <summary>Unit tests for <see cref="StreamSource"/> exercised directly through its internal members.</summary>
public sealed class StreamSourceTests
{
    /// <summary>An undefined <see cref="StreamingContentFormat"/> value used to exercise the range check.</summary>
    private const StreamingContentFormat UndefinedFormat = (StreamingContentFormat)999;

    /// <summary>The serializer used to bind sources for direct chunk tests.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new();

    /// <summary>A serializer that writes indented JSON, used to produce a multi-line payload for the server-sent-events framing test.</summary>
    private static readonly SystemTextJsonContentSerializer IndentedSerializer = new(new JsonSerializerOptions { WriteIndented = true });

    /// <summary>Verifies the parameterless constructor selects JSON Lines.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DefaultConstructorSelectsJsonLines()
    {
        var source = new StreamSource();

        await Assert.That(source.Format).IsEqualTo(StreamingContentFormat.JsonLines);
        await Assert.That(source.MediaType).IsEqualTo(JsonLinesContent.JsonLinesMediaType);
    }

    /// <summary>Verifies each defined format selects its documented media type.</summary>
    /// <param name="format">The format under test.</param>
    /// <param name="expectedMediaType">The media type the format should select.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(StreamingContentFormat.JsonArray, "application/json")]
    [Arguments(StreamingContentFormat.JsonLines, "application/x-ndjson")]
    [Arguments(StreamingContentFormat.ServerSentEvents, "text/event-stream")]
    public async Task FormatSelectsMediaType(StreamingContentFormat format, string expectedMediaType)
    {
        var source = new StreamSource(format);

        await Assert.That(source.MediaType).IsEqualTo(expectedMediaType);
        await Assert.That(source.Format).IsEqualTo(format);
    }

    /// <summary>Verifies an undefined format value throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UndefinedFormatThrows() =>
        await Assert.That(static () => new StreamSource(UndefinedFormat)).Throws<ArgumentOutOfRangeException>();

    /// <summary>Verifies a JSON Lines item is framed with a trailing line feed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonLinesFramesItemWithTrailingNewline()
    {
        var source = new StreamSource(StreamingContentFormat.JsonLines);
        _ = source.CreateContent(Serializer);

        source.Release(new User(1, "a"));
        var bytes = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(Encoding.UTF8.GetString(bytes!)).IsEqualTo("{\"id\":1,\"login\":\"a\"}\n");
    }

    /// <summary>Verifies a server-sent event frames every line of a multi-line payload with a <c>data:</c> prefix.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServerSentEventsFramesMultiLinePayload()
    {
        var source = new StreamSource(StreamingContentFormat.ServerSentEvents);
        _ = source.CreateContent(IndentedSerializer);

        source.Release(new User(1, "a"));
        var bytes = await source.ReadChunkAsync(CancellationToken.None);
        var text = Encoding.UTF8.GetString(bytes!);

        foreach (var line in text.TrimEnd('\n').Split('\n'))
        {
            await Assert.That(line.StartsWith("data: ", StringComparison.Ordinal)).IsTrue();
        }

        await Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Verifies the JSON array framing: the empty array on completion with no items.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonArrayEmptyCompletesAsEmptyArray()
    {
        var source = new StreamSource(StreamingContentFormat.JsonArray);
        _ = source.CreateContent(Serializer);

        source.Complete();
        var bytes = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(Encoding.UTF8.GetString(bytes!)).IsEqualTo("[]");
        var end = await source.ReadChunkAsync(CancellationToken.None);
        await Assert.That(end).IsNull();
    }

    /// <summary>Verifies the JSON array framing wraps items with brackets and separators, ending with a lone bracket.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonArrayFramesItemsWithBracketsAndSeparators()
    {
        const int SecondId = 2;
        var source = new StreamSource(StreamingContentFormat.JsonArray);
        _ = source.CreateContent(Serializer);

        source.Release(new User(1, "a"));
        source.Release(new User(SecondId, "b"));
        source.Complete();

        var first = Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!);
        var second = Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!);
        var closing = Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!);

        await Assert.That(first).IsEqualTo("[{\"id\":1,\"login\":\"a\"}");
        await Assert.That(second).IsEqualTo(",{\"id\":2,\"login\":\"b\"}");
        await Assert.That(closing).IsEqualTo("]");
    }

    /// <summary>Verifies raw text is sent as-is, with no framing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseTextSendsRawBytes()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        source.ReleaseText("raw-text");
        var bytes = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(Encoding.UTF8.GetString(bytes!)).IsEqualTo("raw-text");
    }

    /// <summary>Verifies raw bytes are sent as-is.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseBytesSendsRawBytes()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);
        byte[] payload = [1, 2, 3];

        source.ReleaseBytes(payload);
        var bytes = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(bytes).IsEquivalentTo(payload);
    }

    /// <summary>Verifies an empty byte chunk is released and read back as zero bytes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseBytesAllowsEmptyChunk()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        source.ReleaseBytes([]);
        var bytes = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(bytes!.Length).IsEqualTo(0);
    }

    /// <summary>Verifies <see cref="StreamSource.ReleaseText"/> rejects a null argument.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseTextRejectsNull()
    {
        var source = new StreamSource();

        await Assert.That(() => source.ReleaseText(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies <see cref="StreamSource.ReleaseBytes"/> rejects a null argument.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseBytesRejectsNull()
    {
        var source = new StreamSource();

        await Assert.That(() => source.ReleaseBytes(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies <see cref="StreamSource.Fail(Exception)"/> rejects a null argument.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FailRejectsNull()
    {
        var source = new StreamSource();

        await Assert.That(() => source.Fail(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies releasing after the body ended throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleaseAfterCompleteThrows()
    {
        var source = new StreamSource();
        source.Complete();

        await Assert.That(() => source.ReleaseText("late")).Throws<InvalidOperationException>();
        await Assert.That(() => source.ReleaseBytes([1])).Throws<InvalidOperationException>();
        await Assert.That(() => source.Release(1)).Throws<InvalidOperationException>();
        await Assert.That(() => source.Complete()).Throws<InvalidOperationException>();
        await Assert.That(() => source.Fail(new InvalidOperationException())).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies <see cref="StreamSource.Disconnect()"/> ends the body with an I/O failure after any released chunks.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisconnectDeliversChunksThenIOException()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        source.ReleaseText("a");
        source.ReleaseText("b");
        source.Disconnect();

        await Assert.That(Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!)).IsEqualTo("a");
        await Assert.That(Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!)).IsEqualTo("b");
#if NET8_0_OR_GREATER
        await Assert.That(async () => await source.ReadChunkAsync(CancellationToken.None)).ThrowsExactly<HttpIOException>();
#else
        await Assert.That(async () => await source.ReadChunkAsync(CancellationToken.None)).ThrowsExactly<IOException>();
#endif
    }

    /// <summary>Verifies <see cref="StreamSource.Fail(Exception)"/> delivers the custom exception at the end of the body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FailDeliversCustomException()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);
        var error = new InvalidTimeZoneException("boom");

        source.ReleaseText("x");
        source.Fail(error);

        await Assert.That(Encoding.UTF8.GetString((await source.ReadChunkAsync(CancellationToken.None))!)).IsEqualTo("x");
        await Assert.That(async () => await source.ReadChunkAsync(CancellationToken.None)).ThrowsExactly<InvalidTimeZoneException>();
    }

    /// <summary>Verifies <see cref="StreamSource.Close"/> completes <see cref="StreamSource.Closed"/> and sets <see cref="StreamSource.IsClosed"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CloseCompletesClosedTask()
    {
        var source = new StreamSource();

        await Assert.That(source.IsClosed).IsFalse();

        source.Close();

        await Assert.That(source.IsClosed).IsTrue();
        await Assert.That(source.Closed.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Verifies reading after the body is closed throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadAfterCloseThrowsObjectDisposed()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        source.Close();

        await Assert.That(async () => await source.ReadChunkAsync(CancellationToken.None)).ThrowsExactly<ObjectDisposedException>();
    }

    /// <summary>Verifies binding a source to a serializer twice throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateContentTwiceThrows()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        await Assert.That(() => source.CreateContent(Serializer)).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies a read waiting on an empty queue completes once a chunk is released, and cancellation throws
    /// while no chunk is available.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadWaitsThenCompletesOnRelease()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        var pending = source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(pending.IsCompleted).IsFalse();

        source.ReleaseText("late-arrival");
        var bytes = await pending;

        await Assert.That(Encoding.UTF8.GetString(bytes!)).IsEqualTo("late-arrival");
    }

    /// <summary>Verifies cancelling a stalled read throws without consuming the parked waiter for other readers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancelledReadThrowsOperationCanceled()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);
        using var cts = new CancellationTokenSource();

        var pending = source.ReadChunkAsync(cts.Token);
        await cts.CancelAsync();

        await Assert.That(async () => await pending).Throws<OperationCanceledException>();
    }

    /// <summary>Verifies two concurrent pending reads both complete once enough chunks are released.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TwoConcurrentPendingReadsBothComplete()
    {
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        var first = source.ReadChunkAsync(CancellationToken.None);
        var second = source.ReadChunkAsync(CancellationToken.None);

        source.ReleaseText("one");
        source.ReleaseText("two");

        var results = new[] { Encoding.UTF8.GetString((await first)!), Encoding.UTF8.GetString((await second)!) };
        Array.Sort(results, StringComparer.Ordinal);

        await Assert.That(results[0]).IsEqualTo("one");
        await Assert.That(results[1]).IsEqualTo("two");
    }

    /// <summary>Verifies <see cref="StreamSource.ReleasedChunks"/> and <see cref="StreamSource.ReadChunks"/> track releases and reads, with the terminal chunk excluded from the read count.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReleasedAndReadCountersTrackActivity()
    {
        const int ExpectedReleasedChunks = 3;
        const int ExpectedReadChunks = 2;
        var source = new StreamSource();
        _ = source.CreateContent(Serializer);

        source.ReleaseText("a");
        source.ReleaseText("b");
        source.Complete();

        await Assert.That(source.ReleasedChunks).IsEqualTo(ExpectedReleasedChunks);
        await Assert.That(source.ReadChunks).IsEqualTo(0);

        _ = await source.ReadChunkAsync(CancellationToken.None);
        await Assert.That(source.ReadChunks).IsEqualTo(1);

        _ = await source.ReadChunkAsync(CancellationToken.None);
        var end = await source.ReadChunkAsync(CancellationToken.None);

        await Assert.That(end).IsNull();
        await Assert.That(source.ReadChunks).IsEqualTo(ExpectedReadChunks);
    }
}
