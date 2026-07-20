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

    /// <summary>Gets the preferred serialized name for the given enum field.</summary>
    /// <param name="field">The enum field to inspect.</param>
    /// <returns>The preferred serialized name.</returns>
    internal static string GetPreferredSerializedName(FieldInfo field)
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
    internal static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) || !char.IsUpper(value[0])
            ? value
#if NET8_0_OR_GREATER

            // Produce the camelCase form in a single allocation: copy the source, then lowercase the first character
            // in place. The "char.ToLowerInvariant(value[0]) + value[1..]" form allocates an extra substring tail.
            : string.Create(value.Length, value, static (span, source) =>
            {
                source.AsSpan().CopyTo(span);
                span[0] = char.ToLowerInvariant(source[0]);
            });
#else
            : char.ToLowerInvariant(value[0]) + value[1..];
#endif

    /// <summary>Determines whether the reader is positioned on a JSON null or an empty/whitespace string.</summary>
    /// <param name="reader">The reader to inspect.</param>
    /// <returns><see langword="true"/> when the value should be treated as null.</returns>
    internal static bool IsNullOrEmptyString(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.Null
        || (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName
            && string.IsNullOrWhiteSpace(reader.GetString()));

    /// <summary>A strongly-typed JSON converter that maps enum values to and from their camelCase names.</summary>
    /// <typeparam name="TEnum">The enum type whose fields are inspected.</typeparam>
    internal sealed class EnumConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>The upper bound of serialized names per enum field: its camelCase name plus its declared name.</summary>
        private const int MaxNamesPerField = 2;

        /// <summary>Maps serialized names to enum values using ordinal comparison.</summary>
        private readonly Dictionary<string, TEnum> _namesToValues;

        /// <summary>Maps serialized names to enum values using case-insensitive comparison.</summary>
        private readonly Dictionary<string, TEnum> _namesToValuesIgnoreCase;

        /// <summary>Maps enum values to their preferred serialized names.</summary>
        private readonly Dictionary<TEnum, string> _valuesToNames;

        /// <summary>Initializes a new instance of the <see cref="EnumConverter{TEnum}"/> class, building the three
        /// serialized-name maps from a single scan of the enum's fields.</summary>
        /// <remarks>Public because the converter factory instantiates it through <see cref="Activator.CreateInstance(Type)"/>,
        /// which resolves only a public parameterless constructor.</remarks>
        public EnumConverter() =>
            (_namesToValues, _namesToValuesIgnoreCase, _valuesToNames) = BuildNameMaps();

        /// <summary>Gets the number of names-to-values entries, exposed so a benchmark can force and observe the
        /// converter's one-time name-map construction.</summary>
        internal int MapEntryCount => _namesToValues.Count;

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

        /// <summary>Builds the three serialized-name maps from a single scan of the enum's fields, so the field
        /// metadata and each field's camelCase name are computed once instead of once per map.</summary>
        /// <returns>The ordinal and case-insensitive names-to-values maps and the values-to-names map.</returns>
        internal static (
            Dictionary<string, TEnum> NamesToValues,
            Dictionary<string, TEnum> NamesToValuesIgnoreCase,
            Dictionary<TEnum, string> ValuesToNames) BuildNameMaps()
        {
            var fields = typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static);

            // Each field contributes its camelCase name and, when different, its declared name, so two entries per field
            // is the upper bound for the name-keyed maps; presizing to it avoids the grow-and-rehash cycle.
            var namesToValues = new Dictionary<string, TEnum>(fields.Length * MaxNamesPerField, StringComparer.Ordinal);
            var namesToValuesIgnoreCase = new Dictionary<string, TEnum>(fields.Length * MaxNamesPerField, StringComparer.OrdinalIgnoreCase);
            var valuesToNames = new Dictionary<TEnum, string>(fields.Length);

            foreach (var field in fields)
            {
                var value = EnumHelpers.Info<TEnum>.ParseName(field.Name);
                var preferredName = GetPreferredSerializedName(field);

                // The single camelCase name string is shared as a key in both name maps and as the reverse-map value.
                namesToValues[preferredName] = value;
                namesToValuesIgnoreCase[preferredName] = value;

                // The declared name is a second lookup key only when it differs from the camelCase preferred name.
                if (!string.Equals(field.Name, preferredName, StringComparison.Ordinal))
                {
                    namesToValues[field.Name] = value;
                    namesToValuesIgnoreCase[field.Name] = value;
                }

                valuesToNames[value] = preferredName;
            }

            return (namesToValues, namesToValuesIgnoreCase, valuesToNames);
        }

        /// <summary>Reads an enum value from either a string name or a numeric value.</summary>
        /// <param name="reader">The reader positioned on the value to read.</param>
        /// <returns>The parsed enum value.</returns>
        internal TEnum ReadValue(ref Utf8JsonReader reader)
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
    internal sealed class NullableEnumConverter<
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
    }
}
