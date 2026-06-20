// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Refit;

/// <summary>Default form Url-encoded parameter formatter.</summary>
public class DefaultFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
{
    /// <summary>Caches resolved enum member attributes keyed by enum type and member name.</summary>
    private static readonly ConcurrentDictionary<
        Type,
        ConcurrentDictionary<string, EnumMemberAttribute?>
    > _enumMemberCache = new();

    /// <summary>Formats the specified parameter value.</summary>
    /// <param name="value">The parameter value.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    public virtual string? Format(object? value, string? formatString)
    {
        if (value is null)
        {
            return null;
        }

        var parameterType = value.GetType();

        EnumMemberAttribute? enumMember = null;
        if (parameterType.GetTypeInfo().IsEnum)
        {
            var cached = _enumMemberCache.GetOrAdd(
                parameterType,
                _ => new());
            enumMember = cached.GetOrAdd(
                value.ToString()!,
                val =>
                    GetEnumField(parameterType, val)?.GetCustomAttribute<EnumMemberAttribute>());
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            string.IsNullOrWhiteSpace(formatString) ? "{0}" : $"{{0:{formatString}}}",
            enumMember?.Value ?? value);
    }

    /// <summary>Finds an enum field by name.</summary>
    /// <param name="enumType">The enum type to inspect.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The matching field, or null when absent.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "Enum member names are read from a runtime Type to support EnumMemberAttribute formatting.")]
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
}
