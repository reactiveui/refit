// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http;

namespace Refit;

/// <summary>Base type for multipart form items that carry a file name and optional content metadata.</summary>
public abstract class MultipartItem
{
    /// <summary>Initializes a new instance of the <see cref="MultipartItem"/> class.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    protected MultipartItem(string fileName, string? contentType)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType;
    }

    /// <summary>Initializes a new instance of the <see cref="MultipartItem"/> class.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    /// <param name="name">The name.</param>
    protected MultipartItem(string fileName, string? contentType, string? name)
        : this(fileName, contentType) =>
        Name = name;

    /// <summary>Gets the name.</summary>
    public string? Name { get; }

    /// <summary>Gets the type of the content.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the name of the file.</summary>
    public string FileName { get; }

    /// <summary>Converts the item to HTTP content.</summary>
    /// <returns>The HTTP content representing this item.</returns>
    public HttpContent ToContent()
    {
        var content = CreateContent();
        if (!string.IsNullOrEmpty(ContentType))
        {
            content.Headers.ContentType = new(ContentType);
        }

        return content;
    }

    /// <summary>Creates the underlying HTTP content for this item.</summary>
    /// <returns>The HTTP content for this item.</returns>
    protected abstract HttpContent CreateContent();
}
