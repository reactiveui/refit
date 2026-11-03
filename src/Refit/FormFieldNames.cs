// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.ComponentModel;

namespace Refit;

/// <summary>The compile-time names a form field can take, resolved by the Refit source generator.</summary>
/// <param name="ExplicitName">The <c>[AliasAs]</c> or <c>[Query]</c> name, which wins over every serializer name, or <see langword="null"/>.</param>
/// <param name="JsonName">The <c>[JsonPropertyName]</c> name used with a System.Text.Json serializer, or <see langword="null"/>.</param>
/// <param name="NewtonsoftName">The Newtonsoft.Json <c>[JsonProperty]</c> name, or <see langword="null"/>.</param>
/// <param name="XmlName">The <c>[XmlElement]</c> or <c>[XmlAttribute]</c> name, or <see langword="null"/>.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // The only uncovered members are the compiler-generated record equality and formatting.
public readonly record struct FormFieldNames(
    string? ExplicitName,
    string? JsonName,
    string? NewtonsoftName,
    string? XmlName)
{
    /// <summary>Picks the serializer-specific name for the given attribute family.</summary>
    /// <param name="nameStyle">The attribute family the configured content serializer reads names from.</param>
    /// <returns>The serializer name, or <see langword="null"/> when the property has none for that family.</returns>
    public string? ForStyle(ContentSerializerNameStyle nameStyle) =>
        nameStyle switch
        {
            ContentSerializerNameStyle.Newtonsoft => NewtonsoftName,
            ContentSerializerNameStyle.Xml => XmlName,
            _ => JsonName,
        };
}
