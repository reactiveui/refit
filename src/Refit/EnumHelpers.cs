// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Refit;

/// <summary>Provides shared enum helpers for formatting, serialization, and reflection-backed metadata.</summary>
internal static class EnumHelpers
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
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "Enum field metadata is inspected for EnumMemberAttribute formatting on runtime enum values.")]
    private static Dictionary<string, string?> CreateEnumMemberValueMap(Type enumType)
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

    /// <summary>Provides cached helpers for a single enum type.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    internal static class Info<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>The enum backing type used when reading and writing undefined numeric values.</summary>
        private static readonly TypeCode _underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));

        /// <summary>Formats an undefined enum value using the correct signedness for the enum backing type.</summary>
        /// <param name="value">The enum value to format.</param>
        /// <returns>The invariant numeric string.</returns>
        internal static string FormatNumericValue(TEnum value) =>
            IsUnsignedBackingType()
                ? ToUInt64(value).ToString(CultureInfo.InvariantCulture)
                : ToInt64(value).ToString(CultureInfo.InvariantCulture);

        /// <summary>Parses an enum field name while using the generic overload on supported targets.</summary>
        /// <param name="name">The enum field name.</param>
        /// <returns>The parsed enum value.</returns>
        internal static TEnum ParseName(string name)
        {
#if NET8_0_OR_GREATER
            return Enum.Parse<TEnum>(name, ignoreCase: false);
#else
            return (TEnum)Enum.Parse(typeof(TEnum), name, false);
#endif
        }

        /// <summary>Reads a numeric enum value using the correct signedness for the enum backing type.</summary>
        /// <param name="reader">The reader positioned on the numeric token.</param>
        /// <returns>The enum value represented by the numeric token.</returns>
        internal static TEnum ReadJsonNumericValue(ref Utf8JsonReader reader) =>
            ReadJsonNumericValue(_underlyingTypeCode, ref reader);

        /// <summary>Reads an undefined enum value using the supplied backing type code.</summary>
        /// <param name="underlyingTypeCode">The enum backing type code.</param>
        /// <param name="reader">The JSON reader positioned at a numeric token.</param>
        /// <returns>The enum value represented by the numeric token.</returns>
        internal static TEnum ReadJsonNumericValue(TypeCode underlyingTypeCode, ref Utf8JsonReader reader) =>
            underlyingTypeCode switch
            {
                TypeCode.SByte => ToEnum(checked((sbyte)reader.GetInt64())),
                TypeCode.Byte => ToEnum(checked((byte)reader.GetUInt64())),
                TypeCode.Int16 => ToEnum(checked((short)reader.GetInt64())),
                TypeCode.UInt16 => ToEnum(checked((ushort)reader.GetUInt64())),
                TypeCode.Int32 => ToEnum(checked((int)reader.GetInt64())),
                TypeCode.UInt32 => ToEnum(checked((uint)reader.GetUInt64())),
                TypeCode.Int64 => ToEnum(reader.GetInt64()),
                TypeCode.UInt64 => ToEnum(reader.GetUInt64()),
                _ => throw new JsonException($"Unsupported enum backing type for {typeof(TEnum)}.")
            };

        /// <summary>Writes an undefined enum value using the correct signedness for the enum backing type.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The enum value to write.</param>
        internal static void WriteJsonNumericValue(Utf8JsonWriter writer, TEnum value)
        {
            if (IsUnsignedBackingType())
            {
                writer.WriteNumberValue(ToUInt64(value));
                return;
            }

            writer.WriteNumberValue(ToInt64(value));
        }

        /// <summary>Converts an enum value to a signed 64-bit number without boxing.</summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The signed numeric value.</returns>
        internal static long ToInt64(TEnum value) =>
            _underlyingTypeCode switch
            {
                TypeCode.SByte => Unsafe.As<TEnum, sbyte>(ref value),
                TypeCode.Int16 => Unsafe.As<TEnum, short>(ref value),
                TypeCode.Int32 => Unsafe.As<TEnum, int>(ref value),
                TypeCode.Int64 => Unsafe.As<TEnum, long>(ref value),
                _ => throw new JsonException($"Enum {typeof(TEnum)} does not use a signed backing type.")
            };

        /// <summary>Converts an enum value to an unsigned 64-bit number without boxing.</summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The unsigned numeric value.</returns>
        internal static ulong ToUInt64(TEnum value) =>
            _underlyingTypeCode switch
            {
                TypeCode.Byte => Unsafe.As<TEnum, byte>(ref value),
                TypeCode.UInt16 => Unsafe.As<TEnum, ushort>(ref value),
                TypeCode.UInt32 => Unsafe.As<TEnum, uint>(ref value),
                TypeCode.UInt64 => Unsafe.As<TEnum, ulong>(ref value),
                _ => throw new JsonException($"Enum {typeof(TEnum)} does not use an unsigned backing type.")
            };

        /// <summary>Determines whether the enum backing type is unsigned.</summary>
        /// <returns><see langword="true"/> when the enum backing type is unsigned.</returns>
        private static bool IsUnsignedBackingType() =>
            _underlyingTypeCode is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

        /// <summary>Converts a numeric backing value to the enum type without boxing.</summary>
        /// <typeparam name="TUnderlying">The enum backing value type.</typeparam>
        /// <param name="value">The numeric backing value.</param>
        /// <returns>The enum value.</returns>
        private static TEnum ToEnum<TUnderlying>(TUnderlying value)
            where TUnderlying : struct =>
            Unsafe.As<TUnderlying, TEnum>(ref value);
    }
}
