// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Defines methods to serialize HTTP requests' bodies.</summary>
public enum BodySerializationMethod
{
    /// <summary>Encodes everything using the ContentSerializer in RefitSettings except for strings. Strings are set as-is.</summary>
    Default,

    /// <summary>Json encodes everything, including strings.</summary>
    [Obsolete("Use BodySerializationMethod.Serialized instead", false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2310:Deprecated code should be removed",
        Justification = "Public API retained for backwards compatibility; cannot remove without a breaking change.")]
    Json,

    /// <summary>Form-UrlEncode's the values.</summary>
    UrlEncoded,

    /// <summary>Encodes everything using the ContentSerializer in RefitSettings.</summary>
    Serialized,

    /// <summary>
    /// Encodes an enumerable body as JSON Lines (newline-delimited JSON): each element is serialized
    /// with the ContentSerializer and emitted on its own line. See <see href="https://jsonlines.org"/>.
    /// </summary>
    JsonLines
}
