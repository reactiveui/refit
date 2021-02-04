using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the Newtonsoft.Json APIs
    /// </summary>
    public sealed class NewtonsoftJsonContentSerializer : IHttpContentSerializer
    {
        /// <summary>
        /// The <see cref="Lazy{T}"/> instance providing the JSON serialization settings to use
        /// </summary>
        readonly Lazy<JsonSerializerSettings> jsonSerializerSettings;

        /// <summary>
        /// Creates a new <see cref="NewtonsoftJsonContentSerializer"/> instance
        /// </summary>
        public NewtonsoftJsonContentSerializer() : this(null) { }

        /// <summary>
        /// Creates a new <see cref="NewtonsoftJsonContentSerializer"/> instance with the specified parameters
        /// </summary>
        /// <param name="jsonSerializerSettings">The serialization settings to use for the current instance</param>
        public NewtonsoftJsonContentSerializer(JsonSerializerSettings? jsonSerializerSettings)
        {
            this.jsonSerializerSettings = new Lazy<JsonSerializerSettings>(() => jsonSerializerSettings
                                                                                 ?? JsonConvert.DefaultSettings?.Invoke()
                                                                                 ?? new JsonSerializerSettings());
        }

        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item)
        {
            var content = new StringContent(JsonConvert.SerializeObject(item, jsonSerializerSettings.Value), Encoding.UTF8, "application/json");

            return content;
        }

        /// <inheritdoc/>
        public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings.Value);

            using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(reader);

            return serializer.Deserialize<T>(jsonTextReader);
        }

        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
                throw new ArgumentNullException(nameof(propertyInfo));

            return propertyInfo.GetCustomAttributes<JsonPropertyAttribute>(true)
                              .Select(a => a.PropertyName)
                              .FirstOrDefault();
        }
    }
}
