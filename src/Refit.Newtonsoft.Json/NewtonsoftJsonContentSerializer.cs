using System.Net.Http;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the Newtonsoft.Json APIs
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="NewtonsoftJsonContentSerializer"/> instance with the specified parameters
    /// </remarks>
    /// <param name="jsonSerializerSettings">The serialization settings to use for the current instance</param>
    public sealed class NewtonsoftJsonContentSerializer(
        JsonSerializerSettings? jsonSerializerSettings
    ) : IHttpContentSerializer
    {
        /// <summary>
        /// The <see cref="Lazy{T}"/> instance providing the JSON serialization settings to use
        /// </summary>
        readonly Lazy<JsonSerializerSettings> jsonSerializerSettings =
            new(
                () =>
                    jsonSerializerSettings
                    ?? JsonConvert.DefaultSettings?.Invoke()
                    ?? new JsonSerializerSettings()
            );

        /// <summary>
        /// Creates a new <see cref="NewtonsoftJsonContentSerializer"/> instance
        /// </summary>
        public NewtonsoftJsonContentSerializer()
            : this(null) { }

        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(item, jsonSerializerSettings.Value),
                Encoding.UTF8,
                "application/json"
            );

            return content;
        }

        /// <inheritdoc/>
        public async Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        )
        {
            if (content == null)
            {
                return default;
            }

            var serializer = JsonSerializer.Create(jsonSerializerSettings.Value);

            await content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

#if NET6_0_OR_GREATER
            await using (stream.ConfigureAwait(false))
#else
            using (stream)
#endif
            {
                using var reader = new StreamReader(stream, GetEncoding(content) ?? Encoding.UTF8);

                var jsonTextReader = new JsonTextReader(reader);
#if NET6_0_OR_GREATER
                await using (jsonTextReader.ConfigureAwait(false))
#else
                using (jsonTextReader)
#endif
                {
                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }

        // Mirrors System.Text.Json's charset handling so behavior matches across serializers.
        // See JsonHelpers.GetEncoding in System.Net.Http.Json:
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http.Json/src/System/Net/Http/Json/JsonHelpers.cs
        private static Encoding? GetEncoding(HttpContent content)
        {
            var charset = content.Headers.ContentType?.CharSet;
            if (charset is null)
            {
                return null;
            }

            try
            {
                if (charset.Length > 2 && charset[0] == '"' && charset[charset.Length - 1] == '"')
                {
                    charset = charset.Substring(1, charset.Length - 2);
                }
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("The character set provided in ContentType is invalid.", e);
            }
        }

        /// <summary>
        /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands
        /// </summary>
        /// <param name="propertyInfo">A PropertyInfo object.</param>
        /// <returns>
        /// The calculated field name.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">propertyInfo</exception>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo switch
            {
                null => throw new ArgumentNullException(nameof(propertyInfo)),
                _
                    => propertyInfo
                        .GetCustomAttributes<JsonPropertyAttribute>(true)
                        .Select(a => a.PropertyName)
                        .FirstOrDefault()
            };
        }
    }
}
