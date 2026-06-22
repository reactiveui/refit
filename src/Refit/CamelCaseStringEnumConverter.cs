// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>A converter factory that serializes enum values using camelCase names.</summary>
internal sealed class CamelCaseStringEnumConverter : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        (Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert).IsEnum;

    /// <inheritdoc/>
    [UnconditionalSuppressMessage(
        "AssemblyLoadTrimming",
        "IL2026:RequiresUnreferencedCode",
        Justification =
            "JsonConverterFactory.CreateConverter cannot carry trim annotations, but this implementation creates a converter from a runtime enum Type.")]
    [UnconditionalSuppressMessage(
        "AssemblyLoadTrimming",
        "IL3050:RequiresDynamicCode",
        Justification =
            "JsonConverterFactory.CreateConverter cannot carry AOT annotations, but this implementation closes a converter over a runtime enum Type.")]
    [UnconditionalSuppressMessage(
        "AssemblyLoadTrimming",
        "IL2070:DynamicallyAccessedMembers",
        Justification = "The converter is created from a runtime enum Type and preserves enum fields on the closed converter.")]
    [UnconditionalSuppressMessage(
        "AssemblyLoadTrimming",
        "IL2071:DynamicallyAccessedMembers",
        Justification = "The converter is created from a runtime enum Type and preserves enum fields on the closed converter.")]
    [UnconditionalSuppressMessage(
        "AssemblyLoadTrimming",
        "IL2055:MakeGenericType",
        Justification = "The converter is created from a runtime enum Type and preserves enum fields on the closed converter.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        var openConverterType = underlyingType is null
            ? typeof(EnumConverter<>)
            : typeof(NullableEnumConverter<>);
        var converterType = openConverterType.MakeGenericType(underlyingType ?? typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    /// <summary>A strongly-typed JSON converter that maps enum values to and from their camelCase names.</summary>
    /// <typeparam name="TEnum">The enum type whose fields are inspected.</typeparam>
    private sealed class EnumConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>Maps serialized names to enum values using ordinal comparison.</summary>
        private readonly Dictionary<string, TEnum> _namesToValues = GetNamesToValues(StringComparer.Ordinal);

        /// <summary>Maps serialized names to enum values using case-insensitive comparison.</summary>
        private readonly Dictionary<string, TEnum> _namesToValuesIgnoreCase =
            GetNamesToValues(StringComparer.OrdinalIgnoreCase);

        /// <summary>Maps enum values to their preferred serialized names.</summary>
        private readonly Dictionary<TEnum, string> _valuesToNames = GetValuesToNames();

        /// <inheritdoc/>
        public override TEnum Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            ReadValue(ref reader);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (_valuesToNames.TryGetValue(value, out var name))
            {
                writer.WriteStringValue(name);
                return;
            }

            EnumHelpers.Info<TEnum>.WriteJsonNumericValue(writer, value);
        }

        /// <inheritdoc/>
        public override TEnum ReadAsPropertyName(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            ReadValue(ref reader);

        /// <inheritdoc/>
        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            TEnum value,
            JsonSerializerOptions options)
        {
            if (_valuesToNames.TryGetValue(value, out var name))
            {
                writer.WritePropertyName(name);
                return;
            }

            writer.WritePropertyName(EnumHelpers.Info<TEnum>.FormatNumericValue(value));
        }

        /// <summary>Builds a map of serialized names to enum values using the given comparer.</summary>
        /// <param name="comparer">The comparer used for the resulting dictionary keys.</param>
        /// <returns>A dictionary mapping serialized names to enum values.</returns>
        private static Dictionary<string, TEnum> GetNamesToValues(StringComparer comparer)
        {
            var map = new Dictionary<string, TEnum>(comparer);

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = EnumHelpers.Info<TEnum>.ParseName(field.Name);
                foreach (var name in GetSerializedNames(field))
                {
                    map[name] = value;
                }
            }

            return map;
        }

        /// <summary>Builds a map of enum values to their preferred serialized names.</summary>
        /// <returns>A dictionary mapping enum values to serialized names.</returns>
        private static Dictionary<TEnum, string> GetValuesToNames()
        {
            var map = new Dictionary<TEnum, string>();

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = EnumHelpers.Info<TEnum>.ParseName(field.Name);
                map[value] = GetPreferredSerializedName(field);
            }

            return map;
        }

        /// <summary>Yields the serialized names that should map to the given enum field.</summary>
        /// <param name="field">The enum field to inspect.</param>
        /// <returns>The serialized names for the field.</returns>
        private static IEnumerable<string> GetSerializedNames(FieldInfo field)
        {
            var preferredName = GetPreferredSerializedName(field);
            yield return preferredName;

            if (string.Equals(field.Name, preferredName, StringComparison.Ordinal))
            {
                yield break;
            }

            yield return field.Name;
        }

        /// <summary>Gets the preferred serialized name for the given enum field.</summary>
        /// <param name="field">The enum field to inspect.</param>
        /// <returns>The preferred serialized name.</returns>
        private static string GetPreferredSerializedName(FieldInfo field)
        {
#if NET9_0_OR_GREATER
            var enumMemberNameAttribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            return enumMemberNameAttribute?.Name ?? ToCamelCase(field.Name);
#else
            return ToCamelCase(field.Name);
#endif
        }

        /// <summary>Converts the given value to camelCase.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The camelCase form of the value.</returns>
        private static string ToCamelCase(string value) =>
            string.IsNullOrEmpty(value) || !char.IsUpper(value[0])
                ? value
                : char.ToLowerInvariant(value[0]) + value[1..];

        /// <summary>Reads an enum value from either a string name or a numeric value.</summary>
        /// <param name="reader">The reader positioned on the value to read.</param>
        /// <returns>The parsed enum value.</returns>
        private TEnum ReadValue(ref Utf8JsonReader reader)
        {
            if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
            {
                var value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new JsonException($"Cannot convert an empty value to {typeof(TEnum)}.");
                }

                if (_namesToValues.TryGetValue(value!, out var namedValue))
                {
                    return namedValue;
                }

                if (_namesToValuesIgnoreCase.TryGetValue(value!, out var namedValueIgnoreCase))
                {
                    return namedValueIgnoreCase;
                }

                throw new JsonException($"Unable to convert '{value}' to {typeof(TEnum)}.");
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return EnumHelpers.Info<TEnum>.ReadJsonNumericValue(ref reader);
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing {typeof(TEnum)}.");
        }
    }

    /// <summary>A strongly-typed JSON converter for nullable enums that maps values to and from camelCase names.</summary>
    /// <typeparam name="TEnum">The underlying enum type.</typeparam>
    private sealed class NullableEnumConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        /// <summary>The underlying non-nullable enum converter that performs the name/value mapping.</summary>
        private readonly EnumConverter<TEnum> _inner = new();

        /// <inheritdoc/>
        public override TEnum? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            IsNullOrEmptyString(ref reader) ? null : _inner.Read(ref reader, typeof(TEnum), options);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            _inner.Write(writer, value.Value, options);
        }

        /// <inheritdoc/>
        public override TEnum? ReadAsPropertyName(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            IsNullOrEmptyString(ref reader) ? null : _inner.ReadAsPropertyName(ref reader, typeof(TEnum), options);

        /// <inheritdoc/>
        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            TEnum? value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WritePropertyName(string.Empty);
                return;
            }

            _inner.WriteAsPropertyName(writer, value.Value, options);
        }

        /// <summary>Determines whether the reader is positioned on a JSON null or an empty/whitespace string.</summary>
        /// <param name="reader">The reader to inspect.</param>
        /// <returns><see langword="true"/> when the value should be treated as null.</returns>
        private static bool IsNullOrEmptyString(ref Utf8JsonReader reader) =>
            reader.TokenType == JsonTokenType.Null
            || (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName
                && string.IsNullOrWhiteSpace(reader.GetString()));
    }
}
