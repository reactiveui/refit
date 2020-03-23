using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    [Obsolete("Use NewtonsoftJsonContentSerializer instead", false)]
    public class JsonContentSerializer : IContentSerializer
    {
        readonly Lazy<JsonSerializerSettings> jsonSerializerSettings;

        public JsonContentSerializer() : this(null) { }

        public JsonContentSerializer(JsonSerializerSettings jsonSerializerSettings)
        {
            this.jsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
            {
                if (jsonSerializerSettings == null)
                {
                    if (JsonConvert.DefaultSettings == null)
                    {
                        return new JsonSerializerSettings();
                    }
                    return JsonConvert.DefaultSettings();
                }
                return jsonSerializerSettings;
            });
        }

        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var content = new StringContent(JsonConvert.SerializeObject(item, jsonSerializerSettings.Value), Encoding.UTF8, "application/json");
            return Task.FromResult((HttpContent)content);
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings.Value);

            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(reader);
            return serializer.Deserialize<T>(jsonTextReader);
        }
    }
}
