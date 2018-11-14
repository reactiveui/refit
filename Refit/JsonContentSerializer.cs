using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    public class JsonContentSerializer : IContentSerializer
    {
        private readonly Lazy<JsonSerializerSettings> jsonSerializerSettings;

        public JsonContentSerializer() : this(null) { }

        public JsonContentSerializer(JsonSerializerSettings jsonSerializerSettings)
        {
            this.jsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
            {
                if (jsonSerializerSettings == null)
                {
                    if(JsonConvert.DefaultSettings == null)
                    {
                        return new JsonSerializerSettings();
                    }
                    return JsonConvert.DefaultSettings();
                }
                return jsonSerializerSettings;
            });
        }

        public HttpContent Serialize(object item)
        {
            return new StringContent(JsonConvert.SerializeObject(item, jsonSerializerSettings.Value), Encoding.UTF8, "application/json");
        }

        public async Task<object> DeserializeAsync(HttpContent content, Type objectType)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings.Value);

            using (var stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var reader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(reader))
                return serializer.Deserialize(jsonTextReader, objectType);
        }

        public T Deserialize<T>(string content)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings.Value);

            using (var reader = new StringReader(content))
            using (var jsonTextReader = new JsonTextReader(reader))
                return serializer.Deserialize<T>(jsonTextReader);
        }
    }
}
