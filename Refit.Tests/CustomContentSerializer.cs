using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Refit.Tests.Collections;

namespace Refit.Tests
{
    public class CustomContentSerializer : IHttpResponseMessageSerializer
    {
        private readonly IHttpContentSerializer serializer;

        public CustomContentSerializer(IHttpContentSerializer serializer)
        {
            this.serializer = serializer;
        }

        public HttpContent ToHttpContent<T>(T item)
        {
            return serializer.ToHttpContent<T>(item);
        }

        public Task<T> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            return FromHttpContentAsync<T>(null, content, cancellationToken);
        }

        public async Task<T> FromHttpContentAsync<T>(HttpResponseMessage responseMessage, HttpContent content, CancellationToken cancellationToken = default)
        {
            var item = await serializer.FromHttpContentAsync<T>(content, cancellationToken).ConfigureAwait(false);

            if (responseMessage != null &&
                item != null &&
                item is System.Collections.IEnumerable)
            {
                var type = typeof(T);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                {
                    var parameters = responseMessage.Headers.ToDictionary(f => f.Key, f => f.Value.FirstOrDefault());
                    var method = typeof(EnumerableExtensions).GetMethod("Extend", BindingFlags.Public | BindingFlags.Static);
                    item = (T)method
                        .MakeGenericMethod(type.GetGenericArguments().First())
                        .Invoke(null, new object[] { item, parameters });
                }
            }

            return item;
        }

        public string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            return serializer.GetFieldNameForProperty(propertyInfo);
        }
    }
}
