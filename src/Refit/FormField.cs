// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.ComponentModel;

namespace Refit;

/// <summary>
/// Describes a single form-url-encoded field for a strongly-typed body, allowing source-generated code to
/// flatten a form body without reflection. The getter and all naming metadata are resolved at compile time
/// by the Refit source generator; the runtime only formats the value using the configured
/// <see cref="RefitSettings"/>.
/// </summary>
/// <typeparam name="TBody">The declared body type the field belongs to.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FormField<TBody>
{
    /// <summary>Initializes a new instance of the <see cref="FormField{TBody}"/> class.</summary>
    /// <param name="getter">Reads the property value from a body instance without reflection.</param>
    /// <param name="clrName">The declared CLR property name, used when no explicit name is configured.</param>
    /// <param name="explicitName">The explicit field name from <c>[AliasAs]</c> or <c>[JsonPropertyName]</c>, or <see langword="null"/>.</param>
    /// <param name="prefixSegment">The precomputed <c>prefix + delimiter</c> prepended to the field name, or <see langword="null"/> when no prefix applies.</param>
    /// <param name="format">The <see cref="QueryAttribute.Format"/> applied to the value, or <see langword="null"/>.</param>
    /// <param name="collectionFormat">The explicit <see cref="CollectionFormat"/>, or <see langword="null"/> to use the settings default.</param>
    /// <param name="serializeNull">Whether a <see langword="null"/> value should be serialized as an empty field instead of omitted.</param>
    public FormField(
        Func<TBody, object?> getter,
        string clrName,
        string? explicitName,
        string? prefixSegment,
        string? format,
        CollectionFormat? collectionFormat,
        bool serializeNull)
    {
        Getter = getter;
        ClrName = clrName;
        ExplicitName = explicitName;
        PrefixSegment = prefixSegment;
        Format = format;
        CollectionFormat = collectionFormat;
        SerializeNull = serializeNull;
    }

    /// <summary>Gets the delegate that reads the property value from a body instance.</summary>
    public Func<TBody, object?> Getter { get; }

    /// <summary>Gets the declared CLR property name.</summary>
    public string ClrName { get; }

    /// <summary>Gets the explicit field name from <c>[AliasAs]</c> or <c>[JsonPropertyName]</c>, if any.</summary>
    public string? ExplicitName { get; }

    /// <summary>Gets the precomputed <c>prefix + delimiter</c> prepended to the field name, if any.</summary>
    public string? PrefixSegment { get; }

    /// <summary>Gets the value format string, if any.</summary>
    public string? Format { get; }

    /// <summary>Gets the explicit collection format, or <see langword="null"/> to use the settings default.</summary>
    public CollectionFormat? CollectionFormat { get; }

    /// <summary>Gets a value indicating whether a <see langword="null"/> value should be serialized as an empty field.</summary>
    public bool SerializeNull { get; }

    /// <summary>Resolves the form field name, applying the key formatter and any prefix.</summary>
    /// <param name="urlParameterKeyFormatter">The formatter applied to the CLR name when no explicit name is set.</param>
    /// <returns>The resolved field name.</returns>
    public string? ResolveFieldName(IUrlParameterKeyFormatter urlParameterKeyFormatter)
    {
        var name = ExplicitName ?? urlParameterKeyFormatter.Format(ClrName);
        return PrefixSegment is null ? name : PrefixSegment + name;
    }
}
