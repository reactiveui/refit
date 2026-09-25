// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>
/// HTTP content that writes a typed sequence as JSON Lines (newline-delimited JSON): each element is serialized with
/// <see cref="IHttpContentSerializer.ToHttpContent{T}(T)"/> for <typeparamref name="T"/> and emitted on its own line.
/// </summary>
/// <typeparam name="T">The declared element type, passed to the serializer for every element.</typeparam>
/// <remarks>
/// <para>
/// The sequence is read lazily while the request body is written, one element at a time, so only the element being
/// written is held in memory. The content length is unknown, so the request is sent chunked.
/// </para>
/// <para>
/// When the content is sent with a cancellation token (.NET 8 and later), that token is passed to
/// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> and to every write. The enumerator is
/// always disposed, including when the send fails or is cancelled. Before waiting on an element the producer has not
/// finished, the lines already written are flushed so they reach the server without waiting for the next element.
/// </para>
/// <para>
/// Lines from an <see cref="IAsyncEnumerable{T}"/> each end with a line feed, so every line is complete the moment it
/// is written and a server reading line by line can act on it while the producer is still working. Lines from an
/// <see cref="IEnumerable{T}"/> are separated by line feeds with none after the last, byte for byte as
/// <see cref="JsonLinesContent"/> writes them.
/// </para>
/// <para>
/// Content built from an <see cref="IAsyncEnumerable{T}"/> can be sent once. Sending it again throws
/// <see cref="InvalidOperationException"/>: nothing is buffered to replay it, so a retry needs a new request built
/// from a fresh sequence. Content built from an <see cref="IEnumerable{T}"/> enumerates the sequence again on every
/// send, as <see cref="JsonLinesContent"/> does.
/// </para>
/// <para>
/// Each element is serialized as <typeparamref name="T"/>, not as its runtime type. A derived type's extra members
/// are written only if the serializer's configuration describes <typeparamref name="T"/> as polymorphic.
/// </para>
/// </remarks>
/// <seealso href="https://jsonlines.org"/>
[System.Diagnostics.DebuggerDisplay("JsonLinesContent<{typeof(T).Name,nq}>")]
public sealed class JsonLinesContent<T> : HttpContent
{
    /// <summary>The single line-feed byte written between serialized elements.</summary>
    private static readonly byte[] LineSeparator = [(byte)'\n'];

    /// <summary>The synchronous sequence, or <see langword="null"/> when the content wraps an asynchronous one.</summary>
    private readonly IEnumerable<T>? _items;

    /// <summary>The asynchronous sequence, or <see langword="null"/> when the content wraps a synchronous one.</summary>
    private readonly IAsyncEnumerable<T>? _asyncItems;

    /// <summary>The serializer used to encode each element.</summary>
    private readonly IHttpContentSerializer _serializer;

    /// <summary>Set to 1 once an asynchronous sequence has been enumerated.</summary>
    private int _asyncItemsTaken;

    /// <summary>Initializes a new instance of the <see cref="JsonLinesContent{T}"/> class over a synchronous sequence.</summary>
    /// <param name="items">The sequence of values to serialize, one per line. It is enumerated on every send.</param>
    /// <param name="serializer">The serializer used to encode each element.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> or <paramref name="serializer"/> is <see langword="null"/>.</exception>
    public JsonLinesContent(IEnumerable<T> items, IHttpContentSerializer serializer)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Headers.ContentType = new(JsonLinesContent.JsonLinesMediaType);
    }

    /// <summary>Initializes a new instance of the <see cref="JsonLinesContent{T}"/> class over an asynchronous sequence.</summary>
    /// <param name="items">The sequence of values to serialize, one per line. It is enumerated once; the content cannot be sent again.</param>
    /// <param name="serializer">The serializer used to encode each element.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> or <paramref name="serializer"/> is <see langword="null"/>.</exception>
    public JsonLinesContent(IAsyncEnumerable<T> items, IHttpContentSerializer serializer)
    {
        _asyncItems = items ?? throw new ArgumentNullException(nameof(items));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Headers.ContentType = new(JsonLinesContent.JsonLinesMediaType);
    }

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeCoreAsync(stream, CancellationToken.None);

#if NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
        SerializeCoreAsync(stream, cancellationToken);
