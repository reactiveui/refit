using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the System.Text.Json APIs
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters
    /// </remarks>
    /// <param name="jsonSerializerOptions">The serialization options to use for the current instance</param>
    public sealed class SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions) : IHttpContentSerializer
    {
        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance
        /// </summary>
        public SystemTextJsonContentSerializer()
            : this(GetDefaultJsonSerializerOptions()) { }

        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item)
        {
#if NET8_0_OR_GREATER
            if (TryGetJsonTypeInfo<T>(out var jsonTypeInfo))
            {
                return JsonContent.Create(item, jsonTypeInfo);
            }
#endif
            return JsonContent.Create(item, options: jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public async Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        )
        {
#if NET8_0_OR_GREATER
            if (TryGetJsonTypeInfo<T>(out var jsonTypeInfo))
            {
                return await content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            }
#endif
            return await content
                .ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands
        /// </summary>
        /// <param name="propertyInfo">A PropertyInfo object.</param>
        /// <returns>
        /// The calculated field name.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">propertyInfo</exception>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => propertyInfo switch
        {
            null => throw new ArgumentNullException(nameof(propertyInfo)),
            _ => propertyInfo
            .GetCustomAttributes<JsonPropertyNameAttribute>(true)
            .Select(a => a.Name)
            .FirstOrDefault()
        };

        /// <summary>
        /// Creates new <see cref="JsonSerializerOptions"/> and fills it with default parameters
        /// </summary>
        public static JsonSerializerOptions GetDefaultJsonSerializerOptions()
        {
            // Default to case insensitive property name matching as that's likely the behavior most users expect
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
            jsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter());

            return jsonSerializerOptions;
        }

#if NET8_0_OR_GREATER
        bool TryGetJsonTypeInfo<T>(out JsonTypeInfo<T> jsonTypeInfo)
        {
            if (jsonSerializerOptions.TypeInfoResolver is not null)
            {
                var typeInfo = jsonSerializerOptions.GetTypeInfo(typeof(T));
                if (typeInfo is JsonTypeInfo<T> typedTypeInfo)
                {
                    jsonTypeInfo = typedTypeInfo;
                    return true;
                }
            }

            jsonTypeInfo = null!;
            return false;
        }
#endif
    }

    /// <summary>
    /// ObjectToInferredTypesConverter.
    /// From https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
    /// </summary>
    public class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        /// <summary>
        /// Reads and converts the JSON to type typeToConvert />.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public override object? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) =>
            reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out var datetime) => datetime,
                JsonTokenType.String => reader.GetString(),
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="objectToWrite">The object to write.</param>
        /// <param name="options">The options.</param>
        public override void Write(
            Utf8JsonWriter writer,
            object objectToWrite,
            JsonSerializerOptions options
        )
        {
#if NET8_0_OR_GREATER
            if (options.TypeInfoResolver is not null)
            {
                JsonSerializer.Serialize(writer, objectToWrite, options.GetTypeInfo(objectToWrite.GetType()));
                return;
            }
#endif
            JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
        }
    }

    sealed class CamelCaseStringEnumConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            (Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert).IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            var isNullable = Nullable.GetUnderlyingType(typeToConvert) != null;

            return new NonGenericEnumConverter(typeToConvert, enumType, isNullable);
        }

        sealed class NonGenericEnumConverter(Type targetType, Type enumType, bool isNullable)
            : JsonConverter<object?>
        {
            readonly Dictionary<string, object> namesToValues = GetNamesToValues(
                enumType,
                StringComparer.Ordinal
            );
            readonly Dictionary<string, object> namesToValuesIgnoreCase = GetNamesToValues(
                enumType,
                StringComparer.OrdinalIgnoreCase
            );
            readonly Dictionary<object, string> valuesToNames = GetValuesToNames(enumType);

            public override bool CanConvert(Type typeToConvert) => typeToConvert == targetType;

            public override object? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            )
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    if (!isNullable)
                        throw new JsonException($"Cannot convert null to {targetType}.");

                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        if (isNullable)
                            return null;

                        throw new JsonException($"Cannot convert an empty value to {targetType}.");
                    }

                    if (namesToValues.TryGetValue(value, out var namedValue))
                        return namedValue;

                    if (namesToValuesIgnoreCase.TryGetValue(value, out var namedValueIgnoreCase))
                        return namedValueIgnoreCase;

                    throw new JsonException($"Unable to convert '{value}' to {targetType}.");
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    var numericValue = reader.GetInt64();
                    return Enum.ToObject(enumType, numericValue);
                }

                throw new JsonException($"Unexpected token {reader.TokenType} when parsing {targetType}.");
            }

            public override void Write(
                Utf8JsonWriter writer,
                object? value,
                JsonSerializerOptions options
            )
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                if (!valuesToNames.TryGetValue(value, out var name))
                {
                    writer.WriteNumberValue(Convert.ToInt64(value));
                    return;
                }

                writer.WriteStringValue(name);
            }

            static Dictionary<string, object> GetNamesToValues(
                Type enumType,
                StringComparer comparer
            )
            {
                var map = new Dictionary<string, object>(comparer);

                foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var value = Enum.Parse(enumType, field.Name, ignoreCase: false);
                    foreach (var name in GetSerializedNames(field))
                    {
                        map[name] = value;
                    }
                }

                return map;
            }

            static Dictionary<object, string> GetValuesToNames(Type enumType)
            {
                var map = new Dictionary<object, string>();

                foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var value = Enum.Parse(enumType, field.Name, ignoreCase: false);
                    map[value] = GetPreferredSerializedName(field);
                }

                return map;
            }

            static IEnumerable<string> GetSerializedNames(FieldInfo field)
            {
                var preferredName = GetPreferredSerializedName(field);
                yield return preferredName;

                if (!string.Equals(field.Name, preferredName, StringComparison.Ordinal))
                    yield return field.Name;
            }

            static string GetPreferredSerializedName(FieldInfo field)
            {
#if NET9_0_OR_GREATER
                var enumMemberNameAttribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
                if (enumMemberNameAttribute is not null)
                    return enumMemberNameAttribute.Name;
#endif
                return ToCamelCase(field.Name);
            }

            static string ToCamelCase(string value) =>
                string.IsNullOrEmpty(value) || !char.IsUpper(value[0])
                    ? value
                    : char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }
}
