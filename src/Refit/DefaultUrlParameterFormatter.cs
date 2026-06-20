// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Refit;

/// <summary>Default Url parameter formater.</summary>
public class DefaultUrlParameterFormatter : IUrlParameterFormatter
{
    /// <summary>Caches resolved enum member attributes keyed by enum type and member name.</summary>
    private static readonly ConcurrentDictionary<
        Type,
        ConcurrentDictionary<string, EnumMemberAttribute?>
    > _enumMemberCache = new();

    /// <summary>Gets the registered format strings keyed by container and parameter type.</summary>
    private Dictionary<(Type containerType, Type parameterType), string> SpecificFormats { get; } = [];

    /// <summary>Gets the registered format strings keyed by parameter type.</summary>
    private Dictionary<Type, string> GeneralFormats { get; } = [];

    /// <summary>
    /// Add format for specified parameter type contained within container class of specified type.
    /// Might be suppressed by a QueryAttribute format.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <typeparam name="TContainer">Container class type.</typeparam>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    public void AddFormat<TContainer, TParameter>(string format) =>
        SpecificFormats.Add((typeof(TContainer), typeof(TParameter)), format);

    /// <summary>Add format for specified parameter type. Might be suppressed by a QueryAttribute format or a container specific format.</summary>
    /// <param name="format">The format string.</param>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    public void AddFormat<TParameter>(string format) => GeneralFormats.Add(typeof(TParameter), format);

    /// <summary>Formats the specified parameter value.</summary>
    /// <param name="value">The parameter value.</param>
    /// <param name="attributeProvider">The attribute provider.</param>
    /// <param name="type">Container class type.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    /// <exception cref="ArgumentNullException">attributeProvider.</exception>
    [RequiresUnreferencedCode(
        "Formatting enum values may reflect over runtime enum fields to read EnumMember metadata. Use the Refit source generator for trimmed/AOT apps.")]
    public virtual string? Format(
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        if (attributeProvider is null)
        {
            throw new ArgumentNullException(nameof(attributeProvider));
        }

        if (value is null)
        {
            return null;
        }

        var queryAttribute = GetFirstQueryAttribute(attributeProvider);
        var formatString = queryAttribute?.Format;

        var parameterType = value.GetType();
        var enumMember = ResolveEnumMember(parameterType, value);

        formatString = ResolveFormatString(formatString, type, parameterType);

        return string.Format(
            CultureInfo.InvariantCulture,
            string.IsNullOrWhiteSpace(formatString) ? "{0}" : $"{{0:{formatString}}}",
            enumMember?.Value ?? value);
    }

    /// <summary>Resolves the cached <see cref="EnumMemberAttribute"/> for the given enum value, if any.</summary>
    /// <param name="parameterType">The runtime type of the value.</param>
    /// <param name="value">The value to inspect.</param>
    /// <returns>The matching <see cref="EnumMemberAttribute"/>, or null when not an enum or no attribute is present.</returns>
    [RequiresUnreferencedCode(
        "Formatting enum values may reflect over runtime enum fields to read EnumMember metadata. Use the Refit source generator for trimmed/AOT apps.")]
    private static EnumMemberAttribute? ResolveEnumMember(Type parameterType, object value)
    {
        if (!parameterType.IsEnum)
        {
            return null;
        }

        var cached = _enumMemberCache.GetOrAdd(parameterType, _ => new());
        return cached.GetOrAdd(
            value.ToString()!,
            val =>
                GetEnumField(parameterType, val)?.GetCustomAttribute<EnumMemberAttribute>());
    }

    /// <summary>Gets the first query attribute from an attribute provider.</summary>
    /// <param name="attributeProvider">The attribute provider to inspect.</param>
    /// <returns>The first query attribute, or null when absent.</returns>
    private static QueryAttribute? GetFirstQueryAttribute(ICustomAttributeProvider attributeProvider)
    {
        var attributes = attributeProvider.GetCustomAttributes(typeof(QueryAttribute), true);
        for (var i = 0; i < attributes.Length; i++)
        {
            if (attributes[i] is QueryAttribute attribute)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Finds an enum field by name.</summary>
    /// <param name="enumType">The enum type to inspect.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The matching field, or null when absent.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "The caller is already annotated with RequiresUnreferencedCode because enum metadata may be trimmed.")]
    private static FieldInfo? GetEnumField(Type enumType, string name)
    {
        foreach (var field in enumType.GetTypeInfo().DeclaredFields)
        {
            if (field.Name == name)
            {
                return field;
            }
        }

        return null;
    }

    /// <summary>Selects the effective format string, preferring the attribute format, then specific, then general formats.</summary>
    /// <param name="formatString">The format string supplied by the query attribute, if any.</param>
    /// <param name="type">The container class type.</param>
    /// <param name="parameterType">The runtime type of the value.</param>
    /// <returns>The resolved format string, or null when none applies.</returns>
    private string? ResolveFormatString(string? formatString, Type type, Type parameterType)
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
