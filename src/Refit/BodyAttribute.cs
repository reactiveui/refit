// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit;

/// <summary>Set a parameter to be sent as the HTTP request's body.</summary>
/// <remarks>
/// There are four behaviors when sending a parameter as the request body:<br/>
/// - If the type is/implements <see cref="System.IO.Stream"/>, the content will be streamed via <see cref="StreamContent"/>.<br/>
/// - If the type is <see cref="string"/>, it will be used directly as the content unless <c>[Body(BodySerializationMethod.Json)]</c> is set
/// which will send it as a <see cref="StringContent"/>.<br/>
/// - If the parameter has the attribute <c>[Body(BodySerializationMethod.UrlEncoded)]</c>, the content will be URL-encoded.<br/>
/// - For all other types, the object will be serialized using the content serializer specified in the request's <see cref="RefitSettings"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BodyAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    public BodyAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="buffered">if set to <c>true</c> [buffered].</param>
    public BodyAttribute(bool buffered) => Buffered = buffered;

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="serializationMethod">The serialization method.</param>
    /// <param name="buffered">if set to <c>true</c> [buffered].</param>
    public BodyAttribute(BodySerializationMethod serializationMethod, bool buffered)
    {
        SerializationMethod = serializationMethod;
        Buffered = buffered;
    }

    /// <summary>Initializes a new instance of the <see cref="BodyAttribute"/> class.</summary>
    /// <param name="serializationMethod">The serialization method.</param>
    public BodyAttribute(BodySerializationMethod serializationMethod) =>
        SerializationMethod = serializationMethod;

    /// <summary>Gets the buffered.</summary>
    /// <value>
    /// The buffered.
    /// </value>
    public bool? Buffered { get; }

    /// <summary>Gets the serialization method.</summary>
    /// <value>
    /// The serialization method.
    /// </value>
    public BodySerializationMethod SerializationMethod { get; } =
        BodySerializationMethod.Default;
}
