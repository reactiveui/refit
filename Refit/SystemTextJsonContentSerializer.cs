using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the System.Text.Json APIs
    /// </summary>
    public sealed class SystemTextJsonContentSerializer : IHttpContentSerializer
    {
        /// <summary>
        /// The JSON serialization options to use
        /// </summary>
        readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance
        /// </summary>
        public SystemTextJsonContentSerializer()
            : this(GetDefaultJsonSerializerOptions()) { }

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters
        /// </summary>
        /// <param name="jsonSerializerOptions">The serialization options to use for the current instance</param>
        public SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item)
        {
            var content = JsonContent.Create(item, options: jsonSerializerOptions);

            return content;
        }

        /// <inheritdoc/>
        public async Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        )
        {
            var item = await content
                .ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return item;
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
            jsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );

            return jsonSerializerOptions;
        }
    }

    /// <summary>
    /// ObjectToInferredTypesConverter.
    /// From https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
    /// </summary>
    public class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        /// <summary>
        /// Reads and converts the JSON to type <typeparamref name="T" />.
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
        ) => JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
    }
}
