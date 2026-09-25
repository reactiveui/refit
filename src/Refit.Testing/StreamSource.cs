// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Testing;

/// <summary>
/// A response body the test releases one chunk at a time. Reply with it through <see cref="Reply.Stream(StreamSource)"/>:
/// the handler returns the response headers at once, and the client reads each chunk only after the test releases it.
/// </summary>
/// <remarks>
/// <para>
/// A reader waits, without a timer, until the test calls <see cref="Release{T}(T)"/>, <see cref="ReleaseText(string)"/>,
/// <see cref="ReleaseBytes(byte[])"/>, <see cref="Complete"/>, <see cref="Disconnect()"/> or <see cref="Fail(Exception)"/>.
/// A body that is never released is a response that sends its headers and then stalls.
/// </para>
/// <para>
/// Typed items are framed for <see cref="Format"/> and serialized with the client's content serializer when the client
/// reads them. <see cref="Closed"/> completes when the client disposes the response or its body stream, which is how a
/// test observes that cancellation or disposal closed the response. A source feeds exactly one response.
/// </para>
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{Format} ReleasedChunks = {ReleasedChunks}")]
public sealed class StreamSource
{
    /// <summary>The server-sent events field prefix written before each line of an item.</summary>
    private const string EventDataPrefix = "data: ";

    /// <summary>The line feed written after a JSON Lines item and each server-sent events field.</summary>
    private const string LineFeed = "\n";

    /// <summary>Guards the queue and state fields.</summary>
    private readonly Lock _gate = new();

    /// <summary>The released chunks not yet handed to the reader.</summary>
    private readonly Queue<Chunk> _pending = new();

    /// <summary>Completes when the reader disposes the body.</summary>
    private readonly TaskCompletionSource<bool> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The waiter a reader parks on while no chunk is pending.</summary>
    private TaskCompletionSource<bool>? _available;

    /// <summary>The serializer bound when the source became a response body.</summary>
    private IHttpContentSerializer? _serializer;

    /// <summary>The number of typed items released, used for JSON array separators.</summary>
    private int _items;

    /// <summary>The number of chunks released.</summary>
    private int _released;

    /// <summary>The number of chunks the reader has taken.</summary>
    private int _read;

    /// <summary>Whether the test ended the body with <see cref="Complete"/>, <see cref="Disconnect()"/> or <see cref="Fail(Exception)"/>.</summary>
    private bool _ended;

