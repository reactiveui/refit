﻿using System.Net.Http;
using System.Reflection;

namespace Refit
{
    [Obsolete(
        "Use NewtonsoftJsonContentSerializer in the Refit.Newtonsoft.Json package instead",
        true
    )]
    public class JsonContentSerializer : IHttpContentSerializer
    {
        public HttpContent ToHttpContent<T>(T item)
        {
            throw new NotImplementedException();
        }

        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }
    }
}
