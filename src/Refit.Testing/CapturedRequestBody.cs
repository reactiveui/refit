// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Refit.Testing;

/// <summary>The recorded body of one request: complete when fully buffered, or filled as the reply code reads it.</summary>
internal sealed class CapturedRequestBody
{
    /// <summary>Guards the recorded bytes and state.</summary>
    private readonly Lock _gate = new();

    /// <summary>The recorded bytes.</summary>
    private readonly List<byte> _bytes = [];

    /// <summary>The most bytes to record, or <see langword="null"/> for no limit.</summary>
    private readonly int? _limit;

    /// <summary>Whether the reader reached the end of the body.</summary>
    private bool _completed;

    /// <summary>Whether the body exceeded <see cref="_limit"/>.</summary>
    private bool _truncated;

    /// <summary>Initializes a new instance of the <see cref="CapturedRequestBody"/> class.</summary>
    /// <param name="mediaType">The request content media type, if any.</param>
    /// <param name="limit">The most bytes to record, or <see langword="null"/> for no limit.</param>
    internal CapturedRequestBody(string? mediaType, int? limit)
    {
        MediaType = mediaType;
        _limit = limit;
    }

    /// <summary>Gets the request content media type, if any.</summary>
    internal string? MediaType { get; }

    /// <summary>Records bytes as they are read, keeping at most the limit.</summary>
    /// <param name="bytes">The bytes that passed through.</param>
    internal void Append(ReadOnlySpan<byte> bytes)
    {
        lock (_gate)
        {
            var room = _limit is { } limit ? limit - _bytes.Count : bytes.Length;
            if (bytes.Length > room)
            {
                _truncated = true;
                bytes = bytes[..room];
            }

            foreach (var value in bytes)
            {
                _bytes.Add(value);
            }
        }
    }

    /// <summary>Marks the body as read to the end.</summary>
    internal void Complete()
    {
        lock (_gate)
        {
            _completed = true;
        }
    }

    /// <summary>Returns the recorded text when the whole body was recorded.</summary>
    /// <returns>The body text decoded as UTF-8.</returns>
    /// <exception cref="InvalidOperationException">The body exceeded the limit or has not been read to the end.</exception>
    internal string GetText()
    {
        lock (_gate)
        {
            if (_truncated)
            {
                throw new InvalidOperationException(
                    $"The request body exceeded the capture limit of {_limit} bytes; raise RequestCapture.Bounded or use RequestCapture.Full.");
            }

            if (!_completed)
            {
                throw new InvalidOperationException(
                    "The request body has not been read to the end; with bounded capture the reply code must consume the body before it can be inspected.");
            }

            return Encoding.UTF8.GetString(_bytes.ToArray());
        }
    }
}