    /// <summary>Initializes a new instance of the <see cref="StreamSource"/> class for JSON Lines.</summary>
    public StreamSource()
        : this(StreamingContentFormat.JsonLines)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StreamSource"/> class.</summary>
    /// <param name="format">The framing applied to typed items; it also selects the response content type.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined format.</exception>
    public StreamSource(StreamingContentFormat format)
    {
        MediaType = format switch
        {
            StreamingContentFormat.JsonArray => "application/json",
            StreamingContentFormat.JsonLines => JsonLinesContent.JsonLinesMediaType,
            StreamingContentFormat.ServerSentEvents => "text/event-stream",
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        Format = format;
    }

    /// <summary>Gets the framing applied to typed items.</summary>
    public StreamingContentFormat Format { get; }

    /// <summary>Gets the response content type for <see cref="Format"/>.</summary>
    public string MediaType { get; }

    /// <summary>Gets the number of chunks released so far, including the end or failure of the body.</summary>
    public int ReleasedChunks
    {
        get
        {
            lock (_gate)
            {
                return _released;
            }
        }
    }

    /// <summary>Gets the number of released data chunks the client has started to read; the end or failure of the body is not counted.</summary>
    public int ReadChunks
    {
        get
        {
            lock (_gate)
            {
                return _read;
            }
        }
    }

    /// <summary>Gets a task that completes when the client disposes the response or its body stream.</summary>
    public Task Closed => _closed.Task;

    /// <summary>Gets a value indicating whether the client has disposed the response or its body stream.</summary>
    public bool IsClosed => _closed.Task.IsCompleted;

    /// <summary>Releases one typed item, framed for <see cref="Format"/> and serialized by the client's serializer.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item">The item to send.</param>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    public void Release<T>(T item)
    {
        lock (_gate)
        {
            var prefix = Format switch
            {
                StreamingContentFormat.JsonArray => _items == 0 ? "[" : ",",
                _ => string.Empty,
            };
            _items++;
            Enqueue(new(serializer => SerializeAsync(serializer, item, prefix, Format)));
        }
    }

    /// <summary>Releases raw UTF-8 text, sent as-is without framing.</summary>
    /// <param name="text">The text to send.</param>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    public void ReleaseText(string text)
    {
        ArgumentExceptionHelper.ThrowIfNull(text);
        ReleaseBytes(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>Releases raw bytes, sent as-is without framing.</summary>
    /// <param name="bytes">The bytes to send.</param>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    public void ReleaseBytes(byte[] bytes)
    {
        ArgumentExceptionHelper.ThrowIfNull(bytes);
        lock (_gate)
        {
            Enqueue(new(bytes));
        }
    }

    /// <summary>Ends the body normally. A JSON array is closed first; the reader then sees the end of the stream.</summary>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    public void Complete()
    {
        lock (_gate)
        {
            if (Format == StreamingContentFormat.JsonArray)
            {
                Enqueue(new(Encoding.UTF8.GetBytes(_items == 0 ? "[]" : "]")));
            }

            End(Chunk.EndOfStream);
        }
    }

    /// <summary>
    /// Simulates the connection dropping after the chunks released so far: the reader receives them, then its next
    /// read throws an <see cref="IOException"/> (an <c>HttpIOException</c> on .NET 8 and later).
    /// </summary>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disconnect() =>
#if NET8_0_OR_GREATER
        Fail(new HttpIOException(HttpRequestError.ResponseEnded, "Refit.Testing simulated disconnect."));
#else
        Fail(new IOException("Refit.Testing simulated disconnect."));
#endif

    /// <summary>Ends the body with an error: the reader receives the chunks released so far, then its next read throws <paramref name="error"/>.</summary>
    /// <param name="error">The exception the reader receives.</param>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    public void Fail(Exception error)
    {
        ArgumentExceptionHelper.ThrowIfNull(error);
        lock (_gate)
        {
            End(new(error));
        }
    }

    /// <summary>Binds the source to a serializer and returns the response body that reads it.</summary>
    /// <param name="serializer">The client's content serializer, used for typed items.</param>
    /// <returns>The response content.</returns>
    /// <exception cref="InvalidOperationException">The source already feeds a response.</exception>
    internal HttpContent CreateContent(IHttpContentSerializer serializer)
    {
        lock (_gate)
        {
            if (_serializer is not null)
            {
                throw new InvalidOperationException("A StreamSource feeds exactly one response; create a new source for each request.");
            }

            _serializer = serializer;
        }

        return new StreamSourceContent(this);
    }

    /// <summary>Waits for the next released chunk and converts it to bytes.</summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>The chunk bytes, or <see langword="null"/> at the end of the body.</returns>
    /// <exception cref="ObjectDisposedException">The body was closed.</exception>
    internal async Task<byte[]?> ReadChunkAsync(CancellationToken cancellationToken)
    {
        var chunk = TryTake(cancellationToken, out var available);
        while (chunk is null)
        {
            await WaitAsync(available!.Task, cancellationToken).ConfigureAwait(false);
            chunk = TryTake(cancellationToken, out available);
        }

        return await chunk.ToBytesAsync(_serializer!).ConfigureAwait(false);
    }

    /// <summary>Marks the body closed by the reader and wakes a parked read.</summary>
    internal void Close()
    {
        TaskCompletionSource<bool>? available;
        lock (_gate)
        {
            available = _available;
            _available = null;
        }

        _ = _closed.TrySetResult(true);
        _ = available?.TrySetResult(true);
    }

    /// <summary>Serializes a typed item and applies the framing for <paramref name="format"/>.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="serializer">The client's content serializer.</param>
    /// <param name="item">The item.</param>
    /// <param name="prefix">The JSON array bracket or separator written before the item, if any.</param>
    /// <param name="format">The framing to apply.</param>
    /// <returns>The framed item bytes.</returns>
    private static async Task<byte[]?> SerializeAsync<T>(IHttpContentSerializer serializer, T item, string prefix, StreamingContentFormat format)
    {
        using var content = serializer.ToHttpContent(item);
        var json = await content.ReadAsStringAsync().ConfigureAwait(false);
        var framed = format switch
        {
            StreamingContentFormat.JsonLines => json + LineFeed,
            StreamingContentFormat.ServerSentEvents => FrameEvent(json),
            _ => prefix + json,
        };
        return Encoding.UTF8.GetBytes(framed);
    }

    /// <summary>Frames a JSON payload as one server-sent event, prefixing every payload line with <c>data:</c>.</summary>
    /// <param name="json">The serialized payload.</param>
    /// <returns>The event text, terminated by a blank line.</returns>
    private static string FrameEvent(string json)
    {
        var builder = new StringBuilder();
        foreach (var line in json.Replace("\r\n", LineFeed).Split('\n'))
        {
            _ = builder.Append(EventDataPrefix).Append(line).Append('\n');
        }

        return builder.Append('\n').ToString();
    }

    /// <summary>Waits for a task without cancelling it, so a shared waiter survives a cancelled read.</summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A token that abandons the wait.</param>
    /// <returns>A task that completes with <paramref name="task"/> or is cancelled by <paramref name="cancellationToken"/>.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled first.</exception>
#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task WaitAsync(Task task, CancellationToken cancellationToken) => task.WaitAsync(cancellationToken);
#else
    private static async Task WaitAsync(Task task, CancellationToken cancellationToken)
    {
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancelled))
        {
            if (await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false) != task)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
#endif

    /// <summary>Takes the next pending chunk, or returns the waiter to park on when none is pending.</summary>
    /// <param name="cancellationToken">A token checked before taking.</param>
    /// <param name="available">The waiter completed by the next release, set when no chunk is pending.</param>
    /// <returns>The next chunk; a terminal chunk stays queued. <see langword="null"/> when none is pending.</returns>
    /// <exception cref="ObjectDisposedException">The body was closed.</exception>
    private Chunk? TryTake(CancellationToken cancellationToken, out TaskCompletionSource<bool>? available)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(IsClosed, this);
#else
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(StreamSource));
            }
#endif

