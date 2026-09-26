// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Classifies how one multipart part is added to the generated <c>MultipartFormDataContent</c>.</summary>
/// <remarks>
/// Each value maps to one arm of the reflection request builder's <c>AddMultipartItem</c> dispatch, resolved statically
/// from the part's declared type. A declared class or struct goes through the content serializer (<see cref="Serialized"/>).
/// Only a part typed as <c>object</c>, an interface, or an open type parameter is not statically dispatchable, because
/// its runtime type decides the arm, so the whole method keeps using the reflection request builder.
/// </remarks>
internal enum MultipartPartKind
{
    /// <summary>The value is already an <see cref="System.Net.Http.HttpContent"/> and is added verbatim.</summary>
    HttpContent = 0,

    /// <summary>The value is a <c>Refit.MultipartItem</c> (or subclass) added via its <c>ToContent()</c>.</summary>
    MultipartItem = 1,

    /// <summary>The value is a <see cref="System.IO.Stream"/> wrapped in a <c>StreamContent</c>.</summary>
    Stream = 2,

    /// <summary>The value is a <see cref="string"/> wrapped in a <c>StringContent</c>.</summary>
    String = 3,

    /// <summary>The value is a <see cref="System.IO.FileInfo"/> opened into a <c>StreamContent</c>.</summary>
    FileInfo = 4,

    /// <summary>The value is a <see cref="byte"/> array wrapped in a <c>ByteArrayContent</c>.</summary>
    ByteArray = 5,

    /// <summary>The value is a date/time or <see cref="System.Guid"/> rendered by the form URL-encoded formatter.</summary>
    Formattable = 6,

    /// <summary>The value is a declared class or struct (for example a bool, enum, or DTO) written through the content
    /// serializer, matching the reflection builder's <c>AddSerializedMultipartItem</c> serializer fallback.</summary>
    Serialized = 7,
}
