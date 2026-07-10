// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Net;

namespace Refit;

/// <summary>
/// HTTP content that writes a sequence of values as JSON Lines (newline-delimited JSON): each element is
/// serialized with the configured <see cref="IHttpContentSerializer"/> and emitted on its own line.
/// </summary>
/// <seealso href="https://jsonlines.org"/>
public sealed class JsonLinesContent : HttpContent
{
    /// <summary>The single line-feed byte written between serialized elements.</summary>
    private static readonly byte[] LineSeparator = [(byte)'\n'];

    /// <summary>The sequence of values to serialize, one per line.</summary>
    private readonly IEnumerable _items;

    /// <summary>The serializer used to encode each element.</summary>
    private readonly IHttpContentSerializer _serializer;

    /// <summary>Initializes a new instance of the <see cref="JsonLinesContent"/> class.</summary>
    /// <param name="items">The sequence of values to serialize, one per line.</param>
    /// <param name="serializer">The serializer used to encode each element.</param>
    public JsonLinesContent(IEnumerable items, IHttpContentSerializer serializer)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Headers.ContentType = new(JsonLinesMediaType);
    }

    /// <summary>Gets the media type used for JSON Lines content.</summary>
    public static string JsonLinesMediaType { get; } = "application/x-ndjson";

    /// <inheritdoc/>
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var first = true;
        foreach (var item in _items)
        {
            if (!first)
            {
#if NET6_0_OR_GREATER
                await stream.WriteAsync(LineSeparator).ConfigureAwait(false);
#else
                await stream.WriteAsync(LineSeparator, 0, LineSeparator.Length).ConfigureAwait(false);
#endif
            }

            using var content = _serializer.ToHttpContent(item);
            await content.CopyToAsync(stream).ConfigureAwait(false);
            first = false;
        }
    }

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}
