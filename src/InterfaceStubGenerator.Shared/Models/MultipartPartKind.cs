// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Classifies how one multipart part is added to the generated <c>MultipartFormDataContent</c>.</summary>
/// <remarks>
/// Each value maps to one arm of the reflection request builder's <c>AddMultipartItem</c> dispatch, resolved statically
/// from the part's declared type. There is deliberately no serialize arm: a part whose declared type would fall through
/// to the content serializer is not statically dispatchable with byte parity, so the whole method keeps using the
/// reflection request builder instead.
/// </remarks>
internal enum MultipartPartKind
{
    /// <summary>The value is already an <see cref="System.Net.Http.HttpContent"/> and is added verbatim.</summary>
    HttpContent,

    /// <summary>The value is a <c>Refit.MultipartItem</c> (or subclass) added via its <c>ToContent()</c>.</summary>
    MultipartItem,

    /// <summary>The value is a <see cref="System.IO.Stream"/> wrapped in a <c>StreamContent</c>.</summary>
    Stream,

    /// <summary>The value is a <see cref="string"/> wrapped in a <c>StringContent</c>.</summary>
    String,

    /// <summary>The value is a <see cref="System.IO.FileInfo"/> opened into a <c>StreamContent</c>.</summary>
    FileInfo,

    /// <summary>The value is a <see cref="byte"/> array wrapped in a <c>ByteArrayContent</c>.</summary>
    ByteArray,

    /// <summary>The value is a date/time or <see cref="System.Guid"/> rendered by the form URL-encoded formatter.</summary>
    Formattable,

    /// <summary>The value is a sealed or value type (a bool, enum, or sealed DTO) written through the content serializer,
    /// matching the reflection builder's <c>AddSerializedMultipartItem</c> serializer fallback.</summary>
    Serialized
}
