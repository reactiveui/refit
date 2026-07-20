// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Default Url parameter formater.</summary>
public class DefaultUrlParameterFormatter : IUrlParameterFormatter
{
    /// <summary>
    /// Gets a value indicating whether this instance is the unmodified default formatter — not a derived type and
    /// with no registered formats — so generated request building may format statically-known values inline.
    /// </summary>
    internal bool IsPristineDefault =>
        GetType() == typeof(DefaultUrlParameterFormatter)
        && SpecificFormats.Count == 0
        && GeneralFormats.Count == 0;

    /// <summary>Gets the registered format strings keyed by container and parameter type.</summary>
    internal Dictionary<(Type containerType, Type parameterType), string> SpecificFormats { get; } = [];

    /// <summary>Gets the registered format strings keyed by parameter type.</summary>
    internal Dictionary<Type, string> GeneralFormats { get; } = [];

    /// <summary>
    /// Add format for specified parameter type contained within container class of specified type.
    /// Might be suppressed by a QueryAttribute format.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <typeparam name="TContainer">Container class type.</typeparam>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public void AddFormat<TContainer, TParameter>(string format) =>
        SpecificFormats.Add((typeof(TContainer), typeof(TParameter)), format);

    /// <summary>Add format for specified parameter type. Might be suppressed by a QueryAttribute format or a container specific format.</summary>
    /// <param name="format">The format string.</param>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public void AddFormat<TParameter>(string format) => GeneralFormats.Add(typeof(TParameter), format);

    /// <summary>Formats the specified parameter value.</summary>
    /// <param name="value">The parameter value.</param>
    /// <param name="attributeProvider">The attribute provider.</param>
    /// <param name="type">Container class type.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    /// <exception cref="ArgumentNullException">attributeProvider.</exception>
    public virtual string? Format(
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeProvider);

        if (value is null)
        {
            return null;
        }

        var queryAttribute = GetFirstQueryAttribute(attributeProvider);
        var formatString = queryAttribute?.Format;

        var parameterType = value.GetType();
        var enumMemberValue = EnumHelpers.GetEnumMemberValue(parameterType, value);

        formatString = ResolveFormatString(formatString, type, parameterType);

        return InvariantValueRenderer.Render(enumMemberValue ?? value, formatString);
    }

    /// <summary>Gets the first query attribute from an attribute provider.</summary>
    /// <param name="attributeProvider">The attribute provider to inspect.</param>
    /// <returns>The first query attribute, or null when absent.</returns>
    internal static QueryAttribute? GetFirstQueryAttribute(ICustomAttributeProvider attributeProvider)
    {
        // IsDefined avoids materializing an attribute array when none is present. That matters for a Type provider
        // (used when formatting collection elements), whose GetCustomAttributes allocates even for an empty result,
        // whereas a PropertyInfo or ParameterInfo already returns a cached empty array. When the attribute is
        // present, GetCustomAttributes is type-filtered, so the first element is the QueryAttribute.
        if (!attributeProvider.IsDefined(typeof(QueryAttribute), true))
        {
            return null;
        }

        var attributes = attributeProvider.GetCustomAttributes(typeof(QueryAttribute), true);
        return attributes[0] as QueryAttribute;
    }

    /// <summary>Selects the effective format string, preferring the attribute format, then specific, then general formats.</summary>
    /// <param name="formatString">The format string supplied by the query attribute, if any.</param>
    /// <param name="type">The container class type.</param>
    /// <param name="parameterType">The runtime type of the value.</param>
    /// <returns>The resolved format string, or null when none applies.</returns>
    internal string? ResolveFormatString(string? formatString, Type type, Type parameterType)
    {
        if (string.IsNullOrWhiteSpace(formatString) &&
            SpecificFormats.TryGetValue((type, parameterType), out var specificFormat))
        {
            formatString = specificFormat;
        }

        if (string.IsNullOrWhiteSpace(formatString) &&
            GeneralFormats.TryGetValue(parameterType, out var generalFormat))
        {
            formatString = generalFormat;
        }

        return formatString;
    }
}
