using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

public class AotContentSerializer(JsonSerializerContext serializerContext) : IHttpContentSerializer
{
    public HttpContent ToHttpContent<T>(T item) => throw new NotImplementedException();

    public async Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default
    )
    {
        var val = await content.ReadAsStringAsync().ConfigureAwait(false);

        return (T)JsonSerializer.Deserialize(val, typeof(T), serializerContext);
    }

    public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
        throw new NotImplementedException();
}
