using Newtonsoft.Json;

namespace Refit
{
    public class NewtonsoftJsonDeserializer : IDeserializer
    {
        public T Deserialize<T>(string objectToDeserialize)
        {
            return JsonSerializerSettings != null ?
                JsonConvert.DeserializeObject<T>(objectToDeserialize, JsonSerializerSettings) :
                JsonConvert.DeserializeObject<T>(objectToDeserialize);
        }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }
    }
}
