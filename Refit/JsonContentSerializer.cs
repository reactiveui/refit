using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit {

    public class JsonContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public JsonContentSerializer() : this(new JsonSerializerSettings())
        {
        }

        public JsonContentSerializer(JsonSerializerSettings jsonSerializerSettings)
        {
            this.jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
        }

        public HttpContent Serialize(object item)
        {
            return new StringContent(JsonConvert.SerializeObject(item, jsonSerializerSettings), Encoding.UTF8, "application/json");
        }

        public async Task<object> DeserializeAsync(HttpContent content, Type objectType)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings);

            using (var stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var reader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(reader))
                return serializer.Deserialize(jsonTextReader, objectType);
        }

        public T Deserialize<T>(string content)
        {
            var serializer = JsonSerializer.Create(jsonSerializerSettings);

            using (var reader = new StringReader(content))
            using (var jsonTextReader = new JsonTextReader(reader))
                return serializer.Deserialize<T>(jsonTextReader);
        }
    }
}
