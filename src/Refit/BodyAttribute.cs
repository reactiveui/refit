// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Set a parameter to be sent as the HTTP request's body.</summary>
/// <remarks>
/// Supplied HttpContent is used directly. Supplied streams become stream content and remain owned by the caller.
/// Under the default serialization method, strings become unquoted text content.
/// BodySerializationMethod.Serialized uses the configured content serializer, including for strings.
/// BodySerializationMethod.UrlEncoded writes a form body. BodySerializationMethod.JsonLines writes an enumerable as one serialized item per line.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("Body: Buffered = {Buffered}")]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BodyAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    public BodyAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="buffered">True to buffer the body before sending; false to skip that buffering step.</param>
    public BodyAttribute(bool buffered) => Buffered = buffered;

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="serializationMethod">The serialization method.</param>
    /// <param name="buffered">True to buffer the body before sending; false to skip that buffering step.</param>
    public BodyAttribute(BodySerializationMethod serializationMethod, bool buffered)
    {
        SerializationMethod = serializationMethod;
        Buffered = buffered;
    }

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="serializationMethod">The serialization method.</param>
    public BodyAttribute(BodySerializationMethod serializationMethod) =>
        SerializationMethod = serializationMethod;

    /// <summary>Gets whether to buffer the body before sending, or null to use RefitSettings.Buffered.</summary>
    public bool? Buffered { get; }

    /// <summary>Gets the serialization method.</summary>
    /// <value>
    /// The serialization method.
    /// </value>
    public BodySerializationMethod SerializationMethod { get; } =
        BodySerializationMethod.Default;

    /// <summary>Gets or sets the content coding applied to this body, overriding <see cref="RefitSettings.RequestCompression"/>.</summary>
    /// <remarks>
    /// Leave unset to follow the settings. <see cref="RequestCompression.None"/> opts this method out of a coding the
    /// settings turned on. Not every coding exists on every target framework - see <see cref="RequestCompression"/>.
    /// </remarks>
    public RequestCompression Compression { get; set; } = RequestCompression.Default;

    /// <summary>Gets or sets how hard <see cref="Compression"/> compresses (defaults to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>).</summary>
    /// <remarks>Read only when <see cref="Compression"/> names a coding; otherwise the settings supply the level too.</remarks>
    public System.IO.Compression.CompressionLevel CompressionLevel { get; set; } =
        System.IO.Compression.CompressionLevel.Optimal;
}
