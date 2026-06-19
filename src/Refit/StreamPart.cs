// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit;

/// <summary>Allows the use of a generic <see cref="Stream"/> in a multipart form body.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StreamPart"/> class.
/// </remarks>
/// <param name="value">The value.</param>
/// <param name="fileName">Name of the file.</param>
/// <param name="contentType">Type of the content.</param>
/// <param name="name">The name.</param>
/// <exception cref="System.ArgumentNullException">value</exception>
public class StreamPart(
    Stream value,
    string fileName,
    string? contentType = null,
    string? name = null) : MultipartItem(fileName, contentType, name)
{
    /// <summary>Gets the stream value.</summary>
    public Stream Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    /// <inheritdoc/>
    protected override HttpContent CreateContent() => new StreamContent(Value);
}
