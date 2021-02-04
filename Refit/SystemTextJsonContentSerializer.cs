using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Refit.Buffers;

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IContentSerializer"/> using the System.Text.Json APIs
    /// </summary>
    public sealed class SystemTextJsonContentSerializer : IContentSerializer
    {
        /// <summary>
        /// The JSON serialization options to use
        /// </summary>
        readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance
        /// </summary>
        public SystemTextJsonContentSerializer() : this(new JsonSerializerOptions())
        {

            // Set some defaults
            // Default to case insensitive property name matching as that's likely the behavior most users expect
            jsonSerializerOptions.PropertyNameCaseInsensitive = true;
            jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters
        /// </summary>
        /// <param name="jsonSerializerOptions">The serialization options to use for the current instance</param>
        public SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <inheritdoc/>
        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var content = JsonContent.Create(item, options: jsonSerializerOptions);            

            return Task.FromResult<HttpContent>(content);
        }

        /// <inheritdoc/>
        public async Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            var item = await content.ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            return item;
        }

        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
                throw new ArgumentNullException(nameof(propertyInfo));

            return propertyInfo.GetCustomAttributes<JsonPropertyNameAttribute>(true)
                       .Select(a => a.Name)
                       .FirstOrDefault();
        }
    }

    // From https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
    public class ObjectToInferredTypesConverter
       : JsonConverter<object>
    {
        public override object? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out var l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TryGetDateTime(out var datetime))
                {
                    return datetime;
                }

                return reader.GetString();
            }

            // Use JsonElement as fallback.
            // Newtonsoft uses JArray or JObject.
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }

        public override void Write(
            Utf8JsonWriter writer,
            object objectToWrite,
            JsonSerializerOptions options) =>
                throw new InvalidOperationException("Should not get here.");
    }

}