#endif

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }

    /// <summary>Advances the producer, flushing written lines first when the next element is not ready yet.</summary>
    /// <param name="enumerator">The producer's enumerator.</param>
    /// <param name="stream">The request body stream.</param>
    /// <param name="unflushed">Whether lines have been written since the last flush.</param>
    /// <param name="cancellationToken">The send's cancellation token.</param>
    /// <returns><see langword="true"/> when the producer yielded another element.</returns>
    /// <remarks>
    /// The flush runs while the producer works. If it fails, the producer's step is still awaited before the
    /// exception propagates, so the enumerator is never disposed while <c>MoveNextAsync</c> is in flight.
    /// </remarks>
    private static async ValueTask<bool> MoveNextAsync(
        IAsyncEnumerator<T> enumerator,
        Stream stream,
        bool unflushed,
        CancellationToken cancellationToken)
    {
        var pending = enumerator.MoveNextAsync();
        if (!unflushed || pending.IsCompleted)
        {
            return await pending.ConfigureAwait(false);
        }

        var moveNext = pending.AsTask();
        try
        {
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await moveNext.ContinueWith(static _ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).ConfigureAwait(false);
            throw;
        }

        return await moveNext.ConfigureAwait(false);
    }

    /// <summary>Writes the line-feed byte that separates or terminates lines.</summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the byte has been written.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task WriteLineSeparatorAsync(Stream stream, CancellationToken cancellationToken) =>
#if NET8_0_OR_GREATER
        stream.WriteAsync(LineSeparator, cancellationToken).AsTask();
#else
        stream.WriteAsync(LineSeparator, 0, LineSeparator.Length, cancellationToken);
#endif

    /// <summary>Serializes one element as <typeparamref name="T"/> into the request body.</summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="item">The element to serialize.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the element has been written.</returns>
    private async Task WriteItemAsync(Stream stream, T item, CancellationToken cancellationToken)
    {
        using var content = _serializer.ToHttpContent(item);
#if NET8_0_OR_GREATER
        await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        await content.CopyToAsync(stream).ConfigureAwait(false);
#endif
    }

    /// <summary>Writes the wrapped sequence to the request body.</summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="cancellationToken">The send's cancellation token.</param>
    /// <returns>A task that completes when the sequence has been written.</returns>
    /// <exception cref="InvalidOperationException">The content wraps an asynchronous sequence that has already been sent.</exception>
    private Task SerializeCoreAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (_items is not null)
        {
            return WriteItemsAsync(_items, stream, cancellationToken);
        }

        if (Interlocked.Exchange(ref _asyncItemsTaken, 1) != 0)
        {
            throw new InvalidOperationException(
                $"This {nameof(JsonLinesContent)}<{typeof(T).Name}> wraps an IAsyncEnumerable<{typeof(T).Name}> and has already been sent. "
                + "It is not buffered for a repeat send; build a new request from a fresh sequence.");
        }

        return WriteAsyncItemsAsync(_asyncItems!, stream, cancellationToken);
    }

    /// <summary>Writes a synchronous sequence, one element per line.</summary>
    /// <param name="items">The sequence to write.</param>
    /// <param name="stream">The request body stream.</param>
    /// <param name="cancellationToken">The send's cancellation token.</param>
    /// <returns>A task that completes when the sequence has been written.</returns>
    private async Task WriteItemsAsync(IEnumerable<T> items, Stream stream, CancellationToken cancellationToken)
    {
        var first = true;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!first)
            {
                await WriteLineSeparatorAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            await WriteItemAsync(stream, item, cancellationToken).ConfigureAwait(false);
            first = false;
        }
    }

    /// <summary>Writes an asynchronous sequence, one element per line, flushing before waiting on the producer.</summary>
    /// <param name="items">The sequence to write.</param>
    /// <param name="stream">The request body stream.</param>
    /// <param name="cancellationToken">The send's cancellation token, also passed to the producer.</param>
    /// <returns>A task that completes when the sequence has been written.</returns>
    private async Task WriteAsyncItemsAsync(IAsyncEnumerable<T> items, Stream stream, CancellationToken cancellationToken)
    {
        var enumerator = items.GetAsyncEnumerator(cancellationToken);
        try
        {
            var unflushed = false;
            while (await MoveNextAsync(enumerator, stream, unflushed, cancellationToken).ConfigureAwait(false))
            {
                // Terminate every line, so a server reading line by line can act on it before the next one exists.
                await WriteItemAsync(stream, enumerator.Current, cancellationToken).ConfigureAwait(false);
                await WriteLineSeparatorAsync(stream, cancellationToken).ConfigureAwait(false);
                unflushed = true;
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
