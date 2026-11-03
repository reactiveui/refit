// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Refit;

/// <summary>Reads <see cref="EnumMemberAttribute"/> values from run-time-typed enums by reflection.</summary>
internal static class EnumMemberValues
{
    /// <summary>Caches <see cref="EnumMemberAttribute.Value"/> lookups by enum type and field name.</summary>
    private static readonly ConcurrentDictionary<Type, Dictionary<string, string?>> _enumMemberValueCache = new();

    /// <summary>Gets the <see cref="EnumMemberAttribute.Value"/> for a boxed enum value.</summary>
    /// <param name="enumType">The runtime enum type.</param>
    /// <param name="value">The boxed enum value.</param>
    /// <returns>The configured enum member value, or null when none is configured.</returns>
    internal static string? GetEnumMemberValue(Type enumType, object value)
    {
        if (!enumType.IsEnum)
        {
            return null;
        }

        var name = Enum.GetName(enumType, value);
        if (name is null)
        {
            return null;
        }

        var enumMemberValues = _enumMemberValueCache.GetOrAdd(enumType, CreateEnumMemberValueMap);
        return enumMemberValues.TryGetValue(name, out var enumMemberValue) ? enumMemberValue : null;
    }

    /// <summary>Builds the enum-member value map for the given enum type.</summary>
    /// <param name="enumType">The enum type to inspect.</param>
    /// <returns>The enum-member value map keyed by enum field name.</returns>
    internal static Dictionary<string, string?> CreateEnumMemberValueMap(Type enumType)
    {
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        var enumMemberValues = new Dictionary<string, string?>(fields.Length, StringComparer.Ordinal);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var enumMemberAttribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (enumMemberAttribute is not null)
            {
                enumMemberValues[field.Name] = enumMemberAttribute.Value;
            }
        }

        return enumMemberValues;
    }
}
