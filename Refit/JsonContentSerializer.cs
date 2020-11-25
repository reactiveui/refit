using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refit
{
    [Obsolete("Use NewtonsoftJsonContentSerializer in the Refit.Newtonsoft.Json package instead", true)]
    public class JsonContentSerializer : IContentSerializer
    {
        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }
    }
}
