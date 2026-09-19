// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation;

/// <summary>Provides an explicit JSON name for the reflected naming hook.</summary>
internal sealed class JsonNamedValue
{
    /// <summary>Gets the value whose explicit field name is returned by the serializer.</summary>
    [JsonPropertyName("wire-name")]
    public int Value { get; init; }
}