            if (_pending.Count == 0)
            {
                available = _available ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
                return null;
            }

            available = null;
            var chunk = _pending.Peek();
            if (!chunk.IsTerminal)
            {
                _ = _pending.Dequeue();
                _read++;
            }

            return chunk;
        }
    }

    /// <summary>Queues a terminal chunk.</summary>
    /// <param name="chunk">The end-of-stream or failure chunk.</param>
    private void End(Chunk chunk)
    {
        Enqueue(chunk);
        _ended = true;
    }

    /// <summary>Queues a chunk and wakes a parked reader. Called under <see cref="_gate"/>.</summary>
    /// <param name="chunk">The chunk to queue.</param>
    /// <exception cref="InvalidOperationException">The body was already ended.</exception>
    private void Enqueue(Chunk chunk)
    {
        if (_ended)
        {
            throw new InvalidOperationException("The stream body has already been completed, disconnected or failed.");
        }

        _pending.Enqueue(chunk);
        _released++;
        var available = _available;
        _available = null;
        _ = available?.TrySetResult(true);
    }

    /// <summary>One released piece of the body: bytes, a typed item, the end of the body or a failure.</summary>
    private sealed class Chunk
    {
        /// <summary>The shared end-of-stream marker.</summary>
        internal static readonly Chunk EndOfStream = new((byte[]?)null);

        /// <summary>The raw bytes, or <see langword="null"/> for a typed item or a terminal chunk.</summary>
        private readonly byte[]? _bytes;

        /// <summary>The deferred serialization of a typed item.</summary>
        private readonly Func<IHttpContentSerializer, Task<byte[]?>>? _item;

        /// <summary>The failure delivered to the reader.</summary>
        private readonly Exception? _error;

        /// <summary>Initializes a new instance of the <see cref="Chunk"/> class holding bytes, or ending the body when <see langword="null"/>.</summary>
        /// <param name="bytes">The raw bytes.</param>
        internal Chunk(byte[]? bytes) => _bytes = bytes;

        /// <summary>Initializes a new instance of the <see cref="Chunk"/> class holding a typed item.</summary>
        /// <param name="item">The deferred serialization.</param>
        internal Chunk(Func<IHttpContentSerializer, Task<byte[]?>> item) => _item = item;

        /// <summary>Initializes a new instance of the <see cref="Chunk"/> class that fails the body.</summary>
        /// <param name="error">The failure.</param>
        internal Chunk(Exception error) => _error = error;

        /// <summary>Gets a value indicating whether this chunk ends the body; a terminal chunk stays queued for later reads.</summary>
        internal bool IsTerminal => _bytes is null && _item is null;

        /// <summary>Converts the chunk to bytes.</summary>
        /// <param name="serializer">The serializer for a typed item.</param>
        /// <returns>The bytes, or <see langword="null"/> at the end of the body.</returns>
        /// <exception cref="Exception">The failure this chunk carries.</exception>
        internal Task<byte[]?> ToBytesAsync(IHttpContentSerializer serializer)
        {
            if (_error is not null)
            {
                throw _error;
            }

            return _item is null ? Task.FromResult(_bytes) : _item(serializer);
        }
    }
}
