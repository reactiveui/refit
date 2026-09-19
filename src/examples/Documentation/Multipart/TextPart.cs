// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Creates fresh text content using the base class's metadata handling.</summary>
internal sealed class TextPart : MultipartItem
{
    /// <summary>Provides the text used by each fresh content instance.</summary>
    private readonly string _text;

    /// <summary>Initializes a new instance of the <see cref="TextPart"/> class without a name or media-type override.</summary>
    /// <param name="text">The text to encode as UTF-8.</param>
    /// <param name="fileName">The transmitted name, independent of a filesystem path.</param>
    internal TextPart(string text, string fileName)
        : base(fileName, null) => _text = text;

    /// <summary>Initializes a new instance of the <see cref="TextPart"/> class with explicit multipart metadata.</summary>
    /// <param name="text">The text to encode as UTF-8.</param>
    /// <param name="fileName">The transmitted name; empty allows parameter-name fallback.</param>
    /// <param name="contentType">The media type applied after creating content.</param>
    /// <param name="name">The field name that overrides the parameter alias.</param>
    internal TextPart(string text, string fileName, string? contentType, string? name)
        : base(fileName, contentType, name) => _text = text;

    /// <inheritdoc/>
    protected override HttpContent CreateContent() => new StringContent(_text);
}
